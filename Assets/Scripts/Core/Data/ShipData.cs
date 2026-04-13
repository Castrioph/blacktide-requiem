using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Definition of a single ship — the "mega-unit" of naval combat.
    /// ScriptableObject — created as assets, referenced by fleet/roster systems.
    ///
    /// Ships have their own stats (ShipStatBlock), variable role slots for crew,
    /// and an ability pool enhanced by crew contributions.
    /// See design/gdd/ship-data-model.md for full specification.
    /// </summary>
    [CreateAssetMenu(fileName = "NewShip", menuName = "Blacktide/Ship Data")]
    public class ShipData : ScriptableObject
    {
        [Header("Identity")]

        [Tooltip("Unique ship identifier (e.g., 'ship_sloop_01')")]
        public string ShipId;

        [Tooltip("Localized display name")]
        public string DisplayName;

        [TextArea(2, 4)]
        [Tooltip("Flavor text")]
        public string Description;

        [Header("Stats")]

        [Tooltip("Base naval stats at upgrade level 0")]
        public ShipStatBlock BaseStats;

        [Header("Element")]

        [Tooltip("Defensive element — determines weakness/resistance in naval combat")]
        public Element Element;

        [Header("Crew Slots")]

        [Tooltip("Role slots available on this ship (5-7 demo, up to 10+ full game)")]
        public List<RoleSlot> RoleSlots = new();

        [Header("Abilities")]

        [Tooltip("Innate ship abilities (always available regardless of crew)")]
        public List<AbilityData> BaseAbilities = new();

        [Header("Acquisition")]

        [Tooltip("How this ship is acquired")]
        public ShipAcquisition Acquisition;

        // ====================================================================
        // UPGRADE BONUS CALCULATION
        // ====================================================================

        /// <summary>Non-linear upgrade bonus percentages per level [0, 1, 2, 3].</summary>
        public static readonly float[] UpgradePercent = { 0f, 0.10f, 0.25f, 0.45f };

        /// <summary>
        /// Calculates the upgrade bonus for a specific stat given upgrade state.
        /// See Ship Data Model GDD §4.
        /// </summary>
        public float GetUpgradeBonus(ShipStatType stat, ShipUpgradeState upgrades)
        {
            float baseStat = BaseStats[(int)stat];
            float percent = stat switch
            {
                // Hull: HHP, HDF
                ShipStatType.HHP => UpgradePercent[Mathf.Clamp(upgrades.HullLevel, 0, 3)],
                ShipStatType.HDF => UpgradePercent[Mathf.Clamp(upgrades.HullLevel, 0, 3)],
                // Cannons: FPW, MST, MP
                ShipStatType.FPW => UpgradePercent[Mathf.Clamp(upgrades.CannonsLevel, 0, 3)],
                ShipStatType.MST => UpgradePercent[Mathf.Clamp(upgrades.CannonsLevel, 0, 3)],
                ShipStatType.MP  => UpgradePercent[Mathf.Clamp(upgrades.CannonsLevel, 0, 3)],
                // Sails: SPD, RSL
                ShipStatType.SPD => UpgradePercent[Mathf.Clamp(upgrades.SailsLevel, 0, 3)],
                ShipStatType.RSL => UpgradePercent[Mathf.Clamp(upgrades.SailsLevel, 0, 3)],
                _ => 0f
            };
            return Mathf.Floor(baseStat * percent);
        }

        // ====================================================================
        // CREW CONTRIBUTION
        // ====================================================================

        /// <summary>Percentage of unit stat contributed to ship.</summary>
        public const float CREW_SCALING_FACTOR = 0.15f;

        /// <summary>Multiplier when unit role doesn't match slot role.</summary>
        public const float MISMATCH_PENALTY = 0.50f;

        /// <summary>
        /// Maps each NavalRole to the 2 ShipStatTypes it contributes to.
        /// See Ship Data Model GDD §2.
        /// </summary>
        public static (ShipStatType, ShipStatType) GetRoleStats(NavalRole role)
        {
            return role switch
            {
                NavalRole.Capitan       => (ShipStatType.FPW, ShipStatType.SPD),
                NavalRole.Intendente    => (ShipStatType.MST, ShipStatType.RSL),
                NavalRole.Artillero     => (ShipStatType.FPW, ShipStatType.HDF),
                NavalRole.Navegante     => (ShipStatType.SPD, ShipStatType.RSL),
                NavalRole.Carpintero    => (ShipStatType.HHP, ShipStatType.HDF),
                NavalRole.Cirujano      => (ShipStatType.HHP, ShipStatType.RSL),
                NavalRole.Contramaestre => (ShipStatType.HDF, ShipStatType.SPD),
                _ => (ShipStatType.HHP, ShipStatType.HHP)
            };
        }

        /// <summary>
        /// Maps a ShipStatType to the unit StatType used for crew contribution.
        /// See Ship Data Model GDD §Formulas §2.
        /// </summary>
        public static StatType MapNavalToUnitStat(ShipStatType navalStat)
        {
            return navalStat switch
            {
                ShipStatType.HHP => StatType.HP,
                ShipStatType.FPW => StatType.ATK,
                ShipStatType.HDF => StatType.DEF,
                ShipStatType.MST => StatType.MST,
                ShipStatType.RSL => StatType.SPR,
                ShipStatType.SPD => StatType.SPD,
                _ => StatType.HP // MP has no mapping
            };
        }

        /// <summary>
        /// Calculates crew contribution for one stat from one filled slot.
        /// Returns 0 for MP (no unit mapping) or if unitStat is 0.
        /// See Ship Data Model GDD §Formulas §1.
        /// </summary>
        public static int CalculateCrewContribution(
            float unitStat, NavalRole slotRole, NavalRole unitAffinity,
            ShipStatType targetStat)
        {
            // Check if this role contributes to the target stat
            var (stat1, stat2) = GetRoleStats(slotRole);
            if (targetStat != stat1 && targetStat != stat2)
                return 0;

            // MP has no unit stat mapping
            if (targetStat == ShipStatType.MP)
                return 0;

            int baseContribution = Mathf.FloorToInt(unitStat * CREW_SCALING_FACTOR);

            if (slotRole != unitAffinity)
                baseContribution = Mathf.FloorToInt(baseContribution * MISMATCH_PENALTY);

            return baseContribution;
        }
    }
}
