using System.Collections.Generic;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Base data definition shared by all characters (player units and enemies).
    /// Read-only at runtime. Editable in the Unity Inspector.
    ///
    /// This is the most depended-upon data model in the game (12 downstream systems).
    /// See design/gdd/unit-data-model.md for full specification.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Blacktide/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]

        [Tooltip("Unique template identifier (e.g., 'elena_storm')")]
        public string Id;

        [Tooltip("Localized display name")]
        public string DisplayName;

        [Tooltip("Short character bio")]
        [TextArea(2, 4)]
        public string Description;

        [Header("Stats")]

        [Tooltip("Base values for the 7 core stats (before level scaling)")]
        public StatBlock BaseStats;

        [Tooltip("Base values for secondary stats (CRI, LCK)")]
        public SecondaryStatBlock SecondaryStats;

        [Header("Element")]

        [Tooltip("Defensive element — determines weakness/resistance interactions")]
        public Element Element;

        [Header("Abilities")]

        [Tooltip("Abilities available in land combat")]
        public List<AbilityEntry> LandAbilities = new();

        [Tooltip("Abilities available in naval combat")]
        public List<AbilityEntry> SeaAbilities = new();

        [Header("Traits")]

        [Tooltip("1-3 trait entries with per-unit synergy bonuses")]
        public List<UnitTraitEntry> Traits = new();
    }
}
