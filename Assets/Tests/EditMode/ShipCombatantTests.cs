using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for ShipCombatant + CrewMemberState construction (S4-02b).
    /// Covers Ship Data Model GDD AC 1-6 at runtime-entity level and
    /// Combate Naval GDD §6 (crew HP per role, immunities).
    /// Deep stat-recalc-on-death coverage lands with S4-03.
    /// </summary>
    [TestFixture]
    public class ShipCombatantTests
    {
        private ShipData _sloop;
        private readonly List<Object> _toDestroy = new();

        [SetUp]
        public void SetUp()
        {
            _sloop = ScriptableObject.CreateInstance<ShipData>();
            _toDestroy.Add(_sloop);
            _sloop.ShipId = "ship_sloop_test";
            _sloop.DisplayName = "Sloop Test";
            _sloop.Element = Element.Tormenta;
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

        private UnitData MakeUnit(string id, float atk, NavalRole affinity,
            List<AbilityEntry> seaAbilities = null)
        {
            var unit = ScriptableObject.CreateInstance<UnitData>();
            _toDestroy.Add(unit);
            unit.Id = id;
            unit.DisplayName = id;
            unit.BaseStats = new StatBlock
            {
                HP = 300, MP = 50, ATK = atk, DEF = 40, MST = 60, SPR = 45, SPD = 70
            };
            unit.NavalRoleAffinity = new List<NavalRole> { affinity };
            if (seaAbilities != null)
                unit.SeaAbilities = seaAbilities;
            return unit;
        }

        private AbilityData MakeAbility(string id)
        {
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            _toDestroy.Add(ability);
            ability.Id = id;
            ability.DisplayName = id;
            return ability;
        }

        // ====================================================================
        // CREW HP PER ROLE (Combate Naval GDD §6)
        // ====================================================================

        [Test]
        public void test_crew_hp_fixed_per_role_matches_gdd_table()
        {
            // Arrange / Act / Assert — fixed table, independent of unit stats
            Assert.AreEqual(800, CrewMemberState.GetCrewHPForRole(NavalRole.Capitan));
            Assert.AreEqual(600, CrewMemberState.GetCrewHPForRole(NavalRole.Intendente));
            Assert.AreEqual(400, CrewMemberState.GetCrewHPForRole(NavalRole.Artillero));
            Assert.AreEqual(500, CrewMemberState.GetCrewHPForRole(NavalRole.Navegante));
            Assert.AreEqual(700, CrewMemberState.GetCrewHPForRole(NavalRole.Carpintero));
            Assert.AreEqual(500, CrewMemberState.GetCrewHPForRole(NavalRole.Cirujano));
            Assert.AreEqual(600, CrewMemberState.GetCrewHPForRole(NavalRole.Contramaestre));
        }

        [Test]
        public void test_crew_hp_independent_of_unit_stats()
        {
            // Arrange — weak and strong unit in the same role
            var weak = MakeUnit("weak", atk: 10, NavalRole.Capitan);
            var strong = MakeUnit("strong", atk: 999, NavalRole.Capitan);
            var slot = new RoleSlot { SlotIndex = 0, Role = NavalRole.Capitan };

            // Act
            var crewWeak = new CrewMemberState(slot, weak, new BuffStack());
            var crewStrong = new CrewMemberState(slot, strong, new BuffStack());

            // Assert
            Assert.AreEqual(800, crewWeak.MaxHP);
            Assert.AreEqual(800, crewStrong.MaxHP);
        }

        // ====================================================================
        // CONSTRUCTION
        // ====================================================================

        [Test]
        public void test_ship_construction_creates_crew_only_for_assigned_slots()
        {
            // Arrange
            var crewBySlot = new Dictionary<int, CharacterData>
            {
                [0] = MakeUnit("cap", 180, NavalRole.Capitan),
                [2] = MakeUnit("carp", 120, NavalRole.Carpintero)
            };

            // Act
            var ship = new ShipCombatant(_sloop, default, crewBySlot);

            // Assert
            Assert.AreEqual(2, ship.Crew.Count);
            Assert.AreEqual(NavalRole.Capitan, ship.Crew[0].Role);
            Assert.AreEqual(NavalRole.Carpintero, ship.Crew[1].Role);
        }

        [Test]
        public void test_ship_with_no_crew_is_deployable_with_base_plus_upgrades()
        {
            // Arrange — Cannons level 2 → FPW +25% of 100 = +25
            var upgrades = new ShipUpgradeState { CannonsLevel = 2 };

            // Act
            var ship = new ShipCombatant(_sloop, upgrades);

            // Assert — AC 4/7: empty slots contribute zero, ship functional
            Assert.AreEqual(0, ship.Crew.Count);
            Assert.AreEqual(125f, ship.GetEffectiveShipStat(ShipStatType.FPW));
            Assert.AreEqual(500f, ship.GetEffectiveShipStat(ShipStatType.HHP));
            Assert.IsFalse(ship.IsKO);
        }

        [Test]
        public void test_ship_max_resources_fixed_from_initial_effective_stats()
        {
            // Arrange — Carpintero contributes HP×0.15 to HHP (match)
            var carp = MakeUnit("carp", 120, NavalRole.Carpintero);
            var crewBySlot = new Dictionary<int, CharacterData> { [2] = carp };

            // Act
            var ship = new ShipCombatant(_sloop, default, crewBySlot);

            // Assert — HHP = 500 + floor(300 × 0.15) = 545
            Assert.AreEqual(545, ship.MaxHHP);
            Assert.AreEqual(545, ship.CurrentHHP);
            Assert.AreEqual(50, ship.MaxMP);
        }

        [Test]
        public void test_ship_captain_property_finds_capitan_role()
        {
            // Arrange
            var cap = MakeUnit("cap", 180, NavalRole.Capitan);
            var art = MakeUnit("art", 200, NavalRole.Artillero);
            var crewBySlot = new Dictionary<int, CharacterData> { [0] = cap, [1] = art };

            // Act
            var ship = new ShipCombatant(_sloop, default, crewBySlot);

            // Assert
            Assert.IsNotNull(ship.Captain);
            Assert.AreEqual(cap, ship.Captain.Unit);
        }

        // ====================================================================
        // CREW CONTRIBUTION (GDD worked example)
        // ====================================================================

        [Test]
        public void test_ship_effective_fpw_matches_gdd_worked_example()
        {
            // Arrange — Ship Data Model GDD §Formulas 5:
            // Base FPW 100, Cannons L2 +25,
            // Artillero match ATK=200 → +30, Capitán match ATK=180 → +27,
            // Carpintero (no FPW role) → +0,
            // Artillero MISMATCH (Navegante unit) ATK=150 → floor(22×0.5)=+11,
            // guest empty → 0. Total = 193.
            var upgrades = new ShipUpgradeState { CannonsLevel = 2 };
            var crewBySlot = new Dictionary<int, CharacterData>
            {
                [0] = MakeUnit("cap", 180, NavalRole.Capitan),
                [1] = MakeUnit("art1", 200, NavalRole.Artillero),
                [2] = MakeUnit("carp", 120, NavalRole.Carpintero),
                [3] = MakeUnit("nav_in_art", 150, NavalRole.Navegante)
            };

            // Act
            var ship = new ShipCombatant(_sloop, upgrades, crewBySlot);

            // Assert
            Assert.AreEqual(193f, ship.GetEffectiveShipStat(ShipStatType.FPW));
        }

        [Test]
        public void test_ship_mismatch_penalty_halves_contribution()
        {
            // Arrange — same ATK, matching vs mismatched Artillero slot (AC 5)
            var match = new Dictionary<int, CharacterData>
            {
                [1] = MakeUnit("match", 200, NavalRole.Artillero)
            };
            var mismatch = new Dictionary<int, CharacterData>
            {
                [1] = MakeUnit("mismatch", 200, NavalRole.Cirujano)
            };

            // Act
            var shipMatch = new ShipCombatant(_sloop, default, match);
            var shipMismatch = new ShipCombatant(_sloop, default, mismatch);

            // Assert — match: 100+30=130; mismatch: 100+floor(30×0.5)=115
            Assert.AreEqual(130f, shipMatch.GetEffectiveShipStat(ShipStatType.FPW));
            Assert.AreEqual(115f, shipMismatch.GetEffectiveShipStat(ShipStatType.FPW));
        }

        // ====================================================================
        // ABILITY POOL
        // ====================================================================

        [Test]
        public void test_ship_ability_pool_combines_base_and_crew_sea_abilities()
        {
            // Arrange
            var baseAbility = MakeAbility("canonazo_base");
            _sloop.BaseAbilities = new List<AbilityData> { baseAbility };

            var seaAbility = MakeAbility("marea_viva");
            var cap = MakeUnit("cap", 180, NavalRole.Capitan,
                new List<AbilityEntry> { new AbilityEntry { Ability = seaAbility } });
            var crewBySlot = new Dictionary<int, CharacterData> { [0] = cap };

            // Act
            var ship = new ShipCombatant(_sloop, default, crewBySlot);

            // Assert — AC 9
            Assert.AreEqual(2, ship.AbilityPool.Count);
            CollectionAssert.Contains(ship.AbilityPool, baseAbility);
            CollectionAssert.Contains(ship.AbilityPool, seaAbility);
        }

        [Test]
        public void test_ship_ability_pool_allows_duplicate_sea_abilities()
        {
            // Arrange — GDD edge case: same ability from 2 crew = 2 instances
            var shared = MakeAbility("andanada");
            var entryList1 = new List<AbilityEntry> { new AbilityEntry { Ability = shared } };
            var entryList2 = new List<AbilityEntry> { new AbilityEntry { Ability = shared } };
            var crewBySlot = new Dictionary<int, CharacterData>
            {
                [1] = MakeUnit("art1", 200, NavalRole.Artillero, entryList1),
                [3] = MakeUnit("art2", 150, NavalRole.Artillero, entryList2)
            };

            // Act
            var ship = new ShipCombatant(_sloop, default, crewBySlot);

            // Assert
            Assert.AreEqual(2, ship.AbilityPool.Count);
        }

        [Test]
        public void test_ship_recalculate_excludes_dead_crew_stats_and_abilities()
        {
            // Arrange
            var seaAbility = MakeAbility("fuego_certero");
            var art = MakeUnit("art", 200, NavalRole.Artillero,
                new List<AbilityEntry> { new AbilityEntry { Ability = seaAbility } });
            var crewBySlot = new Dictionary<int, CharacterData> { [1] = art };
            var ship = new ShipCombatant(_sloop, default, crewBySlot);
            Assert.AreEqual(130f, ship.GetEffectiveShipStat(ShipStatType.FPW));
            Assert.AreEqual(1, ship.AbilityPool.Count);

            // Act — kill the crew member, then recalculate (AC 10)
            ship.Crew[0].ApplyDamage(ship.Crew[0].MaxHP);
            ship.RecalculateFromCrew();

            // Assert — contribution and sea ability gone
            Assert.IsTrue(ship.Crew[0].IsDead);
            Assert.AreEqual(100f, ship.GetEffectiveShipStat(ShipStatType.FPW));
            Assert.AreEqual(0, ship.AbilityPool.Count);
        }

        // ====================================================================
        // IMMUNITIES & STATUS (Combate Naval GDD §2)
        // ====================================================================

        [Test]
        public void test_ship_immune_to_sleep_stun_and_muerte()
        {
            // Arrange
            var ship = new ShipCombatant(_sloop, default);

            // Act — apply each CC; ships silently ignore them
            ship.ApplyStatus(new StatusInstance { Effect = StatusEffect.Sueno, RemainingTurns = 2 });
            ship.ApplyStatus(new StatusInstance { Effect = StatusEffect.Aturdimiento, RemainingTurns = 1 });
            ship.ApplyStatus(new StatusInstance { Effect = StatusEffect.Muerte, RemainingTurns = 1 });

            // Assert
            Assert.IsTrue(ship.IsImmuneTo(StatusEffect.Sueno));
            Assert.IsTrue(ship.IsImmuneTo(StatusEffect.Aturdimiento));
            Assert.IsTrue(ship.IsImmuneTo(StatusEffect.Muerte));
            Assert.AreEqual(0, ship.StatusEffects.Count);
        }

        [Test]
        public void test_ship_accepts_quemadura_ceguera_silencio()
        {
            // Arrange
            var ship = new ShipCombatant(_sloop, default);

            // Act
            ship.ApplyStatus(new StatusInstance { Effect = StatusEffect.Quemadura, RemainingTurns = 3 });
            ship.ApplyStatus(new StatusInstance { Effect = StatusEffect.Ceguera, RemainingTurns = 2 });
            ship.ApplyStatus(new StatusInstance { Effect = StatusEffect.Silencio, RemainingTurns = 2 });

            // Assert
            Assert.IsTrue(ship.HasStatus(StatusEffect.Quemadura));
            Assert.IsTrue(ship.HasStatus(StatusEffect.Ceguera));
            Assert.IsTrue(ship.HasStatus(StatusEffect.Silencio));
        }

        // ====================================================================
        // HULL DAMAGE
        // ====================================================================

        [Test]
        public void test_ship_hull_damage_sinks_ship_without_touching_crew()
        {
            // Arrange
            var cap = MakeUnit("cap", 180, NavalRole.Capitan);
            var crewBySlot = new Dictionary<int, CharacterData> { [0] = cap };
            var ship = new ShipCombatant(_sloop, default, crewBySlot);

            // Act — sink the hull (GDD §6: crew NOT damaged by hull attacks)
            int dealt = ship.ApplyDamage(ship.MaxHHP);

            // Assert
            Assert.AreEqual(ship.MaxHHP, dealt);
            Assert.IsTrue(ship.IsKO);
            Assert.IsFalse(ship.Crew[0].IsDead);
            Assert.AreEqual(800, ship.Crew[0].CurrentHP);
        }

        // ====================================================================
        // ICOMBATANT BRIDGE
        // ====================================================================

        [Test]
        public void test_ship_icombatant_stat_bridge_maps_naval_stats()
        {
            // Arrange
            var ship = new ShipCombatant(_sloop, default);
            ICombatant combatant = ship;

            // Act / Assert — InitiativeBar reads StatType.SPD transparently
            Assert.AreEqual(ship.GetEffectiveShipStat(ShipStatType.SPD),
                combatant.GetEffectiveStat(StatType.SPD));
            Assert.AreEqual(ship.GetEffectiveShipStat(ShipStatType.FPW),
                combatant.GetEffectiveStat(StatType.ATK));
            Assert.AreEqual(ship.GetEffectiveShipStat(ShipStatType.HDF),
                combatant.GetEffectiveStat(StatType.DEF));
            Assert.AreEqual(ship.GetEffectiveShipStat(ShipStatType.RSL),
                combatant.GetEffectiveStat(StatType.SPR));
            Assert.AreEqual(ship.MaxHHP, combatant.MaxHP);
            Assert.AreEqual(ship.CurrentHHP, combatant.CurrentHP);
        }

        [Test]
        public void test_ship_enters_initiative_bar_as_single_combatant()
        {
            // Arrange — ship + land unit sorted together by SPD
            var ship = new ShipCombatant(_sloop, default); // SPD 90
            var unitTemplate = MakeUnit("unit", 100, NavalRole.Capitan);
            var unit = new CombatantState(unitTemplate, unitTemplate.BaseStats, 1); // SPD 70

            var bar = new InitiativeBar();
            var entries = new List<InitiativeEntry>
            {
                new InitiativeEntry(unit, CombatTeam.Ally, 0),
                new InitiativeEntry(ship, CombatTeam.Enemy, 0)
            };

            // Act
            bar.BeginRound(entries);
            var first = bar.AdvanceTurn();

            // Assert — ship (SPD 90) acts before unit (SPD 70); crew adds no entries
            Assert.AreSame(ship, first.Combatant);
            Assert.AreEqual(2, bar.Entries.Count);
        }
    }
}
