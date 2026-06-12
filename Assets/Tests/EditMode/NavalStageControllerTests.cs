using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BlacktideRequiem.Core.AI;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Stage;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for NavalStageController (S4-07): builds a naval BattleConfig
    /// from NavalStageData — crewed ally ship, per-enemy NavalEnemyAI
    /// (bosses carry phase state), enemy crews from the stage pool, and
    /// creatures without crew.
    /// </summary>
    [TestFixture]
    public class NavalStageControllerTests
    {
        private readonly List<Object> _toDestroy = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _toDestroy)
                Object.DestroyImmediate(obj);
            _toDestroy.Clear();
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private ShipData MakeShipData(string id, bool withCrewSlots = true,
            EnemyTier tier = EnemyTier.Normal)
        {
            var ship = ScriptableObject.CreateInstance<ShipData>();
            _toDestroy.Add(ship);
            ship.ShipId = id;
            ship.DisplayName = id;
            ship.Element = Element.Neutral;
            ship.BaseStats = new ShipStatBlock
            {
                HHP = 500, FPW = 100, HDF = 80, MST = 60, MP = 50, RSL = 70, SPD = 60
            };
            ship.Tier = tier;
            ship.AIProfile = AIProfileType.Agresivo;
            ship.RoleSlots = withCrewSlots
                ? new List<RoleSlot>
                {
                    new RoleSlot { SlotIndex = 0, Role = NavalRole.Capitan },
                    new RoleSlot { SlotIndex = 1, Role = NavalRole.Artillero },
                    new RoleSlot { SlotIndex = 2, Role = NavalRole.Navegante }
                }
                : new List<RoleSlot>();
            ship.BaseAbilities = new List<AbilityData>();
            return ship;
        }

        private UnitData MakeUnit(string id)
        {
            var unit = ScriptableObject.CreateInstance<UnitData>();
            _toDestroy.Add(unit);
            unit.Id = id;
            unit.DisplayName = id;
            unit.Element = Element.Neutral;
            unit.BaseStats = new StatBlock
            {
                HP = 300, MP = 50, ATK = 100, DEF = 40, MST = 60, SPR = 45, SPD = 70
            };
            unit.NavalRoleAffinity = new List<NavalRole>();
            return unit;
        }

        private NavalStageData MakeStage(ShipData playerShip,
            params NavalWaveDefinition[] waves)
        {
            var stage = ScriptableObject.CreateInstance<NavalStageData>();
            _toDestroy.Add(stage);
            stage.Id = "stage_test_naval";
            stage.PlayerShip = playerShip;
            stage.NavalWaves = new List<NavalWaveDefinition>(waves);
            stage.EnemyCrewPool = new List<CharacterData> { MakeUnit("enemy_crew") };
            return stage;
        }

        private static NavalWaveDefinition Wave(params ShipData[] ships)
        {
            return new NavalWaveDefinition { Ships = new List<ShipData>(ships) };
        }

        private IReadOnlyList<CharacterData> MakeCrew(int count)
        {
            var crew = new List<CharacterData>();
            for (int i = 0; i < count; i++)
                crew.Add(MakeUnit($"ally_crew_{i}"));
            return crew;
        }

        // ====================================================================
        // TESTS
        // ====================================================================

        [Test]
        public void test_naval_stage_build_creates_single_crewed_ally_ship()
        {
            // Arrange
            var stage = MakeStage(MakeShipData("player"), Wave(MakeShipData("enemy")));

            // Act
            var setup = NavalStageController.BuildNavalBattle(stage, MakeCrew(3));

            // Assert
            Assert.AreEqual(1, setup.Config.Allies.Count);
            Assert.AreSame(setup.AllyShip, setup.Config.Allies[0].Combatant);
            Assert.AreEqual(3, setup.AllyShip.Crew.Count);
        }

        [Test]
        public void test_naval_stage_build_cycles_crew_when_fewer_units_than_slots()
        {
            // Arrange: 3 slots, 2 units → la tercera asignación recicla la primera
            var stage = MakeStage(MakeShipData("player"), Wave(MakeShipData("enemy")));
            var crew = MakeCrew(2);

            // Act
            var setup = NavalStageController.BuildNavalBattle(stage, crew);

            // Assert
            Assert.AreEqual(3, setup.AllyShip.Crew.Count);
            Assert.AreSame(crew[0], setup.AllyShip.Crew[0].Unit);
            Assert.AreSame(crew[1], setup.AllyShip.Crew[1].Unit);
            Assert.AreSame(crew[0], setup.AllyShip.Crew[2].Unit);
        }

        [Test]
        public void test_naval_stage_build_creates_one_wave_config_per_naval_wave()
        {
            // Arrange
            var stage = MakeStage(MakeShipData("player"),
                Wave(MakeShipData("e1")),
                Wave(MakeShipData("e2"), MakeShipData("e3")));

            // Act
            var setup = NavalStageController.BuildNavalBattle(stage, MakeCrew(2));

            // Assert
            Assert.AreEqual(2, setup.Config.Waves.Count);
            Assert.AreEqual(1, setup.Config.Waves[0].Enemies.Count);
            Assert.AreEqual(2, setup.Config.Waves[1].Enemies.Count);
        }

        [Test]
        public void test_naval_stage_build_assigns_distinct_ai_per_enemy()
        {
            // Arrange: el jefe lleva estado de fase — instancias nunca compartidas
            var stage = MakeStage(MakeShipData("player"),
                Wave(MakeShipData("e1"), MakeShipData("e2")));

            // Act
            var setup = NavalStageController.BuildNavalBattle(stage, MakeCrew(2));

            // Assert
            Assert.AreEqual(2, setup.EnemyInputs.Count);
            var inputs = new List<ICombatInput>(setup.EnemyInputs.Values);
            Assert.AreNotSame(inputs[0], inputs[1]);
            foreach (var input in inputs)
                Assert.IsInstanceOf<NavalEnemyAI>(input);
        }

        [Test]
        public void test_naval_stage_build_marks_jefe_tier_ship_as_boss()
        {
            // Arrange
            var stage = MakeStage(MakeShipData("player"),
                Wave(MakeShipData("boss", tier: EnemyTier.Jefe)));

            // Act
            var setup = NavalStageController.BuildNavalBattle(stage, MakeCrew(2));

            // Assert
            var boss = (ShipCombatant)setup.Config.Waves[0].Enemies[0].Combatant;
            Assert.IsTrue(boss.IsBoss);
        }

        [Test]
        public void test_naval_stage_build_creature_without_slots_has_no_crew()
        {
            // Arrange
            var stage = MakeStage(MakeShipData("player"),
                Wave(MakeShipData("kraken", withCrewSlots: false)));

            // Act
            var setup = NavalStageController.BuildNavalBattle(stage, MakeCrew(2));

            // Assert
            var creature = (ShipCombatant)setup.Config.Waves[0].Enemies[0].Combatant;
            Assert.AreEqual(0, creature.Crew.Count);
        }

        [Test]
        public void test_naval_stage_build_enemy_ships_get_crew_from_stage_pool()
        {
            // Arrange
            var stage = MakeStage(MakeShipData("player"), Wave(MakeShipData("enemy")));

            // Act
            var setup = NavalStageController.BuildNavalBattle(stage, MakeCrew(2));

            // Assert: 3 slots cubiertos ciclando el pool de 1 unit
            var enemy = (ShipCombatant)setup.Config.Waves[0].Enemies[0].Combatant;
            Assert.AreEqual(3, enemy.Crew.Count);
            Assert.AreSame(stage.EnemyCrewPool[0], enemy.Crew[0].Unit);
        }

        [Test]
        public void test_naval_stage_build_without_player_ship_throws()
        {
            // Arrange
            var stage = MakeStage(null, Wave(MakeShipData("enemy")));

            // Act + Assert
            Assert.Throws<System.ArgumentException>(
                () => NavalStageController.BuildNavalBattle(stage, MakeCrew(2)));
        }

        [Test]
        public void test_naval_stage_build_without_waves_throws()
        {
            // Arrange
            var stage = MakeStage(MakeShipData("player"));

            // Act + Assert
            Assert.Throws<System.ArgumentException>(
                () => NavalStageController.BuildNavalBattle(stage, MakeCrew(2)));
        }
    }
}
