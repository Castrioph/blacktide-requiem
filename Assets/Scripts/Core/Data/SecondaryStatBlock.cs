using System;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Secondary stats: percentage/multiplier values that scale slower than core stats.
    /// Primarily influenced by equipment and traits.
    /// </summary>
    [Serializable]
    public struct SecondaryStatBlock
    {
        [Tooltip("Critical Rate — % chance of dealing a critical hit (no hard cap)")]
        [Range(0f, 100f)]
        public float CRI;

        [Tooltip("Luck — affects drop rates, bonus CRI, and evasion (hard cap: 100)")]
        [Range(0f, 100f)]
        public float LCK;
    }
}
