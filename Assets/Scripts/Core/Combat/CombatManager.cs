using System;
using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Orchestrates the combat state machine and turn processing pipeline.
    /// Pure C# class — no MonoBehaviour. Driven by CombatRunner (coroutine wrapper).
    /// Action-phase semantics (land vs naval) are delegated to an ITurnResolver.
    ///
    /// Battle flow: PreCombat → InRound ⇄ WaveTransition → Victory/Defeat
    /// Turn order: buff tick → CC immunity → start DoTs → CC check → action → post DoTs
    ///
    /// See Combate Terrestre GDD, ADR-003 and ADR-004.
    /// </summary>
    public class CombatManager
    {
        // --- Constants (from GDDs) ---
        public const float GUARD_REDUCTION = 0.50f;
        public const float DOT_MIN_DAMAGE = 1f;

        // --- State ---
        public BattlePhase Phase { get; private set; } = BattlePhase.None;
        public int RoundNumber { get; private set; }
        public int WaveIndex { get; private set; }
        public int TotalWaves => _config?.Waves?.Count ?? 0;
        public InitiativeEntry CurrentActor { get; private set; }

        // --- Dependencies ---
        public InitiativeBar Bar => _bar;
        private readonly InitiativeBar _bar;
        private readonly ITurnResolver _resolver;
        private BattleConfig _config;

        // --- Combatant Lists ---
        private readonly List<ICombatant> _allies = new();
        private readonly List<ICombatant> _enemies = new();
        private readonly List<InitiativeEntry> _allEntries = new();
        private List<InitiativeEntry> _allyEntries = new();
        private List<InitiativeEntry> _currentWaveEnemyEntries = new();

        public IReadOnlyList<ICombatant> Allies => _allies;
        public IReadOnlyList<ICombatant> Enemies => _enemies;

        // --- Synergy State ---
        private List<ActiveSynergy> _allySynergiesPrimary = new();
        private List<ActiveSynergy> _allySynergiesSecondary = new();
        private List<ActiveSynergy> _enemySynergies = new();
        private int _captainIndex;
        private bool _isGuestFriend;

        /// <summary>Currently active allied synergies (primary + secondary captain).</summary>
        public IReadOnlyList<ActiveSynergy> AllySynergies
        {
            get
            {
                var all = new List<ActiveSynergy>(_allySynergiesPrimary);
                all.AddRange(_allySynergiesSecondary);
                return all;
            }
        }

        /// <summary>Currently active enemy synergies.</summary>
        public IReadOnlyList<ActiveSynergy> EnemySynergies => _enemySynergies;

        /// <summary>
        /// Creates a manager. Defaults to land combat semantics; pass a
        /// NavalTurnResolver for naval battles (ADR-004 §3).
        /// </summary>
        public CombatManager(InitiativeBar bar, ITurnResolver resolver = null)
        {
            _bar = bar ?? throw new ArgumentNullException(nameof(bar));
            _resolver = resolver ?? new LandTurnResolver();
            _resolver.Initialize(this);
        }

        // ====================================================================
        // BATTLE LIFECYCLE
        // ====================================================================

        /// <summary>
        /// Initializes the battle. Transitions from None → PreCombat → InRound.
        /// </summary>
        public void StartBattle(BattleConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Phase = BattlePhase.PreCombat;

            // Setup allies
            _allyEntries = new List<InitiativeEntry>(config.Allies);
            _allies.Clear();
            foreach (var entry in _allyEntries)
                _allies.Add(entry.Combatant);

            // Store captain config
            _captainIndex = config.CaptainIndex;
            _isGuestFriend = config.IsGuestFriend;

            // Setup first wave
            WaveIndex = 0;
            RoundNumber = 0;
            DeployWave(WaveIndex);

            // Evaluate ally synergies (GDD: at combat start)
            EvaluateAllySynergies();

            GameEvents.PublishBattleStart(new BattleStartEvent
            {
                AllyCount = _allies.Count,
                EnemyCount = _enemies.Count,
                TotalWaves = TotalWaves
            });

            Phase = BattlePhase.InRound;
        }

        /// <summary>
        /// Begins a new round. Resets LB flags, starts Initiative Bar.
        /// </summary>
        public void BeginRound()
        {
            RoundNumber++;

            // Reset per-round flags (GDD §2 step 1)
            foreach (var ally in _allies)
                ally.LBUsedThisRound = false;
            foreach (var enemy in _enemies)
                enemy.LBUsedThisRound = false;

            // Build entry list from living combatants
            _allEntries.Clear();
            foreach (var entry in _allyEntries)
                if (!entry.Combatant.IsKO)
                    _allEntries.Add(entry);
            foreach (var entry in _currentWaveEnemyEntries)
                if (!entry.Combatant.IsKO)
                    _allEntries.Add(entry);

            _bar.BeginRound(_allEntries);
            GameEvents.PublishRoundStart(RoundNumber);
        }

        /// <summary>
        /// Advances to the next combatant's turn. Returns null if round is over.
        /// Handles CC skipping internally — will skip stunned/sleeping units
        /// and advance to the next actionable combatant.
        /// </summary>
        public InitiativeEntry AdvanceTurn()
        {
            while (true)
            {
                var entry = _bar.AdvanceTurn();
                if (entry == null)
                {
                    CurrentActor = null;
                    return null;
                }

                CurrentActor = entry;
                var combatant = entry.Combatant;

                GameEvents.PublishTurnStart(combatant);

                // Step 1: Tick buffs/debuffs, status durations, and cooldowns
                combatant.Buffs.TickTurns();
                var expiredStatuses = combatant.TickStatuses();
                foreach (var effect in expiredStatuses)
                {
                    GameEvents.PublishStatusRemoved(new StatusRemovedEvent
                    {
                        Target = combatant,
                        Effect = effect,
                        Reason = StatusRemovalReason.Expired
                    });
                }
                combatant.TickCooldowns();

                // Step 2: Tick CC immunity
                if (combatant.CCImmunityTurns > 0)
                    combatant.CCImmunityTurns--;

                // Step 3: Start-of-turn DoTs (Sangrado) — domain-specific (resolver)
                _resolver.ApplyStartOfTurnDoTs(combatant);
                if (combatant.IsKO)
                {
                    HandleDeath(combatant);
                    _bar.CompleteCurrentTurn();
                    if (Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat)
                        return null;
                    continue;
                }

                // Step 4: CC check
                if (IsCC(combatant))
                {
                    var ccEffect = GetActiveCC(combatant);
                    GameEvents.PublishTurnSkipped(new TurnSkippedEvent
                    {
                        Combatant = combatant,
                        Reason = ccEffect
                    });

                    // Stun consumed after skip, grants CC immunity
                    if (ccEffect == StatusEffect.Aturdimiento)
                    {
                        combatant.RemoveStatus(StatusEffect.Aturdimiento);
                        combatant.CCImmunityTurns = InitiativeBar.CC_IMMUNITY_DURATION;
                    }

                    // Domain-specific upkeep (land: guard removal)
                    _resolver.OnTurnStart(combatant);

                    _bar.CompleteCurrentTurn();
                    GameEvents.PublishTurnEnd(combatant);

                    if (Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat)
                        return null;
                    continue;
                }

                // Domain-specific upkeep before acting (land: guard removal, GDD §3)
                _resolver.OnTurnStart(combatant);

                // Ready for action — return to caller
                return entry;
            }
        }

        /// <summary>
        /// Resolves a chosen action for the current actor via the turn resolver.
        /// Handles post-action DoT scheduling; death and LB flow through events.
        /// </summary>
        public void ResolveAction(CombatAction action)
        {
            var actor = CurrentActor.Combatant;

            GameEvents.PublishActionChosen(action);

            _resolver.ResolveAction(actor, action, GetCurrentContext());

            // Post-action DoTs (only if actor alive; also process during Victory
            // so simultaneous death → Defeat can override — GDD edge case 1)
            bool actorPassed = action.Type == ActionType.Pass;
            if (!actor.IsKO && (Phase == BattlePhase.InRound || Phase == BattlePhase.Victory))
            {
                _resolver.ApplyPostActionDoTs(actor, actorPassed);
            }
        }

        /// <summary>
        /// Completes the current turn. Call after ResolveAction.
        /// </summary>
        public void CompleteTurn()
        {
            if (CurrentActor != null)
            {
                GameEvents.PublishTurnEnd(CurrentActor.Combatant);
                _bar.CompleteCurrentTurn();
            }
            CurrentActor = null;
        }

        /// <summary>
        /// Transitions to the next wave. Call when current wave is cleared.
        /// </summary>
        public void TransitionToNextWave()
        {
            WaveIndex++;
            Phase = BattlePhase.WaveTransition;

            GameEvents.PublishWaveComplete(WaveIndex - 1);

            DeployWave(WaveIndex);

            GameEvents.PublishWaveStart(WaveIndex);

            Phase = BattlePhase.InRound;
        }

        /// <summary>Whether the current round is over (all units have acted).</summary>
        public bool IsRoundOver => _bar.IsRoundOver;

        /// <summary>
        /// Gets the combat context for the current actor (for ICombatInput).
        /// Team-aware: Allies/Enemies are relative to the actor's team.
        /// </summary>
        public CombatContext GetCurrentContext()
        {
            bool isAlly = CurrentActor?.Team == CombatTeam.Ally;
            return new CombatContext
            {
                Actor = CurrentActor?.Combatant,
                Allies = isAlly ? GetAlive(_allyEntries) : GetAlive(_currentWaveEnemyEntries),
                Enemies = isAlly ? GetAlive(_currentWaveEnemyEntries) : GetAlive(_allyEntries)
            };
        }

        // ====================================================================
        // RESOLVER SUPPORT (internal surface for ITurnResolver implementations)
        // ====================================================================

        /// <summary>Living allies. Used by resolvers for ally-AoE targeting.</summary>
        internal List<ICombatant> GetAliveAllies() => GetAlive(_allyEntries);

        /// <summary>Living enemies of the current wave. Used by resolvers for enemy-AoE targeting.</summary>
        internal List<ICombatant> GetAliveWaveEnemies() => GetAlive(_currentWaveEnemyEntries);

        /// <summary>Notifies the manager that a resolver killed a combatant.</summary>
        internal void NotifyDeath(ICombatant combatant) => HandleDeath(combatant);

        // ====================================================================
        // DEATH & BATTLE END
        // ====================================================================

        private void HandleDeath(ICombatant combatant)
        {
            GameEvents.PublishUnitDied(combatant);
            _bar.RemoveDead(combatant);

            if (combatant is CombatantState unit)
                unit.IsGuarding = false;

            // Synergy re-evaluation on captain KO (GDD §2, §4)
            CheckCaptainKO(combatant);

            CheckBattleEnd();
        }

        private void CheckBattleEnd()
        {
            bool allEnemiesDead = GetAlive(_currentWaveEnemyEntries).Count == 0;
            bool allAlliesDead = GetAlive(_allyEntries).Count == 0;

            // Simultaneous death: Defeat (GDD §8 edge case 1)
            if (allAlliesDead)
            {
                Phase = BattlePhase.Defeat;
                GameEvents.PublishBattleEnd(new BattleEndEvent
                {
                    Result = BattleResult.Defeat,
                    RoundsElapsed = RoundNumber
                });
                return;
            }

            if (allEnemiesDead)
            {
                if (WaveIndex >= TotalWaves - 1)
                {
                    // Last wave cleared — Victory
                    Phase = BattlePhase.Victory;
                    GameEvents.PublishBattleEnd(new BattleEndEvent
                    {
                        Result = BattleResult.Victory,
                        RoundsElapsed = RoundNumber
                    });
                }
                // Wave cleared but more waves remain — caller should call TransitionToNextWave
            }
        }

        /// <summary>Whether all enemies in the current wave are dead (triggers wave transition).</summary>
        public bool IsCurrentWaveCleared => GetAlive(_currentWaveEnemyEntries).Count == 0;

        // ====================================================================
        // WAVE MANAGEMENT
        // ====================================================================

        private void DeployWave(int index)
        {
            if (index < 0 || index >= TotalWaves) return;

            // Remove previous wave's enemy synergies
            SynergyEvaluator.RemoveBuffs(_enemySynergies);
            _enemySynergies.Clear();

            _currentWaveEnemyEntries = new List<InitiativeEntry>(_config.Waves[index].Enemies);
            _enemies.Clear();
            foreach (var entry in _currentWaveEnemyEntries)
                _enemies.Add(entry.Combatant);

            // Evaluate enemy synergies for this wave
            int enemyCaptainIdx = _config.Waves[index].EnemyCaptainIndex;
            EvaluateEnemySynergies(enemyCaptainIdx);
        }

        // ====================================================================
        // SYNERGY MANAGEMENT
        // ====================================================================

        /// <summary>
        /// Evaluates and applies ally synergies (primary + secondary captain).
        /// </summary>
        private void EvaluateAllySynergies()
        {
            // Primary captain synergies
            _allySynergiesPrimary = SynergyEvaluator.Evaluate(
                _allies, _captainIndex, false);
            SynergyEvaluator.ApplyBuffs(_allySynergiesPrimary);

            foreach (var syn in _allySynergiesPrimary)
                GameEvents.PublishSynergyActivated(new SynergyEvent
                    { Synergy = syn, IsAllySide = true });

            // Secondary captain synergies (friend guest)
            _allySynergiesSecondary.Clear();
            if (_isGuestFriend && _allies.Count > 1)
            {
                int guestIndex = _allies.Count - 1;
                var secondarySynergies = SynergyEvaluator.Evaluate(
                    _allies, guestIndex, false);
                _allySynergiesSecondary = secondarySynergies;
                SynergyEvaluator.ApplyBuffs(_allySynergiesSecondary);

                foreach (var syn in _allySynergiesSecondary)
                    GameEvents.PublishSynergyActivated(new SynergyEvent
                        { Synergy = syn, IsAllySide = true });
            }
        }

        /// <summary>
        /// Removes all ally synergies and re-evaluates from scratch.
        /// </summary>
        private void ReEvaluateAllySynergies()
        {
            // Remove existing
            SynergyEvaluator.RemoveBuffs(_allySynergiesPrimary);
            foreach (var syn in _allySynergiesPrimary)
                GameEvents.PublishSynergyDeactivated(new SynergyEvent
                    { Synergy = syn, IsAllySide = true });

            SynergyEvaluator.RemoveBuffs(_allySynergiesSecondary);
            foreach (var syn in _allySynergiesSecondary)
                GameEvents.PublishSynergyDeactivated(new SynergyEvent
                    { Synergy = syn, IsAllySide = true });

            _allySynergiesPrimary.Clear();
            _allySynergiesSecondary.Clear();

            // Re-evaluate (captain may have been revived or KO'd)
            EvaluateAllySynergies();
        }

        /// <summary>
        /// Evaluates and applies enemy synergies for the current wave.
        /// </summary>
        private void EvaluateEnemySynergies(int enemyCaptainIndex)
        {
            _enemySynergies = SynergyEvaluator.EvaluateEnemies(
                _enemies, enemyCaptainIndex);
            SynergyEvaluator.ApplyBuffs(_enemySynergies);

            foreach (var syn in _enemySynergies)
                GameEvents.PublishSynergyActivated(new SynergyEvent
                    { Synergy = syn, IsAllySide = false });
        }

        /// <summary>
        /// Checks if a dying combatant is a captain and deactivates synergies.
        /// </summary>
        private void CheckCaptainKO(ICombatant combatant)
        {
            // Allied primary captain KO
            if (_captainIndex >= 0 && _captainIndex < _allies.Count
                && _allies[_captainIndex] == combatant)
            {
                SynergyEvaluator.RemoveBuffs(_allySynergiesPrimary);
                foreach (var syn in _allySynergiesPrimary)
                    GameEvents.PublishSynergyDeactivated(new SynergyEvent
                        { Synergy = syn, IsAllySide = true });
                _allySynergiesPrimary.Clear();
            }

            // Allied secondary captain (friend guest) KO
            if (_isGuestFriend && _allies.Count > 1
                && _allies[_allies.Count - 1] == combatant)
            {
                SynergyEvaluator.RemoveBuffs(_allySynergiesSecondary);
                foreach (var syn in _allySynergiesSecondary)
                    GameEvents.PublishSynergyDeactivated(new SynergyEvent
                        { Synergy = syn, IsAllySide = true });
                _allySynergiesSecondary.Clear();
            }

            // Enemy captain KO — deactivate all enemy synergies
            int enemyCaptainIdx = _config.Waves[WaveIndex].EnemyCaptainIndex;
            if (enemyCaptainIdx >= 0 && enemyCaptainIdx < _enemies.Count
                && _enemies[enemyCaptainIdx] == combatant)
            {
                SynergyEvaluator.RemoveBuffs(_enemySynergies);
                foreach (var syn in _enemySynergies)
                    GameEvents.PublishSynergyDeactivated(new SynergyEvent
                        { Synergy = syn, IsAllySide = false });
                _enemySynergies.Clear();
            }
        }

        /// <summary>
        /// Call when a captain is revived to reactivate their synergies.
        /// </summary>
        public void OnCaptainRevived(CombatantState combatant)
        {
            bool isPrimaryCaptain = _captainIndex >= 0 && _captainIndex < _allies.Count
                && ReferenceEquals(_allies[_captainIndex], combatant);
            bool isSecondaryCaptain = _isGuestFriend && _allies.Count > 1
                && ReferenceEquals(_allies[_allies.Count - 1], combatant);

            if (isPrimaryCaptain || isSecondaryCaptain)
                ReEvaluateAllySynergies();
        }

        // ====================================================================
        // CC HELPERS
        // ====================================================================

        private bool IsCC(ICombatant combatant)
        {
            if (combatant.CCImmunityTurns > 0)
                return false;

            return combatant.HasStatus(StatusEffect.Aturdimiento)
                || combatant.HasStatus(StatusEffect.Sueno);
        }

        private StatusEffect GetActiveCC(ICombatant combatant)
        {
            if (combatant.HasStatus(StatusEffect.Aturdimiento))
                return StatusEffect.Aturdimiento;
            if (combatant.HasStatus(StatusEffect.Sueno))
                return StatusEffect.Sueno;
            return StatusEffect.Aturdimiento; // fallback, shouldn't reach
        }

        // ====================================================================
        // UTILITY
        // ====================================================================

        private List<ICombatant> GetAlive(List<InitiativeEntry> entries)
        {
            var result = new List<ICombatant>();
            foreach (var e in entries)
                if (!e.Combatant.IsKO)
                    result.Add(e.Combatant);
            return result;
        }
    }
}
