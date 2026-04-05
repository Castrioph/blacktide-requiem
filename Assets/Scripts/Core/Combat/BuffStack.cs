using System;
using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// A single buff or debuff instance applied to a combatant.
    /// See Damage & Stats Engine GDD §4.
    /// </summary>
    [Serializable]
    public class BuffInstance
    {
        public StatType StatAffected;

        /// <summary>Magnitude as decimal (e.g., +0.30 = +30%, -0.20 = -20%).</summary>
        public float Percentage;

        /// <summary>Turns remaining. -1 = permanent (until dispelled or combat ends).</summary>
        public int RemainingTurns;

        /// <summary>Source ability for UI display.</summary>
        public string SourceAbilityId;

        /// <summary>Whether offensive/defensive dispel can remove this.</summary>
        public bool IsDispellable;

        public bool IsPermanent => RemainingTurns == -1;
        public bool IsBuff => Percentage > 0f;
        public bool IsDebuff => Percentage < 0f;
    }

    /// <summary>
    /// Manages all buff/debuff instances for a single combatant.
    /// Buffs stack additively per stat, clamped to ±100% (StatModifier range 0.0–2.0).
    /// </summary>
    public class BuffStack
    {
        private readonly List<BuffInstance> _buffs = new();

        public IReadOnlyList<BuffInstance> All => _buffs;

        /// <summary>
        /// Adds a buff/debuff to the stack.
        /// </summary>
        public void Add(BuffInstance buff)
        {
            _buffs.Add(buff);
        }

        /// <summary>
        /// Calculates the effective stat modifier for a given stat.
        /// StatModifier = clamp(1.0 + sum(buffs) - sum(debuffs), 0.0, 2.0)
        /// </summary>
        public float GetStatModifier(StatType stat)
        {
            float total = 0f;
            for (int i = 0; i < _buffs.Count; i++)
            {
                if (_buffs[i].StatAffected == stat)
                    total += _buffs[i].Percentage;
            }
            return Mathf.Clamp(1f + total, 0f, 2f);
        }

        /// <summary>
        /// Decrements turn counters and removes expired buffs.
        /// Call at the start of the combatant's turn (step 1 of turn processing).
        /// </summary>
        public void TickTurns()
        {
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                if (_buffs[i].IsPermanent) continue;

                _buffs[i].RemainingTurns--;
                if (_buffs[i].RemainingTurns <= 0)
                    _buffs.RemoveAt(i);
            }
        }

        /// <summary>
        /// Removes all temporary (dispellable) buffs (positive percentages).
        /// Permanent buffs are not affected.
        /// </summary>
        public int DispelBuffs()
        {
            int removed = 0;
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                if (_buffs[i].IsBuff && _buffs[i].IsDispellable)
                {
                    _buffs.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>
        /// Removes all temporary (dispellable) debuffs (negative percentages).
        /// Permanent debuffs are not affected.
        /// </summary>
        public int PurgeDebuffs()
        {
            int removed = 0;
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                if (_buffs[i].IsDebuff && _buffs[i].IsDispellable)
                {
                    _buffs.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>
        /// Removes all buffs and debuffs.
        /// </summary>
        public void Clear()
        {
            _buffs.Clear();
        }
    }
}
