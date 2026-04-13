using System;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// The 7 naval stats for ships. Mirrors StatBlock structure for units.
    /// See Ship Data Model GDD §1.
    /// </summary>
    [Serializable]
    public struct ShipStatBlock
    {
        [Tooltip("Hull HP — ship health pool, sinks at 0")]
        public float HHP;

        [Tooltip("Firepower — base cannon/weapon damage")]
        public float FPW;

        [Tooltip("Hull Defense — physical damage reduction")]
        public float HDF;

        [Tooltip("Mística — magical damage output")]
        public float MST;

        [Tooltip("Magic Points — resource for ship abilities")]
        public float MP;

        [Tooltip("Resilience — special/magical damage reduction")]
        public float RSL;

        [Tooltip("Sail Speed — turn order on naval initiative bar")]
        public float SPD;

        /// <summary>
        /// Returns the stat value by ShipStatType index.
        /// </summary>
        public float this[int index]
        {
            get => index switch
            {
                0 => HHP,
                1 => FPW,
                2 => HDF,
                3 => MST,
                4 => MP,
                5 => RSL,
                6 => SPD,
                _ => throw new IndexOutOfRangeException($"ShipStatBlock index {index} out of range (0-6)")
            };
            set
            {
                switch (index)
                {
                    case 0: HHP = value; break;
                    case 1: FPW = value; break;
                    case 2: HDF = value; break;
                    case 3: MST = value; break;
                    case 4: MP = value; break;
                    case 5: RSL = value; break;
                    case 6: SPD = value; break;
                    default: throw new IndexOutOfRangeException($"ShipStatBlock index {index} out of range (0-6)");
                }
            }
        }

        /// <summary>Number of stats in the block.</summary>
        public const int COUNT = 7;
    }
}
