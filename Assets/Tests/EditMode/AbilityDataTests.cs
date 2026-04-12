using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for AbilityData ScriptableObject integration with CombatManager.
    /// Covers ability resolution, MP costs, cooldowns, and secondary effects.
    /// See Combate Terrestre GDD §6 and ADR-003.
    /// </summary>
    [TestFixture]
    public class AbilityDataTests
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

        // --- Helpers ---

        private static CombatantState MakeCombatant(string id, float hp, float atk, float def,
            float spd, float mp = 100, Element element = Element.Neutral)
        {
            var data = ScriptableObject.CreateInstance<CharacterData>();
            data.Id = id;
            data.DisplayName = id;
            data.Element = element;
            data.BaseStats = new StatBlock
            {
                HP = hp, ATK = atk, DEF = def, MST = atk, SPR = def, SPD = spd, MP = mp
            };
            data.SecondaryStats = new SecondaryStatBlock { CRI = 0, LCK = 0 };

            var stats = new StatBlock
            {
                HP = hp, ATK = atk, DEF = def, MST = atk, SPR = def, SPD = spd, MP = mp
            };
            return new CombatantState(data, stats, 1);
        }

        private static InitiativeEntry MakeEntry(CombatantState c, CombatTeam team, int slot)
        {
            return new InitiativeEntry(c, team, slot);
        }

        private static AbilityData MakeAbility(string id, float power = 1.5f, int mpCost = 10,
            int cooldown = 0, Element element = Element.Tormenta, bool isPhysical = false,
            TargetType targetType = TargetType.SingleEnemy,
            AbilityCategory category = AbilityCategory.Damage)
        {
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            ability.Id = id;
            ability.DisplayName = id;
            ability.AbilityPower = power;
            ability.MPCost = mpCost;
            ability.Cooldown = cooldown;
            ability.Element = element;
            ability.IsPhysical = isPhysical;
            ability.TargetType = targetType;
            ability.Category = category;
            ability.SecondaryEffects = new List<AbilitySecondaryEffect>();
            return ability;
        }

        private BattleConfig MakeBattle(CombatantState ally, CombatantState enemy)
        {
            return new BattleConfig
            {
                Allies = new List<InitiativeEntry>
                    { MakeEntry(ally, CombatTeam.Ally, 0) },
                Waves = new List<WaveConfig>
                {
                    new WaveConfig
                    {
                        Enemies = new List<InitiativeEntry>
                            { MakeEntry(enemy, CombatTeam.Enemy, 0) }
                    }
                }
            };
        }

        // ====================================================================
        // ABILITY RESOLUTION
        // ====================================================================

        [Test]
        public void FromAbility_CreatesActionWithAbilityDataFields()
        {
            // Arrange
            var ability = MakeAbility("rayo", power: 2.0f, element: Element.Tormenta,
                isPhysical: false, targetType: TargetType.SingleEnemy);
            var target = MakeCombatant("enemy", 100, 30, 20, 70);

            // Act
            var action = CombatAction.FromAbility(ability, target);

            // Assert
            Assert.AreEqual(ActionType.Ability, action.Type);
            Assert.AreEqual(2.0f, action.AbilityPower);
            Assert.AreEqual(Element.Tormenta, action.Element);
            Assert.IsFalse(action.IsPhysical);
            Assert.AreEqual(TargetType.SingleEnemy, action.TargetType);
            Assert.AreEqual(target, action.Target);
            Assert.AreEqual(ability, action.AbilityData);
        }

        [Test]
        public void Ability_DealsDamageUsingAbilityPower()
        {
            // Arrange: ally (ATK=50, SPD=90) vs enemy (DEF=0, HP=200, SPD=70)
            var ally = MakeCombatant("ally", 100, 50, 20, 90);
            var enemy = MakeCombatant("enemy", 200, 30, 0, 70);
            var ability = MakeAbility("golpe_fuerte", power: 2.0f, isPhysical: true,
                element: Element.Neutral, mpCost: 5);
            var config = MakeBattle(ally, enemy);

            _manager.StartBattle(config);
            _manager.BeginRound();

            // Act: ally uses ability on enemy
            _manager.AdvanceTurn();
            int hpBefore = enemy.CurrentHP;
            _manager.ResolveAction(CombatAction.FromAbility(ability, enemy));

            // Assert: damage should be greater than 0 (AbilityPower 2.0 amplifies)
            int damage = hpBefore - enemy.CurrentHP;
            Assert.IsTrue(damage > 0, "Ability should deal damage");
        }

        // ====================================================================
        // MP COST
        // ====================================================================

        [Test]
        public void Ability_DeductsMPOnUse()
        {
            // Arrange
            var ally = MakeCombatant("ally", 100, 50, 20, 90, mp: 50);
            var enemy = MakeCombatant("enemy", 200, 30, 20, 70);
            var ability = MakeAbility("rayo", mpCost: 15);
            var config = MakeBattle(ally, enemy);

            _manager.StartBattle(config);
            _manager.BeginRound();

            // Act
            _manager.AdvanceTurn();
            _manager.ResolveAction(CombatAction.FromAbility(ability, enemy));

            // Assert
            Assert.AreEqual(35, ally.CurrentMP, "Should deduct 15 MP from 50");
        }

        [Test]
        public void IsAbilityReady_FalseWhenInsufficientMP()
        {
            // Arrange
            var ally = MakeCombatant("ally", 100, 50, 20, 90, mp: 5);
            var ability = MakeAbility("rayo", mpCost: 15);

            // Act & Assert
            Assert.IsFalse(ally.IsAbilityReady(ability),
                "Ability requiring 15 MP should not be ready with 5 MP");
        }

        [Test]
        public void IsAbilityReady_TrueWhenSufficientMP()
        {
            // Arrange
            var ally = MakeCombatant("ally", 100, 50, 20, 90, mp: 50);
            var ability = MakeAbility("rayo", mpCost: 15);

            // Act & Assert
            Assert.IsTrue(ally.IsAbilityReady(ability));
        }

        // ====================================================================
        // COOLDOWNS
        // ====================================================================

        [Test]
        public void Ability_ActivatesCooldownAfterUse()
        {
            // Arrange
            var ally = MakeCombatant("ally", 100, 50, 20, 90, mp: 100);
            var enemy = MakeCombatant("enemy", 200, 30, 20, 70);
            var ability = MakeAbility("maremoto", mpCost: 10, cooldown: 3);
            var config = MakeBattle(ally, enemy);

            _manager.StartBattle(config);
            _manager.BeginRound();

            // Act
            _manager.AdvanceTurn();
            _manager.ResolveAction(CombatAction.FromAbility(ability, enemy));

            // Assert
            Assert.AreEqual(3, ally.GetCooldownRemaining(ability));
            Assert.IsFalse(ally.IsAbilityReady(ability),
                "Ability should not be ready while on cooldown");
        }

        [Test]
        public void Cooldown_TicksDownEachTurn()
        {
            // Arrange
            var ally = MakeCombatant("ally", 100, 50, 20, 90, mp: 100);
            var ability = MakeAbility("maremoto", mpCost: 10, cooldown: 2);

            // Activate cooldown manually
            ally.ActivateCooldown(ability);
            Assert.AreEqual(2, ally.GetCooldownRemaining(ability));

            // Act: tick once
            ally.TickCooldowns();
            Assert.AreEqual(1, ally.GetCooldownRemaining(ability));
            Assert.IsFalse(ally.IsAbilityReady(ability));

            // Act: tick again — should be ready
            ally.TickCooldowns();
            Assert.AreEqual(0, ally.GetCooldownRemaining(ability));
            Assert.IsTrue(ally.IsAbilityReady(ability));
        }

        [Test]
        public void Cooldown_TicksDuringCombatTurn()
        {
            // Arrange: 1v1, ally faster. Ability with 1-turn cooldown.
            var ally = MakeCombatant("ally", 100, 50, 20, 90, mp: 100);
            var enemy = MakeCombatant("enemy", 200, 30, 20, 70);
            var ability = MakeAbility("golpe", mpCost: 5, cooldown: 1);
            var config = MakeBattle(ally, enemy);

            _manager.StartBattle(config);

            // Round 1: use ability
            _manager.BeginRound();
            _manager.AdvanceTurn(); // ally
            _manager.ResolveAction(CombatAction.FromAbility(ability, enemy));
            Assert.AreEqual(1, ally.GetCooldownRemaining(ability));
            _manager.CompleteTurn();

            _manager.AdvanceTurn(); // enemy
            _manager.ResolveAction(CombatAction.PassTurn());
            _manager.CompleteTurn();

            // Round 2: ally's turn ticks cooldown (step 1 of turn processing)
            _manager.BeginRound();
            _manager.AdvanceTurn(); // ally — cooldown ticks here
            Assert.AreEqual(0, ally.GetCooldownRemaining(ability),
                "Cooldown should tick at start of combatant's turn");
            Assert.IsTrue(ally.IsAbilityReady(ability));
        }

        // ====================================================================
        // SILENCE BLOCKS ABILITIES
        // ====================================================================

        [Test]
        public void IsAbilityReady_FalseWhenSilenced()
        {
            // Arrange
            var ally = MakeCombatant("ally", 100, 50, 20, 90, mp: 100);
            var ability = MakeAbility("rayo", mpCost: 5);
            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Silencio,
                RemainingTurns = 2
            });

            // Act & Assert
            Assert.IsFalse(ally.IsAbilityReady(ability),
                "Silenced units cannot use abilities");
        }

        // ====================================================================
        // SECONDARY EFFECTS
        // ====================================================================

        [Test]
        public void Ability_AppliesSecondaryEffect_WhenProbability100()
        {
            // Arrange
            var ally = MakeCombatant("ally", 100, 50, 20, 90, mp: 100);
            var enemy = MakeCombatant("enemy", 200, 30, 20, 70);
            var ability = MakeAbility("golpe_venenoso", mpCost: 10, isPhysical: true,
                element: Element.Neutral);
            ability.SecondaryEffects.Add(new AbilitySecondaryEffect
            {
                Effect = StatusEffect.Veneno,
                Probability = 1.0f, // guaranteed
                Duration = 3,
                Param = 0.05f
            });
            var config = MakeBattle(ally, enemy);

            StatusAppliedEvent? statusEvent = null;
            GameEvents.OnStatusApplied += e => statusEvent = e;

            _manager.StartBattle(config);
            _manager.BeginRound();

            // Act
            _manager.AdvanceTurn();
            _manager.ResolveAction(CombatAction.FromAbility(ability, enemy));

            // Assert
            Assert.IsTrue(enemy.HasStatus(StatusEffect.Veneno),
                "100% probability effect should always apply");
            Assert.IsNotNull(statusEvent);
            Assert.AreEqual(StatusEffect.Veneno, statusEvent.Value.Status.Effect);
        }

        [Test]
        public void Ability_DoesNotApplySecondaryEffect_WhenProbability0()
        {
            // Arrange
            var ally = MakeCombatant("ally", 100, 50, 20, 90, mp: 100);
            var enemy = MakeCombatant("enemy", 200, 30, 20, 70);
            var ability = MakeAbility("golpe_debil", mpCost: 5, isPhysical: true,
                element: Element.Neutral);
            ability.SecondaryEffects.Add(new AbilitySecondaryEffect
            {
                Effect = StatusEffect.Aturdimiento,
                Probability = 0.0f, // never
                Duration = 1,
                Param = 0f
            });
            var config = MakeBattle(ally, enemy);

            _manager.StartBattle(config);
            _manager.BeginRound();

            // Act
            _manager.AdvanceTurn();
            _manager.ResolveAction(CombatAction.FromAbility(ability, enemy));

            // Assert
            Assert.IsFalse(enemy.HasStatus(StatusEffect.Aturdimiento),
                "0% probability effect should never apply");
        }

        // ====================================================================
        // HEAL ABILITY
        // ====================================================================

        [Test]
        public void HealAbility_RestoresAllyHP()
        {
            // Arrange: 2 allies, healer faster
            var healer = MakeCombatant("healer", 100, 20, 20, 90, mp: 50);
            var tank = MakeCombatant("tank", 200, 50, 40, 80, mp: 20);
            var enemy = MakeCombatant("enemy", 100, 30, 20, 70);

            // Damage the tank first
            tank.ApplyDamage(80);
            Assert.AreEqual(120, tank.CurrentHP);

            var healAbility = MakeAbility("curar", mpCost: 15, power: 2.0f,
                isPhysical: false, element: Element.Neutral,
                targetType: TargetType.SingleAlly, category: AbilityCategory.Heal);
            healAbility.HealPower = 2.0f;

            var config = new BattleConfig
            {
                Allies = new List<InitiativeEntry>
                {
                    MakeEntry(healer, CombatTeam.Ally, 0),
                    MakeEntry(tank, CombatTeam.Ally, 1)
                },
                Waves = new List<WaveConfig>
                {
                    new WaveConfig
                    {
                        Enemies = new List<InitiativeEntry>
                            { MakeEntry(enemy, CombatTeam.Enemy, 0) }
                    }
                }
            };

            HealEvent? healEvent = null;
            GameEvents.OnHealApplied += e => healEvent = e;

            _manager.StartBattle(config);
            _manager.BeginRound();

            // Act: healer heals tank
            _manager.AdvanceTurn(); // healer (SPD 90)
            _manager.ResolveAction(CombatAction.FromAbility(healAbility, tank));

            // Assert
            Assert.IsTrue(tank.CurrentHP > 120, "Tank should be healed above 120 HP");
            Assert.AreEqual(35, healer.CurrentMP, "Healer should spend 15 MP");
            Assert.IsNotNull(healEvent);
        }

        // ====================================================================
        // AOE ABILITY
        // ====================================================================

        [Test]
        public void AoeAbility_HitsAllEnemies()
        {
            // Arrange
            var ally = MakeCombatant("ally", 100, 50, 20, 90, mp: 100);
            var enemy1 = MakeCombatant("enemy1", 200, 30, 0, 70);
            var enemy2 = MakeCombatant("enemy2", 200, 30, 0, 60);
            var aoeAbility = MakeAbility("maremoto", mpCost: 20, power: 1.5f,
                isPhysical: true, element: Element.Neutral,
                targetType: TargetType.AoeEnemy);

            var config = new BattleConfig
            {
                Allies = new List<InitiativeEntry>
                    { MakeEntry(ally, CombatTeam.Ally, 0) },
                Waves = new List<WaveConfig>
                {
                    new WaveConfig
                    {
                        Enemies = new List<InitiativeEntry>
                        {
                            MakeEntry(enemy1, CombatTeam.Enemy, 0),
                            MakeEntry(enemy2, CombatTeam.Enemy, 1)
                        }
                    }
                }
            };

            var damageEvents = new List<DamageEvent>();
            GameEvents.OnDamageDealt += e => damageEvents.Add(e);

            _manager.StartBattle(config);
            _manager.BeginRound();

            // Act
            _manager.AdvanceTurn();
            _manager.ResolveAction(CombatAction.FromAbility(aoeAbility, null));

            // Assert
            Assert.IsTrue(enemy1.CurrentHP < 200, "Enemy1 should take damage");
            Assert.IsTrue(enemy2.CurrentHP < 200, "Enemy2 should take damage");
            Assert.AreEqual(2, damageEvents.Count, "Should emit damage event per target");
            Assert.AreEqual(80, ally.CurrentMP, "Should deduct 20 MP");
        }
    }
}
