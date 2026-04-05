using System.Collections.Generic;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Player-owned unit data. Extends CharacterData with progression,
    /// rarity, equipment slots, and gacha metadata.
    ///
    /// See design/gdd/unit-data-model.md for full specification.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnit", menuName = "Blacktide/Unit Data")]
    public class UnitData : CharacterData
    {
        [Header("Rarity")]

        [Tooltip("Base rarity tier — affects stat growth curves and ability pool size")]
        public Rarity Rarity;

        [Header("Progression")]

        [Tooltip("Base level cap (before awakening)")]
        [Min(1)]
        public int MaxLevel = 40;

        [Tooltip("Per-level stat growth rates for core stats")]
        public StatBlock StatGrowth;

        [Tooltip("Per-level growth rates for secondary stats")]
        public SecondaryStatBlock SecondaryStatGrowth;

        [Tooltip("Awakening tiers, stat bonuses, and level cap increases")]
        public AwakeningInfo AwakeningData;

        [Header("Naval")]

        [Tooltip("Ship roles this unit can fill (typically 1, rarely 2)")]
        public List<NavalRole> NavalRoleAffinity = new();

        [Header("Gacha")]

        [Tooltip("Which gacha banner pool(s) this unit appears in")]
        public string GachaPool;
    }
}
