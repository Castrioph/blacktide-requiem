using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.UI.Combat
{
    /// <summary>
    /// Code-behind for the Combat HUD. Subscribes to GameEvents and updates
    /// all UI elements. Manages action selection flow and target picking.
    /// See Combat UI GDD and ADR-003 §5.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CombatHUD : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _root;
        private PlayerCombatInput _playerInput;

        // --- Cached UI elements ---
        private VisualElement _initiativeIcons;
        private Label _roundLabel;
        private Label _waveLabel;
        private VisualElement _allyColumn;
        private VisualElement _enemyColumn;
        private Label _unitName;
        private Label _unitHpText;
        private Label _unitMpText;
        private Label _unitAtkText;
        private VisualElement _mainActions;
        private VisualElement _abilityMenu;
        private ScrollView _abilityList;
        private Label _targetHint;
        private ScrollView _battleLog;
        private VisualElement _resultOverlay;
        private Label _resultText;
        private Label _resultDetails;

        // --- State ---
        private enum UIState { WaitingForTurn, ActionSelect, TargetSelect, AbilitySelect, EnemyTurn, BattleOver }
        private UIState _state = UIState.WaitingForTurn;
        private AbilityData _selectedAbility;
        private bool _isAttackTargeting;

        // --- Combatant card tracking ---
        private readonly Dictionary<CombatantState, VisualElement> _combatantCards = new();

        /// <summary>Sets the PlayerCombatInput reference for action submission.</summary>
        public void Bind(PlayerCombatInput playerInput)
        {
            // Unsubscribe from previous input if any
            if (_playerInput != null)
                _playerInput.OnInputRequested -= HandleInputRequested;

            _playerInput = playerInput;

            if (_playerInput != null)
                _playerInput.OnInputRequested += HandleInputRequested;
        }

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            _root = _doc.rootVisualElement;
            CacheElements();
            SetupButtons();
            SubscribeEvents();
            SetState(UIState.WaitingForTurn);
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        // ====================================================================
        // SETUP
        // ====================================================================

        private void CacheElements()
        {
            _initiativeIcons = _root.Q<VisualElement>("initiative-icons");
            _roundLabel = _root.Q<Label>("round-label");
            _waveLabel = _root.Q<Label>("wave-label");
            _allyColumn = _root.Q<VisualElement>("ally-column");
            _enemyColumn = _root.Q<VisualElement>("enemy-column");
            _unitName = _root.Q<Label>("unit-name");
            _unitHpText = _root.Q<Label>("unit-hp-text");
            _unitMpText = _root.Q<Label>("unit-mp-text");
            _unitAtkText = _root.Q<Label>("unit-atk-text");
            _mainActions = _root.Q<VisualElement>("main-actions");
            _abilityMenu = _root.Q<VisualElement>("ability-menu");
            _abilityList = _root.Q<ScrollView>("ability-list");
            _targetHint = _root.Q<Label>("target-hint");
            _battleLog = _root.Q<ScrollView>("battle-log");
            _resultOverlay = _root.Q<VisualElement>("result-overlay");
            _resultText = _root.Q<Label>("result-text");
            _resultDetails = _root.Q<Label>("result-details");
        }

        private void SetupButtons()
        {
            _root.Q<Button>("btn-attack").clicked += OnAttackClicked;
            _root.Q<Button>("btn-abilities").clicked += OnAbilitiesClicked;
            _root.Q<Button>("btn-guard").clicked += OnGuardClicked;
            _root.Q<Button>("btn-pass").clicked += OnPassClicked;
            _root.Q<Button>("btn-ability-back").clicked += OnAbilityBackClicked;
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

            if (_playerInput != null)
                _playerInput.OnInputRequested += HandleInputRequested;
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

            if (_playerInput != null)
                _playerInput.OnInputRequested -= HandleInputRequested;
        }

        // ====================================================================
        // STATE MANAGEMENT
        // ====================================================================

        private void SetState(UIState state)
        {
            _state = state;
            _selectedAbility = null;
            _isAttackTargeting = false;

            // Show/hide panels
            bool showActions = state == UIState.ActionSelect;
            bool showAbilities = state == UIState.AbilitySelect;
            bool showTargetHint = state == UIState.TargetSelect;

            _mainActions.EnableInClassList("hidden", !showActions);
            _abilityMenu.EnableInClassList("hidden", !showAbilities);
            _targetHint.EnableInClassList("hidden", !showTargetHint);

            // Clear targetable highlights
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
            _targetHint.text = "Select an enemy to attack";
            _targetHint.RemoveFromClassList("hidden");
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
            AddLogEntry("Guard activated", "log-entry-system");
        }

        private void OnPassClicked()
        {
            if (_state != UIState.ActionSelect) return;
            _playerInput.SubmitPass();
            SetState(UIState.WaitingForTurn);
            AddLogEntry("Turn passed", "log-entry-system");
        }

        private void OnAbilityBackClicked()
        {
            SetState(UIState.ActionSelect);
        }

        private void OnAbilitySelected(AbilityData ability)
        {
            // Self/AoE targets resolve immediately
            if (ability.TargetType == TargetType.Self ||
                ability.TargetType == TargetType.AoeEnemy ||
                ability.TargetType == TargetType.AllyAoe)
            {
                _playerInput.SubmitAbility(ability, null);
                SetState(UIState.WaitingForTurn);
                return;
            }

            // Single target — enter targeting mode
            bool isEnemyTarget = ability.TargetType == TargetType.SingleEnemy;
            SetState(UIState.TargetSelect);
            _selectedAbility = ability;
            _targetHint.text = isEnemyTarget ? "Select an enemy target" : "Select an ally target";
            _targetHint.RemoveFromClassList("hidden");
            HighlightTargets(isEnemyTarget);
        }

        private void OnCombatantCardClicked(CombatantState combatant)
        {
            if (_state != UIState.TargetSelect) return;

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
            AddLogEntry($"Battle started! {e.AllyCount} allies vs {e.EnemyCount} enemies ({e.TotalWaves} waves)", "log-entry-system");
            _resultOverlay.AddToClassList("hidden");
        }

        private void HandleRoundStart(int round)
        {
            _roundLabel.text = $"Round {round}";
            AddLogEntry($"--- Round {round} ---", "log-entry-system");
        }

        private void HandleTurnStart(CombatantState combatant)
        {
            UpdateUnitInfo(combatant);
            UpdateAllCombatantCards();
            HighlightActiveUnit(combatant);

            // Default to EnemyTurn; HandleInputRequested switches to ActionSelect
            // when CombatRunner calls PlayerCombatInput.RequestAction (after turn delay).
            SetState(UIState.EnemyTurn);
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
            AddLogEntry(msg);
        }

        private void HandleDamageDealt(DamageEvent e)
        {
            string targetName = e.Target?.Template?.DisplayName ?? "???";

            if (e.Result.IsMiss)
            {
                AddLogEntry($"MISS on {targetName}!", "log-entry-status");
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

            AddLogEntry(msg, "log-entry-damage");
            UpdateAllCombatantCards();
        }

        private void HandleHealApplied(HealEvent e)
        {
            string targetName = e.Target?.Template?.DisplayName ?? "???";
            AddLogEntry($"{targetName} healed for {e.Amount} HP", "log-entry-heal");
            UpdateAllCombatantCards();
        }

        private void HandleTurnSkipped(TurnSkippedEvent e)
        {
            string name = e.Combatant?.Template?.DisplayName ?? "???";
            string reason = e.Reason == StatusEffect.Aturdimiento ? "Stunned" : "Asleep";
            AddLogEntry($"{name} is {reason}! Turn skipped.", "log-entry-status");
        }

        private void HandleUnitDied(CombatantState combatant)
        {
            string name = combatant?.Template?.DisplayName ?? "???";
            AddLogEntry($"{name} has been defeated!", "log-entry-damage");
            UpdateAllCombatantCards();
        }

        private void HandleGuardActivated(CombatantState combatant)
        {
            UpdateAllCombatantCards();
        }

        private void HandleWaveComplete(int wave)
        {
            AddLogEntry($"Wave {wave + 1} cleared!", "log-entry-system");
        }

        private void HandleWaveStart(int wave)
        {
            _waveLabel.text = $"Wave {wave + 1}";
            AddLogEntry($"Wave {wave + 1} begins!", "log-entry-system");
            RebuildCombatantCards();
        }

        private void HandleBattleEnd(BattleEndEvent e)
        {
            SetState(UIState.BattleOver);
            _resultOverlay.RemoveFromClassList("hidden");

            if (e.Result == BattleResult.Victory)
            {
                _resultText.text = "VICTORY";
                _resultText.style.color = new Color(0.3f, 0.9f, 0.4f);
                _resultDetails.text = $"Battle won in {e.RoundsElapsed} rounds";
            }
            else
            {
                _resultText.text = "DEFEAT";
                _resultText.style.color = new Color(0.9f, 0.3f, 0.25f);
                _resultDetails.text = $"All allies defeated after {e.RoundsElapsed} rounds";
            }

            AddLogEntry($"=== {e.Result} ===", "log-entry-system");
        }

        private void HandleStatusApplied(StatusAppliedEvent e)
        {
            string targetName = e.Target?.Template?.DisplayName ?? "???";
            AddLogEntry($"{targetName} afflicted with {e.Status.Effect}!", "log-entry-status");
        }

        // ====================================================================
        // COMBATANT CARDS
        // ====================================================================

        /// <summary>
        /// Builds initial combatant cards. Called once after BattleStart and
        /// again after wave transitions.
        /// </summary>
        public void BuildCombatantCards(IReadOnlyList<CombatantState> allies,
            IReadOnlyList<CombatantState> enemies, int totalWaves)
        {
            _waveLabel.text = $"Wave 1/{totalWaves}";
            _combatantCards.Clear();
            _allyColumn.Clear();
            _enemyColumn.Clear();

            foreach (var ally in allies)
                _allyColumn.Add(CreateCombatantCard(ally, true));

            foreach (var enemy in enemies)
                _enemyColumn.Add(CreateCombatantCard(enemy, false));
        }

        private void RebuildCombatantCards()
        {
            // Rebuild enemy column for new wave
            var runner = GetComponent<Runtime.Combat.CombatRunner>();
            if (runner?.Manager == null) return;

            _enemyColumn.Clear();
            foreach (var enemy in runner.Manager.Enemies)
            {
                if (_combatantCards.ContainsKey(enemy))
                    _combatantCards.Remove(enemy);
                _enemyColumn.Add(CreateCombatantCard(enemy, false));
            }
        }

        private VisualElement CreateCombatantCard(CombatantState combatant, bool isAlly)
        {
            var card = new VisualElement();
            card.AddToClassList("combatant-card");

            var nameLabel = new Label(combatant.Template.DisplayName);
            nameLabel.AddToClassList("combatant-name");
            card.Add(nameLabel);

            // HP bar
            var hpBg = new VisualElement();
            hpBg.AddToClassList("hp-bar-bg");
            var hpFill = new VisualElement();
            hpFill.AddToClassList("hp-bar-fill");
            hpFill.name = "hp-fill";
            hpFill.style.width = Length.Percent(100);
            hpBg.Add(hpFill);
            card.Add(hpBg);

            var hpText = new Label($"{combatant.CurrentHP}/{combatant.MaxHP}");
            hpText.AddToClassList("hp-text");
            hpText.name = "hp-label";
            card.Add(hpText);

            // MP bar (allies only)
            if (isAlly && combatant.MaxMP > 0)
            {
                var mpBg = new VisualElement();
                mpBg.AddToClassList("mp-bar-bg");
                var mpFill = new VisualElement();
                mpFill.AddToClassList("mp-bar-fill");
                mpFill.name = "mp-fill";
                mpFill.style.width = Length.Percent(100);
                mpBg.Add(mpFill);
                card.Add(mpBg);
            }

            // Click handler for target selection
            card.RegisterCallback<ClickEvent>(evt => OnCombatantCardClicked(combatant));

            _combatantCards[combatant] = card;
            return card;
        }

        private void UpdateAllCombatantCards()
        {
            foreach (var kvp in _combatantCards)
            {
                var combatant = kvp.Key;
                var card = kvp.Value;

                // HP bar
                float hpPercent = combatant.MaxHP > 0
                    ? (float)combatant.CurrentHP / combatant.MaxHP * 100f
                    : 0f;
                var hpFill = card.Q("hp-fill");
                if (hpFill != null)
                {
                    hpFill.style.width = Length.Percent(hpPercent);
                    hpFill.RemoveFromClassList("hp-bar-fill-warning");
                    hpFill.RemoveFromClassList("hp-bar-fill-critical");
                    if (hpPercent <= 25f) hpFill.AddToClassList("hp-bar-fill-critical");
                    else if (hpPercent <= 50f) hpFill.AddToClassList("hp-bar-fill-warning");
                }

                var hpLabel = card.Q<Label>("hp-label");
                if (hpLabel != null)
                    hpLabel.text = $"{combatant.CurrentHP}/{combatant.MaxHP}";

                // MP bar
                var mpFill = card.Q("mp-fill");
                if (mpFill != null && combatant.MaxMP > 0)
                {
                    float mpPercent = (float)combatant.CurrentMP / combatant.MaxMP * 100f;
                    mpFill.style.width = Length.Percent(mpPercent);
                }

                // Dead state
                card.EnableInClassList("combatant-card-dead", combatant.IsKO);
            }
        }

        private void HighlightActiveUnit(CombatantState combatant)
        {
            foreach (var kvp in _combatantCards)
                kvp.Value.RemoveFromClassList("combatant-card-active");

            if (combatant != null && _combatantCards.TryGetValue(combatant, out var card))
                card.AddToClassList("combatant-card-active");
        }

        private void HighlightTargets(bool isEnemyTarget)
        {
            var runner = GetComponent<Runtime.Combat.CombatRunner>();
            if (runner?.Manager == null) return;

            var targets = isEnemyTarget ? runner.Manager.Enemies : runner.Manager.Allies;
            foreach (var target in targets)
            {
                if (target.IsKO) continue;
                if (_combatantCards.TryGetValue(target, out var card))
                    card.AddToClassList("combatant-card-targetable");
            }
        }

        private void ClearTargetHighlights()
        {
            foreach (var kvp in _combatantCards)
                kvp.Value.RemoveFromClassList("combatant-card-targetable");
        }

        // ====================================================================
        // INITIATIVE BAR
        // ====================================================================

        /// <summary>Updates the initiative bar icons from current turn order.</summary>
        public void UpdateInitiativeBar(List<InitiativeEntry> entries, CombatantState activeUnit)
        {
            _initiativeIcons.Clear();
            foreach (var entry in entries)
            {
                if (entry.Combatant.IsKO) continue;

                var icon = new Label(entry.Combatant.Template.DisplayName.Substring(0, 1).ToUpper());
                icon.AddToClassList("init-icon");
                icon.AddToClassList(entry.Team == CombatTeam.Ally ? "init-icon-ally" : "init-icon-enemy");
                if (entry.Combatant == activeUnit)
                    icon.AddToClassList("init-icon-active");
                icon.tooltip = entry.Combatant.Template.DisplayName;
                _initiativeIcons.Add(icon);
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
                ? $"MP: {combatant.CurrentMP}/{combatant.MaxMP}"
                : "";
            _unitAtkText.text = $"ATK:{combatant.GetEffectiveStat(StatType.ATK):F0}  DEF:{combatant.GetEffectiveStat(StatType.DEF):F0}";
        }

        // ====================================================================
        // ABILITY MENU
        // ====================================================================

        private void PopulateAbilityMenu()
        {
            _abilityList.Clear();
            if (_playerInput == null) return;

            var abilities = _playerInput.GetAvailableAbilities();
            if (abilities.Count == 0)
            {
                var noAbilityLabel = new Label("No abilities available");
                noAbilityLabel.AddToClassList("log-entry");
                _abilityList.Add(noAbilityLabel);
                return;
            }

            foreach (var ability in abilities)
            {
                string mpText = ability.MPCost > 0 ? $" [{ability.MPCost} MP]" : "";
                var btn = new Button(() => OnAbilitySelected(ability));
                btn.text = $"{ability.DisplayName}{mpText}";
                btn.AddToClassList("ability-btn");

                // Disable if not enough MP
                var actor = _playerInput.CurrentContext.Actor;
                if (actor != null && actor.CurrentMP < ability.MPCost)
                    btn.SetEnabled(false);

                _abilityList.Add(btn);
            }
        }

        // ====================================================================
        // BATTLE LOG
        // ====================================================================

        private void AddLogEntry(string message, string cssClass = "log-entry")
        {
            var entry = new Label(message);
            entry.AddToClassList("log-entry");
            if (cssClass != "log-entry")
                entry.AddToClassList(cssClass);
            _battleLog.Add(entry);

            // Auto-scroll to bottom
            _battleLog.schedule.Execute(() =>
                _battleLog.scrollOffset = new Vector2(0, float.MaxValue));
        }
    }
}
