using UnityEngine;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Result of a healing calculation.
    /// </summary>
    public struct HealResult
    {
        public int HealAmount;
        public int ActualHealed;
    }

    /// <summary>
    /// Pure calculation class for healing.
    /// HealAmount = floor(HealerStat * HealPower * BuffMod)
    /// Healing does not crit. No elemental advantage.
    /// See Damage & Stats Engine GDD §7.
    /// </summary>
    public static class HealCalculator
    {
        /// <summary>
        /// Calculates healing amount.
        /// </summary>
        /// <param name="effectiveMST">Healer's effective MST (after buffs).</param>
        /// <param name="healPower">Ability heal multiplier.</param>
        public static int Calculate(float effectiveMST, float healPower)
        {
            return Mathf.Max(Mathf.FloorToInt(effectiveMST * healPower), 1);
        }
    }
}
