using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for status effect duration, ticking, removal, sleep-wake,
    /// Muerte threshold, and Silencio ability blocking.
    /// Covers S2-07 acceptance criteria.
    /// </summary>
    [TestFixture]
    public class StatusEffectTests
    {
        private CombatManager _manager;
        private InitiativeBar _bar;

        [SetUp]
        public void SetUp()
        {
            _bar = new InitiativeBar();
            _manager = new CombatManager(_bar);
            GameEvents.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAll();
        }

        // --- Helpers ---

        private static CombatantState MakeCombatant(string id, float hp, float atk, float def,
            float spd, Element element = Element.Neutral, bool isBoss = false)
        {
            var data = ScriptableObject.CreateInstance<CharacterData>();
            data.Id = id;
            data.DisplayName = id;
            data.Element = element;
            data.BaseStats = new StatBlock { HP = hp, ATK = atk, DEF = def, MST = atk, SPR = def, SPD = spd };
            data.SecondaryStats = new SecondaryStatBlock { CRI = 0, LCK = 0 };

            var stats = new StatBlock { HP = hp, ATK = atk, DEF = def, MST = atk, SPR = def, SPD = spd };
            return new CombatantState(data, stats, 1) { IsBoss = isBoss };
        }

        private static InitiativeEntry MakeEntry(CombatantState c, CombatTeam team, int slot)
        {
            return new InitiativeEntry(c, team, slot);
        }

        private BattleConfig MakeOnePairBattle(CombatantState ally, CombatantState enemy)
        {
            return new BattleConfig
            {
                Allies = new List<InitiativeEntry>
                {
                    MakeEntry(ally, CombatTeam.Ally, 0)
                },
                Waves = new List<WaveConfig>
                {
                    new WaveConfig
                    {
                        EnemyCaptainIndex = -1,
                        Enemies = new List<InitiativeEntry>
                        {
                            MakeEntry(enemy, CombatTeam.Enemy, 0)
                        }
                    }
                }
            };
        }

        /// <summary>Run a full round (advance all turns, pass, complete).</summary>
        private void RunFullRound()
        {
            _manager.BeginRound();
            while (true)
            {
                var entry = _manager.AdvanceTurn();
                if (entry == null) break;
                _manager.ResolveAction(CombatAction.PassTurn());
                _manager.CompleteTurn();
            }
        }

        // ====================================================================
        // STATUS DURATION TICKING
        // ====================================================================

        [Test]
        public void test_status_duration_decrements_each_turn()
        {
            // Arrange: ally with 3-turn poison, faster than enemy
            var ally = MakeCombatant("ally", 200, 50, 20, 90);
            var enemy = MakeCombatant("enemy", 200, 30, 20, 70);
            var config = MakeOnePairBattle(ally, enemy);
            _manager.StartBattle(config);

            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Veneno,
                RemainingTurns = 3,
                Param = 0.05f
            });

            // Act: run 1 round — ally's turn ticks status from 3→2
            RunFullRound();

            // Assert: status still present with 2 turns left
            Assert.IsTrue(ally.HasStatus(StatusEffect.Veneno));
            Assert.AreEqual(2, ally.StatusEffects[0].RemainingTurns);
        }

        [Test]
        public void test_status_removed_when_duration_expires()
        {
            // Arrange: ally with 1-turn stun
            var ally = MakeCombatant("ally", 200, 50, 20, 90);
            var enemy = MakeCombatant("enemy", 200, 30, 20, 70);
            var config = MakeOnePairBattle(ally, enemy);
            _manager.StartBattle(config);

            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Ceguera,
                RemainingTurns = 1,
                Param = 0f
            });

            // Act: run 1 round — ticks from 1→0, removed
            RunFullRound();

            // Assert
            Assert.IsFalse(ally.HasStatus(StatusEffect.Ceguera),
                "Ceguera with 1-turn duration should expire after 1 turn tick");
        }

        [Test]
        public void test_status_expired_fires_removal_event()
        {
            // Arrange
            var ally = MakeCombatant("ally", 200, 50, 20, 90);
            var enemy = MakeCombatant("enemy", 200, 30, 20, 70);
            var config = MakeOnePairBattle(ally, enemy);
            _manager.StartBattle(config);

            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Ceguera,
                RemainingTurns = 1,
                Param = 0f
            });

            StatusRemovedEvent? removedEvent = null;
            GameEvents.OnStatusRemoved += e => removedEvent = e;

            // Act
            RunFullRound();

            // Assert
            Assert.IsNotNull(removedEvent);
            Assert.AreEqual(StatusEffect.Ceguera, removedEvent.Value.Effect);
            Assert.AreEqual(StatusRemovalReason.Expired, removedEvent.Value.Reason);
            Assert.AreEqual(ally, removedEvent.Value.Target);
        }

        // ====================================================================
        // STATUS REFRESH
        // ====================================================================

        [Test]
        public void test_status_reapply_refreshes_duration()
        {
            // Arrange
            var unit = MakeCombatant("unit", 100, 50, 20, 80);
            unit.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Veneno,
                RemainingTurns = 2,
                Param = 0.05f
            });

            // Act: reapply with longer duration
            unit.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Veneno,
                RemainingTurns = 5,
                Param = 0.10f
            });

            // Assert: refreshed, not stacked
            Assert.AreEqual(1, unit.StatusEffects.Count);
            Assert.AreEqual(5, unit.StatusEffects[0].RemainingTurns);
            Assert.AreEqual(0.10f, unit.StatusEffects[0].Param, 0.001f);
        }

        // ====================================================================
        // MULTIPLE STATUSES
        // ====================================================================

        [Test]
        public void test_status_multiple_tick_independently()
        {
            // Arrange: unit with 2-turn poison and 1-turn blindness
            var ally = MakeCombatant("ally", 200, 50, 20, 90);
            var enemy = MakeCombatant("enemy", 200, 30, 20, 70);
            var config = MakeOnePairBattle(ally, enemy);
            _manager.StartBattle(config);

            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Veneno,
                RemainingTurns = 2,
                Param = 0.05f
            });
            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Ceguera,
                RemainingTurns = 1,
                Param = 0f
            });

            // Act: 1 round
            RunFullRound();

            // Assert: blindness expired, poison remains
            Assert.IsTrue(ally.HasStatus(StatusEffect.Veneno));
            Assert.IsFalse(ally.HasStatus(StatusEffect.Ceguera));
        }

        // ====================================================================
        // SLEEP — WAKE ON DAMAGE
        // ====================================================================

        [Test]
        public void test_status_sleep_removed_by_damage()
        {
            // Arrange: enemy faster (SPD 95), ally sleeping (SPD 80)
            var ally = MakeCombatant("ally", 200, 50, 20, 80);
            var enemy = MakeCombatant("enemy", 200, 50, 20, 95);
            var config = MakeOnePairBattle(ally, enemy);
            _manager.StartBattle(config);
            _manager.BeginRound();

            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Sueno,
                RemainingTurns = 3
            });

            StatusRemovedEvent? removedEvent = null;
            GameEvents.OnStatusRemoved += e => removedEvent = e;

            // Act: enemy attacks sleeping ally
            var entry = _manager.AdvanceTurn(); // enemy (faster)
            Assert.AreEqual("enemy", entry.Combatant.Template.Id);

            var attack = CombatAction.BasicAttack(ally, true);
            _manager.ResolveAction(attack);
            _manager.CompleteTurn();

            // Assert: sleep removed
            Assert.IsFalse(ally.HasStatus(StatusEffect.Sueno),
                "Sleep should be removed when target takes damage");
            Assert.IsNotNull(removedEvent);
            Assert.AreEqual(StatusEffect.Sueno, removedEvent.Value.Effect);
            Assert.AreEqual(StatusRemovalReason.WokenByDamage, removedEvent.Value.Reason);
        }

        // ====================================================================
        // MUERTE — INSTANT KILL THRESHOLD
        // ====================================================================

        [Test]
        public void test_status_muerte_kills_below_threshold()
        {
            // Arrange: enemy at 20% HP, Muerte threshold 30%
            var ally = MakeCombatant("ally", 200, 50, 20, 90);
            var enemy = MakeCombatant("enemy", 100, 30, 0, 70);
            var config = MakeOnePairBattle(ally, enemy);
            _manager.StartBattle(config);
            _manager.BeginRound();

            // Weaken enemy to 20 HP (20%)
            enemy.ApplyDamage(80);
            Assert.AreEqual(20, enemy.CurrentHP);

            // Create ability with Muerte secondary effect (100% chance, threshold 30%)
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            ability.Id = "soul_reap";
            ability.AbilityPower = 0.01f; // minimal damage, Muerte does the work
            ability.IsPhysical = true;
            ability.Element = Element.Neutral;
            ability.TargetType = TargetType.SingleEnemy;
            ability.Category = AbilityCategory.Damage;
            ability.MPCost = 0;
            ability.Cooldown = 0;
            ability.SecondaryEffects = new List<AbilitySecondaryEffect>
            {
                new AbilitySecondaryEffect
                {
                    Effect = StatusEffect.Muerte,
                    Probability = 1.0f,
                    Duration = 1,
                    Param = 0.30f // 30% threshold
                }
            };

            var entry = _manager.AdvanceTurn(); // ally first
            var action = new CombatAction
            {
                Type = ActionType.Ability,
                Target = enemy,
                AbilityData = ability,
                AbilityPower = ability.AbilityPower,
                Element = ability.Element,
                IsPhysical = ability.IsPhysical,
                TargetType = ability.TargetType
            };
            _manager.ResolveAction(action);

            // Assert: enemy killed by Muerte
            Assert.IsTrue(enemy.IsKO, "Muerte should instant kill target below 30% HP");
        }

        [Test]
        public void test_status_muerte_blocked_on_boss()
        {
            // Arrange: boss enemy at 10% HP
            var ally = MakeCombatant("ally", 200, 50, 20, 90);
            var enemy = MakeCombatant("enemy", 100, 30, 0, 70, isBoss: true);
            var config = MakeOnePairBattle(ally, enemy);
            _manager.StartBattle(config);
            _manager.BeginRound();

            // Weaken boss to 10 HP (10%)
            enemy.ApplyDamage(90);

            var ability = ScriptableObject.CreateInstance<AbilityData>();
            ability.Id = "soul_reap";
            ability.AbilityPower = 0.01f;
            ability.IsPhysical = true;
            ability.Element = Element.Neutral;
            ability.TargetType = TargetType.SingleEnemy;
            ability.Category = AbilityCategory.Damage;
            ability.MPCost = 0;
            ability.Cooldown = 0;
            ability.SecondaryEffects = new List<AbilitySecondaryEffect>
            {
                new AbilitySecondaryEffect
                {
                    Effect = StatusEffect.Muerte,
                    Probability = 1.0f,
                    Duration = 1,
                    Param = 0.50f // 50% threshold — boss at 10%, still immune
                }
            };

            var entry = _manager.AdvanceTurn();
            var action = new CombatAction
            {
                Type = ActionType.Ability,
                Target = enemy,
                AbilityData = ability,
                AbilityPower = ability.AbilityPower,
                Element = ability.Element,
                IsPhysical = ability.IsPhysical,
                TargetType = ability.TargetType
            };
            _manager.ResolveAction(action);

            // Assert: boss survives (may take normal ability damage, but not Muerte kill)
            // Boss had 10 HP, ability power 0.01 so normal damage is minimal
            Assert.IsFalse(enemy.IsKO, "Bosses should be immune to Muerte instant kill");
        }

        // ====================================================================
        // SILENCIO — ABILITY BLOCKED
        // ====================================================================

        [Test]
        public void test_status_silencio_blocks_ability_readiness()
        {
            // Arrange
            var unit = MakeCombatant("unit", 100, 50, 20, 80);
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            ability.Id = "fireball";
            ability.MPCost = 0;
            ability.Cooldown = 0;

            Assert.IsTrue(unit.IsAbilityReady(ability), "Ability should be ready before Silencio");

            // Act: apply Silencio
            unit.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Silencio,
                RemainingTurns = 2
            });

            // Assert
            Assert.IsFalse(unit.IsAbilityReady(ability),
                "Silencio should block ability readiness");
        }

        // ====================================================================
        // TICK STATUSES UNIT TEST (CombatantState only)
        // ====================================================================

        [Test]
        public void test_status_tick_returns_expired_effects()
        {
            // Arrange
            var unit = MakeCombatant("unit", 100, 50, 20, 80);
            unit.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Ceguera,
                RemainingTurns = 1
            });
            unit.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Veneno,
                RemainingTurns = 3,
                Param = 0.05f
            });

            // Act
            var expired = unit.TickStatuses();

            // Assert: blindness expired, poison ticked down
            Assert.AreEqual(1, expired.Count);
            Assert.AreEqual(StatusEffect.Ceguera, expired[0]);
            Assert.AreEqual(1, unit.StatusEffects.Count);
            Assert.AreEqual(StatusEffect.Veneno, unit.StatusEffects[0].Effect);
            Assert.AreEqual(2, unit.StatusEffects[0].RemainingTurns);
        }

        // ====================================================================
        // MUERTE ABOVE THRESHOLD — NO KILL
        // ====================================================================

        [Test]
        public void test_status_muerte_no_kill_above_threshold()
        {
            // Arrange: enemy at 50% HP, Muerte threshold 30%
            var ally = MakeCombatant("ally", 200, 50, 20, 90);
            var enemy = MakeCombatant("enemy", 100, 30, 0, 70);
            var config = MakeOnePairBattle(ally, enemy);
            _manager.StartBattle(config);
            _manager.BeginRound();

            enemy.ApplyDamage(50); // 50% HP
            int hpBefore = enemy.CurrentHP;

            var ability = ScriptableObject.CreateInstance<AbilityData>();
            ability.Id = "soul_reap";
            ability.AbilityPower = 0.01f;
            ability.IsPhysical = true;
            ability.Element = Element.Neutral;
            ability.TargetType = TargetType.SingleEnemy;
            ability.Category = AbilityCategory.Damage;
            ability.MPCost = 0;
            ability.Cooldown = 0;
            ability.SecondaryEffects = new List<AbilitySecondaryEffect>
            {
                new AbilitySecondaryEffect
                {
                    Effect = StatusEffect.Muerte,
                    Probability = 1.0f,
                    Duration = 1,
                    Param = 0.30f // 30% — enemy at 50%, above threshold
                }
            };

            var entry = _manager.AdvanceTurn();
            var action = new CombatAction
            {
                Type = ActionType.Ability,
                Target = enemy,
                AbilityData = ability,
                AbilityPower = ability.AbilityPower,
                Element = ability.Element,
                IsPhysical = ability.IsPhysical,
                TargetType = ability.TargetType
            };
            _manager.ResolveAction(action);

            // Assert: enemy survives — Muerte doesn't trigger above threshold
            Assert.IsFalse(enemy.IsKO,
                "Muerte should NOT kill if target HP% is above threshold");
        }
    }
}
