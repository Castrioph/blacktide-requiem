using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for ShipData, ShipStatBlock, crew contribution, and upgrade calculations.
    /// Covers S2-08 acceptance criteria.
    /// </summary>
    [TestFixture]
    public class ShipDataTests
    {
        private ShipData _sloop;

        [SetUp]
        public void SetUp()
        {
            _sloop = ScriptableObject.CreateInstance<ShipData>();
            _sloop.ShipId = "ship_sloop_01";
            _sloop.DisplayName = "Sloop Starter";
            _sloop.Element = Element.Neutral;
            _sloop.BaseStats = new ShipStatBlock
            {
                HHP = 500, FPW = 100, HDF = 80, MST = 60, MP = 50, RSL = 70, SPD = 90
            };
            _sloop.RoleSlots = new List<RoleSlot>
            {
                new RoleSlot { SlotIndex = 0, Role = NavalRole.Capitan },
                new RoleSlot { SlotIndex = 1, Role = NavalRole.Artillero },
                new RoleSlot { SlotIndex = 2, Role = NavalRole.Carpintero },
                new RoleSlot { SlotIndex = 3, Role = NavalRole.Artillero },
                new RoleSlot { SlotIndex = 4, Role = NavalRole.Navegante, IsGuestSlot = true }
            };
            _sloop.BaseAbilities = new List<AbilityData>();
            _sloop.Acquisition = ShipAcquisition.Story;
        }

        // ====================================================================
        // SHIP DATA FIELDS
        // ====================================================================

        [Test]
        public void test_ship_data_fields_populated()
        {
            Assert.AreEqual("ship_sloop_01", _sloop.ShipId);
            Assert.AreEqual("Sloop Starter", _sloop.DisplayName);
            Assert.AreEqual(Element.Neutral, _sloop.Element);
            Assert.AreEqual(5, _sloop.RoleSlots.Count);
            Assert.AreEqual(ShipAcquisition.Story, _sloop.Acquisition);
        }

        [Test]
        public void test_ship_stat_block_indexer()
        {
            var stats = _sloop.BaseStats;

            Assert.AreEqual(500f, stats[(int)ShipStatType.HHP]);
            Assert.AreEqual(100f, stats[(int)ShipStatType.FPW]);
            Assert.AreEqual(80f, stats[(int)ShipStatType.HDF]);
            Assert.AreEqual(60f, stats[(int)ShipStatType.MST]);
            Assert.AreEqual(50f, stats[(int)ShipStatType.MP]);
            Assert.AreEqual(70f, stats[(int)ShipStatType.RSL]);
            Assert.AreEqual(90f, stats[(int)ShipStatType.SPD]);
        }

        [Test]
        public void test_ship_role_slot_guest_designation()
        {
            Assert.IsFalse(_sloop.RoleSlots[0].IsGuestSlot);
            Assert.IsTrue(_sloop.RoleSlots[4].IsGuestSlot);
            Assert.AreEqual(NavalRole.Navegante, _sloop.RoleSlots[4].Role);
        }

        // ====================================================================
        // UPGRADE BONUS CALCULATION
        // ====================================================================

        [Test]
        public void test_ship_upgrade_bonus_nonlinear_progression()
        {
            // Hull Level 0 → +0%
            var upgrades0 = new ShipUpgradeState { HullLevel = 0 };
            Assert.AreEqual(0f, _sloop.GetUpgradeBonus(ShipStatType.HHP, upgrades0));

            // Hull Level 1 → +10% of 500 = 50
            var upgrades1 = new ShipUpgradeState { HullLevel = 1 };
            Assert.AreEqual(50f, _sloop.GetUpgradeBonus(ShipStatType.HHP, upgrades1));

            // Hull Level 2 → +25% of 500 = 125
            var upgrades2 = new ShipUpgradeState { HullLevel = 2 };
            Assert.AreEqual(125f, _sloop.GetUpgradeBonus(ShipStatType.HHP, upgrades2));

            // Hull Level 3 → +45% of 500 = 225
            var upgrades3 = new ShipUpgradeState { HullLevel = 3 };
            Assert.AreEqual(225f, _sloop.GetUpgradeBonus(ShipStatType.HHP, upgrades3));
        }

        [Test]
        public void test_ship_upgrade_bonus_per_component()
        {
            // Cannons Level 2 → FPW: +25% of 100 = 25
            var upgrades = new ShipUpgradeState { CannonsLevel = 2 };
            Assert.AreEqual(25f, _sloop.GetUpgradeBonus(ShipStatType.FPW, upgrades));
            Assert.AreEqual(15f, _sloop.GetUpgradeBonus(ShipStatType.MST, upgrades)); // 25% of 60

            // Sails Level 1 → SPD: +10% of 90 = 9
            var sailUpgrades = new ShipUpgradeState { SailsLevel = 1 };
            Assert.AreEqual(9f, _sloop.GetUpgradeBonus(ShipStatType.SPD, sailUpgrades));
            Assert.AreEqual(7f, _sloop.GetUpgradeBonus(ShipStatType.RSL, sailUpgrades)); // 10% of 70
        }

        // ====================================================================
        // CREW CONTRIBUTION — ROLE MATCHING
        // ====================================================================

        [Test]
        public void test_ship_crew_contribution_matching_role()
        {
            // Artillero with ATK=200, role matches → FPW contribution
            // floor(200 * 0.15) = 30
            int contribution = ShipData.CalculateCrewContribution(
                200f, NavalRole.Artillero, NavalRole.Artillero, ShipStatType.FPW);
            Assert.AreEqual(30, contribution);
        }

        [Test]
        public void test_ship_crew_contribution_mismatched_role()
        {
            // Unit is Navegante but assigned to Artillero slot → mismatch penalty
            // floor(floor(150 * 0.15) * 0.50) = floor(22 * 0.50) = 11
            int contribution = ShipData.CalculateCrewContribution(
                150f, NavalRole.Artillero, NavalRole.Navegante, ShipStatType.FPW);
            Assert.AreEqual(11, contribution);
        }

        [Test]
        public void test_ship_crew_contribution_zero_for_non_role_stat()
        {
            // Carpintero contributes to HHP + HDF, NOT FPW
            int contribution = ShipData.CalculateCrewContribution(
                200f, NavalRole.Carpintero, NavalRole.Carpintero, ShipStatType.FPW);
            Assert.AreEqual(0, contribution);
        }

        [Test]
        public void test_ship_crew_contribution_zero_unit_stat()
        {
            // Unit with 0 ATK → 0 contribution to FPW even with matching role
            int contribution = ShipData.CalculateCrewContribution(
                0f, NavalRole.Artillero, NavalRole.Artillero, ShipStatType.FPW);
            Assert.AreEqual(0, contribution);
        }

        // ====================================================================
        // ROLE → STAT MAPPING
        // ====================================================================

        [Test]
        public void test_ship_role_stat_mapping()
        {
            Assert.AreEqual((ShipStatType.FPW, ShipStatType.SPD), ShipData.GetRoleStats(NavalRole.Capitan));
            Assert.AreEqual((ShipStatType.MST, ShipStatType.RSL), ShipData.GetRoleStats(NavalRole.Intendente));
            Assert.AreEqual((ShipStatType.FPW, ShipStatType.HDF), ShipData.GetRoleStats(NavalRole.Artillero));
            Assert.AreEqual((ShipStatType.SPD, ShipStatType.RSL), ShipData.GetRoleStats(NavalRole.Navegante));
            Assert.AreEqual((ShipStatType.HHP, ShipStatType.HDF), ShipData.GetRoleStats(NavalRole.Carpintero));
            Assert.AreEqual((ShipStatType.HHP, ShipStatType.RSL), ShipData.GetRoleStats(NavalRole.Cirujano));
            Assert.AreEqual((ShipStatType.HDF, ShipStatType.SPD), ShipData.GetRoleStats(NavalRole.Contramaestre));
        }

        // ====================================================================
        // NAVAL → UNIT STAT MAPPING
        // ====================================================================

        [Test]
        public void test_ship_naval_to_unit_stat_mapping()
        {
            Assert.AreEqual(StatType.HP, ShipData.MapNavalToUnitStat(ShipStatType.HHP));
            Assert.AreEqual(StatType.ATK, ShipData.MapNavalToUnitStat(ShipStatType.FPW));
            Assert.AreEqual(StatType.DEF, ShipData.MapNavalToUnitStat(ShipStatType.HDF));
            Assert.AreEqual(StatType.MST, ShipData.MapNavalToUnitStat(ShipStatType.MST));
            Assert.AreEqual(StatType.SPR, ShipData.MapNavalToUnitStat(ShipStatType.RSL));
            Assert.AreEqual(StatType.SPD, ShipData.MapNavalToUnitStat(ShipStatType.SPD));
        }

        // ====================================================================
        // GDD WORKED EXAMPLE
        // ====================================================================

        [Test]
        public void test_ship_gdd_worked_example_effective_fpw()
        {
            // Reproduces the worked example from Ship Data Model GDD §5
            // Ship Base FPW: 100, Cannon upgrade Level 2: +25
            var upgrades = new ShipUpgradeState { CannonsLevel = 2 };
            float upgradeBonus = _sloop.GetUpgradeBonus(ShipStatType.FPW, upgrades);
            Assert.AreEqual(25f, upgradeBonus);

            // Slot 1 — Artillero (ATK=200, matches): 30
            int slot1 = ShipData.CalculateCrewContribution(200f, NavalRole.Artillero, NavalRole.Artillero, ShipStatType.FPW);
            Assert.AreEqual(30, slot1);

            // Slot 2 — Capitán (ATK=180, matches): 27
            int slot2 = ShipData.CalculateCrewContribution(180f, NavalRole.Capitan, NavalRole.Capitan, ShipStatType.FPW);
            Assert.AreEqual(27, slot2);

            // Slot 3 — Carpintero (doesn't contribute to FPW): 0
            int slot3 = ShipData.CalculateCrewContribution(120f, NavalRole.Carpintero, NavalRole.Carpintero, ShipStatType.FPW);
            Assert.AreEqual(0, slot3);

            // Slot 4 — Artillero slot, unit is Navegante (mismatch): 11
            int slot4 = ShipData.CalculateCrewContribution(150f, NavalRole.Artillero, NavalRole.Navegante, ShipStatType.FPW);
            Assert.AreEqual(11, slot4);

            // Slot 5 — empty: 0
            // Effective FPW = 100 + 25 + 30 + 27 + 0 + 11 + 0 = 193
            float effectiveFPW = 100f + upgradeBonus + slot1 + slot2 + slot3 + slot4;
            Assert.AreEqual(193f, effectiveFPW);
        }
    }
}
