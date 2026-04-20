using System.Collections.Generic;
using NUnit.Framework;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Runtime.Demo;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Verifies the S2-10 demo roster spec: 9 abilities + 3 characters,
    /// unique ids, varied elements/roles, and key secondary-effect wiring.
    /// Consumes DemoRosterFactory — same spec used by the Editor asset generator.
    /// </summary>
    [TestFixture]
    public class DemoRosterTests
    {
        private Dictionary<string, AbilityData> _abilities;
        private Dictionary<string, CharacterData> _characters;

        [SetUp]
        public void SetUp()
        {
            _abilities = DemoRosterFactory.BuildAbilities();
            _characters = DemoRosterFactory.BuildCharacters(_abilities);
        }

        // ================================================================
        // ROSTER SHAPE
        // ================================================================

        [Test]
        public void test_roster_builds_nine_abilities()
        {
            Assert.AreEqual(9, _abilities.Count);
        }

        [Test]
        public void test_roster_builds_three_characters()
        {
            Assert.AreEqual(3, _characters.Count);
        }

        [Test]
        public void test_all_ability_ids_unique_and_match_key()
        {
            foreach (var pair in _abilities)
                Assert.AreEqual(pair.Key, pair.Value.Id,
                    $"Ability dictionary key {pair.Key} mismatch asset id {pair.Value.Id}");
        }

        [Test]
        public void test_all_character_ids_unique_and_match_key()
        {
            foreach (var pair in _characters)
                Assert.AreEqual(pair.Key, pair.Value.Id,
                    $"Character dictionary key {pair.Key} mismatch asset id {pair.Value.Id}");
        }

        // ================================================================
        // ELENA — magical DPS + debuffer (Tormenta)
        // ================================================================

        [Test]
        public void test_elena_is_tormenta_with_three_abilities()
        {
            var elena = _characters[DemoRosterFactory.ElenaId];
            Assert.AreEqual(Element.Tormenta, elena.Element);
            Assert.AreEqual(3, elena.LandAbilities.Count);
        }

        [Test]
        public void test_elena_storm_bolt_is_single_target_magical_damage()
        {
            var ability = _abilities[DemoRosterFactory.StormBoltId];
            Assert.AreEqual(TargetType.SingleEnemy, ability.TargetType);
            Assert.AreEqual(AbilityCategory.Damage, ability.Category);
            Assert.IsFalse(ability.IsPhysical);
            Assert.AreEqual(Element.Tormenta, ability.Element);
        }

        [Test]
        public void test_elena_chain_lightning_is_aoe_magical_damage()
        {
            var ability = _abilities[DemoRosterFactory.ChainLightningId];
            Assert.AreEqual(TargetType.AoeEnemy, ability.TargetType);
            Assert.AreEqual(AbilityCategory.Damage, ability.Category);
            Assert.IsFalse(ability.IsPhysical);
        }

        [Test]
        public void test_curse_of_sparks_applies_silencio_secondary()
        {
            var ability = _abilities[DemoRosterFactory.CurseOfSparksId];
            Assert.AreEqual(1, ability.SecondaryEffects.Count);
            Assert.AreEqual(StatusEffect.Silencio, ability.SecondaryEffects[0].Effect);
            Assert.Greater(ability.SecondaryEffects[0].Probability, 0f);
        }

        // ================================================================
        // KAEL — physical DPS (Pólvora)
        // ================================================================

        [Test]
        public void test_kael_is_polvora_with_three_physical_abilities()
        {
            var kael = _characters[DemoRosterFactory.KaelId];
            Assert.AreEqual(Element.Polvora, kael.Element);
            Assert.AreEqual(3, kael.LandAbilities.Count);
            foreach (var entry in kael.LandAbilities)
                Assert.IsTrue(entry.Ability.IsPhysical, $"{entry.Ability.Id} must be physical");
        }

        [Test]
        public void test_kael_scatter_shot_is_aoe_physical_damage()
        {
            var ability = _abilities[DemoRosterFactory.ScatterShotId];
            Assert.AreEqual(TargetType.AoeEnemy, ability.TargetType);
            Assert.AreEqual(AbilityCategory.Damage, ability.Category);
            Assert.IsTrue(ability.IsPhysical);
        }

        [Test]
        public void test_searing_shot_applies_quemadura_secondary()
        {
            var ability = _abilities[DemoRosterFactory.SearingShotId];
            Assert.AreEqual(1, ability.SecondaryEffects.Count);
            Assert.AreEqual(StatusEffect.Quemadura, ability.SecondaryEffects[0].Effect);
            Assert.Greater(ability.SecondaryEffects[0].Param, 0f,
                "Quemadura must have non-zero DoT percent (Param)");
        }

        // ================================================================
        // MIRRA — support / healer (Neutral)
        // ================================================================

        [Test]
        public void test_mirra_is_neutral_with_three_abilities()
        {
            var mirra = _characters[DemoRosterFactory.MirraId];
            Assert.AreEqual(Element.Neutral, mirra.Element);
            Assert.AreEqual(3, mirra.LandAbilities.Count);
        }

        [Test]
        public void test_mirra_healing_tide_is_single_ally_heal()
        {
            var ability = _abilities[DemoRosterFactory.HealingTideId];
            Assert.AreEqual(TargetType.SingleAlly, ability.TargetType);
            Assert.AreEqual(AbilityCategory.Heal, ability.Category);
            Assert.Greater(ability.HealPower, 0f);
        }

        [Test]
        public void test_mirra_mending_current_is_aoe_ally_heal()
        {
            var ability = _abilities[DemoRosterFactory.MendingCurrentId];
            Assert.AreEqual(TargetType.AllyAoe, ability.TargetType);
            Assert.AreEqual(AbilityCategory.Heal, ability.Category);
            Assert.Greater(ability.HealPower, 0f);
        }

        [Test]
        public void test_mirra_lullaby_applies_sueno_secondary()
        {
            var ability = _abilities[DemoRosterFactory.LullabyId];
            Assert.AreEqual(1, ability.SecondaryEffects.Count);
            Assert.AreEqual(StatusEffect.Sueno, ability.SecondaryEffects[0].Effect);
        }

        // ================================================================
        // CROSS-CHARACTER VARIETY
        // ================================================================

        [Test]
        public void test_roster_covers_three_distinct_elements()
        {
            var elements = new HashSet<Element>();
            foreach (var character in _characters.Values)
                elements.Add(character.Element);
            Assert.AreEqual(3, elements.Count, "Each demo character must have a unique element");
        }

        [Test]
        public void test_roster_has_mp_for_highest_cost_ability()
        {
            foreach (var character in _characters.Values)
            {
                int maxCost = 0;
                foreach (var entry in character.LandAbilities)
                    maxCost = System.Math.Max(maxCost, entry.Ability.MPCost);
                Assert.GreaterOrEqual((int)character.BaseStats.MP, maxCost,
                    $"{character.Id} MP pool ({character.BaseStats.MP}) too low for costliest ability ({maxCost})");
            }
        }

        [Test]
        public void test_roster_ability_entries_reference_valid_abilities()
        {
            foreach (var character in _characters.Values)
                foreach (var entry in character.LandAbilities)
                    Assert.IsNotNull(entry.Ability,
                        $"{character.Id} has null AbilityEntry.Ability reference");
        }
    }
}
