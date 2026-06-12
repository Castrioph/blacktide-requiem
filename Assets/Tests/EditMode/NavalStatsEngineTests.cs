using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for the naval stats engine (S4-03): EffectiveStat with synergy
    /// buffs, immediate recalculation on crew death, crew synergy lifecycle
    /// (activation, captain death, trait-count drop — Combate Naval GDD §6),
    /// and the effective-stat cache contract.
    /// </summary>
    [TestFixture]
    public class NavalStatsEngineTests
    {
        private ShipData _sloop;
        private TraitData _hijosDelMar;
        private readonly List<Object> _toDestroy = new();

        [SetUp]
        public void SetUp()
        {
            _sloop = ScriptableObject.CreateInstance<ShipData>();
            _toDestroy.Add(_sloop);
            _sloop.ShipId = "ship_stats_test";
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

            _hijosDelMar = ScriptableObject.CreateInstance<TraitData>();
            _toDestroy.Add(_hijosDelMar);
            _hijosDelMar.TraitId = "hijos_del_mar";
            _hijosDelMar.DisplayName = "Hijos del Mar";
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
            TraitData trait = null, float atkBonus = 0f,
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
            if (trait != null)
            {
                var bonus = new List<StatModifier>();
                if (atkBonus > 0f)
                    bonus.Add(new StatModifier { Stat = StatType.ATK, Percent = atkBonus });
                unit.Traits = new List<UnitTraitEntry>
                {
                    new UnitTraitEntry { Trait = trait, SynergyBonus = bonus }
                };
            }
            if (seaAbilities != null)
                unit.SeaAbilities = seaAbilities;
            return unit;
        }

        private AbilityData MakeAbility(string id)
        {
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            _toDestroy.Add(ability);
            ability.Id = id;
            return ability;
        }

        /// <summary>Cap (ATK 180, +27 FPW) + Art1 (ATK 200, +30 FPW) + Carp (0 FPW).</summary>
        private ShipCombatant MakeShipWithSynergyCrew(out UnitData cap, out UnitData art1,
            out UnitData carp)
        {
            cap = MakeUnit("cap", 180, NavalRole.Capitan, _hijosDelMar, 0.10f);
            art1 = MakeUnit("art1", 200, NavalRole.Artillero, _hijosDelMar, 0.10f);
            carp = MakeUnit("carp", 120, NavalRole.Carpintero, _hijosDelMar); // enabler, no bonus
            var crewBySlot = new Dictionary<int, CharacterData>
            {
                [0] = cap, [1] = art1, [2] = carp
            };
            return new ShipCombatant(_sloop, default, crewBySlot);
        }

        // ====================================================================
        // RECALCULATION ON CREW DEATH (immediate)
        // ====================================================================

        [Test]
        public void test_naval_stats_crew_death_recalculates_in_same_call()
        {
            // Arrange — Art (ATK 200, match): FPW = 100 + 30 = 130
            var art = MakeUnit("art", 200, NavalRole.Artillero);
            var ship = new ShipCombatant(_sloop, default,
                new Dictionary<int, CharacterData> { [1] = art });
            Assert.AreEqual(130f, ship.GetEffectiveShipStat(ShipStatType.FPW));

            // Act — lethal damage through the ship API
            int dealt = ship.DamageCrewMember(ship.Crew[0], ship.Crew[0].MaxHP, out bool died);

            // Assert — contribution gone immediately, no extra recalc call needed
            Assert.AreEqual(400, dealt);
            Assert.IsTrue(died);
            Assert.AreEqual(100f, ship.GetEffectiveShipStat(ShipStatType.FPW));
        }

        [Test]
        public void test_naval_stats_crew_death_removes_sea_abilities_in_same_call()
        {
            // Arrange
            var sea = MakeAbility("andanada_fantasma");
            var art = MakeUnit("art", 200, NavalRole.Artillero, seaAbilities:
                new List<AbilityEntry> { new AbilityEntry { Ability = sea } });
            var ship = new ShipCombatant(_sloop, default,
                new Dictionary<int, CharacterData> { [1] = art });
            Assert.AreEqual(1, ship.AbilityPool.Count);

            // Act
            ship.DamageCrewMember(ship.Crew[0], ship.Crew[0].MaxHP, out _);

            // Assert
            Assert.AreEqual(0, ship.AbilityPool.Count);
        }

        [Test]
        public void test_naval_stats_nonlethal_crew_damage_does_not_recalculate()
        {
            // Arrange
            var art = MakeUnit("art", 200, NavalRole.Artillero);
            var ship = new ShipCombatant(_sloop, default,
                new Dictionary<int, CharacterData> { [1] = art });

            // Act — wound but don't kill (Artillero HP 400)
            int dealt = ship.DamageCrewMember(ship.Crew[0], 150, out bool died);

            // Assert — contribution intact
            Assert.AreEqual(150, dealt);
            Assert.IsFalse(died);
            Assert.AreEqual(250, ship.Crew[0].CurrentHP);
            Assert.AreEqual(130f, ship.GetEffectiveShipStat(ShipStatType.FPW));
        }

        [Test]
        public void test_naval_stats_cache_requires_explicit_recalculation()
        {
            // Arrange — killing crew BEHIND the ship's back must not change
            // cached stats: DamageCrewMember/RecalculateFromCrew is the contract
            var art = MakeUnit("art", 200, NavalRole.Artillero);
            var ship = new ShipCombatant(_sloop, default,
                new Dictionary<int, CharacterData> { [1] = art });

            // Act — direct crew damage bypassing the ship API
            ship.Crew[0].ApplyDamage(ship.Crew[0].MaxHP);

            // Assert — cache still holds the old value (O(1) reads, ADR-004 §4)
            Assert.IsTrue(ship.Crew[0].IsDead);
            Assert.AreEqual(130f, ship.GetEffectiveShipStat(ShipStatType.FPW));

            // Act — explicit recalc updates it
            ship.RecalculateFromCrew();
            Assert.AreEqual(100f, ship.GetEffectiveShipStat(ShipStatType.FPW));
        }

        // ====================================================================
        // CREW SYNERGIES
        // ====================================================================

        [Test]
        public void test_naval_synergy_activates_at_threshold_and_buffs_ship()
        {
            // Arrange — 3 crew share the captain's trait (threshold = 3)
            var ship = MakeShipWithSynergyCrew(out _, out _, out _);

            // Act
            ship.EvaluateCrewSynergies();

            // Assert — additive FPW = 100+27+30 = 157; buffs +10% (cap) +10% (art1)
            // land on the ship's BuffStack → 157 × 1.20 = 188.4
            Assert.AreEqual(1, ship.CrewSynergies.Count);
            Assert.AreEqual(188.4f, ship.GetEffectiveShipStat(ShipStatType.FPW), 0.001f);
        }

        [Test]
        public void test_naval_synergy_below_threshold_does_not_activate()
        {
            // Arrange — only 2 crew share the trait
            var cap = MakeUnit("cap", 180, NavalRole.Capitan, _hijosDelMar, 0.10f);
            var art = MakeUnit("art", 200, NavalRole.Artillero, _hijosDelMar, 0.10f);
            var ship = new ShipCombatant(_sloop, default,
                new Dictionary<int, CharacterData> { [0] = cap, [1] = art });

            // Act
            ship.EvaluateCrewSynergies();

            // Assert — no synergy, plain additive stats
            Assert.AreEqual(0, ship.CrewSynergies.Count);
            Assert.AreEqual(157f, ship.GetEffectiveShipStat(ShipStatType.FPW));
        }

        [Test]
        public void test_naval_synergy_captain_death_deactivates_buffs()
        {
            // Arrange
            var ship = MakeShipWithSynergyCrew(out _, out _, out _);
            ship.EvaluateCrewSynergies();
            Assert.AreEqual(188.4f, ship.GetEffectiveShipStat(ShipStatType.FPW), 0.001f);

            // Act — kill the captain (Capitán HP 800)
            ship.DamageCrewMember(ship.Captain, 800, out bool died);

            // Assert — synergies gone AND captain's FPW contribution gone:
            // additive = 100 + 30 = 130, modifier back to 1.0
            Assert.IsTrue(died);
            Assert.IsFalse(ship.CaptainAlive);
            Assert.AreEqual(0, ship.CrewSynergies.Count);
            Assert.AreEqual(130f, ship.GetEffectiveShipStat(ShipStatType.FPW), 0.001f);
        }

        [Test]
        public void test_naval_synergy_trait_count_drop_below_threshold_deactivates()
        {
            // Arrange — GDD §6: dead crew loses its TRAIT COUNT too
            var ship = MakeShipWithSynergyCrew(out _, out _, out _);
            ship.EvaluateCrewSynergies();

            // Act — kill the Carpintero enabler (3 → 2 trait holders)
            var carp = ship.Crew[2];
            Assert.AreEqual(NavalRole.Carpintero, carp.Role);
            ship.DamageCrewMember(carp, carp.MaxHP, out _);

            // Assert — synergy re-evaluated and deactivated; FPW additive
            // unchanged (Carpintero gave 0 FPW): 157 × 1.0
            Assert.AreEqual(0, ship.CrewSynergies.Count);
            Assert.AreEqual(157f, ship.GetEffectiveShipStat(ShipStatType.FPW), 0.001f);
        }

        [Test]
        public void test_naval_synergy_persists_when_count_stays_at_threshold()
        {
            // Arrange — 4 trait holders: cap(+10%), art1(+10%), carp(enabler), art2(+10%)
            var cap = MakeUnit("cap", 180, NavalRole.Capitan, _hijosDelMar, 0.10f);
            var art1 = MakeUnit("art1", 200, NavalRole.Artillero, _hijosDelMar, 0.10f);
            var carp = MakeUnit("carp", 120, NavalRole.Carpintero, _hijosDelMar);
            var art2 = MakeUnit("art2", 160, NavalRole.Artillero, _hijosDelMar, 0.10f);
            var ship = new ShipCombatant(_sloop, default, new Dictionary<int, CharacterData>
            {
                [0] = cap, [1] = art1, [2] = carp, [3] = art2
            });
            ship.EvaluateCrewSynergies();

            // additive FPW = 100 + 27 + 30 + 24 = 181; modifier 1.30
            Assert.AreEqual(181f * 1.30f, ship.GetEffectiveShipStat(ShipStatType.FPW), 0.001f);

            // Act — kill the enabler: 4 → 3 trait holders (still ≥ threshold)
            ship.DamageCrewMember(ship.Crew[2], ship.Crew[2].MaxHP, out _);

            // Assert — synergy persists after re-evaluation; FPW additive
            // unchanged (Carpintero gave 0 FPW)
            Assert.AreEqual(1, ship.CrewSynergies.Count);
            Assert.AreEqual(181f * 1.30f, ship.GetEffectiveShipStat(ShipStatType.FPW), 0.001f);
        }

        [Test]
        public void test_naval_synergy_reevaluation_does_not_stack_buffs()
        {
            // Arrange
            var ship = MakeShipWithSynergyCrew(out _, out _, out _);

            // Act — evaluating twice must not double the buffs
            ship.EvaluateCrewSynergies();
            ship.EvaluateCrewSynergies();

            // Assert — still ×1.20, not ×1.40
            Assert.AreEqual(1, ship.CrewSynergies.Count);
            Assert.AreEqual(188.4f, ship.GetEffectiveShipStat(ShipStatType.FPW), 0.001f);
        }

        [Test]
        public void test_naval_stats_living_crew_targeting_excludes_dead()
        {
            // Arrange — Abordaje/DoT targeting pool (used by NavalTurnResolver)
            var ship = MakeShipWithSynergyCrew(out _, out _, out _);
            Assert.AreEqual(3, ship.GetLivingCrew().Count);

            // Act
            ship.DamageCrewMember(ship.Crew[1], ship.Crew[1].MaxHP, out _);

            // Assert
            Assert.AreEqual(2, ship.GetLivingCrew().Count);
            CollectionAssert.DoesNotContain(ship.GetLivingCrew(), ship.Crew[1]);
        }
    }
}
