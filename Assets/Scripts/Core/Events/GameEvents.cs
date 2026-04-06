using System;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Events
{
    /// <summary>
    /// Static event bus for decoupled system communication.
    /// Systems publish events here; UI and other systems subscribe.
    /// See ADR-001 (State Management) and ADR-003 (Combat Architecture).
    /// </summary>
    public static class GameEvents
    {
        // --- Combat Lifecycle ---

        /// <summary>Fired when a battle begins (after PreCombat setup).</summary>
        public static event Action<BattleStartEvent> OnBattleStart;

        /// <summary>Fired at the start of each round.</summary>
        public static event Action<int> OnRoundStart;

        /// <summary>Fired when a new wave begins.</summary>
        public static event Action<int> OnWaveStart;

        /// <summary>Fired when all enemies in a wave are eliminated.</summary>
        public static event Action<int> OnWaveComplete;

        /// <summary>Fired when the battle ends (victory or defeat).</summary>
        public static event Action<BattleEndEvent> OnBattleEnd;

        // --- Turn Events ---

        /// <summary>Fired when a combatant's turn starts (before buff tick).</summary>
        public static event Action<CombatantState> OnTurnStart;

        /// <summary>Fired when a combatant's turn ends.</summary>
        public static event Action<CombatantState> OnTurnEnd;

        /// <summary>Fired when a combatant's turn is skipped (CC).</summary>
        public static event Action<TurnSkippedEvent> OnTurnSkipped;

        /// <summary>Fired when a player/AI chooses an action.</summary>
        public static event Action<CombatAction> OnActionChosen;

        // --- Damage & Healing ---

        /// <summary>Fired when damage is dealt (attacks, abilities, DoTs).</summary>
        public static event Action<DamageEvent> OnDamageDealt;

        /// <summary>Fired when healing is applied.</summary>
        public static event Action<HealEvent> OnHealApplied;

        // --- Buffs & Status ---

        /// <summary>Fired when a buff/debuff is applied.</summary>
        public static event Action<BuffInstance> OnBuffApplied;

        /// <summary>Fired when a status effect is applied.</summary>
        public static event Action<StatusAppliedEvent> OnStatusApplied;

        /// <summary>Fired when a buff/debuff expires.</summary>
        public static event Action<BuffInstance> OnBuffExpired;

        // --- Unit State ---

        /// <summary>Fired when a combatant dies (HP reaches 0).</summary>
        public static event Action<CombatantState> OnUnitDied;

        /// <summary>Fired when a combatant is revived.</summary>
        public static event Action<CombatantState> OnUnitRevived;

        /// <summary>Fired when a combatant activates Guard.</summary>
        public static event Action<CombatantState> OnGuardActivated;

        /// <summary>Fired when a Limit Break extra turn is inserted.</summary>
        public static event Action<CombatantState> OnLimitBreakActivated;

        // --- Publish Methods ---

        public static void PublishBattleStart(BattleStartEvent e) => OnBattleStart?.Invoke(e);
        public static void PublishRoundStart(int round) => OnRoundStart?.Invoke(round);
        public static void PublishWaveStart(int wave) => OnWaveStart?.Invoke(wave);
        public static void PublishWaveComplete(int wave) => OnWaveComplete?.Invoke(wave);
        public static void PublishBattleEnd(BattleEndEvent e) => OnBattleEnd?.Invoke(e);
        public static void PublishTurnStart(CombatantState c) => OnTurnStart?.Invoke(c);
        public static void PublishTurnEnd(CombatantState c) => OnTurnEnd?.Invoke(c);
        public static void PublishTurnSkipped(TurnSkippedEvent e) => OnTurnSkipped?.Invoke(e);
        public static void PublishActionChosen(CombatAction a) => OnActionChosen?.Invoke(a);
        public static void PublishDamageDealt(DamageEvent e) => OnDamageDealt?.Invoke(e);
        public static void PublishHealApplied(HealEvent e) => OnHealApplied?.Invoke(e);
        public static void PublishBuffApplied(BuffInstance b) => OnBuffApplied?.Invoke(b);
        public static void PublishStatusApplied(StatusAppliedEvent e) => OnStatusApplied?.Invoke(e);
        public static void PublishBuffExpired(BuffInstance b) => OnBuffExpired?.Invoke(b);
        public static void PublishUnitDied(CombatantState c) => OnUnitDied?.Invoke(c);
        public static void PublishUnitRevived(CombatantState c) => OnUnitRevived?.Invoke(c);
        public static void PublishGuardActivated(CombatantState c) => OnGuardActivated?.Invoke(c);
        public static void PublishLimitBreakActivated(CombatantState c) => OnLimitBreakActivated?.Invoke(c);

        /// <summary>
        /// Removes all subscribers. Call during scene transitions to prevent leaks.
        /// </summary>
        public static void ClearAll()
        {
            OnBattleStart = null;
            OnRoundStart = null;
            OnWaveStart = null;
            OnWaveComplete = null;
            OnBattleEnd = null;
            OnTurnStart = null;
            OnTurnEnd = null;
            OnTurnSkipped = null;
            OnActionChosen = null;
            OnDamageDealt = null;
            OnHealApplied = null;
            OnBuffApplied = null;
            OnStatusApplied = null;
            OnBuffExpired = null;
            OnUnitDied = null;
            OnUnitRevived = null;
            OnGuardActivated = null;
            OnLimitBreakActivated = null;
        }
    }

    // --- Event Data Structs ---

    public struct BattleStartEvent
    {
        public int AllyCount;
        public int EnemyCount;
        public int TotalWaves;
    }

    public struct BattleEndEvent
    {
        public BattleResult Result;
        public int RoundsElapsed;
    }

    public enum BattleResult
    {
        Victory,
        Defeat
    }

    public struct TurnSkippedEvent
    {
        public CombatantState Combatant;
        public StatusEffect Reason;
    }

    public struct DamageEvent
    {
        public CombatantState Source;
        public CombatantState Target;
        public DamageResult Result;
        public bool IsGuarded;
        public int ActualDamage;
        public DamageSource DamageSource;
    }

    public enum DamageSource
    {
        Attack,
        Ability,
        Bleed,
        Burn,
        Poison
    }

    public struct HealEvent
    {
        public CombatantState Source;
        public CombatantState Target;
        public int Amount;
    }

    public struct StatusAppliedEvent
    {
        public CombatantState Source;
        public CombatantState Target;
        public StatusInstance Status;
    }
}
