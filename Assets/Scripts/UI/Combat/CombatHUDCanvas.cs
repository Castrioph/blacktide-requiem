using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.UI.Combat
{
    /// <summary>
    /// UGUI Canvas code-behind for the Combat HUD. Subscribes to GameEvents
    /// and updates all UI elements. Manages action selection and target picking.
    /// See Combat UI GDD and ADR-003 §5.
    /// </summary>
    public class CombatHUDCanvas : MonoBehaviour
    {
        // --- Scene references (wired in Inspector) ---
        [Header("Initiative Bar")]
        [SerializeField] private Transform _iconsContainer;

        [Header("Round Info")]
        [SerializeField] private Text _roundLabel;
        [SerializeField] private Text _waveLabel;

        [Header("Battlefield")]
        [SerializeField] private Transform _allyColumn;
        [SerializeField] private Transform _enemyColumn;

        [Header("Unit Info")]
        [SerializeField] private Text _unitName;
        [SerializeField] private Text _unitHpText;
        [SerializeField] private Text _unitMpText;
        [SerializeField] private Text _unitAtkText;

        [Header("Action Panel")]
        [SerializeField] private GameObject _mainActions;
        [SerializeField] private Button _btnAttack;
        [SerializeField] private Button _btnAbilities;
        [SerializeField] private Button _btnGuard;
        [SerializeField] private Button _btnPass;

        [Header("Ability Menu")]
        [SerializeField] private GameObject _abilityMenu;
        [SerializeField] private Transform _abilityListContent;
        [SerializeField] private Button _btnBack;

        [Header("Target Hint")]
        [SerializeField] private GameObject _targetHintObj;
        [SerializeField] private Text _targetHintText;

        [Header("Battle Log")]
        [SerializeField] private Transform _battleLogContent;
        [SerializeField] private ScrollRect _battleLogScroll;

        [Header("Result Overlay")]
        [SerializeField] private GameObject _resultOverlay;
        [SerializeField] private Text _resultText;
        [SerializeField] private Text _resultDetails;

        // --- State ---
        private PlayerCombatInput _playerInput;
        private enum UIState { WaitingForTurn, ActionSelect, TargetSelect, AbilitySelect, EnemyTurn, BattleOver }
        private UIState _state = UIState.WaitingForTurn;
        private AbilityData _selectedAbility;
        private bool _isAttackTargeting;

        // --- Combatant card tracking ---
        private readonly Dictionary<CombatantState, GameObject> _combatantCards = new();

        // --- Prefab-like colors ---
        private static readonly Color ALLY_CARD_COLOR = new(0.15f, 0.22f, 0.35f, 0.9f);
        private static readonly Color ENEMY_CARD_COLOR = new(0.35f, 0.15f, 0.12f, 0.9f);
        private static readonly Color ACTIVE_BORDER_COLOR = new(1f, 0.86f, 0.31f, 1f);
        private static readonly Color TARGETABLE_COLOR = new(0.39f, 0.78f, 0.39f, 1f);
        private static readonly Color DEAD_COLOR = new(0.3f, 0.3f, 0.3f, 0.4f);
        private static readonly Color HP_GREEN = new(0.2f, 0.7f, 0.27f, 1f);
        private static readonly Color HP_YELLOW = new(0.86f, 0.7f, 0.12f, 1f);
        private static readonly Color HP_RED = new(0.78f, 0.2f, 0.16f, 1f);
        private static readonly Color MP_BLUE = new(0.24f, 0.39f, 0.78f, 1f);
        private static readonly Color LOG_DEFAULT = new(0.63f, 0.59f, 0.51f, 1f);
        private static readonly Color LOG_DAMAGE = new(0.86f, 0.31f, 0.24f, 1f);
        private static readonly Color LOG_HEAL = new(0.31f, 0.78f, 0.39f, 1f);
        private static readonly Color LOG_STATUS = new(0.78f, 0.7f, 0.31f, 1f);
        private static readonly Color LOG_SYSTEM = new(0.47f, 0.7f, 0.86f, 1f);

        /// <summary>Sets the PlayerCombatInput reference for action submission.</summary>
        public void Bind(PlayerCombatInput playerInput)
        {
            if (_playerInput != null)
                _playerInput.OnInputRequested -= HandleInputRequested;

            _playerInput = playerInput;

            if (_playerInput != null)
                _playerInput.OnInputRequested += HandleInputRequested;
        }

        private void OnEnable()
        {
            SetupButtons();
            SubscribeEvents();
            SetState(UIState.WaitingForTurn);
            FixScrollContentAnchors();
        }

        /// <summary>
        /// Ensures ScrollRect Content areas stretch horizontally and grow
        /// vertically. Fixes anchors at runtime in case scene data is stale.
        /// </summary>
        private void FixScrollContentAnchors()
        {
            // Fix AbilityMenu to stretch-fill ActionPanel
            StretchToFillParent(_abilityMenu);

            // Fix AbilityList ScrollRect to fill AbilityMenu, leaving 40px for BtnBack at bottom
            var abilityScroll = _abilityListContent?.GetComponentInParent<ScrollRect>();
            if (abilityScroll != null)
            {
                var scrollRt = abilityScroll.GetComponent<RectTransform>();
                scrollRt.anchorMin = Vector2.zero;
                scrollRt.anchorMax = Vector2.one;
                scrollRt.offsetMin = new Vector2(0, 44); // leave room for BtnBack
                scrollRt.offsetMax = Vector2.zero;
            }

            // Fix BtnBack anchored at bottom of AbilityMenu
            if (_btnBack != null)
            {
                var backRt = _btnBack.GetComponent<RectTransform>();
                backRt.anchorMin = new Vector2(0, 0);
                backRt.anchorMax = new Vector2(1, 0);
                backRt.pivot = new Vector2(0.5f, 0);
                backRt.offsetMin = new Vector2(4, 4);
                backRt.offsetMax = new Vector2(-4, 40);
            }

            // Fix Viewport rects (must stretch to fill ScrollRect)
            FixViewportRect(_battleLogScroll);
            FixViewportRect(abilityScroll);

            // Fix Content rects
            FixContentRect(_battleLogContent);
            FixContentRect(_abilityListContent);
        }

        private static void StretchToFillParent(GameObject go)
        {
            if (go == null) return;
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void FixViewportRect(ScrollRect scroll)
        {
            if (scroll == null || scroll.viewport == null) return;
            var rt = scroll.viewport.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void FixContentRect(Transform content)
        {
            if (content == null) return;
            var rt = content.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var csf = content.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Ensure VLG so children stretch horizontally and stack vertically
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.spacing = 4;
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            if (_playerInput != null)
                _playerInput.OnInputRequested -= HandleInputRequested;
        }

        // ====================================================================
        // SETUP
        // ====================================================================

        private void SetupButtons()
        {
            _btnAttack.onClick.AddListener(OnAttackClicked);
            _btnAbilities.onClick.AddListener(OnAbilitiesClicked);
            _btnGuard.onClick.AddListener(OnGuardClicked);
            _btnPass.onClick.AddListener(OnPassClicked);
            _btnBack.onClick.AddListener(OnAbilityBackClicked);
        }

        private void SubscribeEvents()
        {
            GameEvents.OnBattleStart += HandleBattleStart;
            GameEvents.OnRoundStart += HandleRoundStart;
            GameEvents.OnTurnStart += HandleTurnStart;
            GameEvents.OnTurnEnd += HandleTurnEnd;
            GameEvents.OnActionChosen += HandleActionChosen;
            GameEvents.OnDamageDealt += HandleDamageDealt;
            GameEvents.OnHealApplied += HandleHealApplied;
            GameEvents.OnTurnSkipped += HandleTurnSkipped;
            GameEvents.OnUnitDied += HandleUnitDied;
            GameEvents.OnGuardActivated += HandleGuardActivated;
            GameEvents.OnWaveComplete += HandleWaveComplete;
            GameEvents.OnWaveStart += HandleWaveStart;
            GameEvents.OnBattleEnd += HandleBattleEnd;
            GameEvents.OnStatusApplied += HandleStatusApplied;
        }

        private void UnsubscribeEvents()
        {
            GameEvents.OnBattleStart -= HandleBattleStart;
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnTurnStart -= HandleTurnStart;
            GameEvents.OnTurnEnd -= HandleTurnEnd;
            GameEvents.OnActionChosen -= HandleActionChosen;
            GameEvents.OnDamageDealt -= HandleDamageDealt;
            GameEvents.OnHealApplied -= HandleHealApplied;
            GameEvents.OnTurnSkipped -= HandleTurnSkipped;
            GameEvents.OnUnitDied -= HandleUnitDied;
            GameEvents.OnGuardActivated -= HandleGuardActivated;
            GameEvents.OnWaveComplete -= HandleWaveComplete;
            GameEvents.OnWaveStart -= HandleWaveStart;
            GameEvents.OnBattleEnd -= HandleBattleEnd;
            GameEvents.OnStatusApplied -= HandleStatusApplied;
        }

        // ====================================================================
        // STATE MANAGEMENT
        // ====================================================================

        private void SetState(UIState state)
        {
            _state = state;
            _selectedAbility = null;
            _isAttackTargeting = false;

            _mainActions.SetActive(state == UIState.ActionSelect);
            _abilityMenu.SetActive(state == UIState.AbilitySelect);
            _targetHintObj.SetActive(state == UIState.TargetSelect);

            if (state != UIState.TargetSelect)
                ClearTargetHighlights();
        }

        // ====================================================================
        // BUTTON HANDLERS
        // ====================================================================

        private void OnAttackClicked()
        {
            if (_state != UIState.ActionSelect) return;
            SetState(UIState.TargetSelect);
            _isAttackTargeting = true;
            _targetHintText.text = "Select an enemy to attack";
            HighlightTargets(isEnemyTarget: true);
        }

        private void OnAbilitiesClicked()
        {
            if (_state != UIState.ActionSelect) return;
            PopulateAbilityMenu();
            SetState(UIState.AbilitySelect);
        }

        private void OnGuardClicked()
        {
            if (_state != UIState.ActionSelect) return;
            _playerInput.SubmitGuard();
            SetState(UIState.WaitingForTurn);
            AddLogEntry("Guard activated", LOG_SYSTEM);
        }

        private void OnPassClicked()
        {
            if (_state != UIState.ActionSelect) return;
            _playerInput.SubmitPass();
            SetState(UIState.WaitingForTurn);
            AddLogEntry("Turn passed", LOG_SYSTEM);
        }

        private void OnAbilityBackClicked()
        {
            SetState(UIState.ActionSelect);
        }

        private void OnAbilitySelected(AbilityData ability)
        {
            if (ability.TargetType == TargetType.Self ||
                ability.TargetType == TargetType.AoeEnemy ||
                ability.TargetType == TargetType.AllyAoe)
            {
                _playerInput.SubmitAbility(ability, null);
                SetState(UIState.WaitingForTurn);
                return;
            }

            bool isEnemyTarget = ability.TargetType == TargetType.SingleEnemy;
            SetState(UIState.TargetSelect);
            _selectedAbility = ability;
            _targetHintText.text = isEnemyTarget ? "Select an enemy target" : "Select an ally target";
            HighlightTargets(isEnemyTarget);
        }

        private void OnCombatantCardClicked(CombatantState combatant)
        {
            if (_state != UIState.TargetSelect) return;
            if (combatant.IsKO) return;

            if (_isAttackTargeting)
            {
                _playerInput.SubmitAttack(combatant);
                SetState(UIState.WaitingForTurn);
            }
            else if (_selectedAbility != null)
            {
                _playerInput.SubmitAbility(_selectedAbility, combatant);
                SetState(UIState.WaitingForTurn);
            }
        }

        // ====================================================================
        // EVENT HANDLERS
        // ====================================================================

        private void HandleBattleStart(BattleStartEvent e)
        {
            AddLogEntry($"Battle started! {e.AllyCount} allies vs {e.EnemyCount} enemies ({e.TotalWaves} waves)", LOG_SYSTEM);
            _resultOverlay.SetActive(false);
        }

        private void HandleRoundStart(int round)
        {
            _roundLabel.text = $"Round {round}";
            AddLogEntry($"--- Round {round} ---", LOG_SYSTEM);
            RefreshInitiativeBar(null);
        }

        private void HandleTurnStart(CombatantState combatant)
        {
            UpdateUnitInfo(combatant);
            UpdateAllCombatantCards();
            HighlightActiveUnit(combatant);
            RefreshInitiativeBar(combatant);
            SetState(UIState.EnemyTurn);
        }

        private void RefreshInitiativeBar(CombatantState activeUnit)
        {
            var runner = FindAnyObjectByType<Runtime.Combat.CombatRunner>();
            if (runner?.Manager?.Bar == null) return;
            var entries = new List<InitiativeEntry>(runner.Manager.Bar.Entries);
            UpdateInitiativeBar(entries, activeUnit);
        }

        private void HandleInputRequested()
        {
            SetState(UIState.ActionSelect);
        }

        private void HandleTurnEnd(CombatantState combatant)
        {
            UpdateAllCombatantCards();
        }

        private void HandleActionChosen(CombatAction action)
        {
            string actorName = _playerInput?.CurrentContext.Actor?.Template?.DisplayName ?? "???";
            string msg = action.Type switch
            {
                ActionType.Attack => $"{actorName} attacks {action.Target?.Template?.DisplayName}!",
                ActionType.Ability => $"{actorName} uses {action.ActionName}!",
                ActionType.Guard => $"{actorName} guards!",
                ActionType.Pass => $"{actorName} passes.",
                _ => $"{actorName} acts."
            };
            AddLogEntry(msg, LOG_DEFAULT);
        }

        private void HandleDamageDealt(DamageEvent e)
        {
            string targetName = e.Target?.Template?.DisplayName ?? "???";

            if (e.Result.IsMiss)
            {
                AddLogEntry($"MISS on {targetName}!", LOG_STATUS);
                return;
            }

            string source = e.DamageSource switch
            {
                DamageSource.Bleed => "Bleed",
                DamageSource.Burn => "Burn",
                DamageSource.Poison => "Poison",
                _ => null
            };

            string guardText = e.IsGuarded ? " (guarded)" : "";
            string msg = source != null
                ? $"{targetName} takes {e.ActualDamage} {source} damage"
                : $"{targetName} takes {e.ActualDamage} damage{guardText}";

            AddLogEntry(msg, LOG_DAMAGE);
            UpdateAllCombatantCards();
        }

        private void HandleHealApplied(HealEvent e)
        {
            string targetName = e.Target?.Template?.DisplayName ?? "???";
            AddLogEntry($"{targetName} healed for {e.Amount} HP", LOG_HEAL);
            UpdateAllCombatantCards();
        }

        private void HandleTurnSkipped(TurnSkippedEvent e)
        {
            string name = e.Combatant?.Template?.DisplayName ?? "???";
            string reason = e.Reason == StatusEffect.Aturdimiento ? "Stunned" : "Asleep";
            AddLogEntry($"{name} is {reason}! Turn skipped.", LOG_STATUS);
        }

        private void HandleUnitDied(CombatantState combatant)
        {
            string name = combatant?.Template?.DisplayName ?? "???";
            AddLogEntry($"{name} has been defeated!", LOG_DAMAGE);
            UpdateAllCombatantCards();
        }

        private void HandleGuardActivated(CombatantState combatant)
        {
            UpdateAllCombatantCards();
        }

        private void HandleWaveComplete(int wave)
        {
            int totalWaves = FindTotalWaves();
            AddLogEntry($"Wave {wave + 1}/{totalWaves} cleared!", LOG_SYSTEM);
        }

        private void HandleWaveStart(int wave)
        {
            int totalWaves = FindTotalWaves();
            _waveLabel.text = $"Wave {wave + 1}/{totalWaves}";
            AddLogEntry($"Wave {wave + 1}/{totalWaves} begins!", LOG_SYSTEM);
            RebuildEnemyCards();
        }

        private int FindTotalWaves()
        {
            var runner = FindAnyObjectByType<Runtime.Combat.CombatRunner>();
            return runner?.Manager?.TotalWaves ?? 1;
        }

        private void HandleBattleEnd(BattleEndEvent e)
        {
            SetState(UIState.BattleOver);
            _resultOverlay.SetActive(true);

            if (e.Result == BattleResult.Victory)
            {
                _resultText.text = "VICTORY";
                _resultText.color = new Color(0.3f, 0.9f, 0.4f);
                _resultDetails.text = $"Battle won in {e.RoundsElapsed} rounds";
            }
            else
            {
                _resultText.text = "DEFEAT";
                _resultText.color = new Color(0.9f, 0.3f, 0.25f);
                _resultDetails.text = $"All allies defeated after {e.RoundsElapsed} rounds";
            }

            AddLogEntry($"=== {e.Result} ===", LOG_SYSTEM);
        }

        private void HandleStatusApplied(StatusAppliedEvent e)
        {
            string targetName = e.Target?.Template?.DisplayName ?? "???";
            AddLogEntry($"{targetName} afflicted with {e.Status.Effect}!", LOG_STATUS);
        }

        // ====================================================================
        // COMBATANT CARDS
        // ====================================================================

        /// <summary>
        /// Builds initial combatant cards. Called by DemoBattleSetup after config.
        /// </summary>
        public void BuildCombatantCards(IReadOnlyList<CombatantState> allies,
            IReadOnlyList<CombatantState> enemies, int totalWaves)
        {
            _waveLabel.text = $"Wave 1/{totalWaves}";
            _combatantCards.Clear();
            ClearChildren(_allyColumn);
            ClearChildren(_enemyColumn);

            foreach (var ally in allies)
                CreateCombatantCard(ally, true, _allyColumn);

            foreach (var enemy in enemies)
                CreateCombatantCard(enemy, false, _enemyColumn);
        }

        private void RebuildEnemyCards()
        {
            var runner = GetComponentInParent<Runtime.Combat.CombatRunner>();
            if (runner == null) runner = FindAnyObjectByType<Runtime.Combat.CombatRunner>();
            if (runner?.Manager == null) return;

            // Remove old enemy entries from tracking dict
            var toRemove = new List<CombatantState>();
            foreach (var kvp in _combatantCards)
            {
                if (kvp.Value != null && kvp.Value.transform.parent == _enemyColumn)
                    toRemove.Add(kvp.Key);
            }
            foreach (var key in toRemove)
                _combatantCards.Remove(key);

            ClearChildren(_enemyColumn);
            foreach (var enemy in runner.Manager.Enemies)
                CreateCombatantCard(enemy, false, _enemyColumn);
        }

        private void CreateCombatantCard(CombatantState combatant, bool isAlly, Transform parent)
        {
            var card = new GameObject(combatant.Template.DisplayName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
            card.transform.SetParent(parent, false);

            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 80);

            var img = card.GetComponent<Image>();
            img.color = isAlly ? ALLY_CARD_COLOR : ENEMY_CARD_COLOR;

            var vlg = card.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 4, 4);
            vlg.spacing = 2;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = false;

            // Name
            var nameGo = CreateText(card.transform, combatant.Template.DisplayName, 14,
                new Color(0.86f, 0.78f, 0.63f), TextAnchor.MiddleLeft, 18);

            // HP bar
            var hpBar = CreateBar(card.transform, "HPBar", HP_GREEN, 12);
            var hpFill = hpBar.transform.Find("Fill").GetComponent<Image>();
            hpFill.fillAmount = 1f;

            // HP text
            var hpText = CreateText(card.transform, $"{combatant.CurrentHP}/{combatant.MaxHP}", 10,
                new Color(0.7f, 0.67f, 0.59f), TextAnchor.MiddleRight, 14);

            // MP bar (allies only)
            GameObject mpBar = null;
            if (isAlly && combatant.MaxMP > 0)
            {
                mpBar = CreateBar(card.transform, "MPBar", MP_BLUE, 6);
                mpBar.transform.Find("Fill").GetComponent<Image>().fillAmount = 1f;
            }

            // Click handler
            card.GetComponent<Button>().onClick.AddListener(() => OnCombatantCardClicked(combatant));

            // Store reference
            _combatantCards[combatant] = card;
        }

        private void UpdateAllCombatantCards()
        {
            foreach (var kvp in _combatantCards)
            {
                var combatant = kvp.Key;
                var card = kvp.Value;

                // HP bar
                float hpPercent = combatant.MaxHP > 0
                    ? (float)combatant.CurrentHP / combatant.MaxHP
                    : 0f;
                var hpBar = card.transform.Find("HPBar");
                if (hpBar != null)
                {
                    var fill = hpBar.Find("Fill").GetComponent<Image>();
                    fill.fillAmount = hpPercent;
                    fill.color = hpPercent <= 0.25f ? HP_RED : hpPercent <= 0.5f ? HP_YELLOW : HP_GREEN;
                }

                // HP text (child index 2 after name and bar)
                var texts = card.GetComponentsInChildren<Text>();
                if (texts.Length >= 2)
                    texts[1].text = $"{combatant.CurrentHP}/{combatant.MaxHP}";

                // MP bar
                var mpBar = card.transform.Find("MPBar");
                if (mpBar != null && combatant.MaxMP > 0)
                {
                    mpBar.Find("Fill").GetComponent<Image>().fillAmount =
                        (float)combatant.CurrentMP / combatant.MaxMP;
                }

                // Dead state
                var cardImg = card.GetComponent<Image>();
                if (combatant.IsKO)
                    cardImg.color = DEAD_COLOR;
            }
        }

        private void HighlightActiveUnit(CombatantState combatant)
        {
            foreach (var kvp in _combatantCards)
            {
                var outline = kvp.Value.GetComponent<Outline>();
                if (outline != null) Destroy(outline);
            }

            if (combatant != null && _combatantCards.TryGetValue(combatant, out var card))
            {
                var outline = card.AddComponent<Outline>();
                outline.effectColor = ACTIVE_BORDER_COLOR;
                outline.effectDistance = new Vector2(3, 3);
            }
        }

        private void HighlightTargets(bool isEnemyTarget)
        {
            var runner = FindAnyObjectByType<Runtime.Combat.CombatRunner>();
            if (runner?.Manager == null) return;

            var targets = isEnemyTarget ? runner.Manager.Enemies : runner.Manager.Allies;
            foreach (var target in targets)
            {
                if (target.IsKO) continue;
                if (_combatantCards.TryGetValue(target, out var card))
                {
                    var outline = card.GetComponent<Outline>() ?? card.AddComponent<Outline>();
                    outline.effectColor = TARGETABLE_COLOR;
                    outline.effectDistance = new Vector2(2, 2);
                }
            }
        }

        private void ClearTargetHighlights()
        {
            foreach (var kvp in _combatantCards)
            {
                var outline = kvp.Value.GetComponent<Outline>();
                if (outline != null) Destroy(outline);
            }
        }

        // ====================================================================
        // INITIATIVE BAR
        // ====================================================================

        /// <summary>Updates initiative bar icons from current turn order.</summary>
        public void UpdateInitiativeBar(List<InitiativeEntry> entries, CombatantState activeUnit)
        {
            ClearChildren(_iconsContainer);
            foreach (var entry in entries)
            {
                if (entry.Combatant.IsKO) continue;

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(_iconsContainer, false);

                var rt = iconGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(36, 36);

                var img = iconGo.GetComponent<Image>();
                img.color = entry.Team == CombatTeam.Ally
                    ? new Color(0.16f, 0.31f, 0.55f) : new Color(0.51f, 0.16f, 0.14f);

                // Text as child so Image and Text don't conflict (both are Graphic)
                var label = CreateText(iconGo.transform,
                    entry.Combatant.Template.DisplayName.Substring(0, 1).ToUpper(),
                    14, Color.white, TextAnchor.MiddleCenter, 36);
                var labelRt = label.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;

                if (entry.Combatant == activeUnit)
                {
                    var outline = iconGo.AddComponent<Outline>();
                    outline.effectColor = ACTIVE_BORDER_COLOR;
                    outline.effectDistance = new Vector2(2, 2);
                }
            }
        }

        // ====================================================================
        // UNIT INFO PANEL
        // ====================================================================

        private void UpdateUnitInfo(CombatantState combatant)
        {
            if (combatant == null) return;
            _unitName.text = combatant.Template.DisplayName;
            _unitHpText.text = $"HP: {combatant.CurrentHP}/{combatant.MaxHP}";
            _unitMpText.text = combatant.MaxMP > 0
                ? $"MP: {combatant.CurrentMP}/{combatant.MaxMP}" : "";
            _unitAtkText.text = $"ATK:{combatant.GetEffectiveStat(StatType.ATK):F0}  DEF:{combatant.GetEffectiveStat(StatType.DEF):F0}";
        }

        // ====================================================================
        // ABILITY MENU
        // ====================================================================

        private void PopulateAbilityMenu()
        {
            ClearChildren(_abilityListContent);
            if (_playerInput == null) return;

            var abilities = _playerInput.GetAvailableAbilities();
            if (abilities.Count == 0)
            {
                CreateText(_abilityListContent, "No abilities available", 14, LOG_DEFAULT, TextAnchor.MiddleCenter, 36);
                return;
            }

            foreach (var ability in abilities)
            {
                string mpText = ability.MPCost > 0 ? $" [{ability.MPCost} MP]" : "";
                var btnGo = new GameObject(ability.Id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                btnGo.transform.SetParent(_abilityListContent, false);

                var le = btnGo.GetComponent<LayoutElement>();
                le.preferredHeight = 40;
                le.flexibleWidth = 1;

                var img = btnGo.GetComponent<Image>();
                img.color = new Color(0.2f, 0.16f, 0.24f);

                var label = CreateText(btnGo.transform, $"{ability.DisplayName}{mpText}", 14,
                    new Color(0.86f, 0.82f, 0.7f), TextAnchor.MiddleLeft, 36);
                // Stretch label to fill button
                var labelRt = label.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = new Vector2(8, 0);
                labelRt.offsetMax = new Vector2(-4, 0);

                var actor = _playerInput.CurrentContext.Actor;
                bool canAfford = actor == null || actor.CurrentMP >= ability.MPCost;

                var btn = btnGo.GetComponent<Button>();
                btn.interactable = canAfford;
                var captured = ability;
                btn.onClick.AddListener(() => OnAbilitySelected(captured));
            }
        }

        // ====================================================================
        // BATTLE LOG
        // ====================================================================

        private void AddLogEntry(string message, Color color)
        {
            var go = new GameObject("Log", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_battleLogContent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(0, 20);

            var txt = go.GetComponent<Text>();
            txt.text = message;
            txt.fontSize = 14;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Limit log entries to avoid unbounded growth
            while (_battleLogContent.childCount > 50)
                Destroy(_battleLogContent.GetChild(0).gameObject);

            // Auto-scroll to bottom
            Canvas.ForceUpdateCanvases();
            if (_battleLogScroll != null)
                _battleLogScroll.verticalNormalizedPosition = 0f;
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private static GameObject CreateText(Transform parent, string content, int fontSize,
            Color color, TextAnchor alignment, float height)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, height);
            var txt = go.GetComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return go;
        }

        private static GameObject CreateBar(Transform parent, string name, Color fillColor, float height)
        {
            var bar = new GameObject(name, typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.sizeDelta = new Vector2(0, height);
            bar.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.1f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;

            return bar;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
