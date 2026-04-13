using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Global trait definition (shared across all units that have this trait).
    /// Defines identity and display info — per-unit bonus values live in UnitTraitEntry.
    /// See Traits/Sinergias GDD §1 (TraitDefinition).
    /// </summary>
    [CreateAssetMenu(fileName = "NewTrait", menuName = "Blacktide/Trait Data")]
    public class TraitData : ScriptableObject
    {
        [Tooltip("Unique identifier (e.g., 'hijos_del_mar')")]
        public string TraitId;

        [Tooltip("Localized display name (e.g., 'Hijos del Mar')")]
        public string DisplayName;

        [Tooltip("Flavor text describing the faction/origin")]
        [TextArea(2, 4)]
        public string Description;

        [Tooltip("Trait icon displayed on unit cards and synergy UI")]
        public Sprite Icon;

        [Tooltip("Trait classification (Faction for demo)")]
        public TraitCategory Category;
    }
}
