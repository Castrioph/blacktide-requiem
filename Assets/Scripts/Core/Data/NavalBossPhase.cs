using System;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// One phase of a naval boss behavior tree. The phase becomes active when
    /// the boss hull HP fraction drops below HPThreshold. Phases are
    /// one-directional: healing above the threshold never reverts the phase.
    /// Author phases in descending threshold order (e.g., 0.75, 0.40).
    /// See Enemy System GDD §3 and Combate Naval GDD §4.
    /// </summary>
    [Serializable]
    public struct NavalBossPhase
    {
        [Tooltip("Phase activates when HHP fraction drops below this value (e.g., 0.5 = below 50%)")]
        [Range(0f, 1f)]
        public float HPThreshold;

        [Tooltip("AI profile the boss uses while this phase is active")]
        public AI.AIProfileType Profile;
    }
}
