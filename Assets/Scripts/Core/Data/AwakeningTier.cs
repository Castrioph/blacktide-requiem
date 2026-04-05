using System;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Defines the bonuses and requirements for a single awakening tier.
    /// </summary>
    [Serializable]
    public struct AwakeningTier
    {
        [Tooltip("Flat stat bonuses granted by this awakening tier")]
        public StatBlock StatBonuses;

        [Tooltip("New max level cap after this awakening")]
        [Min(1)]
        public int NewMaxLevel;
    }

    /// <summary>
    /// Container for all awakening tiers of a unit.
    /// 3-star: 2 tiers, 4-star: 2 tiers, 5-star: 3 tiers.
    /// </summary>
    [Serializable]
    public struct AwakeningInfo
    {
        [Tooltip("Awakening tiers in order (1st, 2nd, optionally 3rd)")]
        public AwakeningTier[] Tiers;
    }
}
