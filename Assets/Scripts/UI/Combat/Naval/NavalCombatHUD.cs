using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.UI.Combat.Naval
{
    /// <summary>
    /// uGUI HUD for naval combat (S4-06). Builds its hierarchy at runtime in
    /// Awake (primitive-first per visual design spec; sprites from
    /// Resources/Sprites/UI/Naval upgrade visuals when present).
    /// Layout, states and data bindings follow docs/art/ui-s406-naval-ux-spec.md;
    /// colors follow docs/art/ui-s406-naval-visual-design.md.
    /// The HUD never owns game state: it reads ShipCombatant/CombatManager and
    /// submits actions through NavalPlayerCombatInput.
    /// </summary>
    public class NavalCombatHUD : MonoBehaviour
    {
        private enum UIState
        {
            WaitingForTurn, ActionSelect, AbilitySelect,
            TargetShip, TargetCrew, EnemyTurn, BattleOver
        }

        private UIState _state = UIState.WaitingForTurn;
        private NavalPlayerCombatInput _input;
        private Runtime.Combat.CombatRunner _runner;

        /// <summary>Lazy lookup: combat events can fire before BuildBattlefield
        /// (the battle coroutine runs its first segment inside StartBattle).</summary>
        private Runtime.Combat.CombatRunner Runner
        {
            get
            {
                if (_runner == null)
                    _runner = FindAnyObjectByType<Runtime.Combat.CombatRunner>();
                return _runner;
            }
        }

        // Targeting state
        private AbilityData _pendingAbility;       // null = Cañonazo in TargetShip / Abordaje in TargetCrew
        private AbilityEntry? _pendingEntry;
        private int _keyboardTargetIndex = -1;
        private NavalShipView _boardingView;       // enemy view with chips deployed
        private NavalShipView _inspectedView;      // enemy view showing Inspect chips

        // Built zones
        private Transform _ibIcons;
        private Text _roundLabel;
        private Text _waveLabel;
        private Transform _allyColumn;
        private Transform _enemyColumn;

        // Stats panel (allied ship)
        private Text _statsName;
        private Image _statsHpFill;
        private Image _statsHpBg;
        private Text _statsHpText;
        private Image _statsMpFill;
        private Text _statsMpText;
        private Image _statsLbFill;
        private Text _statsLine;

        // Crew panel (allied)
        private Transform _crewRow;
        private Text _synergyLabel;
        private readonly Dictionary<CrewMemberState, GameObject> _crewSlots = new();

        // Action panel
        private GameObject _actionPanel;
        private Button _btnCannon, _btnAbility, _btnManeuver, _btnBoarding, _btnRepair, _btnPass;

        // Ability menu
        private GameObject _abilityMenu;
        private Transform _abilityContent;
        private GameObject _silenceBanner;
        private Button _btnBack;

        // Hint bar / tooltip
        private GameObject _hintBar;
        private Text _hintText;
        private GameObject _shipTooltip;
        private Text _shipTooltipText;

        // Battle log
        private Transform _logContent;
        private ScrollRect _logScroll;

        // Overlays
        private GameObject _waveOverlay;
        private Text _waveOverlayText;
        private GameObject _resultOverlay;
        private Text _resultText;
        private Text _resultDetails;
        private Button _btnContinue;
        private Button _btnRetry;

        // Ship views
        private NavalShipView _allyView;
        private readonly List<NavalShipView> _enemyViews = new();

        // ====================================================================
        // LIFECYCLE
        // ====================================================================

        private void Awake()
        {
            BuildLayout();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            if (_input != null)
                _input.OnInputRequested -= HandleInputRequested;
        }

        private void Update()
        {
            // Cancellation: Escape or right click in any targeting/submenu state
            bool cancelPressed =
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
            if (cancelPressed)
            {
                if (_state == UIState.TargetShip || _state == UIState.TargetCrew ||
                    _state == UIState.AbilitySelect)
                {
                    CancelToActionSelect();
                    return;
                }
                HideInspection();
            }

            if (_state == UIState.TargetShip)
                HandleTargetShipKeyboard();
        }

        /// <summary>Binds the player input bridge. Call before the battle starts.</summary>
        public void Bind(NavalPlayerCombatInput input)
        {
            if (_input != null)
                _input.OnInputRequested -= HandleInputRequested;
            _input = input;
            if (_input != null)
                _input.OnInputRequested += HandleInputRequested;
        }

        /// <summary>Builds battlefield views. Called by the bootstrap after StartBattle.</summary>
        public void BuildBattlefield(ShipCombatant ally, IReadOnlyList<ICombatant> enemies)
        {
            NavalUIFactory.ClearChildren(_allyColumn);
            NavalUIFactory.ClearChildren(_enemyColumn);
            _enemyViews.Clear();

            _allyView = CreateShipView(ally, true, _allyColumn);
            RebuildEnemyViews(enemies);
            BuildCrewPanel(ally);
            RefreshStatsPanel();
            RefreshSynergies();

            // The first round/wave events fired inside StartBattle, before this
            // call — refresh what they would have painted.
            var manager = Runner?.Manager;
            if (manager != null)
            {
                _waveLabel.text = $"OLEADA {manager.WaveIndex + 1}/{manager.TotalWaves}";
                _roundLabel.text = $"Ronda {Mathf.Max(manager.RoundNumber, 1)}";
                RefreshInitiativeBar(manager.CurrentActor?.Combatant);
            }
        }

        // ====================================================================
        // LAYOUT CONSTRUCTION (portrait 1080×1920, zones per UX spec §2.1)
        // ====================================================================

        private void BuildLayout()
        {
            var root = GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            // Background: BgDark + voodoo violet ambient tint (visual design §2)
            var bg = NavalUIFactory.CreateZone(transform, "Bg", 0, 0, 1, 1, NavalUIColors.BgDark);
            NavalUIFactory.CreateZone(bg.transform, "VoodooTint", 0, 0, 1, 1,
                new Color(0.42f, 0.11f, 0.60f, 0.07f));

            // --- Initiative bar ---
            var ib = NavalUIFactory.CreateZone(transform, "InitiativeBar",
                0f, 0.945f, 1f, 1f, NavalUIColors.HeaderBase);
            var ibIcons = NavalUIFactory.CreateZone(ib.transform, "Icons", 0f, 0f, 1f, 1f);
            var hlg = ibIcons.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.padding = new RectOffset(12, 12, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            _ibIcons = ibIcons.transform;

            // --- Round / wave row ---
            var info = NavalUIFactory.CreateZone(transform, "RoundInfo", 0f, 0.915f, 1f, 0.945f);
            _roundLabel = NavalUIFactory.CreateStretchedText(
                NavalUIFactory.CreateZone(info.transform, "Round", 0f, 0f, 0.5f, 1f).transform,
                "Text", "Ronda 1", 15, NavalUIColors.Cream, TextAnchor.MiddleLeft, 16);
            _waveLabel = NavalUIFactory.CreateStretchedText(
                NavalUIFactory.CreateZone(info.transform, "Wave", 0.5f, 0f, 1f, 1f).transform,
                "Text", "OLEADA 1/1", 15, NavalUIColors.Cream, TextAnchor.MiddleRight, 0, 16);

            // --- Battlefield ---
            var field = NavalUIFactory.CreateZone(transform, "Battlefield", 0f, 0.60f, 1f, 0.915f);
            var allyCol = NavalUIFactory.CreateZone(field.transform, "AllyColumn", 0f, 0f, 0.48f, 1f);
            var enemyCol = NavalUIFactory.CreateZone(field.transform, "EnemyColumn", 0.52f, 0f, 1f, 1f);
            AddColumnLayout(allyCol);
            AddColumnLayout(enemyCol);
            _allyColumn = allyCol.transform;
            _enemyColumn = enemyCol.transform;

            // Ship tooltip (inspection summary, top of battlefield)
            _shipTooltip = NavalUIFactory.CreateZone(field.transform, "ShipTooltip",
                0.05f, 0.86f, 0.95f, 1f, NavalUIColors.PanelDark).gameObject;
            var ttOutline = _shipTooltip.AddComponent<Outline>();
            ttOutline.effectColor = NavalUIColors.WoodBorder;
            ttOutline.effectDistance = new Vector2(1, 1);
            _shipTooltipText = NavalUIFactory.CreateStretchedText(_shipTooltip.transform,
                "Text", "", 13, NavalUIColors.Cream, TextAnchor.MiddleCenter);
            _shipTooltip.SetActive(false);

            // --- Allied ship stats panel ---
            BuildStatsPanel();

            // --- Allied crew panel ---
            BuildCrewZone();

            // --- Action panel + ability menu + hint bar ---
            BuildActionPanel();
            BuildAbilityMenu();
            BuildHintBar();

            // --- Battle log ---
            var logZone = NavalUIFactory.CreateZone(transform, "BattleLog",
                0f, 0f, 1f, 0.115f, NavalUIColors.HintBarBg);
            _logContent = NavalUIFactory.CreateScrollList(logZone, 2f);
            _logScroll = logZone.GetComponent<ScrollRect>();

            // --- Overlays ---
            BuildWaveOverlay();
            BuildResultOverlay();
        }

        private static void AddColumnLayout(GameObject column)
        {
            var vlg = column.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;
        }

        private void BuildStatsPanel()
        {
            var panel = NavalUIFactory.CreateZone(transform, "StatsPanel",
                0.02f, 0.525f, 0.98f, 0.598f, NavalUIColors.PanelDark);
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = NavalUIColors.WoodBorder;
            outline.effectDistance = new Vector2(1, 1);

            _statsName = NavalUIFactory.CreateStretchedText(
                NavalUIFactory.CreateZone(panel.transform, "Name", 0.02f, 0.68f, 0.6f, 1f).transform,
                "Text", "", 16, NavalUIColors.Gold, TextAnchor.MiddleLeft);

            _statsLine = NavalUIFactory.CreateStretchedText(
                NavalUIFactory.CreateZone(panel.transform, "Stats", 0.6f, 0.68f, 0.98f, 1f).transform,
                "Text", "", 12, NavalUIColors.Cream, TextAnchor.MiddleRight);

            // HHP bar
            var hpZone = NavalUIFactory.CreateZone(panel.transform, "Hp", 0.02f, 0.46f, 0.98f, 0.66f);
            var hpBar = NavalUIFactory.CreateBar(hpZone.transform, "Bar",
                NavalUIColors.HpBgNormal, NavalUIColors.HpHigh);
            Stretch(hpBar);
            _statsHpFill = NavalUIFactory.GetBarFill(hpBar);
            _statsHpBg = hpBar.GetComponent<Image>();
            _statsHpText = NavalUIFactory.CreateStretchedText(hpBar.transform, "Text",
                "", 12, NavalUIColors.CreamMuted, TextAnchor.MiddleCenter);

            // MP bar
            var mpZone = NavalUIFactory.CreateZone(panel.transform, "Mp", 0.02f, 0.26f, 0.98f, 0.44f);
            var mpBar = NavalUIFactory.CreateBar(mpZone.transform, "Bar",
                NavalUIColors.MpBarBg, NavalUIColors.MpBlue);
            Stretch(mpBar);
            _statsMpFill = NavalUIFactory.GetBarFill(mpBar);
            _statsMpText = NavalUIFactory.CreateStretchedText(mpBar.transform, "Text",
                "", 12, NavalUIColors.CreamMuted, TextAnchor.MiddleCenter);

            // LB bar (binary — decision D2)
            var lbZone = NavalUIFactory.CreateZone(panel.transform, "Lb", 0.02f, 0.06f, 0.98f, 0.24f);
            var lbBar = NavalUIFactory.CreateBar(lbZone.transform, "Bar",
                NavalUIColors.LbEmpty, NavalUIColors.LbGold);
            Stretch(lbBar);
            _statsLbFill = NavalUIFactory.GetBarFill(lbBar);
            var lbLabel = NavalUIFactory.CreateStretchedText(lbBar.transform, "Text",
                "LB", 11, NavalUIColors.CreamMuted, TextAnchor.MiddleCenter);
            lbLabel.raycastTarget = false;
        }

        private void BuildCrewZone()
        {
            var panel = NavalUIFactory.CreateZone(transform, "CrewPanel",
                0.02f, 0.43f, 0.98f, 0.523f, NavalUIColors.PanelDark);
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = NavalUIColors.WoodBorder;
            outline.effectDistance = new Vector2(1, 1);

            NavalUIFactory.CreateStretchedText(
                NavalUIFactory.CreateZone(panel.transform, "Title", 0.02f, 0.78f, 0.5f, 1f).transform,
                "Text", "TRIPULACIÓN", 12, NavalUIColors.Cream, TextAnchor.MiddleLeft);

            _synergyLabel = NavalUIFactory.CreateStretchedText(
                NavalUIFactory.CreateZone(panel.transform, "Synergies", 0.3f, 0.78f, 0.98f, 1f).transform,
                "Text", "", 11, NavalUIColors.Cream, TextAnchor.MiddleRight);

            var row = NavalUIFactory.CreateZone(panel.transform, "Slots", 0.01f, 0.02f, 0.99f, 0.76f);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.padding = new RectOffset(4, 4, 2, 2);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            _crewRow = row.transform;
        }

        private void BuildActionPanel()
        {
            _actionPanel = NavalUIFactory.CreateZone(transform, "ActionPanel",
                0.02f, 0.125f, 0.98f, 0.425f).gameObject;
            var grid = _actionPanel.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(495, 165);
            grid.spacing = new Vector2(14, 14);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(8, 8, 8, 8);

            _btnCannon = NavalUIFactory.CreateActionButton(_actionPanel.transform,
                "BtnCannon", "CAÑONAZO", "Sprites/UI/Naval/ui_action_cannon_32", NavalUIColors.HpLow);
            _btnAbility = NavalUIFactory.CreateActionButton(_actionPanel.transform,
                "BtnAbility", "HABILIDAD NAVAL", "Sprites/UI/Naval/ui_action_ability_32", NavalUIColors.Gold);
            _btnManeuver = NavalUIFactory.CreateActionButton(_actionPanel.transform,
                "BtnManeuver", "MANIOBRA EVASIVA", "Sprites/UI/Naval/ui_action_maneuver_32", NavalUIColors.ManeuverBlue);
            _btnBoarding = NavalUIFactory.CreateActionButton(_actionPanel.transform,
                "BtnBoarding", "ABORDAJE", "Sprites/UI/Naval/ui_action_boarding_32", NavalUIColors.GoldMid);
            _btnRepair = NavalUIFactory.CreateActionButton(_actionPanel.transform,
                "BtnRepair", "REPARAR", "Sprites/UI/Naval/ui_action_repair_32", NavalUIColors.HpHigh);
            _btnPass = NavalUIFactory.CreateActionButton(_actionPanel.transform,
                "BtnPass", "PASAR TURNO", "Sprites/UI/Naval/ui_action_pass_32", NavalUIColors.CreamMuted);

            _btnCannon.onClick.AddListener(OnCannonClicked);
            _btnAbility.onClick.AddListener(OnAbilityClicked);
            _btnManeuver.onClick.AddListener(OnManeuverClicked);
            _btnBoarding.onClick.AddListener(OnBoardingClicked);
            _btnRepair.onClick.AddListener(OnRepairClicked);
            _btnPass.onClick.AddListener(OnPassClicked);

            _actionPanel.SetActive(false);
        }

        private void BuildAbilityMenu()
        {
            _abilityMenu = NavalUIFactory.CreateZone(transform, "AbilityMenu",
                0.02f, 0.125f, 0.98f, 0.425f, NavalUIColors.PanelDark).gameObject;

            _silenceBanner = NavalUIFactory.CreateZone(_abilityMenu.transform, "SilenceBanner",
                0f, 0.88f, 1f, 1f, NavalUIColors.DotSilence).gameObject;
            NavalUIFactory.CreateStretchedText(_silenceBanner.transform, "Text",
                "SILENCIO ACTIVO — Habilidades bloqueadas", 14, Color.white, TextAnchor.MiddleCenter);
            _silenceBanner.SetActive(false);

            var listZone = NavalUIFactory.CreateZone(_abilityMenu.transform, "List",
                0.01f, 0.12f, 0.99f, 0.87f);
            _abilityContent = NavalUIFactory.CreateScrollList(listZone.gameObject, 6f);

            var backZone = NavalUIFactory.CreateZone(_abilityMenu.transform, "BackZone",
                0.01f, 0.01f, 0.5f, 0.11f);
            _btnBack = NavalUIFactory.CreateActionButton(backZone.transform, "BtnBack",
                "← VOLVER", null, NavalUIColors.Cream);
            Stretch(_btnBack.gameObject);
            _btnBack.onClick.AddListener(CancelToActionSelect);

            _abilityMenu.SetActive(false);
        }

        private void BuildHintBar()
        {
            _hintBar = NavalUIFactory.CreateZone(transform, "HintBar",
                0.02f, 0.37f, 0.98f, 0.425f, NavalUIColors.HintBarBg).gameObject;
            _hintText = NavalUIFactory.CreateStretchedText(_hintBar.transform, "Text",
                "", 15, NavalUIColors.Cream, TextAnchor.MiddleCenter);
            _hintBar.SetActive(false);
        }

        private void BuildWaveOverlay()
        {
            _waveOverlay = NavalUIFactory.CreateZone(transform, "WaveOverlay",
                0f, 0.4f, 1f, 0.6f, new Color(0.05f, 0.04f, 0.08f, 0.92f)).gameObject;
            _waveOverlayText = NavalUIFactory.CreateStretchedText(_waveOverlay.transform, "Text",
                "OLEADA COMPLETADA", 26, NavalUIColors.CreamMuted, TextAnchor.MiddleCenter);
            _waveOverlay.SetActive(false);
        }

        private void BuildResultOverlay()
        {
            _resultOverlay = NavalUIFactory.CreateZone(transform, "ResultOverlay",
                0f, 0f, 1f, 1f, new Color(0.03f, 0.02f, 0.06f, 0.95f)).gameObject;

            _resultText = NavalUIFactory.CreateStretchedText(
                NavalUIFactory.CreateZone(_resultOverlay.transform, "Result", 0.1f, 0.55f, 0.9f, 0.7f).transform,
                "Text", "", 42, NavalUIColors.VictoryGold, TextAnchor.MiddleCenter);

            _resultDetails = NavalUIFactory.CreateStretchedText(
                NavalUIFactory.CreateZone(_resultOverlay.transform, "Details", 0.1f, 0.38f, 0.9f, 0.55f).transform,
                "Text", "", 16, NavalUIColors.Cream, TextAnchor.UpperCenter);

            var btnZone = NavalUIFactory.CreateZone(_resultOverlay.transform, "Buttons",
                0.15f, 0.26f, 0.85f, 0.34f);
            var hlg = btnZone.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            _btnContinue = NavalUIFactory.CreateActionButton(btnZone.transform, "BtnContinue",
                "CONTINUAR", null, NavalUIColors.Gold);
            _btnContinue.onClick.AddListener(OnContinueClicked);

            _btnRetry = NavalUIFactory.CreateActionButton(btnZone.transform, "BtnRetry",
                "REINTENTAR", null, NavalUIColors.Gold);
            _btnRetry.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));

            _resultOverlay.SetActive(false);
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ====================================================================
        // EVENTS
        // ====================================================================

        private void SubscribeEvents()
        {
            GameEvents.OnBattleStart += HandleBattleStart;
            GameEvents.OnRoundStart += HandleRoundStart;
            GameEvents.OnTurnStart += HandleTurnStart;
            GameEvents.OnActionChosen += HandleActionChosen;
            GameEvents.OnDamageDealt += HandleDamageDealt;
            GameEvents.OnHealApplied += HandleHealApplied;
            GameEvents.OnUnitDied += HandleUnitDied;
            GameEvents.OnWaveComplete += HandleWaveComplete;
            GameEvents.OnWaveStart += HandleWaveStart;
            GameEvents.OnBattleEnd += HandleBattleEnd;
            GameEvents.OnStatusApplied += HandleStatusApplied;
            GameEvents.OnCrewDamaged += HandleCrewDamaged;
            GameEvents.OnCrewDied += HandleCrewDied;
            GameEvents.OnShipStatsRecalculated += HandleShipStatsRecalculated;
            GameEvents.OnManeuverActivated += HandleManeuverActivated;
            GameEvents.OnLimitBreakActivated += HandleLimitBreakActivated;
        }

        private void UnsubscribeEvents()
        {
            GameEvents.OnBattleStart -= HandleBattleStart;
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnTurnStart -= HandleTurnStart;
            GameEvents.OnActionChosen -= HandleActionChosen;
            GameEvents.OnDamageDealt -= HandleDamageDealt;
            GameEvents.OnHealApplied -= HandleHealApplied;
            GameEvents.OnUnitDied -= HandleUnitDied;
            GameEvents.OnWaveComplete -= HandleWaveComplete;
            GameEvents.OnWaveStart -= HandleWaveStart;
            GameEvents.OnBattleEnd -= HandleBattleEnd;
            GameEvents.OnStatusApplied -= HandleStatusApplied;
            GameEvents.OnCrewDamaged -= HandleCrewDamaged;
            GameEvents.OnCrewDied -= HandleCrewDied;
            GameEvents.OnShipStatsRecalculated -= HandleShipStatsRecalculated;
            GameEvents.OnManeuverActivated -= HandleManeuverActivated;
            GameEvents.OnLimitBreakActivated -= HandleLimitBreakActivated;
        }

        private void HandleBattleStart(BattleStartEvent e)
        {
            AddLog($"¡Combate naval! {e.TotalWaves} oleada(s) de enemigos", NavalUIColors.Cream);
            _resultOverlay.SetActive(false);
        }

        private void HandleRoundStart(int round)
        {
            _roundLabel.text = $"Ronda {round}";
            RefreshInitiativeBar(null);
        }

        private void HandleTurnStart(ICombatant actor)
        {
            if (actor is not ShipCombatant) return; // naval HUD: ships only
            HideInspection();
            RefreshInitiativeBar(actor);
            RefreshAllViews();
            SetState(UIState.EnemyTurn);
        }

        private void HandleInputRequested()
        {
            SetState(UIState.ActionSelect);
            RefreshActionButtons();
            RefreshStatsPanel();
        }

        private void HandleActionChosen(CombatAction action)
        {
            string actor = Runner?.Manager?.CurrentActor?.Combatant?.DisplayName ?? "???";
            string msg = action.Type switch
            {
                ActionType.Attack => $"{actor} dispara un cañonazo contra {action.Target?.DisplayName}",
                ActionType.Ability => $"{actor} usa {action.ActionName}",
                ActionType.Maneuver => $"{actor} ejecuta una maniobra evasiva",
                ActionType.Boarding => $"{actor} aborda al {RoleName(action.TargetCrew)} de {action.Target?.DisplayName}",
                ActionType.Repair => $"{actor} repara el casco",
                ActionType.Pass => $"{actor} pasa el turno",
                _ => $"{actor} actúa"
            };
            AddLog(msg, NavalUIColors.Cream);
        }

        private static string RoleName(CrewMemberState crew)
        {
            return crew == null ? "???" : crew.Role.ToString();
        }

        private void HandleDamageDealt(DamageEvent e)
        {
            string target = e.Target?.DisplayName ?? "???";
            if (e.Result.IsMiss)
            {
                AddLog($"¡FALLO contra {target}!", NavalUIColors.HpMid);
            }
            else
            {
                string source = e.DamageSource switch
                {
                    DamageSource.Burn => " (Quemadura)",
                    DamageSource.Bleed => " (Sangrado)",
                    DamageSource.Poison => " (Veneno)",
                    _ => ""
                };
                string maneuvered = e.IsGuarded ? " [maniobra]" : "";
                AddLog($"{target} recibe {e.ActualDamage} de daño al casco{source}{maneuvered}",
                    NavalUIColors.LogDamageReceived);
            }
            RefreshAllViews();
            RefreshStatsPanel();
        }

        private void HandleHealApplied(HealEvent e)
        {
            AddLog($"{e.Target?.DisplayName} repara {e.Amount} de casco", NavalUIColors.HpHigh);
            RefreshAllViews();
            RefreshStatsPanel();
        }

        private void HandleUnitDied(ICombatant ship)
        {
            AddLog($"¡{ship?.DisplayName} se hunde!", NavalUIColors.LogCrewDeath);
            RefreshAllViews();
        }

        private void HandleCrewDamaged(CrewDamageEvent e)
        {
            string source = e.Source switch
            {
                DamageSource.Boarding => "Abordaje",
                DamageSource.Bleed => "Sangrado",
                DamageSource.Poison => "Veneno",
                _ => "Ataque"
            };
            AddLog($"{source}: {e.Crew.Role} de {e.Ship.DisplayName} recibe {e.ActualDamage} de daño",
                NavalUIColors.LogDamageReceived);

            if (_allyView != null && e.Ship == _allyView.Ship)
                RefreshCrewPanel();

            var view = FindView(e.Ship);
            view?.ChipOverlay?.OnCrewDamaged(e.Crew);
        }

        private void HandleCrewDied(CrewDiedEvent e)
        {
            AddLog($"¡El {e.Crew.Role} de {e.Ship.DisplayName} ha caído!", NavalUIColors.LogCrewDeath);

            if (_allyView != null && e.Ship == _allyView.Ship)
            {
                RefreshCrewPanel();
                RefreshSynergies();
            }
            var view = FindView(e.Ship);
            view?.ChipOverlay?.OnCrewDied(e.Crew);
        }

        private void HandleShipStatsRecalculated(ShipCombatant ship)
        {
            if (_allyView != null && ship == _allyView.Ship)
            {
                RefreshStatsPanel();
                RefreshSynergies();
            }
        }

        private void HandleManeuverActivated(ShipCombatant ship)
        {
            RefreshAllViews();
        }

        private void HandleLimitBreakActivated(ICombatant ship)
        {
            AddLog($"¡LIMIT BREAK! {ship?.DisplayName} obtiene un turno extra", NavalUIColors.GoldBright);
            RefreshStatsPanel();
        }

        private void HandleWaveComplete(int wave)
        {
            int total = Runner?.Manager?.TotalWaves ?? 1;
            AddLog($"Oleada {wave + 1}/{total} superada", NavalUIColors.Cream);
            _waveOverlayText.text = "OLEADA COMPLETADA\nTu barco retiene daño y MP";
            _waveOverlay.SetActive(true);
        }

        private void HandleWaveStart(int wave)
        {
            int total = Runner?.Manager?.TotalWaves ?? 1;
            _waveLabel.text = $"OLEADA {wave + 1}/{total}";
            _waveOverlay.SetActive(false);
            if (Runner?.Manager != null && wave > 0)
                RebuildEnemyViews(Runner.Manager.Enemies);
            AddLog($"Comienza la oleada {wave + 1}/{total}", NavalUIColors.Cream);
        }

        private void HandleBattleEnd(BattleEndEvent e)
        {
            SetState(UIState.BattleOver);
            _resultOverlay.SetActive(true);

            var ally = _allyView?.Ship;
            if (e.Result == BattleResult.Victory)
            {
                _resultText.text = "¡VICTORIA!";
                _resultText.color = NavalUIColors.VictoryGold;
                int crewAlive = ally?.GetLivingCrew().Count ?? 0;
                int crewTotal = ally?.Crew.Count ?? 0;
                _resultDetails.text =
                    $"{ally?.DisplayName} ha triunfado\n\n" +
                    $"Casco: {ally?.CurrentHHP}/{ally?.MaxHHP}\n" +
                    $"Tripulación superviviente: {crewAlive}/{crewTotal}\n" +
                    $"MP restante: {ally?.CurrentMP}/{ally?.MaxMP}\n" +
                    $"Rondas: {e.RoundsElapsed}";
                _btnRetry.gameObject.SetActive(false);
            }
            else
            {
                _resultText.text = "DERROTA";
                _resultText.color = NavalUIColors.DefeatRed;
                _resultDetails.text = $"El barco se ha hundido tras {e.RoundsElapsed} rondas";
                _btnRetry.gameObject.SetActive(true);
            }
        }

        private void HandleStatusApplied(StatusAppliedEvent e)
        {
            AddLog($"{e.Target?.DisplayName} sufre {e.Status.Effect}", NavalUIColors.DotPoison);
            RefreshAllViews();
        }

        private void OnContinueClicked()
        {
            var flow = FindAnyObjectByType<Runtime.Flow.GameFlowManager>();
            if (flow != null)
                flow.LoadResults();
            else
                _resultOverlay.SetActive(false); // standalone test scene (S4-07 wires the flow)
        }

        // ====================================================================
        // STATE MACHINE
        // ====================================================================

        private void SetState(UIState state)
        {
            _state = state;

            _actionPanel.SetActive(state == UIState.ActionSelect);
            _abilityMenu.SetActive(state == UIState.AbilitySelect);
            _hintBar.SetActive(state == UIState.TargetShip || state == UIState.TargetCrew);

            if (state != UIState.TargetShip && state != UIState.TargetCrew)
            {
                ClearTargetHighlights();
                _keyboardTargetIndex = -1;
            }
        }

        private void CancelToActionSelect()
        {
            if (_boardingView != null)
            {
                _boardingView.ChipOverlay.OnCrewChipClicked -= HandleCrewChipClicked;
                _boardingView.ChipOverlay.Hide();
                _boardingView = null;
            }
            SetAllDimmed(false);
            _pendingAbility = null;
            _pendingEntry = null;
            SetState(UIState.ActionSelect);
            RefreshActionButtons();
        }

        // ====================================================================
        // ACTION BUTTONS (state table: UX spec §4)
        // ====================================================================

        private void RefreshActionButtons()
        {
            var ship = _input?.ActorShip;
            if (ship == null) return;

            NavalUIFactory.SetActionButtonEnabled(_btnCannon, true, "FPW vs casco enemigo");

            bool anyAbilityReady = false;
            bool silenced = ship.HasStatus(StatusEffect.Silencio);
            foreach (var pair in EnumerateAbilities(ship))
            {
                if (ship.IsAbilityReady(pair.Ability)) { anyAbilityReady = true; break; }
            }
            string abilityReason = silenced ? "Silencio activo"
                : !anyAbilityReady ? "Sin habilidades disponibles" : "MP y mareas a tu favor";
            NavalUIFactory.SetActionButtonEnabled(_btnAbility, anyAbilityReady && !silenced, abilityReason);

            NavalUIFactory.SetActionButtonEnabled(_btnManeuver, true,
                ship.IsManeuvering ? "ACTIVA" : "Mitad de daño hasta tu turno");

            bool boardable = _input.IsBoardingAvailable() && ship.GetLivingCrew().Count > 0;
            string boardReason = boardable
                ? "FPW vs DEF de la tripulación"
                : "Imposible: no hay tripulación que abordar";
            NavalUIFactory.SetActionButtonEnabled(_btnBoarding, boardable, boardReason);

            bool canRepair = ship.CurrentMP >= NavalTurnResolver.REPAIR_MP_COST;
            NavalUIFactory.SetActionButtonEnabled(_btnRepair, canRepair,
                canRepair ? $"{NavalTurnResolver.REPAIR_MP_COST} MP" : $"Sin MP (necesitas {NavalTurnResolver.REPAIR_MP_COST})");

            NavalUIFactory.SetActionButtonEnabled(_btnPass, true, "");
        }

        private void OnCannonClicked()
        {
            if (_state != UIState.ActionSelect) return;
            _pendingAbility = null;
            _pendingEntry = null;
            EnterTargetShip("Selecciona un enemigo para el cañonazo  |  [ESC] Cancelar");
        }

        private void OnAbilityClicked()
        {
            if (_state != UIState.ActionSelect) return;
            PopulateAbilityMenu();
            SetState(UIState.AbilitySelect);
        }

        private void OnManeuverClicked()
        {
            if (_state != UIState.ActionSelect) return;
            _input.SubmitManeuver();
            SetState(UIState.WaitingForTurn);
        }

        private void OnBoardingClicked()
        {
            if (_state != UIState.ActionSelect) return;
            _pendingAbility = null;
            _pendingEntry = null;
            EnterTargetCrew();
        }

        private void OnRepairClicked()
        {
            if (_state != UIState.ActionSelect) return;
            _input.SubmitRepair();
            SetState(UIState.WaitingForTurn);
        }

        private void OnPassClicked()
        {
            if (_state != UIState.ActionSelect) return;
            _input.SubmitPass();
            SetState(UIState.WaitingForTurn);
        }

        // ====================================================================
        // ABILITY MENU
        // ====================================================================

        /// <summary>
        /// Enumerates (AbilityData, AbilityEntry?) pairs in the same order the
        /// ship builds its pool: BaseAbilities (no entry → no LB) + living crew
        /// SeaAbilities (entry carries the LB flag). Duplicates intentional.
        /// </summary>
        private static IEnumerable<(AbilityData Ability, AbilityEntry? Entry)>
            EnumerateAbilities(ShipCombatant ship)
        {
            if (ship.Ship.BaseAbilities != null)
                foreach (var ability in ship.Ship.BaseAbilities)
                    if (ability != null)
                        yield return (ability, null);

            foreach (var crew in ship.Crew)
            {
                if (crew.IsDead || crew.Unit == null || crew.Unit.SeaAbilities == null)
                    continue;
                foreach (var entry in crew.Unit.SeaAbilities)
                    if (entry.Ability != null)
                        yield return (entry.Ability, entry);
            }
        }

        private void PopulateAbilityMenu()
        {
            NavalUIFactory.ClearChildren(_abilityContent);
            var ship = _input.ActorShip;
            bool silenced = ship.HasStatus(StatusEffect.Silencio);
            _silenceBanner.SetActive(silenced);

            int count = 0;
            foreach (var pair in EnumerateAbilities(ship))
            {
                count++;
                CreateAbilityCard(ship, pair.Ability, pair.Entry, silenced);
            }

            if (count == 0)
            {
                NavalUIFactory.CreateText(_abilityContent, "Empty",
                    "Sin habilidades en el pool (tripulación caída)", 14,
                    NavalUIColors.Cream, TextAnchor.MiddleCenter);
            }
        }

        private void CreateAbilityCard(ShipCombatant ship, AbilityData ability,
            AbilityEntry? entry, bool silenced)
        {
            var card = new GameObject($"Ability_{ability.Id}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            card.transform.SetParent(_abilityContent, false);
            card.GetComponent<LayoutElement>().preferredHeight = 64;

            bool ready = !silenced && ship.IsAbilityReady(ability);
            card.GetComponent<Image>().color = ready
                ? NavalUIColors.WoodBase : NavalUIColors.BtnDisabledBg;

            int cd = ship.GetCooldownRemaining(ability);
            string lbBadge = entry.HasValue && entry.Value.CanLimitBreak ? "[LB] " : "";
            string blockBadge = silenced ? " [SILENCIO]"
                : cd > 0 ? $" [CD: {cd}]"
                : ship.CurrentMP < ability.MPCost ? " [sin MP]" : "";

            var title = NavalUIFactory.CreateText(card.transform, "Title",
                $"{lbBadge}{ability.DisplayName}{blockBadge}", 15,
                ready ? NavalUIColors.Gold : NavalUIColors.DisabledLabel, TextAnchor.UpperLeft);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(10, 0);
            titleRt.offsetMax = new Vector2(-10, -4);

            var info = NavalUIFactory.CreateText(card.transform, "Info",
                $"Elem: {ability.Element}  [{ability.MPCost} MP]  {TargetLabel(ability.TargetType)}", 12,
                ready ? NavalUIColors.Cream : NavalUIColors.DisabledLabel, TextAnchor.UpperLeft);
            var infoRt = info.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0f, 0f);
            infoRt.anchorMax = new Vector2(1f, 0.5f);
            infoRt.offsetMin = new Vector2(10, 4);
            infoRt.offsetMax = new Vector2(-10, 0);

            var btn = card.GetComponent<Button>();
            btn.interactable = ready;
            var capturedAbility = ability;
            var capturedEntry = entry;
            btn.onClick.AddListener(() => OnAbilitySelected(capturedAbility, capturedEntry));
        }

        private static string TargetLabel(TargetType type)
        {
            return type switch
            {
                TargetType.SingleEnemy => "Único",
                TargetType.AoeEnemy => "Todos",
                TargetType.SingleCrewEnemy => "Crew enemiga",
                TargetType.Self => "Propio",
                TargetType.SingleAlly => "Aliado",
                TargetType.AllyAoe => "Aliados",
                _ => "?"
            };
        }

        private void OnAbilitySelected(AbilityData ability, AbilityEntry? entry)
        {
            _pendingAbility = ability;
            _pendingEntry = entry;

            switch (ability.TargetType)
            {
                case TargetType.Self:
                case TargetType.AoeEnemy:
                case TargetType.AllyAoe:
                    _input.SubmitAbility(ability, null, entry);
                    _pendingAbility = null;
                    SetState(UIState.WaitingForTurn);
                    break;
                case TargetType.SingleAlly:
                    // Single-ship side: the only ally target is the own ship
                    _input.SubmitAbility(ability, _input.ActorShip, entry);
                    _pendingAbility = null;
                    SetState(UIState.WaitingForTurn);
                    break;
                case TargetType.SingleCrewEnemy:
                    EnterTargetCrew();
                    break;
                default:
                    EnterTargetShip($"Selecciona objetivo para {ability.DisplayName}  |  [ESC] Cancelar");
                    break;
            }
        }

        // ====================================================================
        // TARGETING: ENEMY SHIP (Cañonazo / SingleEnemy ability)
        // ====================================================================

        private void EnterTargetShip(string hint)
        {
            SetState(UIState.TargetShip);
            _hintText.text = hint;

            _keyboardTargetIndex = -1;
            for (int i = 0; i < _enemyViews.Count; i++)
            {
                var view = _enemyViews[i];
                if (view.Ship.IsKO) continue;
                view.SetTargetHighlight(NavalShipView.TargetHighlight.Attackable);
                if (_keyboardTargetIndex < 0) _keyboardTargetIndex = i;
            }
            UpdateKeyboardTargetFocus();
        }

        private void HandleTargetShipKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.rightArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
                kb.tabKey.wasPressedThisFrame)
            {
                _keyboardTargetIndex = NextLivingEnemyIndex(_keyboardTargetIndex, 1);
                UpdateKeyboardTargetFocus();
            }
            else if (kb.leftArrowKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            {
                _keyboardTargetIndex = NextLivingEnemyIndex(_keyboardTargetIndex, -1);
                UpdateKeyboardTargetFocus();
            }
            else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                if (_keyboardTargetIndex >= 0 && _keyboardTargetIndex < _enemyViews.Count)
                    ConfirmShipTarget(_enemyViews[_keyboardTargetIndex]);
            }
        }

        private int NextLivingEnemyIndex(int current, int step)
        {
            if (_enemyViews.Count == 0) return -1;
            int idx = current;
            for (int i = 0; i < _enemyViews.Count; i++)
            {
                idx = (idx + step + _enemyViews.Count) % _enemyViews.Count;
                if (!_enemyViews[idx].Ship.IsKO) return idx;
            }
            return current;
        }

        private void UpdateKeyboardTargetFocus()
        {
            for (int i = 0; i < _enemyViews.Count; i++)
            {
                if (_enemyViews[i].Ship.IsKO) continue;
                _enemyViews[i].SetTargetHighlight(i == _keyboardTargetIndex
                    ? NavalShipView.TargetHighlight.Boardable      // bright gold = focus
                    : NavalShipView.TargetHighlight.Attackable);
            }
        }

        private void ConfirmShipTarget(NavalShipView view)
        {
            if (view.Ship.IsKO) return;

            if (_pendingAbility != null)
                _input.SubmitAbility(_pendingAbility, view.Ship, _pendingEntry);
            else
                _input.SubmitCannonball(view.Ship);

            _pendingAbility = null;
            _pendingEntry = null;
            SetState(UIState.WaitingForTurn);
        }

        // ====================================================================
        // TARGETING: ENEMY CREW (Abordaje / SingleCrewEnemy — decision D1: chips)
        // ====================================================================

        private void EnterTargetCrew()
        {
            HideInspection();
            SetState(UIState.TargetCrew);
            _hintText.text = "Selecciona tripulante enemigo  |  [ESC] Cancelar";

            var boardable = new List<NavalShipView>();
            foreach (var view in _enemyViews)
                if (!view.Ship.IsKO && view.Ship.GetLivingCrew().Count > 0)
                    boardable.Add(view);

            if (boardable.Count == 0)
            {
                CancelToActionSelect();
                return;
            }

            if (boardable.Count == 1)
            {
                SelectBoardingShip(boardable[0]);
            }
            else
            {
                // Step A: pick the ship first (gold pulsing border on boardables)
                foreach (var view in boardable)
                    view.SetTargetHighlight(NavalShipView.TargetHighlight.Boardable);
                _hintText.text = "Selecciona el barco a abordar  |  [ESC] Cancelar";
            }
        }

        private void SelectBoardingShip(NavalShipView view)
        {
            _boardingView = view;
            ClearTargetHighlights();
            view.SetTargetHighlight(NavalShipView.TargetHighlight.Boardable);

            // Dim everything except the boarding target (UX spec §2.4)
            if (_allyView != null) _allyView.SetDimmed(true);
            foreach (var other in _enemyViews)
                if (other != view)
                    other.SetDimmed(true);

            view.ChipOverlay.OnCrewChipClicked += HandleCrewChipClicked;
            view.ChipOverlay.Show(view.Ship, CrewChipOverlay.OverlayMode.Target, view.SpriteRT);
            _hintText.text = "Selecciona tripulante  |  Flechas + Enter  |  [ESC] Cancelar";
        }

        private void HandleCrewChipClicked(ShipCombatant ship, CrewMemberState crew)
        {
            if (_boardingView == null) return;
            _boardingView.ChipOverlay.OnCrewChipClicked -= HandleCrewChipClicked;
            _boardingView.ChipOverlay.Hide();
            _boardingView = null;
            SetAllDimmed(false);

            if (_pendingAbility != null)
                _input.SubmitAbilityOnCrew(_pendingAbility, ship, crew, _pendingEntry);
            else
                _input.SubmitBoarding(ship, crew);

            _pendingAbility = null;
            _pendingEntry = null;
            SetState(UIState.WaitingForTurn);
        }

        private void SetAllDimmed(bool dimmed)
        {
            if (_allyView != null) _allyView.SetDimmed(dimmed);
            foreach (var view in _enemyViews)
                view.SetDimmed(dimmed);
        }

        private void ClearTargetHighlights()
        {
            _allyView?.SetTargetHighlight(NavalShipView.TargetHighlight.None);
            foreach (var view in _enemyViews)
                view.SetTargetHighlight(NavalShipView.TargetHighlight.None);
        }

        // ====================================================================
        // SHIP VIEWS
        // ====================================================================

        private NavalShipView CreateShipView(ShipCombatant ship, bool isAlly, Transform column)
        {
            var go = new GameObject($"Ship_{ship.DisplayName}", typeof(RectTransform));
            go.transform.SetParent(column, false);
            var view = go.AddComponent<NavalShipView>();
            view.Build(ship, isAlly);
            view.OnClicked += HandleShipViewClicked;
            view.OnHoverEnter += HandleShipHoverEnter;
            view.OnHoverExit += HandleShipHoverExit;
            return view;
        }

        private void RebuildEnemyViews(IReadOnlyList<ICombatant> enemies)
        {
            foreach (var view in _enemyViews)
                if (view != null)
                    Destroy(view.gameObject);
            _enemyViews.Clear();
            _boardingView = null;
            _inspectedView = null;

            foreach (var enemy in enemies)
                if (enemy is ShipCombatant ship)
                    _enemyViews.Add(CreateShipView(ship, false, _enemyColumn));
        }

        private NavalShipView FindView(ShipCombatant ship)
        {
            if (_allyView != null && _allyView.Ship == ship) return _allyView;
            foreach (var view in _enemyViews)
                if (view.Ship == ship) return view;
            return null;
        }

        private void HandleShipViewClicked(NavalShipView view)
        {
            switch (_state)
            {
                case UIState.TargetShip when !view.IsAlly:
                    ConfirmShipTarget(view);
                    break;
                case UIState.TargetCrew when !view.IsAlly && _boardingView == null:
                    if (view.Ship.GetLivingCrew().Count > 0)
                        SelectBoardingShip(view);
                    break;
            }
        }

        private void HandleShipHoverEnter(NavalShipView view)
        {
            // Inspection mode (decision D3): hover outside active targeting
            if (view.IsAlly) return;
            if (_state != UIState.ActionSelect && _state != UIState.EnemyTurn &&
                _state != UIState.WaitingForTurn) return;
            if (view.Ship.IsKO || view.Ship.Crew.Count == 0) return;

            _inspectedView = view;
            view.ChipOverlay.Show(view.Ship, CrewChipOverlay.OverlayMode.Inspect, view.SpriteRT);

            int living = view.Ship.GetLivingCrew().Count;
            int total = view.Ship.Crew.Count;
            _shipTooltipText.text =
                $"{view.Ship.DisplayName}  —  HHP {view.Ship.CurrentHHP}/{view.Ship.MaxHHP}  —  " +
                $"Crew viva: {living}/{total}" + (living < total ? $"  ({total - living} caído/s)" : "");
            _shipTooltip.SetActive(true);
        }

        private void HandleShipHoverExit(NavalShipView view)
        {
            if (_inspectedView != view) return;
            HideInspection();
        }

        private void HideInspection()
        {
            if (_inspectedView == null) return;
            _inspectedView.ChipOverlay.Hide();
            _inspectedView = null;
            _shipTooltip.SetActive(false);
        }

        private void RefreshAllViews()
        {
            _allyView?.Refresh();
            foreach (var view in _enemyViews)
                view.Refresh();
        }

        // ====================================================================
        // STATS PANEL (allied ship)
        // ====================================================================

        private void RefreshStatsPanel()
        {
            var ship = _allyView?.Ship;
            if (ship == null) return;

            _statsName.text = $"{ship.DisplayName}   ELE: {ship.Element}";

            float hpRatio = ship.MaxHHP > 0 ? (float)ship.CurrentHHP / ship.MaxHHP : 0f;
            _statsHpFill.fillAmount = hpRatio;
            _statsHpFill.color = NavalUIColors.HpBarColor(hpRatio);
            _statsHpBg.color = NavalUIColors.HpBgColor(hpRatio);
            _statsHpText.text = $"HHP {ship.CurrentHHP}/{ship.MaxHHP}";

            _statsMpFill.fillAmount = ship.MaxMP > 0 ? (float)ship.CurrentMP / ship.MaxMP : 0f;
            _statsMpText.text = $"MP {ship.CurrentMP}/{ship.MaxMP}";

            // LB binary bar (decision D2): full if an LB ability is in the pool
            // and the LB hasn't been used this round
            bool lbReady = !ship.LBUsedThisRound && HasLimitBreakAbility(ship);
            _statsLbFill.fillAmount = lbReady ? 1f : 0f;

            _statsLine.text =
                $"FPW {ship.GetEffectiveShipStat(ShipStatType.FPW):F0}  " +
                $"HDF {ship.GetEffectiveShipStat(ShipStatType.HDF):F0}  " +
                $"SPD {ship.GetEffectiveShipStat(ShipStatType.SPD):F0}";
        }

        private static bool HasLimitBreakAbility(ShipCombatant ship)
        {
            foreach (var crew in ship.Crew)
            {
                if (crew.IsDead || crew.Unit == null || crew.Unit.SeaAbilities == null)
                    continue;
                foreach (var entry in crew.Unit.SeaAbilities)
                    if (entry.CanLimitBreak && entry.Ability != null)
                        return true;
            }
            return false;
        }

        // ====================================================================
        // CREW PANEL (allied)
        // ====================================================================

        private void BuildCrewPanel(ShipCombatant ally)
        {
            NavalUIFactory.ClearChildren(_crewRow);
            _crewSlots.Clear();

            foreach (var crew in ally.Crew)
            {
                var slot = new GameObject($"Crew_{crew.Role}", typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(_crewRow, false);
                slot.GetComponent<Image>().color = NavalUIColors.SlotFilled;

                var slotOutline = slot.AddComponent<Outline>();
                slotOutline.effectColor = NavalUIColors.WoodBorder;
                slotOutline.effectDistance = new Vector2(1, 1);

                // Role icon
                var iconGo = NavalUIFactory.CreateZone(slot.transform, "Icon", 0.25f, 0.45f, 0.75f, 0.95f);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.raycastTarget = false;
                var sprite = Resources.Load<Sprite>($"Sprites/UI/Naval/{RoleIconName(crew.Role)}");
                if (sprite != null)
                {
                    iconImg.sprite = sprite;
                    iconImg.preserveAspect = true;
                }
                else
                {
                    iconImg.color = NavalUIColors.RoleColor(crew.Role);
                }

                // 2-letter label
                NavalUIFactory.CreateStretchedText(
                    NavalUIFactory.CreateZone(slot.transform, "Label", 0f, 0.22f, 1f, 0.45f).transform,
                    "Text", NavalUIColors.RoleLabel(crew.Role), 11,
                    NavalUIColors.Cream, TextAnchor.MiddleCenter);

                // HP minibar
                var barZone = NavalUIFactory.CreateZone(slot.transform, "HpZone", 0.1f, 0.05f, 0.9f, 0.18f);
                var bar = NavalUIFactory.CreateBar(barZone.transform, "Bar",
                    new Color(0.1f, 0.1f, 0.1f, 0.78f), NavalUIColors.HpHigh);
                Stretch(bar);

                _crewSlots[crew] = slot;
            }
            RefreshCrewPanel();
        }

        private static string RoleIconName(NavalRole role)
        {
            return role switch
            {
                NavalRole.Capitan => "ui_role_capitan_32",
                NavalRole.Intendente => "ui_role_intendente_32",
                NavalRole.Artillero => "ui_role_artillero_32",
                NavalRole.Navegante => "ui_role_navegante_32",
                NavalRole.Carpintero => "ui_role_carpintero_32",
                NavalRole.Cirujano => "ui_role_cirujano_32",
                NavalRole.Contramaestre => "ui_role_contramaestre_32",
                _ => ""
            };
        }

        private void RefreshCrewPanel()
        {
            foreach (var kvp in _crewSlots)
            {
                var crew = kvp.Key;
                var slot = kvp.Value;

                var fill = slot.transform.Find("HpZone/Bar/Fill")?.GetComponent<Image>();
                if (fill != null)
                {
                    float ratio = crew.MaxHP > 0 ? (float)crew.CurrentHP / crew.MaxHP : 0f;
                    fill.fillAmount = ratio;
                    fill.color = NavalUIColors.HpBarColor(ratio);
                }

                if (crew.IsDead && slot.transform.Find("DeadX") == null)
                {
                    slot.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.86f);
                    var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
                    if (icon != null)
                        icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 0.35f);

                    NavalUIFactory.CreateStretchedText(slot.transform, "DeadX",
                        "X", 26, new Color(1f, 1f, 1f, 0.86f), TextAnchor.MiddleCenter);

                    var label = slot.transform.Find("Label/Text")?.GetComponent<Text>();
                    if (label != null) label.text = "CAÍDO";
                }
            }
        }

        private void RefreshSynergies()
        {
            var ship = _allyView?.Ship;
            if (ship == null) return;

            if (!ship.CaptainAlive)
            {
                _synergyLabel.text = "Sinergias: INACTIVAS (capitán caído)";
                _synergyLabel.color = NavalUIColors.DisabledLabel;
                return;
            }

            var synergies = ship.CrewSynergies;
            if (synergies == null || synergies.Count == 0)
            {
                _synergyLabel.text = "Sinergias: ninguna";
                _synergyLabel.color = NavalUIColors.Cream;
                return;
            }

            var names = new List<string>();
            foreach (var synergy in synergies)
                names.Add(synergy.Trait != null ? synergy.Trait.DisplayName : "?");
            _synergyLabel.text = "Sinergias: " + string.Join("  ", names);
            _synergyLabel.color = NavalUIColors.Gold;
        }

        // ====================================================================
        // INITIATIVE BAR
        // ====================================================================

        private void RefreshInitiativeBar(ICombatant active)
        {
            if (Runner?.Manager?.Bar == null) return;
            NavalUIFactory.ClearChildren(_ibIcons);

            foreach (var entry in Runner.Manager.Bar.Entries)
            {
                if (entry.Combatant.IsKO) continue;
                if (entry.Combatant is not ShipCombatant ship) continue;

                var iconGo = new GameObject("IbIcon",
                    typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconGo.transform.SetParent(_ibIcons, false);
                var le = iconGo.GetComponent<LayoutElement>();
                le.preferredWidth = 44;
                le.preferredHeight = 44;

                bool isCreature = ship.Crew.Count == 0 && entry.Team == CombatTeam.Enemy;
                string iconName = entry.Team == CombatTeam.Ally ? "ui_ib_ship_allied_44"
                    : isCreature ? "ui_ib_creature_44" : "ui_ib_ship_enemy_44";

                var img = iconGo.GetComponent<Image>();
                var sprite = Resources.Load<Sprite>($"Sprites/UI/Naval/{iconName}");
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.preserveAspect = true;
                }
                else
                {
                    img.color = entry.Team == CombatTeam.Ally
                        ? new Color(0.12f, 0.53f, 0.90f)
                        : isCreature ? NavalUIColors.VoodooViolet : new Color(0.75f, 0.21f, 0.05f);
                }

                if (entry.IsLimitBreak)
                {
                    NavalUIFactory.CreateStretchedText(iconGo.transform, "Lb", "LB",
                        11, NavalUIColors.GoldBright, TextAnchor.LowerRight);
                }

                if (entry.Combatant == active)
                {
                    var outline = iconGo.AddComponent<Outline>();
                    outline.effectColor = NavalUIColors.Gold;
                    outline.effectDistance = new Vector2(2, 2);
                }
            }
        }

        // ====================================================================
        // BATTLE LOG
        // ====================================================================

        private void AddLog(string message, Color color)
        {
            var go = new GameObject("Log", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(_logContent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 20;

            var txt = go.GetComponent<Text>();
            txt.text = message;
            txt.fontSize = 14;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.font = NavalUIFactory.DefaultFont;

            while (_logContent.childCount > 50)
                Destroy(_logContent.GetChild(0).gameObject);

            Canvas.ForceUpdateCanvases();
            if (_logScroll != null)
                _logScroll.verticalNormalizedPosition = 0f;
        }
    }
}
