using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BlacktideRequiem.Core.AI;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for EnemyAI decision-making across AI profiles.
    /// See Enemy System GDD §7 and ADR-003 §4.
    /// </summary>
    [TestFixture]
    public class EnemyAITests
    {
        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
        }

        // --- Helpers ---

        private static CombatantState MakeCombatant(string id, float hp, float atk, float def,
            float spd, float mst = 0, Element element = Element.Neutral,
            List<AbilityEntry> abilities = null)
        {
            var data = ScriptableObject.CreateInstance<CharacterData>();
            data.Id = id;
            data.DisplayName = id;
            data.Element = element;
            data.BaseStats = new StatBlock
            {
                HP = hp, ATK = atk, DEF = def,
                MST = mst > 0 ? mst : atk, SPR = def, SPD = spd, MP = 100
            };
            data.SecondaryStats = new SecondaryStatBlock { CRI = 0, LCK = 0 };
            data.LandAbilities = abilities ?? new List<AbilityEntry>();

            var stats = new StatBlock
            {
                HP = hp, ATK = atk, DEF = def,
                MST = mst > 0 ? mst : atk, SPR = def, SPD = spd, MP = 100
            };
            return new CombatantState(data, stats, 1);
        }

        private static AbilityData MakeAbility(string id, float power = 1.5f,
            AbilityCategory category = AbilityCategory.Damage,
            TargetType targetType = TargetType.SingleEnemy,
            int cooldown = 0, int mpCost = 0)
        {
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            ability.Id = id;
            ability.DisplayName = id;
            ability.AbilityPower = power;
            ability.Category = category;
            ability.TargetType = targetType;
            ability.IsPhysical = true;
            ability.Element = Element.Neutral;
            ability.Cooldown = cooldown;
            ability.MPCost = mpCost;
            ability.SecondaryEffects = new List<AbilitySecondaryEffect>();
            return ability;
        }

        private static CombatContext MakeContext(CombatantState actor,
            List<CombatantState> allies, List<CombatantState> enemies)
        {
            return new CombatContext
            {
                Actor = actor,
                Allies = allies,
                Enemies = enemies
            };
        }

        private static CombatAction GetAIAction(EnemyAI ai, CombatContext context)
        {
            CombatAction result = default;
            ai.RequestAction(context, action => result = action);
            return result;
        }

        // ====================================================================
        // AGRESIVO — targets lowest HP, highest damage ability
        // ====================================================================

        [Test]
        public void Agresivo_TargetsLowestHPEnemy()
        {
            // Arrange
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80);
            var allyHigh = MakeCombatant("ally_high", 200, 30, 20, 70);
            var allyLow = MakeCombatant("ally_low", 50, 30, 20, 60);
            allyLow.ApplyDamage(30); // 20 HP remaining

            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { allyHigh, allyLow });

            var ai = new EnemyAI(AIProfileType.Agresivo);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.AreEqual(allyLow, action.Target,
                "Agresivo should target the ally with lowest current HP");
        }

        [Test]
        public void Agresivo_UsesHighestDamageAbility()
        {
            // Arrange
            var weakAbility = MakeAbility("slash", power: 1.2f);
            var strongAbility = MakeAbility("mega_slash", power: 2.5f);
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80, abilities: new List<AbilityEntry>
            {
                new AbilityEntry { Ability = weakAbility, UnlockLevel = 1 },
                new AbilityEntry { Ability = strongAbility, UnlockLevel = 1 }
            });
            var ally = MakeCombatant("ally", 100, 30, 20, 70);

            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Agresivo);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.AreEqual(ActionType.Ability, action.Type);
            Assert.AreEqual(strongAbility, action.AbilityData,
                "Agresivo should pick the highest AbilityPower ability");
        }

        [Test]
        public void Agresivo_UsesBasicAttackWhenNoAbilities()
        {
            // Arrange: enemy with no abilities
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80);
            var ally = MakeCombatant("ally", 100, 30, 20, 70);

            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Agresivo);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.AreEqual(ActionType.Attack, action.Type);
            Assert.AreEqual(ally, action.Target);
        }

        [Test]
        public void Agresivo_UsesBasicAttackWhenSilenced()
        {
            // Arrange: enemy with abilities but silenced
            var ability = MakeAbility("fireball", power: 2.0f);
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80, abilities: new List<AbilityEntry>
            {
                new AbilityEntry { Ability = ability, UnlockLevel = 1 }
            });
            enemy.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Silencio,
                RemainingTurns = 2
            });
            var ally = MakeCombatant("ally", 100, 30, 20, 70);

            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Agresivo);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.AreEqual(ActionType.Attack, action.Type,
                "Silenced enemies must fall back to basic attack");
        }

        [Test]
        public void Agresivo_SkipsAbilityOnCooldown()
        {
            // Arrange
            var ability = MakeAbility("fireball", power: 2.0f, cooldown: 3);
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80, abilities: new List<AbilityEntry>
            {
                new AbilityEntry { Ability = ability, UnlockLevel = 1 }
            });
            enemy.ActivateCooldown(ability);
            var ally = MakeCombatant("ally", 100, 30, 20, 70);

            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Agresivo);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.AreEqual(ActionType.Attack, action.Type,
                "Should use basic attack when ability is on cooldown");
        }

        [Test]
        public void Agresivo_IgnoresMPCostForAbilitySelection()
        {
            // Arrange: enemy with 0 MP but has an ability with MP cost
            var ability = MakeAbility("expensive_move", power: 2.0f, mpCost: 50);
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80, abilities: new List<AbilityEntry>
            {
                new AbilityEntry { Ability = ability, UnlockLevel = 1 }
            });
            enemy.ConsumeMP(100); // drain all MP
            Assert.AreEqual(0, enemy.CurrentMP);

            var ally = MakeCombatant("ally", 100, 30, 20, 70);

            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Agresivo);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.AreEqual(ActionType.Ability, action.Type,
                "Enemies should ignore MP costs per GDD");
            Assert.AreEqual(ability, action.AbilityData);
        }

        // ====================================================================
        // DEFENSIVO — buffs when no buffs active, else attacks
        // ====================================================================

        [Test]
        public void Defensivo_UsesBuffAbilityWhenNoBuffsActive()
        {
            // Arrange
            var buffAbility = MakeAbility("iron_wall", power: 1.0f,
                category: AbilityCategory.Buff, targetType: TargetType.Self);
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80, abilities: new List<AbilityEntry>
            {
                new AbilityEntry { Ability = buffAbility, UnlockLevel = 1 }
            });
            var ally = MakeCombatant("ally", 100, 30, 20, 70);

            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Defensivo);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.AreEqual(ActionType.Ability, action.Type);
            Assert.AreEqual(buffAbility, action.AbilityData,
                "Defensivo should use buff ability when no buffs active");
        }

        [Test]
        public void Defensivo_GuardsWhenNoBuffAbilityAvailable()
        {
            // Arrange: enemy with only damage abilities, no buffs
            var damageAbility = MakeAbility("slash", power: 1.5f,
                category: AbilityCategory.Damage);
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80, abilities: new List<AbilityEntry>
            {
                new AbilityEntry { Ability = damageAbility, UnlockLevel = 1 }
            });
            var ally = MakeCombatant("ally", 100, 30, 20, 70);

            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Defensivo);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.AreEqual(ActionType.Guard, action.Type,
                "Defensivo with no buff ability should Guard when no buffs active");
        }

        [Test]
        public void Defensivo_AttacksWhenBuffsAlreadyActive()
        {
            // Arrange: enemy already has a buff
            var buffAbility = MakeAbility("iron_wall", power: 1.0f,
                category: AbilityCategory.Buff, targetType: TargetType.Self);
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80, abilities: new List<AbilityEntry>
            {
                new AbilityEntry { Ability = buffAbility, UnlockLevel = 1 }
            });
            enemy.Buffs.Add(new BuffInstance
            {
                StatAffected = StatType.DEF,
                Percentage = 0.30f,
                RemainingTurns = 3,
                IsDispellable = true
            });
            var ally = MakeCombatant("ally", 100, 30, 20, 70);

            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Defensivo);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.AreNotEqual(ActionType.Guard, action.Type,
                "Defensivo should attack when buffs are already active");
            Assert.AreEqual(ally, action.Target);
        }

        [Test]
        public void Defensivo_BuffsLowestHPAlly()
        {
            // Arrange: two enemies, one hurt
            var buffAbility = MakeAbility("iron_wall", power: 1.0f,
                category: AbilityCategory.Buff, targetType: TargetType.AllySingle);
            var enemy1 = MakeCombatant("enemy1", 100, 50, 20, 80, abilities: new List<AbilityEntry>
            {
                new AbilityEntry { Ability = buffAbility, UnlockLevel = 1 }
            });
            var enemy2 = MakeCombatant("enemy2", 100, 30, 20, 70);
            enemy2.ApplyDamage(60); // 40 HP remaining

            var ally = MakeCombatant("ally", 100, 30, 20, 60);

            var context = MakeContext(enemy1,
                allies: new List<CombatantState> { enemy1, enemy2 },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Defensivo);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.AreEqual(ActionType.Ability, action.Type);
            Assert.AreEqual(enemy2, action.Target,
                "Defensivo should target the lowest HP ally for buff");
        }

        // ====================================================================
        // CAOTICO — random target, random ability
        // ====================================================================

        [Test]
        public void Caotico_SelectsValidTarget()
        {
            // Arrange
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80);
            var ally1 = MakeCombatant("ally1", 100, 30, 20, 70);
            var ally2 = MakeCombatant("ally2", 100, 30, 20, 60);

            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally1, ally2 });

            var ai = new EnemyAI(AIProfileType.Caotico);

            // Act: run multiple times, verify target is always valid
            for (int i = 0; i < 20; i++)
            {
                var action = GetAIAction(ai, context);
                Assert.IsTrue(action.Target == ally1 || action.Target == ally2,
                    "Caotico must select a valid enemy target");
            }
        }

        [Test]
        public void Caotico_UsesAbilitiesWhenAvailable()
        {
            // Arrange
            var ability1 = MakeAbility("slash", power: 1.2f);
            var ability2 = MakeAbility("fireball", power: 2.0f);
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80, abilities: new List<AbilityEntry>
            {
                new AbilityEntry { Ability = ability1, UnlockLevel = 1 },
                new AbilityEntry { Ability = ability2, UnlockLevel = 1 }
            });
            var ally = MakeCombatant("ally", 100, 30, 20, 70);

            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Caotico);

            // Act: run 50 times, at least some should use abilities
            int abilityCount = 0;
            for (int i = 0; i < 50; i++)
            {
                var action = GetAIAction(ai, context);
                if (action.Type == ActionType.Ability)
                    abilityCount++;
            }

            Assert.IsTrue(abilityCount > 0,
                "Caotico should sometimes use abilities when available");
        }

        [Test]
        public void Caotico_PassesWhenNoEnemies()
        {
            // Arrange
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80);
            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState>());

            var ai = new EnemyAI(AIProfileType.Caotico);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.AreEqual(ActionType.Pass, action.Type,
                "Should pass when no valid targets exist");
        }

        // ====================================================================
        // ICOMBATINPUT INTEGRATION
        // ====================================================================

        [Test]
        public void RequestAction_InvokesCallbackImmediately()
        {
            // Arrange
            var enemy = MakeCombatant("enemy", 100, 50, 20, 80);
            var ally = MakeCombatant("ally", 100, 30, 20, 70);
            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Agresivo);
            bool callbackInvoked = false;

            // Act
            ai.RequestAction(context, action =>
            {
                callbackInvoked = true;
                Assert.IsNotNull(action.Target);
            });

            // Assert
            Assert.IsTrue(callbackInvoked,
                "Enemy AI should invoke callback immediately (synchronous)");
        }

        [Test]
        public void PhysicalAttacker_UsesATKForBasicAttack()
        {
            // Arrange: ATK > MST → physical attacker
            var enemy = MakeCombatant("enemy", 100, 80, 20, 80, mst: 30);
            var ally = MakeCombatant("ally", 100, 30, 20, 70);
            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Agresivo);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.IsTrue(action.IsPhysical,
                "Enemy with ATK > MST should use physical basic attack");
        }

        [Test]
        public void MagicalAttacker_UsesMSTForBasicAttack()
        {
            // Arrange: MST > ATK → magical attacker
            var enemy = MakeCombatant("enemy", 100, 30, 20, 80, mst: 80);
            var ally = MakeCombatant("ally", 100, 30, 20, 70);
            var context = MakeContext(enemy,
                allies: new List<CombatantState> { enemy },
                enemies: new List<CombatantState> { ally });

            var ai = new EnemyAI(AIProfileType.Agresivo);

            // Act
            var action = GetAIAction(ai, context);

            // Assert
            Assert.IsFalse(action.IsPhysical,
                "Enemy with MST > ATK should use magical basic attack");
        }
    }
}
