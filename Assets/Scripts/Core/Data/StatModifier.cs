using System;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// A single stat + percentage modifier pair used by synergy bonuses.
    /// E.g., { Stat = ATK, Percent = 0.12f } means +12% ATK.
    /// See Traits/Sinergias GDD §1 (UnitTraitEntry.SynergyBonus).
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        [Tooltip("Which stat this modifier affects")]
        public StatType Stat;

        [Tooltip("Percentage as decimal (0.12 = +12%)")]
        public float Percent;
    }
}
