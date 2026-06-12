using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.UI.Combat.Naval
{
    /// <summary>
    /// Battlefield card for one ship (or sea creature): sprite area, HHP/MP
    /// bars, DoT icon row, maneuver shield indicator and the crew chip overlay
    /// anchor (UX spec §2.1 / §2.6). Raises hover/click events; the HUD decides
    /// what they mean according to its current UI state.
    /// </summary>
    public class NavalShipView : MonoBehaviour
    {
        /// <summary>Click anywhere on the ship card (targeting).</summary>
        public event Action<NavalShipView> OnClicked;

        /// <summary>Pointer entered / left the ship card (inspection hover).</summary>
        public event Action<NavalShipView> OnHoverEnter;
        public event Action<NavalShipView> OnHoverExit;

        public ShipCombatant Ship { get; private set; }
        public bool IsAlly { get; private set; }
        public CrewChipOverlay ChipOverlay { get; private set; }

        /// <summary>RectTransform of the sprite area (zoom target for boarding).</summary>
        public RectTransform SpriteRT => _spriteRt;

        private RectTransform _spriteRt;
        private Image _spriteImg;
        private Image _hpFill;
        private Image _hpBg;
        private Text _hpText;
        private GameObject _mpBar;
        private Image _mpFill;
        private Text _nameLabel;
        private Transform _dotRow;
        private GameObject _maneuverShield;
        private Outline _targetOutline;
        private GameObject _bossTag;

        private static readonly Color ALLY_SPRITE = new Color(0.12f, 0.33f, 0.56f, 0.95f);
        private static readonly Color ENEMY_SPRITE = new Color(0.56f, 0.21f, 0.08f, 0.95f);
        private static readonly Color CREATURE_SPRITE = new Color(0.42f, 0.11f, 0.60f, 0.95f);

        /// <summary>Builds the full card hierarchy under this GameObject.</summary>
        public void Build(ShipCombatant ship, bool isAlly)
        {
            Ship = ship;
            IsAlly = isAlly;

            bool isCreature = ship.Crew.Count == 0 && !isAlly;

            // --- Sprite area (upper ~70% of the card) ---
            var spriteZone = NavalUIFactory.CreateZone(transform, "Sprite", 0.08f, 0.30f, 0.92f, 1f);
            _spriteRt = spriteZone.GetComponent<RectTransform>();
            _spriteImg = spriteZone.AddComponent<Image>();
            _spriteImg.color = isAlly ? ALLY_SPRITE : (isCreature ? CREATURE_SPRITE : ENEMY_SPRITE);
            _spriteImg.raycastTarget = true;
            TryLoadShipSprite(ship);

            // Ship initial as placeholder identity mark on the primitive sprite
            var mark = NavalUIFactory.CreateStretchedText(spriteZone.transform, "Mark",
                string.IsNullOrEmpty(ship.DisplayName) ? "?" : ship.DisplayName.Substring(0, 1),
                34, new Color(1f, 1f, 1f, 0.35f), TextAnchor.MiddleCenter);
            mark.raycastTarget = false;

            // Boss tag (visual cue for Jefe — boss phase data lives inside the AI)
            if (ship.IsBoss)
            {
                _bossTag = NavalUIFactory.CreateZone(spriteZone.transform, "BossTag",
                    0f, 0.85f, 0.5f, 1f, NavalUIColors.GoldDark).gameObject;
                NavalUIFactory.CreateStretchedText(_bossTag.transform, "Text", "JEFE",
                    12, NavalUIColors.GoldBright, TextAnchor.MiddleCenter);
            }

            // Maneuver shield indicator (top-right of sprite)
            _maneuverShield = NavalUIFactory.CreateZone(spriteZone.transform, "ManeuverShield",
                0.78f, 0.78f, 1f, 1f, NavalUIColors.ManeuverBlue).gameObject;
            NavalUIFactory.CreateStretchedText(_maneuverShield.transform, "Text", "≈",
                18, Color.white, TextAnchor.MiddleCenter);
            _maneuverShield.SetActive(false);

            // DoT icon row (above the bars)
            var dotZone = NavalUIFactory.CreateZone(transform, "DotRow", 0.08f, 0.22f, 0.92f, 0.30f);
            var hlg = dotZone.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            _dotRow = dotZone.transform;

            // Name
            var nameZone = NavalUIFactory.CreateZone(transform, "Name", 0.08f, 0.14f, 0.92f, 0.22f);
            _nameLabel = NavalUIFactory.CreateStretchedText(nameZone.transform, "Text",
                ship.DisplayName ?? "???", 14, NavalUIColors.CreamMuted, TextAnchor.MiddleLeft);

            // HHP bar
            var hpZone = NavalUIFactory.CreateZone(transform, "HpZone", 0.08f, 0.07f, 0.92f, 0.14f);
            var hpBar = NavalUIFactory.CreateBar(hpZone.transform, "HpBar",
                NavalUIColors.HpBgNormal, NavalUIColors.HpHigh);
            StretchToParent(hpBar);
            _hpFill = NavalUIFactory.GetBarFill(hpBar);
            _hpBg = hpBar.GetComponent<Image>();
            _hpText = NavalUIFactory.CreateStretchedText(hpBar.transform, "Text", "", 11,
                NavalUIColors.CreamMuted, TextAnchor.MiddleRight, 0, 4);

            // MP bar (only if the ship has MP)
            if (ship.MaxMP > 0)
            {
                var mpZone = NavalUIFactory.CreateZone(transform, "MpZone", 0.08f, 0.01f, 0.92f, 0.06f);
                _mpBar = NavalUIFactory.CreateBar(mpZone.transform, "MpBar",
                    NavalUIColors.MpBarBg, NavalUIColors.MpBlue);
                StretchToParent(_mpBar);
                _mpFill = NavalUIFactory.GetBarFill(_mpBar);
            }

            // Crew chip overlay (anchored to the sprite centre; chips position around it)
            var overlayGo = NavalUIFactory.CreateZone(spriteZone.transform, "ChipOverlay",
                0.5f, 0.5f, 0.5f, 0.5f);
            ChipOverlay = overlayGo.AddComponent<CrewChipOverlay>();

            WireInteractivity(spriteZone);
            Refresh();
        }

        private static void StretchToParent(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void TryLoadShipSprite(ShipCombatant ship)
        {
            if (ship.Ship == null || string.IsNullOrEmpty(ship.Ship.ShipId)) return;
            var sprite = Resources.Load<Sprite>($"Sprites/Ships/{ship.Ship.ShipId}");
            if (sprite != null)
            {
                _spriteImg.sprite = sprite;
                _spriteImg.color = Color.white;
                _spriteImg.preserveAspect = true;
            }
        }

        private void WireInteractivity(GameObject spriteZone)
        {
            var et = spriteZone.AddComponent<EventTrigger>();

            var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            click.callback.AddListener(_ => OnClicked?.Invoke(this));
            et.triggers.Add(click);

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => OnHoverEnter?.Invoke(this));
            et.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => OnHoverExit?.Invoke(this));
            et.triggers.Add(exit);
        }

        // ====================================================================
        // REFRESH
        // ====================================================================

        /// <summary>Updates bars, DoT icons, maneuver shield and dead state.</summary>
        public void Refresh()
        {
            if (Ship == null) return;

            float hpRatio = Ship.MaxHHP > 0 ? (float)Ship.CurrentHHP / Ship.MaxHHP : 0f;
            _hpFill.fillAmount = hpRatio;
            _hpFill.color = NavalUIColors.HpBarColor(hpRatio);
            _hpBg.color = NavalUIColors.HpBgColor(hpRatio);
            _hpText.text = $"{Ship.CurrentHHP}/{Ship.MaxHHP}";

            if (_mpFill != null)
                _mpFill.fillAmount = Ship.MaxMP > 0 ? (float)Ship.CurrentMP / Ship.MaxMP : 0f;

            _maneuverShield.SetActive(Ship.IsManeuvering && !Ship.IsKO);

            RefreshDotIcons();

            if (Ship.IsKO)
            {
                _spriteImg.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
                _nameLabel.color = NavalUIColors.DisabledLabel;
                SetTargetHighlight(TargetHighlight.None);
            }
        }

        private void RefreshDotIcons()
        {
            NavalUIFactory.ClearChildren(_dotRow);
            foreach (var status in Ship.StatusEffects)
            {
                var iconGo = new GameObject($"Dot_{status.Effect}",
                    typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconGo.transform.SetParent(_dotRow, false);
                var le = iconGo.GetComponent<LayoutElement>();
                le.preferredWidth = 24;
                le.preferredHeight = 24;

                var img = iconGo.GetComponent<Image>();
                img.raycastTarget = false;
                var sprite = Resources.Load<Sprite>($"Sprites/UI/Naval/{DotIconName(status.Effect)}");
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.color = Color.white;
                    img.preserveAspect = true;
                }
                else
                {
                    img.color = NavalUIColors.DotColor(status.Effect);
                }

                // Remaining turns — text counter, never colour-only (UX spec §6)
                var turns = NavalUIFactory.CreateText(iconGo.transform, "Turns",
                    status.RemainingTurns.ToString(), 10, Color.white, TextAnchor.LowerRight);
                var turnsRt = turns.GetComponent<RectTransform>();
                turnsRt.anchorMin = Vector2.zero;
                turnsRt.anchorMax = Vector2.one;
                turnsRt.offsetMin = Vector2.zero;
                turnsRt.offsetMax = Vector2.zero;
            }
        }

        private static string DotIconName(StatusEffect effect)
        {
            return effect switch
            {
                StatusEffect.Quemadura => "ui_dot_burn_24",
                StatusEffect.Veneno    => "ui_dot_poison_24",
                StatusEffect.Sangrado  => "ui_dot_bleed_24",
                StatusEffect.Silencio  => "ui_dot_silence_24",
                _ => "ui_dot_buff_24"
            };
        }

        // ====================================================================
        // TARGET HIGHLIGHT
        // ====================================================================

        public enum TargetHighlight { None, Attackable, Boardable, ActiveTurn }

        public void SetTargetHighlight(TargetHighlight highlight)
        {
            if (_targetOutline == null)
                _targetOutline = _spriteRt.gameObject.AddComponent<Outline>();

            switch (highlight)
            {
                case TargetHighlight.Attackable:
                    _targetOutline.enabled = true;
                    _targetOutline.effectColor = NavalUIColors.TargetGreen;
                    _targetOutline.effectDistance = new Vector2(3, 3);
                    break;
                case TargetHighlight.Boardable:
                    _targetOutline.enabled = true;
                    _targetOutline.effectColor = NavalUIColors.TargetGold;
                    _targetOutline.effectDistance = new Vector2(3, 3);
                    break;
                case TargetHighlight.ActiveTurn:
                    _targetOutline.enabled = true;
                    _targetOutline.effectColor = NavalUIColors.Gold;
                    _targetOutline.effectDistance = new Vector2(2, 2);
                    break;
                default:
                    _targetOutline.enabled = false;
                    break;
            }
        }

        /// <summary>Dim the card while the player targets another ship (UX §2.4).</summary>
        public void SetDimmed(bool dimmed)
        {
            var group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = dimmed ? 0.6f : 1f;
            group.interactable = !dimmed;
            group.blocksRaycasts = !dimmed;
        }
    }
}
