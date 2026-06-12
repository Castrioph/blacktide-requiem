using System.Collections.Generic;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// A participant whose traits can activate synergies and whose BuffStack
    /// receives the resulting synergy buffs. Implemented by CombatantState
    /// (land units) and, in naval combat, by CrewMemberState — whose buffs
    /// target the ship's BuffStack, not the crew member itself.
    /// See ADR-004 §5.
    /// </summary>
    public interface ITraitCarrier
    {
        /// <summary>Trait entries for synergy evaluation. May be null.</summary>
        IReadOnlyList<UnitTraitEntry> Traits { get; }

        /// <summary>The BuffStack that receives synergy buffs for this carrier.</summary>
        BuffStack SynergyBuffTarget { get; }
    }
}
