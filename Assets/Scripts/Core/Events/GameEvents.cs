using System;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Events
{
    /// <summary>
    /// Static event bus for decoupled system communication.
    /// Systems publish events here; UI and other systems subscribe.
    /// Combat events carry ICombatant (land unit or ship); land-only
    /// subscribers pattern-match to CombatantState.
    /// See ADR-001 (State Management), ADR-003 and ADR-004.
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
        public static event Action<ICombatant> OnTurnStart;

        /// <summary>Fired when a combatant's turn ends.</summary>
        public static event Action<ICombatant> OnTurnEnd;

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

        /// <summary>Fired when a status effect is removed (expired, consumed, or cleansed).</summary>
        public static event Action<StatusRemovedEvent> OnStatusRemoved;

        /// <summary>Fired when a buff/debuff expires.</summary>
        public static event Action<BuffInstance> OnBuffExpired;

        // --- Unit State ---

        /// <summary>Fired when a combatant dies (unit KO / ship sunk).</summary>
        public static event Action<ICombatant> OnUnitDied;

        /// <summary>Fired when a combatant is revived (land only — naval crew
        /// cannot be revived, Combate Naval GDD §6).</summary>
        public static event Action<CombatantState> OnUnitRevived;

        /// <summary>Fired when a combatant activates Guard (land).</summary>
        public static event Action<CombatantState> OnGuardActivated;

        /// <summary>Fired when a Limit Break extra turn is inserted (unit or ship).</summary>
        public static event Action<ICombatant> OnLimitBreakActivated;

        // --- Synergy Events ---

        /// <summary>Fired when a synergy activates (at combat start or captain revive).</summary>
        public static event Action<SynergyEvent> OnSynergyActivated;

        /// <summary>Fired when a synergy deactivates (captain KO or enemy captain killed).</summary>
        public static event Action<SynergyEvent> OnSynergyDeactivated;

        // --- Naval Events (ADR-004 §4) ---

        /// <summary>Fired when a crew member takes damage (Abordaje or crew DoT).</summary>
        public static event Action<CrewDamageEvent> OnCrewDamaged;

        /// <summary>Fired when a crew member dies. Stats/pool recalculation has
        /// already happened when this fires.</summary>
        public static event Action<CrewDiedEvent> OnCrewDied;

        /// <summary>Fired after a ship recalculates effective stats from crew
        /// (crew death). UI uses this to refresh stat panels and synergies.</summary>
        public static event Action<ShipCombatant> OnShipStatsRecalculated;

        /// <summary>Fired when a ship activates Maniobra Evasiva.</summary>
        public static event Action<ShipCombatant> OnManeuverActivated;

        // --- Publish Methods ---

        public static void PublishBattleStart(BattleStartEvent e) => OnBattleStart?.Invoke(e);
        public static void PublishRoundStart(int round) => OnRoundStart?.Invoke(round);
        public static void PublishWaveStart(int wave) => OnWaveStart?.Invoke(wave);
        public static void PublishWaveComplete(int wave) => OnWaveComplete?.Invoke(wave);
        public static void PublishBattleEnd(BattleEndEvent e) => OnBattleEnd?.Invoke(e);
        public static void PublishTurnStart(ICombatant c) => OnTurnStart?.Invoke(c);
        public static void PublishTurnEnd(ICombatant c) => OnTurnEnd?.Invoke(c);
        public static void PublishTurnSkipped(TurnSkippedEvent e) => OnTurnSkipped?.Invoke(e);
        public static void PublishActionChosen(CombatAction a) => OnActionChosen?.Invoke(a);
        public static void PublishDamageDealt(DamageEvent e) => OnDamageDealt?.Invoke(e);
        public static void PublishHealApplied(HealEvent e) => OnHealApplied?.Invoke(e);
        public static void PublishBuffApplied(BuffInstance b) => OnBuffApplied?.Invoke(b);
        public static void PublishStatusApplied(StatusAppliedEvent e) => OnStatusApplied?.Invoke(e);
        public static void PublishStatusRemoved(StatusRemovedEvent e) => OnStatusRemoved?.Invoke(e);
        public static void PublishBuffExpired(BuffInstance b) => OnBuffExpired?.Invoke(b);
        public static void PublishUnitDied(ICombatant c) => OnUnitDied?.Invoke(c);
        public static void PublishUnitRevived(CombatantState c) => OnUnitRevived?.Invoke(c);
        public static void PublishGuardActivated(CombatantState c) => OnGuardActivated?.Invoke(c);
        public static void PublishLimitBreakActivated(ICombatant c) => OnLimitBreakActivated?.Invoke(c);
        public static void PublishSynergyActivated(SynergyEvent e) => OnSynergyActivated?.Invoke(e);
        public static void PublishSynergyDeactivated(SynergyEvent e) => OnSynergyDeactivated?.Invoke(e);
        public static void PublishCrewDamaged(CrewDamageEvent e) => OnCrewDamaged?.Invoke(e);
        public static void PublishCrewDied(CrewDiedEvent e) => OnCrewDied?.Invoke(e);
        public static void PublishShipStatsRecalculated(ShipCombatant s) => OnShipStatsRecalculated?.Invoke(s);
        public static void PublishManeuverActivated(ShipCombatant s) => OnManeuverActivated?.Invoke(s);

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
            OnStatusRemoved = null;
            OnBuffExpired = null;
            OnUnitDied = null;
            OnUnitRevived = null;
            OnGuardActivated = null;
            OnLimitBreakActivated = null;
            OnSynergyActivated = null;
            OnSynergyDeactivated = null;
            OnCrewDamaged = null;
            OnCrewDied = null;
            OnShipStatsRecalculated = null;
            OnManeuverActivated = null;
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
        public ICombatant Combatant;
        public StatusEffect Reason;
    }

    public struct DamageEvent
    {
        public ICombatant Source;
        public ICombatant Target;
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
        Poison,
        Boarding
    }

    public struct HealEvent
    {
        public ICombatant Source;
        public ICombatant Target;
        public int Amount;
    }

    public struct StatusAppliedEvent
    {
        public ICombatant Source;
        public ICombatant Target;
        public StatusInstance Status;
    }

    public enum StatusRemovalReason
    {
        Expired,
        Consumed,
        WokenByDamage,
        Cleansed
    }

    public struct StatusRemovedEvent
    {
        public ICombatant Target;
        public StatusEffect Effect;
        public StatusRemovalReason Reason;
    }

    public struct SynergyEvent
    {
        public ActiveSynergy Synergy;
        public bool IsAllySide;
    }

    /// <summary>Damage to a crew member (Abordaje, crew-target ability, Veneno/Sangrado).</summary>
    public struct CrewDamageEvent
    {
        /// <summary>Ship the damaged crew member belongs to.</summary>
        public ShipCombatant Ship;
        public CrewMemberState Crew;
        public int ActualDamage;
        public DamageSource Source;
        /// <summary>Attacking combatant. Null for DoTs.</summary>
        public ICombatant Attacker;
    }

    /// <summary>A crew member died (Abordaje or crew DoT). Permanent for the battle.</summary>
    public struct CrewDiedEvent
    {
        public ShipCombatant Ship;
        public CrewMemberState Crew;
    }
}
