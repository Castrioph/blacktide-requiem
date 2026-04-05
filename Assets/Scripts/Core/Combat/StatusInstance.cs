using System;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// A single active status effect on a combatant.
    /// Same status type does not stack — refreshes duration instead.
    /// Different DoTs (Veneno, Sangrado, Quemadura) stack with each other.
    /// See Damage & Stats Engine GDD §6.
    /// </summary>
    [Serializable]
    public class StatusInstance
    {
        public StatusEffect Effect;

        /// <summary>Turns remaining. Decremented per turn.</summary>
        public int RemainingTurns;

        /// <summary>Magnitude parameter (e.g., DOT_PERCENT for DoTs, threshold for Muerte).</summary>
        public float Param;

        /// <summary>Source ability for UI display.</summary>
        public string SourceAbilityId;
    }
}
