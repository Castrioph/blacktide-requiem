using System;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// The 7 core stats shared by all characters (units and enemies).
    /// Used for both base values and per-level growth rates.
    /// </summary>
    [Serializable]
    public struct StatBlock
    {
        [Tooltip("Health Points — how much damage the character can take")]
        public float HP;

        [Tooltip("Magic Points — resource spent to use abilities")]
        public float MP;

        [Tooltip("Attack — physical damage output")]
        public float ATK;

        [Tooltip("Defense — physical damage reduction")]
        public float DEF;

        [Tooltip("Mistica — magical damage output")]
        public float MST;

        [Tooltip("Spirit — magical damage reduction")]
        public float SPR;

        [Tooltip("Speed — turn order position on the initiative bar")]
        public float SPD;

        /// <summary>
        /// Returns the stat value by index (0=HP, 1=MP, 2=ATK, 3=DEF, 4=MST, 5=SPR, 6=SPD).
        /// </summary>
        public float this[int index]
        {
            get => index switch
            {
                0 => HP,
                1 => MP,
                2 => ATK,
                3 => DEF,
                4 => MST,
                5 => SPR,
                6 => SPD,
                _ => throw new IndexOutOfRangeException($"StatBlock index {index} out of range (0-6)")
            };
            set
            {
                switch (index)
                {
                    case 0: HP = value; break;
                    case 1: MP = value; break;
                    case 2: ATK = value; break;
                    case 3: DEF = value; break;
                    case 4: MST = value; break;
                    case 5: SPR = value; break;
                    case 6: SPD = value; break;
                    default: throw new IndexOutOfRangeException($"StatBlock index {index} out of range (0-6)");
                }
            }
        }

        /// <summary>Number of stats in the block.</summary>
        public const int COUNT = 7;
    }
}
