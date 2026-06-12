using System.Collections.Generic;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// What the CombatManager and InitiativeBar need from any combat
    /// participant, be it a land unit (CombatantState) or a naval ship
    /// (ShipCombatant). Keeps the orchestrator agnostic to entity semantics.
    /// Deliberately small: action resolution differences live in ITurnResolver,
    /// not here. Crew members are NOT combatants (they take no turns).
    /// See ADR-004 §2.
    /// </summary>
    public interface ICombatant : ITraitCarrier
    {
        Element Element { get; }
        bool IsKO { get; }
        bool IsBoss { get; }

        // HP surface (ship maps these to hull HP/HHP) — shared by UI and DoTs
        int CurrentHP { get; }
        int MaxHP { get; }
        int ApplyDamage(int damage);
        int ApplyHealing(int amount);

        // Initiative + shared pipeline
        float GetEffectiveStat(StatType stat);
        BuffStack Buffs { get; }
        bool LBUsedThisRound { get; set; }

        // Status / CC (ships ignore Sueño/Aturdimiento/Muerte via IsImmuneTo)
        bool HasStatus(StatusEffect effect);
        void ApplyStatus(StatusInstance status);
        bool RemoveStatus(StatusEffect effect);
        List<StatusEffect> TickStatuses();
        bool IsImmuneTo(StatusEffect effect);
        int CCImmunityTurns { get; set; }
        IReadOnlyList<StatusInstance> StatusEffects { get; }

        // Cooldowns (shared)
        void TickCooldowns();
    }
}
