using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.UI.Combat.Naval
{
    /// <summary>
    /// Manages the overlay of crew chips on an enemy ship sprite.
    /// Used in two modes (UX spec §2.4 / §2.6 / Decision D3):
    ///   - Inspect: read-only hover view, no zoom, chips not clickable.
    ///   - Target:  boarding targeting, zoom ×1.3, chips clickable (living only).
    ///
    /// Instantiated and positioned by NavalCombatHUD over the ship's Image.
    /// Chip layout: grid 3 columns (row-major), 7th chip centred if crew is odd.
    /// Fallback abanico (fan) activates if any chips overlap after layout.
    /// </summary>
    public class CrewChipOverlay : MonoBehaviour
    {
        // ====================================================================
        // PUBLIC INTERFACE
        // ====================================================================

        public enum OverlayMode { Inspect, Target }

        /// <summary>Fired when the player clicks a living crew chip in Target mode.</summary>
        public event Action<ShipCombatant, CrewMemberState> OnCrewChipClicked;

        // ====================================================================
        // CHIP CONSTANTS (visual design spec §11)
        // ====================================================================

        private const float CHIP_W            = 44f;
        private const float CHIP_H            = 56f;
        private const float CHIP_SPACING_X    = 6f;
        private const float CHIP_SPACING_Y    = 6f;
        private const int   CHIPS_PER_ROW     = 3;
        private const float ICON_SIZE         = 20f;
        private const float BAR_W             = 36f;
        private const float BAR_H             = 6f;
        private const float CHIP_APPEAR_FADE  = 0.12f;       // seconds per chip
        private const float CHIP_STAGGER      = 0.04f;       // delay between chips
        private const float INSPECT_FADE      = 0.10f;       // all chips together
        private const float HOVER_SCALE       = 1.15f;
        private const float HOVER_SCALE_SPEED = 0.08f;       // ease-out duration
        private const float ZOOM_SCALE        = 1.3f;
        private const float ZOOM_DURATION     = 0.15f;
        private const float FAN_RADIUS        = 80f;          // fallback abanico

        // ====================================================================
        // RUNTIME STATE
        // ====================================================================

        private ShipCombatant _ship;
        private OverlayMode   _mode;
        private RectTransform _shipImageRT;          // RectTransform of the ship Image

        private readonly List<ChipState> _chips = new();
        private int _focusedChipIndex = -1;          // keyboard nav (Target mode)

        private Coroutine _zoomCoroutine;
        private bool _isZoomed;
        private bool _useFanLayout;

        // ====================================================================
        // INNER TYPES
        // ====================================================================

        private class ChipState
        {
            public CrewMemberState Crew;
            public GameObject Root;
            public RectTransform RT;
            public Image Background;
            public Image RoleIcon;
            public Image HpBarFill;
            public Image DeadOverlay;
            public Image DeadX;
            public Text  Label;           // 2-letter ID or "CAÍDO"
            public Outline Border;        // hover (green/gray) and keyboard focus (white)
            public bool  IsHovered;
            public bool  HasFocus;
        }

        // ====================================================================
        // LIFECYCLE
        // ====================================================================

        private void Update()
        {
            if (_mode == OverlayMode.Target)
                HandleKeyboardNavigation();

            // Live HP update (polling — chips are small and don't use events)
            for (int i = 0; i < _chips.Count; i++)
                RefreshChipHpBar(_chips[i]);
        }

        private void OnDisable()
        {
            HideImmediate();
        }

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Shows chips for the given ship in the specified mode.
        /// <paramref name="shipImageRT"/> is the RectTransform of the ship sprite
        /// that the chips overlay on.
        /// </summary>
        public void Show(ShipCombatant ship, OverlayMode mode, RectTransform shipImageRT)
        {
            _ship = ship;
            _mode = mode;
            _shipImageRT = shipImageRT;

            BuildChips();
            LayoutChips();

            if (mode == OverlayMode.Target)
            {
                ZoomIn();
                StartCoroutine(AppearStaggered());
                // Select first living chip by default
                _focusedChipIndex = FindFirstLivingChipIndex();
                UpdateFocusBorder();
            }
            else
            {
                StartCoroutine(AppearTogether());
            }
        }

        /// <summary>Hides chips and reverses zoom if active.</summary>
        public void Hide()
        {
            StartCoroutine(DisappearAndCleanup());
        }

        /// <summary>Instantly hides without animation (used on scene cleanup).</summary>
        public void HideImmediate()
        {
            if (_zoomCoroutine != null) StopCoroutine(_zoomCoroutine);

            if (_isZoomed && _shipImageRT != null)
                _shipImageRT.localScale = Vector3.one;
            _isZoomed = false;

            foreach (var chip in _chips)
                if (chip.Root != null)
                    Destroy(chip.Root);
            _chips.Clear();
            _focusedChipIndex = -1;
        }

        /// <summary>Refreshes one chip's visual state after a crew damage event.</summary>
        public void OnCrewDamaged(CrewMemberState crew)
        {
            var chip = FindChip(crew);
            if (chip == null) return;
            RefreshChipHpBar(chip);
        }

        /// <summary>Transitions a chip to the dead visual state after OnCrewDied.</summary>
        public void OnCrewDied(CrewMemberState crew)
        {
            var chip = FindChip(crew);
            if (chip == null) return;
            StartCoroutine(AnimateChipDeath(chip));

            // Advance focus to next living chip if this one had focus
            if (_mode == OverlayMode.Target)
            {
                int deadIndex = _chips.IndexOf(chip);
                if (_focusedChipIndex == deadIndex)
                {
                    _focusedChipIndex = FindNextLivingChipIndex(_focusedChipIndex);
                    UpdateFocusBorder();
                }
            }
        }

        // ====================================================================
        // CHIP CONSTRUCTION
        // ====================================================================

        private void BuildChips()
        {
            foreach (var chip in _chips)
                if (chip.Root != null)
                    Destroy(chip.Root);
            _chips.Clear();

            foreach (var crew in _ship.Crew)
            {
                var chip = CreateChipGO(crew);
                _chips.Add(chip);
            }
        }

        private ChipState CreateChipGO(CrewMemberState crew)
        {
            var root = new GameObject($"Chip_{NavalUIColors.RoleLabel(crew.Role)}",
                typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);

            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CHIP_W, CHIP_H);

            // Background (solid — chips must read over any ship art, visual §4)
            var bg = AddImage(root, "Bg");
            bg.color = crew.IsDead
                ? new Color(0.10f, 0.10f, 0.10f, 0.86f)
                : NavalUIColors.PanelDark;

            // Border: Outline on the background (hover green / focus white)
            var border = bg.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.36f, 0.24f, 0.12f); // WoodBorder rest state
            border.effectDistance = new Vector2(2, 2);

            // Role icon (20x20, centred in upper portion)
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(root.transform, false);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 1f);
            iconRT.anchorMax = new Vector2(0.5f, 1f);
            iconRT.pivot = new Vector2(0.5f, 1f);
            iconRT.anchoredPosition = new Vector2(0, -4f);
            iconRT.sizeDelta = new Vector2(ICON_SIZE, ICON_SIZE);
            var roleIcon = iconGO.GetComponent<Image>();
            roleIcon.color = crew.IsDead
                ? new Color(0.23f, 0.23f, 0.23f, 0.40f)
                : NavalUIColors.RoleColor(crew.Role);
            TryLoadRoleSprite(roleIcon, crew.Role);

            // HP bar background
            var barBgGO = new GameObject("HpBarBg", typeof(RectTransform), typeof(Image));
            barBgGO.transform.SetParent(root.transform, false);
            var barBgRT = barBgGO.GetComponent<RectTransform>();
            barBgRT.anchorMin = new Vector2(0.5f, 0f);
            barBgRT.anchorMax = new Vector2(0.5f, 0f);
            barBgRT.pivot = new Vector2(0.5f, 0f);
            barBgRT.anchoredPosition = new Vector2(0, 16f);
            barBgRT.sizeDelta = new Vector2(BAR_W, BAR_H);
            barBgGO.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.10f, 0.78f);

            // HP bar fill
            var fillGO = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(barBgGO.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            var hpFill = fillGO.GetComponent<Image>();
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            float ratio = crew.MaxHP > 0 ? (float)crew.CurrentHP / crew.MaxHP : 0f;
            hpFill.fillAmount = ratio;
            hpFill.color = NavalUIColors.HpBarColor(ratio);

            // 2-letter label
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(root.transform, false);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(1f, 0f);
            labelRT.pivot = new Vector2(0.5f, 0f);
            labelRT.anchoredPosition = new Vector2(0, 4f);
            labelRT.sizeDelta = new Vector2(0, 12f);
            var labelTxt = labelGO.GetComponent<Text>();
            labelTxt.text = crew.IsDead ? "CAÍDO" : NavalUIColors.RoleLabel(crew.Role);
            labelTxt.fontSize = crew.IsDead ? 9 : 10;
            labelTxt.color = new Color(
                NavalUIColors.Cream.r,
                NavalUIColors.Cream.g,
                NavalUIColors.Cream.b,
                crew.IsDead ? 0.71f : 0.86f);
            labelTxt.alignment = TextAnchor.MiddleCenter;
            labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Dead overlay + X
            Image deadOverlay = null;
            Image deadX = null;
            if (crew.IsDead)
            {
                deadOverlay = AddImage(root, "DeadOverlay");
                deadOverlay.color = NavalUIColors.ChipDeadOverlay;

                var xGO = new GameObject("DeadX", typeof(RectTransform), typeof(Image));
                xGO.transform.SetParent(root.transform, false);
                var xRT = xGO.GetComponent<RectTransform>();
                xRT.anchorMin = new Vector2(0.5f, 0.5f);
                xRT.anchorMax = new Vector2(0.5f, 0.5f);
                xRT.pivot = new Vector2(0.5f, 0.5f);
                xRT.anchoredPosition = Vector2.zero;
                xRT.sizeDelta = new Vector2(16f, 16f);
                deadX = xGO.GetComponent<Image>();
                deadX.color = new Color(1f, 1f, 1f, 0.86f);
            }

            // Interactivity
            if (_mode == OverlayMode.Target && !crew.IsDead)
                AddChipInteractivity(root, crew, rt);

            // Start fully transparent — animations will fade in
            SetChipAlpha(root, 0f);

            var state = new ChipState
            {
                Crew        = crew,
                Root        = root,
                RT          = rt,
                Background  = bg,
                RoleIcon    = roleIcon,
                HpBarFill   = hpFill,
                DeadOverlay = deadOverlay,
                DeadX       = deadX,
                Label       = labelTxt,
                Border      = border
            };
            return state;
        }

        // ====================================================================
        // LAYOUT
        // ====================================================================

        private void LayoutChips()
        {
            int count = _chips.Count;
            if (count == 0) return;

            int rows = Mathf.CeilToInt((float)count / CHIPS_PER_ROW);
            float totalW = CHIPS_PER_ROW * CHIP_W + (CHIPS_PER_ROW - 1) * CHIP_SPACING_X;
            float totalH = rows * CHIP_H + (rows - 1) * CHIP_SPACING_Y;

            for (int i = 0; i < count; i++)
            {
                int row = i / CHIPS_PER_ROW;
                int col = i % CHIPS_PER_ROW;

                // Centre-align the last row if it's not full
                int lastRowCount = count - row * CHIPS_PER_ROW;
                bool isLastRow = row == rows - 1;
                float offsetX = isLastRow && lastRowCount < CHIPS_PER_ROW
                    ? (CHIPS_PER_ROW - lastRowCount) * (CHIP_W + CHIP_SPACING_X) * 0.5f
                    : 0f;

                float x = -totalW * 0.5f + col * (CHIP_W + CHIP_SPACING_X) + CHIP_W * 0.5f + offsetX;
                float y =  totalH * 0.5f - row * (CHIP_H + CHIP_SPACING_Y) - CHIP_H * 0.5f;

                _chips[i].RT.anchoredPosition = new Vector2(x, y);
            }

            // Check for overlaps (fan fallback — unlikely with 7 chips at 44px, but guarded)
            _useFanLayout = false;
            for (int i = 0; i < count - 1; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    var a = _chips[i].RT.anchoredPosition;
                    var b = _chips[j].RT.anchoredPosition;
                    if (Vector2.Distance(a, b) < CHIP_W)
                    {
                        _useFanLayout = true;
                        break;
                    }
                }
                if (_useFanLayout) break;
            }

            if (_useFanLayout)
                LayoutChipsFan();
        }

        /// <summary>
        /// Fan (abanico) fallback: distributes chips in a semicircle around the
        /// anchor point (top-centre of the ship sprite). Lines from centre to each
        /// chip are drawn as thin Images — see visual design §11.5.
        /// </summary>
        private void LayoutChipsFan()
        {
            int count = _chips.Count;
            float angleStart = 200f;
            float angleEnd   = 340f;
            float angleStep  = count > 1 ? (angleEnd - angleStart) / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                float angle = (angleStart + i * angleStep) * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * FAN_RADIUS;
                float y = Mathf.Sin(angle) * FAN_RADIUS;
                _chips[i].RT.anchoredPosition = new Vector2(x, y);

                // Anchor line
                DrawAnchorLine(_chips[i].RT.anchoredPosition);
            }
        }

        private void DrawAnchorLine(Vector2 chipPos)
        {
            var lineGO = new GameObject("AnchorLine", typeof(RectTransform), typeof(Image));
            lineGO.transform.SetParent(transform, false);
            var lineImg = lineGO.GetComponent<Image>();
            lineImg.color = new Color(
                NavalUIColors.Gold.r,
                NavalUIColors.Gold.g,
                NavalUIColors.Gold.b,
                0.39f);

            var rt = lineGO.GetComponent<RectTransform>();
            Vector2 origin = Vector2.zero;
            Vector2 dir = chipPos - origin;
            float length = dir.magnitude;
            rt.sizeDelta = new Vector2(1f, length);
            rt.anchoredPosition = (origin + chipPos) * 0.5f;
            rt.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        }

        // ====================================================================
        // ZOOM
        // ====================================================================

        private void ZoomIn()
        {
            if (_zoomCoroutine != null) StopCoroutine(_zoomCoroutine);
            _zoomCoroutine = StartCoroutine(TweenScale(
                _shipImageRT, Vector3.one, Vector3.one * ZOOM_SCALE, ZOOM_DURATION, easeOut: true));
            _isZoomed = true;
        }

        private void ZoomOut()
        {
            if (_zoomCoroutine != null) StopCoroutine(_zoomCoroutine);
            _zoomCoroutine = StartCoroutine(TweenScale(
                _shipImageRT, Vector3.one * ZOOM_SCALE, Vector3.one, ZOOM_DURATION, easeOut: false));
            _isZoomed = false;
        }

        // ====================================================================
        // ANIMATIONS
        // ====================================================================

        private IEnumerator AppearStaggered()
        {
            for (int i = 0; i < _chips.Count; i++)
            {
                int captured = i;
                StartCoroutine(AppearChipDelayed(_chips[captured], i * CHIP_STAGGER));
            }
            yield break;
        }

        private IEnumerator AppearChipDelayed(ChipState chip, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            yield return FadeAndSlideIn(chip.Root, CHIP_APPEAR_FADE);
        }

        private IEnumerator AppearTogether()
        {
            foreach (var chip in _chips)
                StartCoroutine(FadeIn(chip.Root, INSPECT_FADE));
            yield break;
        }

        private IEnumerator DisappearAndCleanup()
        {
            // Fade out all chips simultaneously
            foreach (var chip in _chips)
                if (chip.Root != null)
                    StartCoroutine(FadeOut(chip.Root, INSPECT_FADE));

            yield return new WaitForSeconds(INSPECT_FADE + 0.02f);

            if (_isZoomed) ZoomOut();

            yield return new WaitForSeconds(ZOOM_DURATION + 0.02f);

            foreach (var chip in _chips)
                if (chip.Root != null)
                    Destroy(chip.Root);
            _chips.Clear();
        }

        private IEnumerator AnimateChipDeath(ChipState chip)
        {
            // 1. Scale up (impact)
            yield return TweenScaleLocal(chip.RT, Vector3.one, Vector3.one * 1.2f, 0.1f);
            // 2. Scale back and apply dead visual
            ApplyDeadVisual(chip);
            yield return TweenScaleLocal(chip.RT, Vector3.one * 1.2f, Vector3.one, 0.1f);
        }

        private void ApplyDeadVisual(ChipState chip)
        {
            chip.Background.color = new Color(0.10f, 0.10f, 0.10f, 0.86f);
            chip.RoleIcon.color = new Color(0.23f, 0.23f, 0.23f, 0.40f);

            chip.Label.text = "CAÍDO";
            chip.Label.fontSize = 9;

            if (chip.DeadOverlay == null)
            {
                chip.DeadOverlay = AddImage(chip.Root, "DeadOverlay");
                chip.DeadOverlay.color = NavalUIColors.ChipDeadOverlay;
                chip.DeadOverlay.transform.SetAsLastSibling();
            }
            if (chip.DeadX == null)
            {
                var xGO = new GameObject("DeadX", typeof(RectTransform), typeof(Image));
                xGO.transform.SetParent(chip.Root.transform, false);
                var xRT = xGO.GetComponent<RectTransform>();
                xRT.anchorMin = new Vector2(0.5f, 0.5f);
                xRT.anchorMax = new Vector2(0.5f, 0.5f);
                xRT.pivot = new Vector2(0.5f, 0.5f);
                xRT.anchoredPosition = Vector2.zero;
                xRT.sizeDelta = new Vector2(16f, 16f);
                chip.DeadX = xGO.GetComponent<Image>();
                chip.DeadX.color = new Color(1f, 1f, 1f, 0.86f);
            }
        }

        // ====================================================================
        // KEYBOARD NAVIGATION (Target mode)
        // ====================================================================

        private void HandleKeyboardNavigation()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.rightArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
                kb.tabKey.wasPressedThisFrame)
            {
                _focusedChipIndex = FindNextLivingChipIndex(_focusedChipIndex);
                UpdateFocusBorder();
            }
            else if (kb.leftArrowKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            {
                _focusedChipIndex = FindPrevLivingChipIndex(_focusedChipIndex);
                UpdateFocusBorder();
            }
            else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                if (_focusedChipIndex >= 0 && _focusedChipIndex < _chips.Count)
                {
                    var chip = _chips[_focusedChipIndex];
                    if (!chip.Crew.IsDead)
                        OnCrewChipClicked?.Invoke(_ship, chip.Crew);
                }
            }
        }

        private int FindFirstLivingChipIndex()
        {
            for (int i = 0; i < _chips.Count; i++)
                if (!_chips[i].Crew.IsDead) return i;
            return -1;
        }

        private int FindNextLivingChipIndex(int current)
        {
            if (_chips.Count == 0) return -1;
            int start = (current + 1) % _chips.Count;
            for (int i = 0; i < _chips.Count; i++)
            {
                int idx = (start + i) % _chips.Count;
                if (!_chips[idx].Crew.IsDead) return idx;
            }
            return current;
        }

        private int FindPrevLivingChipIndex(int current)
        {
            if (_chips.Count == 0) return -1;
            int start = (current - 1 + _chips.Count) % _chips.Count;
            for (int i = 0; i < _chips.Count; i++)
            {
                int idx = (start - i + _chips.Count) % _chips.Count;
                if (!_chips[idx].Crew.IsDead) return idx;
            }
            return current;
        }

        private void UpdateFocusBorder()
        {
            for (int i = 0; i < _chips.Count; i++)
            {
                _chips[i].HasFocus = i == _focusedChipIndex && !_chips[i].Crew.IsDead;
                ApplyBorder(_chips[i]);
            }
        }

        /// <summary>Focus (white) wins over hover (green target / gray inspect)
        /// over rest (wood). UX spec §2.4/§2.6.</summary>
        private void ApplyBorder(ChipState chip)
        {
            if (chip.Border == null) return;
            if (chip.HasFocus)
                chip.Border.effectColor = Color.white;
            else if (chip.IsHovered)
                chip.Border.effectColor = _mode == OverlayMode.Target
                    ? NavalUIColors.TargetGreen
                    : new Color(0.53f, 0.53f, 0.53f);
            else
                chip.Border.effectColor = new Color(0.36f, 0.24f, 0.12f);
        }

        // ====================================================================
        // INTERACTIVITY (hover + click)
        // ====================================================================

        private void AddChipInteractivity(GameObject root, CrewMemberState crew,
            RectTransform rt)
        {
            // We use EventTrigger for uGUI hover detection
            var et = root.AddComponent<EventTrigger>();

            var enterEntry = new EventTrigger.Entry
            { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ =>
            {
                var chip = FindChip(crew);
                if (chip == null || chip.Crew.IsDead) return;
                chip.IsHovered = true;
                ApplyBorder(chip);
                StartCoroutine(TweenScaleLocal(rt, rt.localScale, Vector3.one * HOVER_SCALE, HOVER_SCALE_SPEED));
            });
            et.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry
            { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ =>
            {
                var chip = FindChip(crew);
                if (chip == null) return;
                chip.IsHovered = false;
                ApplyBorder(chip);
                StartCoroutine(TweenScaleLocal(rt, rt.localScale, Vector3.one, HOVER_SCALE_SPEED));
            });
            et.triggers.Add(exitEntry);

            var clickEntry = new EventTrigger.Entry
            { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener(_ =>
            {
                if (_mode == OverlayMode.Target && !crew.IsDead)
                    OnCrewChipClicked?.Invoke(_ship, crew);
            });
            et.triggers.Add(clickEntry);
        }

        // ====================================================================
        // REFRESH
        // ====================================================================

        private void RefreshChipHpBar(ChipState chip)
        {
            if (chip.HpBarFill == null) return;
            float ratio = chip.Crew.MaxHP > 0
                ? (float)chip.Crew.CurrentHP / chip.Crew.MaxHP
                : 0f;
            chip.HpBarFill.fillAmount = ratio;
            chip.HpBarFill.color = NavalUIColors.HpBarColor(ratio);
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private ChipState FindChip(CrewMemberState crew)
        {
            for (int i = 0; i < _chips.Count; i++)
                if (_chips[i].Crew == crew) return _chips[i];
            return null;
        }

        private static Image AddImage(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go.GetComponent<Image>();
        }

        private static void TryLoadRoleSprite(Image image, NavalRole role)
        {
            string assetName = role switch
            {
                NavalRole.Capitan       => "ui_role_capitan_32",
                NavalRole.Intendente    => "ui_role_intendente_32",
                NavalRole.Artillero     => "ui_role_artillero_32",
                NavalRole.Navegante     => "ui_role_navegante_32",
                NavalRole.Carpintero    => "ui_role_carpintero_32",
                NavalRole.Cirujano      => "ui_role_cirujano_32",
                NavalRole.Contramaestre => "ui_role_contramaestre_32",
                _ => null
            };

            if (assetName == null) return;
            var sprite = Resources.Load<Sprite>($"Sprites/UI/Naval/{assetName}");
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
            }
            // If sprite not found, colour-only primitive remains (fallback per spec)
        }

        private static void SetChipAlpha(GameObject root, float alpha)
        {
            var group = root.GetComponent<CanvasGroup>();
            if (group != null)
                group.alpha = alpha;
        }

        // ====================================================================
        // COROUTINE TWEENS
        // ====================================================================

        private static IEnumerator FadeIn(GameObject target, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetChipAlpha(target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            SetChipAlpha(target, 1f);
        }

        private static IEnumerator FadeOut(GameObject target, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetChipAlpha(target, 1f - Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            SetChipAlpha(target, 0f);
        }

        private static IEnumerator FadeAndSlideIn(GameObject target, float duration)
        {
            var rt = target.GetComponent<RectTransform>();
            Vector2 startPos = rt.anchoredPosition + new Vector2(0, 8f);
            Vector2 endPos   = rt.anchoredPosition;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t); // ease-out quadratic
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
                SetChipAlpha(target, eased);
                yield return null;
            }
            rt.anchoredPosition = endPos;
            SetChipAlpha(target, 1f);
        }

        private static IEnumerator TweenScale(
            RectTransform rt, Vector3 from, Vector3 to, float duration, bool easeOut)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = easeOut ? 1f - (1f - t) * (1f - t) : t * t;
                rt.localScale = Vector3.Lerp(from, to, eased);
                yield return null;
            }
            rt.localScale = to;
        }

        private static IEnumerator TweenScaleLocal(
            RectTransform rt, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rt.localScale = Vector3.Lerp(from, to, t);
                yield return null;
            }
            rt.localScale = to;
        }
    }
}
