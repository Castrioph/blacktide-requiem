using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Per-unit trait assignment with individual synergy bonus values.
    /// Stored in CharacterData.Traits. The bonus values are defined per unit,
    /// not per trait globally — this enables controlled power creep on newer units.
    /// See Traits/Sinergias GDD §1 (UnitTraitEntry).
    /// </summary>
    [Serializable]
    public struct UnitTraitEntry
    {
        [Tooltip("Reference to the global trait definition")]
        public TraitData Trait;

        [Tooltip("Stat bonuses this unit contributes when the synergy is active. " +
                 "Empty = unit counts toward threshold but receives no buff (enabler unit).")]
        public List<StatModifier> SynergyBonus;
    }
}
