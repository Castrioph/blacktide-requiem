using System;
using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.Combat;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Definition of a single ability (land or naval combat).
    /// ScriptableObject — created as assets, referenced by AbilityEntry in CharacterData.
    ///
    /// Defines power, element, costs, targeting, and secondary effects.
    /// CombatManager resolves abilities using this data.
    /// See Combate Terrestre GDD §6 and ADR-003.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "Blacktide/Ability Data")]
    public class AbilityData : ScriptableObject
    {
        [Header("Identity")]

        [Tooltip("Unique ability identifier (e.g., 'rayo_tormenta')")]
        public string Id;

        [Tooltip("Localized display name shown in combat UI")]
        public string DisplayName;

        [TextArea(2, 4)]
        [Tooltip("Short description of what the ability does")]
        public string Description;

        [Header("Combat Properties")]

        [Tooltip("Damage/heal multiplier (1.0 = basic attack equivalent)")]
        [Min(0f)]
        public float AbilityPower = 1.0f;

        [Tooltip("Offensive element for damage calculation")]
        public Element Element = Element.Neutral;

        [Tooltip("True = ATK vs DEF (physical), false = MST vs SPR (magical)")]
        public bool IsPhysical = true;

        [Tooltip("How this ability selects targets")]
        public TargetType TargetType = TargetType.SingleEnemy;

        [Tooltip("Ability category determines resolution path")]
        public AbilityCategory Category = AbilityCategory.Damage;

        [Header("Costs")]

        [Tooltip("MP consumed on use")]
        [Min(0)]
        public int MPCost;

        [Tooltip("Cooldown in combatant turns after use (0 = no cooldown)")]
        [Min(0)]
        public int Cooldown;

        [Header("Heal / Revive (only if Category = Heal or Revive)")]

        [Tooltip("Heal multiplier on MST (Category = Heal)")]
        [Min(0f)]
        public float HealPower;

        [Tooltip("Fraction of MaxHP restored on revive (Category = Revive)")]
        [Range(0f, 1f)]
        public float ReviveHPPercent = 0.30f;

        [Header("Secondary Effects")]

        [Tooltip("Status effects applied to targets after resolution")]
        public List<AbilitySecondaryEffect> SecondaryEffects = new();
    }

    /// <summary>
    /// Ability type — determines which resolution path CombatManager uses.
    /// </summary>
    public enum AbilityCategory
    {
        /// <summary>Deals damage to enemies.</summary>
        Damage,

        /// <summary>Restores HP to allies.</summary>
        Heal,

        /// <summary>Applies buff to allies.</summary>
        Buff,

        /// <summary>Applies debuff to enemies.</summary>
        Debuff,

        /// <summary>Revives a dead ally with partial HP.</summary>
        Revive
    }

    /// <summary>
    /// A secondary effect that an ability can apply after its main resolution.
    /// Each effect has an independent probability roll.
    /// See Combate Terrestre GDD §6 step 3d.
    /// </summary>
    [Serializable]
    public struct AbilitySecondaryEffect
    {
        [Tooltip("Status effect to apply")]
        public StatusEffect Effect;

        [Tooltip("Chance to apply (0.0–1.0)")]
        [Range(0f, 1f)]
        public float Probability;

        [Tooltip("Duration in turns")]
        [Min(1)]
        public int Duration;

        [Tooltip("Magnitude (DoT percent for Veneno/Sangrado/Quemadura, threshold for Muerte)")]
        public float Param;
    }
}
