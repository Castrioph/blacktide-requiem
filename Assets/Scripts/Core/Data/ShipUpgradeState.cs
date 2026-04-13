using System;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Tracks upgrade levels for the 3 ship components.
    /// Each component has levels 0-3 with non-linear bonuses.
    /// See Ship Data Model GDD §6.
    /// </summary>
    [Serializable]
    public struct ShipUpgradeState
    {
        [Tooltip("Hull upgrade level (0-3). Affects HHP + HDF.")]
        [Range(0, 3)]
        public int HullLevel;

        [Tooltip("Cannons upgrade level (0-3). Affects FPW + MST + MP.")]
        [Range(0, 3)]
        public int CannonsLevel;

        [Tooltip("Sails upgrade level (0-3). Affects SPD + RSL.")]
        [Range(0, 3)]
        public int SailsLevel;
    }
}
