using System;
using System.Collections.Generic;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Runtime combat state for a single land unit.
    /// Created at battle start, discarded at battle end. Not persisted.
    /// Implements ICombatant (shared orchestration contract, ADR-004) and
    /// ITraitCarrier (synergy evaluation).
    /// See Damage & Stats Engine GDD §States and ADR-001.
    /// </summary>
    public class CombatantState : ICombatant
    {
        /// <summary>Reference to the static data template.</summary>
        public CharacterData Template { get; }

        /// <summary>Current max level (after awakening raises).</summary>
        public int Level { get; }

        public int MaxHP { get; }
        public int MaxMP { get; }
        public int CurrentHP { get; set; }
        public int CurrentMP { get; set; }

        /// <summary>Base stats at current level (before buffs).</summary>
        public StatBlock BaseStats { get; }

        /// <summary>Active buff/debuff stack.</summary>
        public BuffStack Buffs { get; } = new();

        /// <summary>Active status effects.</summary>
        public List<StatusInstance> StatusEffects { get; } = new();

        /// <summary>Remaining turns of CC immunity (0 = vulnerable).</summary>
        public int CCImmunityTurns { get; set; }

        /// <summary>Whether this combatant is a boss (immune to Muerte).</summary>
        public bool IsBoss { get; set; }

        /// <summary>Whether this combatant is currently guarding (50% damage reduction).</summary>
        public bool IsGuarding { get; set; }

        /// <summary>Whether this combatant has used Limit Break this round.</summary>
        public bool LBUsedThisRound { get; set; }

        /// <summary>Per-ability cooldown tracker (ability ID → remaining turns).</summary>
        private readonly Dictionary<string, int> _cooldowns = new();

        public bool IsKO => CurrentHP <= 0;

        /// <summary>Element from the data template (Neutral if no template).</summary>
        public Element Element => Template != null ? Template.Element : Element.Neutral;

        /// <summary>Land units have no inherent status immunities (bosses' Muerte immunity is handled via IsBoss).</summary>
        public bool IsImmuneTo(StatusEffect effect) => false;

        // ICombatant exposes statuses read-only; the mutable List stays public for land code/tests.
        IReadOnlyList<StatusInstance> ICombatant.StatusEffects => StatusEffects;

        /// <summary>Traits from the data template (ITraitCarrier).</summary>
        public IReadOnlyList<UnitTraitEntry> Traits => Template?.Traits;

        /// <summary>Land units receive their synergy buffs on their own BuffStack (ITraitCarrier).</summary>
        public BuffStack SynergyBuffTarget => Buffs;

        public CombatantState(CharacterData template, StatBlock levelStats, int level)
        {
            Template = template;
            Level = level;
            BaseStats = levelStats;
            MaxHP = (int)levelStats.HP;
            MaxMP = (int)levelStats.MP;
            CurrentHP = MaxHP;
            CurrentMP = MaxMP;
        }

        /// <summary>
        /// Gets the effective value of a stat after applying buff modifiers.
        /// </summary>
        public float GetEffectiveStat(StatType stat)
        {
            float baseStat = BaseStats[(int)stat];
            float modifier = Buffs.GetStatModifier(stat);
            return baseStat * modifier;
        }

        /// <summary>
        /// Applies damage to this combatant, clamping HP to 0.
        /// Returns actual damage dealt.
        /// </summary>
        public int ApplyDamage(int damage)
        {
            int actual = System.Math.Min(damage, CurrentHP);
            CurrentHP -= actual;
            return actual;
        }

        /// <summary>
        /// Applies healing, clamping HP to MaxHP.
        /// Returns actual amount healed.
        /// </summary>
        public int ApplyHealing(int amount)
        {
            int actual = System.Math.Min(amount, MaxHP - CurrentHP);
            CurrentHP += actual;
            return actual;
        }

        // ====================================================================
        // ABILITY & COOLDOWN MANAGEMENT
        // ====================================================================

        /// <summary>
        /// Checks if an ability is ready to use (enough MP, off cooldown, not silenced).
        /// </summary>
        public bool IsAbilityReady(AbilityData ability)
        {
            if (ability == null) return false;
            if (CurrentMP < ability.MPCost) return false;
            if (_cooldowns.TryGetValue(ability.Id, out int remaining) && remaining > 0) return false;
            if (HasStatus(StatusEffect.Silencio)) return false;
            return true;
        }

        /// <summary>
        /// Deducts MP for an ability. Clamps to 0.
        /// </summary>
        public void ConsumeMP(int amount)
        {
            CurrentMP = Math.Max(0, CurrentMP - amount);
        }

        /// <summary>
        /// Activates the cooldown for an ability after use.
        /// </summary>
        public void ActivateCooldown(AbilityData ability)
        {
            if (ability != null && ability.Cooldown > 0)
                _cooldowns[ability.Id] = ability.Cooldown;
        }

        /// <summary>
        /// Gets remaining cooldown turns for an ability (0 = ready).
        /// </summary>
        public int GetCooldownRemaining(AbilityData ability)
        {
            if (ability == null) return 0;
            return _cooldowns.TryGetValue(ability.Id, out int remaining) ? remaining : 0;
        }

        /// <summary>
        /// Decrements all cooldowns by 1. Called at start of combatant's turn.
        /// </summary>
        public void TickCooldowns()
        {
            var expired = new List<string>();
            foreach (var kvp in _cooldowns)
            {
                if (kvp.Value <= 1)
                    expired.Add(kvp.Key);
            }
            foreach (var key in expired)
                _cooldowns.Remove(key);

            var keys = new List<string>(_cooldowns.Keys);
            foreach (var key in keys)
                _cooldowns[key] = _cooldowns[key] - 1;
        }

        // ====================================================================
        // STATUS EFFECTS
        // ====================================================================

        /// <summary>
        /// Checks if this combatant has a specific status effect active.
        /// </summary>
        public bool HasStatus(StatusEffect effect)
        {
            for (int i = 0; i < StatusEffects.Count; i++)
            {
                if (StatusEffects[i].Effect == effect)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Applies a status effect. Same type refreshes duration (no stacking).
        /// </summary>
        public void ApplyStatus(StatusInstance status)
        {
            for (int i = 0; i < StatusEffects.Count; i++)
            {
                if (StatusEffects[i].Effect == status.Effect)
                {
                    StatusEffects[i].RemainingTurns = status.RemainingTurns;
                    StatusEffects[i].Param = status.Param;
                    return;
                }
            }
            StatusEffects.Add(status);
        }

        /// <summary>
        /// Decrements status durations and removes expired ones.
        /// Returns list of expired effects for event publishing.
        /// Call at the start of the combatant's turn (after buff tick).
        /// </summary>
        public List<StatusEffect> TickStatuses()
        {
            var expired = new List<StatusEffect>();
            for (int i = StatusEffects.Count - 1; i >= 0; i--)
            {
                StatusEffects[i].RemainingTurns--;
                if (StatusEffects[i].RemainingTurns <= 0)
                {
                    expired.Add(StatusEffects[i].Effect);
                    StatusEffects.RemoveAt(i);
                }
            }
            return expired;
        }

        /// <summary>
        /// Removes a specific status effect if present.
        /// </summary>
        public bool RemoveStatus(StatusEffect effect)
        {
            for (int i = 0; i < StatusEffects.Count; i++)
            {
                if (StatusEffects[i].Effect == effect)
                {
                    StatusEffects.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
}
