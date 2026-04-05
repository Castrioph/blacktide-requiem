using System;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Reference to an ability definition within a unit's ability pool.
    /// Ability details (MP cost, damage, targeting) live in the ability data asset,
    /// not here. This struct defines ownership and unlock conditions.
    /// </summary>
    [Serializable]
    public struct AbilityEntry
    {
        [Tooltip("Reference to the ability definition (e.g., 'fireball')")]
        public string AbilityId;

        [Tooltip("Level at which this ability becomes available (1 = immediate)")]
        [Min(1)]
        public int UnlockLevel;

        [Tooltip("How the ability was acquired")]
        public AbilitySource Source;

        [Tooltip("Whether this ability can trigger a Limit Break (extra turn)")]
        public bool CanLimitBreak;

        [Tooltip("Condition type for LB activation. Only used if CanLimitBreak is true.")]
        public LBCondition LBCondition;

        [Tooltip("Numeric threshold for LB conditions (e.g., HP < 0.30 for OnLowHP). -1 if not applicable.")]
        public float LBConditionParam;

        [Tooltip("String reference for LB conditions (e.g., 'Quemadura' for OnStatusTarget). Empty if not applicable.")]
        public string LBConditionTarget;
    }
}
