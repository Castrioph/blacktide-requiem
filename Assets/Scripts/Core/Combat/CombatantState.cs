using System.Collections.Generic;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Runtime combat state for a single combatant (unit or ship).
    /// Created at battle start, discarded at battle end. Not persisted.
    /// See Damage & Stats Engine GDD §States and ADR-001.
    /// </summary>
    public class CombatantState
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

        public bool IsKO => CurrentHP <= 0;

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
