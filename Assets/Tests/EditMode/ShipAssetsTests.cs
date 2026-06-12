using NUnit.Framework;
using UnityEditor;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Validates the authored S4-02 ship assets (Ship Data Model GDD AC 1):
    /// 1 allied ship, 3 enemy ships (Normal/Elite/Jefe), 1 sea creature.
    /// Guards against asset corruption/renaming breaking the naval demo data.
    /// </summary>
    [TestFixture]
    public class ShipAssetsTests
    {
        private const string Dir = "Assets/Data/Ships";

        private static ShipData Load(string id)
        {
            var ship = AssetDatabase.LoadAssetAtPath<ShipData>($"{Dir}/{id}.asset");
            Assert.IsNotNull(ship, $"Missing ship asset: {Dir}/{id}.asset");
            return ship;
        }

        [Test]
        public void test_ship_assets_allied_starter_loads_with_5_slots_and_guest()
        {
            // Arrange / Act
            var ship = Load("ship_marea_espectral");

            // Assert
            Assert.AreEqual("ship_marea_espectral", ship.ShipId);
            Assert.AreEqual(ShipAcquisition.Story, ship.Acquisition);
            Assert.AreEqual(5, ship.RoleSlots.Count);
            Assert.AreEqual(NavalRole.Capitan, ship.RoleSlots[0].Role);
            Assert.IsTrue(ship.RoleSlots[4].IsGuestSlot, "Slot 4 must be the guest slot");
            Assert.AreEqual(500f, ship.BaseStats.HHP);
            Assert.AreEqual(90f, ship.BaseStats.SPD);
        }

        [Test]
        public void test_ship_assets_three_enemy_ships_scale_by_tier()
        {
            // Arrange / Act — Normal < Elite < Jefe in hull and firepower
            var normal = Load("ship_balandra_corsaria");
            var elite = Load("ship_bergantin_maldito");
            var boss = Load("ship_galeon_del_requiem");

            // Assert
            Assert.Less(normal.BaseStats.HHP, elite.BaseStats.HHP);
            Assert.Less(elite.BaseStats.HHP, boss.BaseStats.HHP);
            Assert.Less(normal.BaseStats.FPW, elite.BaseStats.FPW);
            Assert.Less(elite.BaseStats.FPW, boss.BaseStats.FPW);
            Assert.AreEqual(3, normal.RoleSlots.Count);
            Assert.AreEqual(5, elite.RoleSlots.Count);
            Assert.AreEqual(7, boss.RoleSlots.Count);
        }

        [Test]
        public void test_ship_assets_sea_creature_has_no_crew_slots()
        {
            // Arrange / Act — Combate Naval GDD: creatures have no crew (no boarding)
            var creature = Load("creature_serpiente_abisal");

            // Assert
            Assert.AreEqual(0, creature.RoleSlots.Count);
            Assert.AreEqual(Element.Bestia, creature.Element);
        }

        [Test]
        public void test_ship_assets_all_construct_as_ship_combatants()
        {
            // Arrange
            string[] ids =
            {
                "ship_marea_espectral", "ship_balandra_corsaria",
                "ship_bergantin_maldito", "ship_galeon_del_requiem",
                "creature_serpiente_abisal"
            };

            foreach (var id in ids)
            {
                // Act — every authored asset must produce a functional combatant
                var ship = new ShipCombatant(Load(id), default);

                // Assert
                Assert.Greater(ship.MaxHHP, 0, $"{id} must have positive hull HP");
                Assert.IsFalse(ship.IsKO, $"{id} must start afloat");
                Assert.Greater(ship.GetEffectiveShipStat(ShipStatType.SPD), 0f,
                    $"{id} must have positive SPD for the initiative bar");
            }
        }
    }
}
