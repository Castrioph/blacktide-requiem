using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Stage;

namespace BlacktideRequiem.Tests.EditMode
{
    [TestFixture]
    public class StageSystemTests
    {
        private StageData _stage;
        private CharacterData _allyData;
        private CharacterData _enemyData;

        [SetUp]
        public void SetUp()
        {
            _allyData = ScriptableObject.CreateInstance<CharacterData>();
            _allyData.Id = "test_ally";
            _allyData.DisplayName = "Ally";
            _allyData.BaseStats = new StatBlock { HP = 200, MP = 50, ATK = 40, DEF = 20, MST = 30, SPR = 20, SPD = 60 };
            _allyData.SecondaryStats = new SecondaryStatBlock { CRI = 5, LCK = 5 };

            _enemyData = ScriptableObject.CreateInstance<CharacterData>();
            _enemyData.Id = "test_enemy";
            _enemyData.DisplayName = "Enemy";
            _enemyData.BaseStats = new StatBlock { HP = 150, MP = 0, ATK = 35, DEF = 15, MST = 10, SPR = 10, SPD = 45 };
            _enemyData.SecondaryStats = new SecondaryStatBlock { CRI = 5, LCK = 5 };

            _stage = ScriptableObject.CreateInstance<StageData>();
            _stage.Id = "test_stage";
            _stage.DisplayName = "Test Stage";
            _stage.DifficultyLevel = 2;
            _stage.Waves = new List<WaveDefinition>
            {
                new WaveDefinition
                {
                    Enemies = new List<EnemySlot>
                    {
                        new EnemySlot { Enemy = _enemyData, SlotIndex = 0 }
                    },
                    EnemyCaptainIndex = -1
                }
            };
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_stage);
            UnityEngine.Object.DestroyImmediate(_allyData);
            UnityEngine.Object.DestroyImmediate(_enemyData);
        }

        // ====================================================================
        // StageController — null guards
        // ====================================================================

        [Test]
        public void test_stagecontroller_null_stage_throws_argument_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                StageController.BuildBattleConfig(null, new List<CharacterData> { _allyData }));
        }

        [Test]
        public void test_stagecontroller_null_allies_throws_argument_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                StageController.BuildBattleConfig(_stage, null));
        }

        // ====================================================================
        // StageController — ally list
        // ====================================================================

        [Test]
        public void test_stagecontroller_ally_count_matches_input()
        {
            var allies = new List<CharacterData> { _allyData, _allyData };
            var config = StageController.BuildBattleConfig(_stage, allies);
            Assert.AreEqual(2, config.Allies.Count);
        }

        [Test]
        public void test_stagecontroller_ally_stats_match_character_data()
        {
            var config = StageController.BuildBattleConfig(_stage, new List<CharacterData> { _allyData });
            Assert.AreEqual((int)_allyData.BaseStats.HP, config.Allies[0].Combatant.MaxHP);
        }

        [Test]
        public void test_stagecontroller_ally_slot_indices_sequential()
        {
            var allies = new List<CharacterData> { _allyData, _allyData, _allyData };
            var config = StageController.BuildBattleConfig(_stage, allies);
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(i, config.Allies[i].SlotIndex);
        }

        [Test]
        public void test_stagecontroller_ally_team_is_ally()
        {
            var config = StageController.BuildBattleConfig(_stage, new List<CharacterData> { _allyData });
            Assert.AreEqual(CombatTeam.Ally, config.Allies[0].Team);
        }

        [Test]
        public void test_stagecontroller_empty_allies_produces_empty_list()
        {
            var config = StageController.BuildBattleConfig(_stage, new List<CharacterData>());
            Assert.AreEqual(0, config.Allies.Count);
        }

        // ====================================================================
        // StageController — waves
        // ====================================================================

        [Test]
        public void test_stagecontroller_wave_count_matches_stage()
        {
            _stage.Waves.Add(new WaveDefinition
            {
                Enemies = new List<EnemySlot> { new EnemySlot { Enemy = _enemyData, SlotIndex = 0 } }
            });
            var config = StageController.BuildBattleConfig(_stage, new List<CharacterData> { _allyData });
            Assert.AreEqual(2, config.Waves.Count);
        }

        [Test]
        public void test_stagecontroller_enemy_count_per_wave_matches_stage()
        {
            _stage.Waves[0].Enemies.Add(new EnemySlot { Enemy = _enemyData, SlotIndex = 1 });
            var config = StageController.BuildBattleConfig(_stage, new List<CharacterData> { _allyData });
            Assert.AreEqual(2, config.Waves[0].Enemies.Count);
        }

        [Test]
        public void test_stagecontroller_enemy_team_is_enemy()
        {
            var config = StageController.BuildBattleConfig(_stage, new List<CharacterData> { _allyData });
            Assert.AreEqual(CombatTeam.Enemy, config.Waves[0].Enemies[0].Team);
        }

        [Test]
        public void test_stagecontroller_enemy_slot_index_preserved()
        {
            _stage.Waves[0].Enemies[0].SlotIndex = 2;
            var config = StageController.BuildBattleConfig(_stage, new List<CharacterData> { _allyData });
            Assert.AreEqual(2, config.Waves[0].Enemies[0].SlotIndex);
        }

        [Test]
        public void test_stagecontroller_enemy_captain_index_preserved()
        {
            _stage.Waves[0].EnemyCaptainIndex = 0;
            var config = StageController.BuildBattleConfig(_stage, new List<CharacterData> { _allyData });
            Assert.AreEqual(0, config.Waves[0].EnemyCaptainIndex);
        }

        [Test]
        public void test_stagecontroller_enemy_stats_match_character_data()
        {
            var config = StageController.BuildBattleConfig(_stage, new List<CharacterData> { _allyData });
            Assert.AreEqual((int)_enemyData.BaseStats.HP, config.Waves[0].Enemies[0].Combatant.MaxHP);
        }

        // ====================================================================
        // StageRegistry
        // ====================================================================

        [Test]
        public void test_stageregistry_get_by_id_returns_correct_stage()
        {
            var registry = ScriptableObject.CreateInstance<StageRegistry>();
            registry.Stages = new List<StageData> { _stage };
            Assert.AreEqual(_stage, registry.GetById("test_stage"));
            UnityEngine.Object.DestroyImmediate(registry);
        }

        [Test]
        public void test_stageregistry_get_by_id_unknown_returns_null()
        {
            var registry = ScriptableObject.CreateInstance<StageRegistry>();
            registry.Stages = new List<StageData> { _stage };
            Assert.IsNull(registry.GetById("nonexistent"));
            UnityEngine.Object.DestroyImmediate(registry);
        }

        [Test]
        public void test_stageregistry_get_by_id_null_returns_null()
        {
            var registry = ScriptableObject.CreateInstance<StageRegistry>();
            registry.Stages = new List<StageData> { _stage };
            Assert.IsNull(registry.GetById(null));
            UnityEngine.Object.DestroyImmediate(registry);
        }
    }
}
