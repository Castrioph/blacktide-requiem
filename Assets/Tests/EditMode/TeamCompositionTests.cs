using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Team;
using BlacktideRequiem.Core.Stage;
using BlacktideRequiem.Runtime.Demo;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for TeamComposition — selection API, validation, and stage launch integration.
    /// Covers S3-05 acceptance criteria.
    /// </summary>
    [TestFixture]
    public class TeamCompositionTests
    {
        private List<CharacterData> _roster;
        private CharacterData _elena;
        private CharacterData _kael;
        private CharacterData _mirra;

        [SetUp]
        public void SetUp()
        {
            var abilities = DemoRosterFactory.BuildAbilities();
            var characters = DemoRosterFactory.BuildCharacters(abilities);
            _elena = characters[DemoRosterFactory.ElenaId];
            _kael = characters[DemoRosterFactory.KaelId];
            _mirra = characters[DemoRosterFactory.MirraId];
            _roster = new List<CharacterData> { _elena, _kael, _mirra };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var c in _roster)
                UnityEngine.Object.DestroyImmediate(c);
        }

        // ====================================================================
        // CONSTRUCTION
        // ====================================================================

        [Test]
        public void test_teamcomposition_constructor_validRoster_slotCountIsMaxSlots()
        {
            // Act
            var team = new TeamComposition(_roster);

            // Assert
            Assert.AreEqual(TeamComposition.MaxSlots, 3);
            Assert.AreEqual(0, team.FilledSlotCount);
        }

        [Test]
        public void test_teamcomposition_constructor_nullRoster_throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TeamComposition(null));
        }

        [Test]
        public void test_teamcomposition_constructor_emptyRoster_throws()
        {
            Assert.Throws<ArgumentException>(() => new TeamComposition(new List<CharacterData>()));
        }

        // ====================================================================
        // SELECT CHARACTER
        // ====================================================================

        [Test]
        public void test_teamcomposition_selectcharacter_validSlotAndRosterCharacter_returnsTrue()
        {
            // Arrange
            var team = new TeamComposition(_roster);

            // Act
            bool result = team.SelectCharacter(0, _elena);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(_elena, team.GetSlot(0));
        }

        [Test]
        public void test_teamcomposition_selectcharacter_characterNotInRoster_returnsFalse()
        {
            // Arrange
            var team = new TeamComposition(_roster);
            var outsider = ScriptableObject.CreateInstance<CharacterData>();

            // Act
            bool result = team.SelectCharacter(0, outsider);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(team.GetSlot(0));

            UnityEngine.Object.DestroyImmediate(outsider);
        }

        [Test]
        public void test_teamcomposition_selectcharacter_nullCharacter_returnsFalse()
        {
            // Arrange
            var team = new TeamComposition(_roster);

            // Act
            bool result = team.SelectCharacter(0, null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void test_teamcomposition_selectcharacter_negativeSlotIndex_returnsFalse()
        {
            // Arrange
            var team = new TeamComposition(_roster);

            // Act
            bool result = team.SelectCharacter(-1, _elena);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void test_teamcomposition_selectcharacter_slotIndexOutOfRange_returnsFalse()
        {
            // Arrange
            var team = new TeamComposition(_roster);

            // Act
            bool result = team.SelectCharacter(TeamComposition.MaxSlots, _elena);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void test_teamcomposition_selectcharacter_duplicateInDifferentSlot_returnsFalse()
        {
            // Arrange
            var team = new TeamComposition(_roster);
            team.SelectCharacter(0, _elena);

            // Act — try to assign elena to slot 1 as well
            bool result = team.SelectCharacter(1, _elena);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(team.GetSlot(1));
        }

        [Test]
        public void test_teamcomposition_selectcharacter_replaceSlotWithDifferentCharacter_succeeds()
        {
            // Arrange
            var team = new TeamComposition(_roster);
            team.SelectCharacter(0, _elena);

            // Act
            bool result = team.SelectCharacter(0, _kael);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(_kael, team.GetSlot(0));
        }

        // ====================================================================
        // CLEAR
        // ====================================================================

        [Test]
        public void test_teamcomposition_clearslot_filledSlot_becomesNull()
        {
            // Arrange
            var team = new TeamComposition(_roster);
            team.SelectCharacter(0, _elena);

            // Act
            team.ClearSlot(0);

            // Assert
            Assert.IsNull(team.GetSlot(0));
        }

        [Test]
        public void test_teamcomposition_clearall_allSlotsBecomeNull()
        {
            // Arrange
            var team = new TeamComposition(_roster);
            team.SelectCharacter(0, _elena);
            team.SelectCharacter(1, _kael);
            team.SelectCharacter(2, _mirra);

            // Act
            team.ClearAll();

            // Assert
            Assert.AreEqual(0, team.FilledSlotCount);
        }

        // ====================================================================
        // VALIDITY
        // ====================================================================

        [Test]
        public void test_teamcomposition_isvalid_noSlotsFilled_returnsFalse()
        {
            var team = new TeamComposition(_roster);
            Assert.IsFalse(team.IsValid);
        }

        [Test]
        public void test_teamcomposition_isvalid_oneSlotFilled_returnsTrue()
        {
            var team = new TeamComposition(_roster);
            team.SelectCharacter(0, _elena);
            Assert.IsTrue(team.IsValid);
        }

        // ====================================================================
        // GET TEAM
        // ====================================================================

        [Test]
        public void test_teamcomposition_getteam_threeCharacters_returnsListOfThree()
        {
            // Arrange
            var team = new TeamComposition(_roster);
            team.SelectCharacter(0, _elena);
            team.SelectCharacter(1, _kael);
            team.SelectCharacter(2, _mirra);

            // Act
            var result = team.GetTeam();

            // Assert
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void test_teamcomposition_getteam_emptyMiddleSlot_returnsOnlyFilledInOrder()
        {
            // Arrange
            var team = new TeamComposition(_roster);
            team.SelectCharacter(0, _elena);
            // slot 1 empty
            team.SelectCharacter(2, _mirra);

            // Act
            var result = team.GetTeam();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(_elena, result[0]);
            Assert.AreEqual(_mirra, result[1]);
        }

        // ====================================================================
        // INTEGRATION — feeds into StageController
        // ====================================================================

        [Test]
        public void test_teamcomposition_integration_getTeamFeedsStageController()
        {
            // Arrange
            var abilities = DemoRosterFactory.BuildAbilities();
            var characters = DemoRosterFactory.BuildCharacters(abilities);
            var roster = new List<CharacterData>(characters.Values);

            var teamComp = new TeamComposition(roster);
            teamComp.SelectCharacter(0, _elena);
            teamComp.SelectCharacter(1, _kael);

            var stage = ScriptableObject.CreateInstance<StageData>();
            stage.Id = "test_stage";
            stage.Waves = new System.Collections.Generic.List<WaveDefinition>
            {
                new WaveDefinition
                {
                    Enemies = new System.Collections.Generic.List<EnemySlot>
                    {
                        new EnemySlot { Enemy = _elena, SlotIndex = 0 }
                    },
                    EnemyCaptainIndex = -1
                }
            };

            // Act
            var config = StageController.BuildBattleConfig(stage, teamComp.GetTeam());

            // Assert
            Assert.AreEqual(2, config.Allies.Count);
            Assert.AreEqual(1, config.Waves.Count);

            UnityEngine.Object.DestroyImmediate(stage);
            foreach (var ab in abilities.Values) UnityEngine.Object.DestroyImmediate(ab);
            foreach (var ch in characters.Values) UnityEngine.Object.DestroyImmediate(ch);
        }
    }
}
