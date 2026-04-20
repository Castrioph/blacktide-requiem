using NUnit.Framework;
using BlacktideRequiem.Core.Flow;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for GameFlowState, SceneRegistry, and battle result storage logic.
    /// Covers S2-09 acceptance criteria (pure C# tests, no scene loading).
    /// </summary>
    [TestFixture]
    public class GameFlowTests
    {
        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
        }

        // ================================================================
        // SceneRegistry
        // ================================================================

        [Test]
        public void test_SceneRegistry_MainMenu_returns_correct_name()
        {
            // Assert
            Assert.AreEqual("MainMenu", SceneRegistry.MainMenu);
        }

        [Test]
        public void test_SceneRegistry_Combat_returns_correct_name()
        {
            // Assert
            Assert.AreEqual("CombatDemo", SceneRegistry.Combat);
        }

        [Test]
        public void test_SceneRegistry_Results_returns_correct_name()
        {
            // Assert
            Assert.AreEqual("Results", SceneRegistry.Results);
        }

        [Test]
        public void test_SceneRegistry_all_names_unique()
        {
            // Assert
            Assert.AreNotEqual(SceneRegistry.MainMenu, SceneRegistry.Combat);
            Assert.AreNotEqual(SceneRegistry.MainMenu, SceneRegistry.Results);
            Assert.AreNotEqual(SceneRegistry.Combat, SceneRegistry.Results);
        }

        // ================================================================
        // GameFlowState enum
        // ================================================================

        [Test]
        public void test_GameFlowState_has_all_expected_values()
        {
            // Assert — enum has None + 3 screen states
            Assert.IsTrue(System.Enum.IsDefined(typeof(GameFlowState), GameFlowState.None));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GameFlowState), GameFlowState.MainMenu));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GameFlowState), GameFlowState.Combat));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GameFlowState), GameFlowState.Results));
        }

        [Test]
        public void test_GameFlowState_default_is_None()
        {
            // Arrange
            GameFlowState state = default;

            // Assert
            Assert.AreEqual(GameFlowState.None, state);
        }

        // ================================================================
        // BattleEndEvent storage (via GameEvents)
        // ================================================================

        [Test]
        public void test_BattleEndEvent_stores_victory_result()
        {
            // Arrange
            BattleEndEvent? captured = null;
            GameEvents.OnBattleEnd += e => captured = e;

            var battleEnd = new BattleEndEvent
            {
                Result = BattleResult.Victory,
                RoundsElapsed = 5
            };

            // Act
            GameEvents.PublishBattleEnd(battleEnd);

            // Assert
            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual(BattleResult.Victory, captured.Value.Result);
            Assert.AreEqual(5, captured.Value.RoundsElapsed);
        }

        [Test]
        public void test_BattleEndEvent_stores_defeat_result()
        {
            // Arrange
            BattleEndEvent? captured = null;
            GameEvents.OnBattleEnd += e => captured = e;

            var battleEnd = new BattleEndEvent
            {
                Result = BattleResult.Defeat,
                RoundsElapsed = 3
            };

            // Act
            GameEvents.PublishBattleEnd(battleEnd);

            // Assert
            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual(BattleResult.Defeat, captured.Value.Result);
            Assert.AreEqual(3, captured.Value.RoundsElapsed);
        }

        [Test]
        public void test_GameEvents_ClearAll_removes_BattleEnd_subscriber()
        {
            // Arrange
            BattleEndEvent? captured = null;
            GameEvents.OnBattleEnd += e => captured = e;

            // Act
            GameEvents.ClearAll();
            GameEvents.PublishBattleEnd(new BattleEndEvent
            {
                Result = BattleResult.Victory,
                RoundsElapsed = 1
            });

            // Assert — subscriber was cleared, so captured stays null
            Assert.IsNull(captured);
        }

        // ================================================================
        // SceneRegistry matches scene file expectations
        // ================================================================

        [Test]
        public void test_SceneRegistry_Combat_matches_existing_scene_name()
        {
            // CombatDemo.unity exists — scene name must match filename without extension
            Assert.AreEqual("CombatDemo", SceneRegistry.Combat);
        }

        [Test]
        public void test_SceneRegistry_names_are_not_empty()
        {
            // Assert
            Assert.IsNotEmpty(SceneRegistry.MainMenu);
            Assert.IsNotEmpty(SceneRegistry.Combat);
            Assert.IsNotEmpty(SceneRegistry.Results);
        }
    }
}
