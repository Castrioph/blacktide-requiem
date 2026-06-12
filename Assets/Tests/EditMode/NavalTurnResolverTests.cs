using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for NavalTurnResolver (S4-04): the 6 naval actions, naval DoT
    /// split (Quemadura→HHP, Veneno/Sangrado→random living crew), naval LB,
    /// wave persistence and victory/defeat. Covers Combate Naval GDD
    /// AC 1-24 + 33-37. See ADR-004 §3.
    ///
    /// Damage rolls use random variance (0.95–1.05), so hull/crew damage
    /// asserts use the corresponding min/max range. Repair, DoTs and stat
    /// recalculation are deterministic and assert exact values.
    /// </summary>
    [TestFixture]
    public class NavalTurnResolverTests
    {
        private CombatManager _manager;
        private InitiativeBar _bar;
        private readonly List<Object> _toDestroy = new();

        [SetUp]
        public void SetUp()
        {
            _bar = new InitiativeBar();
            _manager = new CombatManager(_bar, new NavalTurnResolver(new System.Random(1234)));
            GameEvents.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAll();
            foreach (var obj in _toDestroy)
                Object.DestroyImmediate(obj);
            _toDestroy.Clear();
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private ShipData MakeShipData(string id, float hhp, float fpw, float hdf,
            float mst, float mp, float rsl, float spd, bool withCrewSlots = true)
        {
            var ship = ScriptableObject.CreateInstance<ShipData>();
            _toDestroy.Add(ship);
            ship.ShipId = id;
            ship.Element = Element.Neutral;
            ship.BaseStats = new ShipStatBlock
            {
                HHP = hhp, FPW = fpw, HDF = hdf, MST = mst, MP = mp, RSL = rsl, SPD = spd
            };
            ship.RoleSlots = withCrewSlots
                ? new List<RoleSlot>
                {
                    new RoleSlot { SlotIndex = 0, Role = NavalRole.Capitan },
                    new RoleSlot { SlotIndex = 1, Role = NavalRole.Artillero },
                    new RoleSlot { SlotIndex = 2, Role = NavalRole.Carpintero }
                }
                : new List<RoleSlot>();
            ship.BaseAbilities = new List<AbilityData>();
            return ship;
        }

        private UnitData MakeUnit(string id, float atk = 100f, float def = 40f,
            TraitData trait = null, float atkBonus = 0f)
        {
            var unit = ScriptableObject.CreateInstance<UnitData>();
            _toDestroy.Add(unit);
            unit.Id = id;
            unit.DisplayName = id;
            unit.Element = Element.Neutral;
            unit.BaseStats = new StatBlock
            {
                HP = 300, MP = 50, ATK = atk, DEF = def, MST = 60, SPR = 45, SPD = 70
            };
            unit.NavalRoleAffinity = new List<NavalRole>();
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
            return unit;
        }

        private AbilityData MakeAbility(string id, float power = 1.5f, int mpCost = 15,
            AbilityCategory category = AbilityCategory.Damage, float healPower = 0f,
            TargetType targetType = TargetType.SingleEnemy)
        {
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            _toDestroy.Add(ability);
            ability.Id = id;
            ability.DisplayName = id;
            ability.AbilityPower = power;
            ability.Element = Element.Neutral;
            ability.IsPhysical = true;
            ability.TargetType = targetType;
            ability.Category = category;
            ability.MPCost = mpCost;
            ability.HealPower = healPower;
            return ability;
        }

        /// <summary>Ally ship: FPW 100, HDF 80, MST 60, MP 50. No crew unless requested.</summary>
        private ShipCombatant MakeAllyShip(bool withCrew = false, float hhp = 500f)
        {
            var data = MakeShipData("ally_ship", hhp, 100, 80, 60, 50, 70, 90,
                withCrewSlots: withCrew);
            Dictionary<int, CharacterData> crew = null;
            if (withCrew)
            {
                crew = new Dictionary<int, CharacterData>
                {
                    [0] = MakeUnit("ally_cap"),
                    [1] = MakeUnit("ally_art")
                };
            }
            return new ShipCombatant(data, default, crew);
        }

        /// <summary>Enemy ship: FPW 100, HDF 80. Crew: Capitán (DEF 40) + Artillero (DEF 40).</summary>
        private ShipCombatant MakeEnemyShip(float hhp = 500f, float spd = 50f,
            Dictionary<int, CharacterData> crewOverride = null)
        {
            var data = MakeShipData("enemy_ship", hhp, 100, 80, 60, 50, 70, spd);
            var crew = crewOverride ?? new Dictionary<int, CharacterData>
            {
                [0] = MakeUnit("enemy_cap"),
                [1] = MakeUnit("enemy_art")
            };
            return new ShipCombatant(data, default, crew);
        }

        /// <summary>Sea creature: a ship with no role slots (no crew, not boardable).</summary>
        private ShipCombatant MakeCreature(float hhp = 400f)
        {
            var data = MakeShipData("kraken", hhp, 90, 70, 80, 40, 60, 60,
                withCrewSlots: false);
            return new ShipCombatant(data, default);
        }

        private BattleConfig MakeNavalBattle(ShipCombatant ally, params ShipCombatant[][] waves)
        {
            var waveConfigs = new List<WaveConfig>();
            foreach (var wave in waves)
            {
                var entries = new List<InitiativeEntry>();
                for (int i = 0; i < wave.Length; i++)
                    entries.Add(new InitiativeEntry(wave[i], CombatTeam.Enemy, i));
                waveConfigs.Add(new WaveConfig { Enemies = entries, EnemyCaptainIndex = -1 });
            }

            return new BattleConfig
            {
                Allies = new List<InitiativeEntry>
                {
                    new InitiativeEntry(ally, CombatTeam.Ally, 0)
                },
                Waves = waveConfigs,
                CaptainIndex = -1
            };
        }

        /// <summary>Starts a 1v1 naval battle and advances to the ally ship's turn (highest SPD).</summary>
        private (ShipCombatant ally, ShipCombatant enemy) StartSimpleBattle(
            ShipCombatant ally = null, ShipCombatant enemy = null)
        {
            ally ??= MakeAllyShip();
            enemy ??= MakeEnemyShip();
            _manager.StartBattle(MakeNavalBattle(ally, new[] { enemy }));
            _manager.BeginRound();
            var entry = _manager.AdvanceTurn();
            Assert.AreSame(ally, entry.Combatant, "ally ship (SPD 90) should act first");
            return (ally, enemy);
        }

        private void ResolveAndComplete(CombatAction action)
        {
            _manager.ResolveAction(action);
            _manager.CompleteTurn();
        }

        private static void AssertDamageInVarianceRange(int actual, float baseDamage)
        {
            Assert.GreaterOrEqual(actual, Mathf.FloorToInt(baseDamage * 0.95f),
                $"damage {actual} below variance range of base {baseDamage}");
            Assert.LessOrEqual(actual, Mathf.FloorToInt(baseDamage * 1.05f),
                $"damage {actual} above variance range of base {baseDamage}");
        }

        // ====================================================================
        // CAÑONAZO (AC 7, 20)
        // ====================================================================

        [Test]
        public void test_naval_cannonball_damages_enemy_hull_with_fpw_vs_hdf()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();

            // Act — Cañonazo: FPW 100 × 1.8 − effHDF 83 × 1.0 = 97 base
            // (effHDF = 80 + Artillero crew contribution floor(40×0.15×0.5) = 3)
            ResolveAndComplete(CombatAction.Cannonball(enemy));

            // Assert
            int damage = enemy.MaxHHP - enemy.CurrentHHP;
            AssertDamageInVarianceRange(damage, 97f);
        }

        [Test]
        public void test_naval_cannonball_does_not_damage_crew()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();

            // Act
            ResolveAndComplete(CombatAction.Cannonball(enemy));

            // Assert — AC 20: hull attacks never touch crew
            foreach (var crew in enemy.Crew)
                Assert.AreEqual(crew.MaxHP, crew.CurrentHP);
        }

        // ====================================================================
        // MANIOBRA EVASIVA (AC 9, 10; edge case 4)
        // ====================================================================

        [Test]
        public void test_naval_maneuver_reduces_hull_damage_by_half()
        {
            // Arrange — enemy maneuvering before the cannonball hits
            var (ally, enemy) = StartSimpleBattle();
            enemy.IsManeuvering = true;

            // Act — base 97 (effHDF 83 with crew contribution) halved by Maniobra
            ResolveAndComplete(CombatAction.Cannonball(enemy));

            // Assert
            int damage = enemy.MaxHHP - enemy.CurrentHHP;
            Assert.GreaterOrEqual(damage, Mathf.FloorToInt(97f * 0.95f * 0.5f));
            Assert.LessOrEqual(damage, Mathf.FloorToInt(97f * 1.05f * 0.5f) + 1);
        }

        [Test]
        public void test_naval_maneuver_reduces_boarding_damage_by_half()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();
            enemy.IsManeuvering = true;
            var targetCrew = enemy.Crew[1]; // Artillero, DEF 40, HP 400

            // Act — Abordaje base: (100×1.8 − 40×1.0) × 0.8 = 112; halved ≈ 56
            ResolveAndComplete(CombatAction.Boarding(enemy, targetCrew));

            // Assert
            int damage = targetCrew.MaxHP - targetCrew.CurrentHP;
            Assert.GreaterOrEqual(damage, Mathf.FloorToInt(112f * 0.95f * 0.5f));
            Assert.LessOrEqual(damage, Mathf.FloorToInt(112f * 1.05f * 0.5f) + 1);
        }

        [Test]
        public void test_naval_maneuver_expires_at_ships_next_turn()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();

            // Act — ally maneuvers; complete round; ally's next turn starts
            ResolveAndComplete(CombatAction.Maneuver());
            Assert.IsTrue(ally.IsManeuvering);

            var enemyEntry = _manager.AdvanceTurn();
            Assert.AreSame(enemy, enemyEntry.Combatant);
            ResolveAndComplete(CombatAction.PassTurn());

            _manager.BeginRound();
            var allyEntry = _manager.AdvanceTurn();

            // Assert — cleared at the start of the ship's own turn (GDD §3)
            Assert.AreSame(ally, allyEntry.Combatant);
            Assert.IsFalse(ally.IsManeuvering);
        }

        [Test]
        public void test_naval_maneuver_does_not_reduce_burn_damage()
        {
            // Arrange — maneuvering ship with Quemadura acts
            var (ally, enemy) = StartSimpleBattle();
            ally.IsManeuvering = true;
            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Quemadura, RemainingTurns = 3, Param = 0.05f
            });

            // Act — Cañonazo (acted → burn ticks). Burn = floor(500 × 0.05) = 25, NOT halved
            ResolveAndComplete(CombatAction.Cannonball(enemy));

            // Assert — AC 10: Maniobra never reduces status effect damage
            Assert.AreEqual(ally.MaxHHP - 25, ally.CurrentHHP);
        }

        // ====================================================================
        // ABORDAJE (AC 11, 12, 16, 20, 32)
        // ====================================================================

        [Test]
        public void test_naval_boarding_damages_targeted_crew_with_fpw_vs_crew_def()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();
            var targetCrew = enemy.Crew[1]; // Artillero DEF 40, HP 400

            // Act — (100×1.8 − 40×1.0) × BOARDING_POWER 0.8 = 112 base
            ResolveAndComplete(CombatAction.Boarding(enemy, targetCrew));

            // Assert
            int damage = targetCrew.MaxHP - targetCrew.CurrentHP;
            AssertDamageInVarianceRange(damage, 112f);
        }

        [Test]
        public void test_naval_boarding_does_not_damage_hull()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();

            // Act
            ResolveAndComplete(CombatAction.Boarding(enemy, enemy.Crew[1]));

            // Assert
            Assert.AreEqual(enemy.MaxHHP, enemy.CurrentHHP);
        }

        [Test]
        public void test_naval_boarding_kill_recalculates_enemy_ship_stats()
        {
            // Arrange — wound the Artillero so the next boarding kills it.
            // Enemy FPW: base 100 + Artillero mismatch contribution.
            var (ally, enemy) = StartSimpleBattle();
            var artillero = enemy.Crew[1];
            enemy.DamageCrewMember(artillero, artillero.CurrentHP - 1, out _);
            float fpwBefore = enemy.GetEffectiveShipStat(ShipStatType.FPW);

            // Act
            ResolveAndComplete(CombatAction.Boarding(enemy, artillero));

            // Assert — AC 16: stats recalculated immediately on crew death
            Assert.IsTrue(artillero.IsDead);
            Assert.Less(enemy.GetEffectiveShipStat(ShipStatType.FPW), fpwBefore);
        }

        [Test]
        public void test_naval_boarding_kill_captain_deactivates_enemy_synergies()
        {
            // Arrange — enemy crew of 3 sharing a trait (threshold 3)
            var trait = ScriptableObject.CreateInstance<TraitData>();
            _toDestroy.Add(trait);
            trait.TraitId = "corsarios";
            var crew = new Dictionary<int, CharacterData>
            {
                [0] = MakeUnit("e_cap", trait: trait, atkBonus: 0.10f),
                [1] = MakeUnit("e_art", trait: trait, atkBonus: 0.10f),
                [2] = MakeUnit("e_carp", trait: trait)
            };
            var enemy = MakeEnemyShip(crewOverride: crew);
            enemy.EvaluateCrewSynergies();
            Assert.AreEqual(1, enemy.CrewSynergies.Count);

            var (ally, _) = StartSimpleBattle(enemy: enemy);
            var captain = enemy.Captain;
            enemy.DamageCrewMember(captain, captain.CurrentHP - 1, out _);

            // Act — boarding kill on the captain
            ResolveAndComplete(CombatAction.Boarding(enemy, captain));

            // Assert — AC 32
            Assert.IsTrue(captain.IsDead);
            Assert.AreEqual(0, enemy.CrewSynergies.Count);
        }

        [Test]
        public void test_naval_boarding_against_creature_without_crew_is_noop()
        {
            // Arrange — sea creatures have no crew (AC 12)
            var creature = MakeCreature();
            var (ally, _) = StartSimpleBattle(enemy: creature);

            // Act — no crew to target; resolver must not throw nor damage hull
            ResolveAndComplete(CombatAction.Boarding(creature, null));

            // Assert
            Assert.AreEqual(creature.MaxHHP, creature.CurrentHHP);
        }

        // ====================================================================
        // REPARAR (AC 13; edge case 5, 14)
        // ====================================================================

        [Test]
        public void test_naval_repair_heals_hull_and_consumes_mp()
        {
            // Arrange — damaged ally hull
            var (ally, enemy) = StartSimpleBattle();
            ally.ApplyDamage(300);
            int mpBefore = ally.CurrentMP;

            // Act — Repair = floor(MST 60 × 1.5) = 90, cost 20 MP
            ResolveAndComplete(CombatAction.Repair());

            // Assert
            Assert.AreEqual(ally.MaxHHP - 300 + 90, ally.CurrentHHP);
            Assert.AreEqual(mpBefore - 20, ally.CurrentMP);
        }

        [Test]
        public void test_naval_repair_available_under_silence()
        {
            // Arrange — Silencio blocks abilities, but Repair is a base action
            var (ally, enemy) = StartSimpleBattle();
            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Silencio, RemainingTurns = 3, Param = 0f
            });
            ally.ApplyDamage(200);

            // Act
            ResolveAndComplete(CombatAction.Repair());

            // Assert — AC 13 / edge case 14
            Assert.AreEqual(ally.MaxHHP - 200 + 90, ally.CurrentHHP);
        }

        [Test]
        public void test_naval_repair_without_enough_mp_does_nothing()
        {
            // Arrange — drain MP below REPAIR_MP_COST
            var (ally, enemy) = StartSimpleBattle();
            ally.ConsumeMP(ally.CurrentMP - 10);
            ally.ApplyDamage(200);

            // Act
            ResolveAndComplete(CombatAction.Repair());

            // Assert — edge case 5: no MP → no repair, MP untouched
            Assert.AreEqual(ally.MaxHHP - 200, ally.CurrentHHP);
            Assert.AreEqual(10, ally.CurrentMP);
        }

        [Test]
        public void test_naval_repair_does_not_exceed_max_hhp()
        {
            // Arrange — barely scratched hull
            var (ally, enemy) = StartSimpleBattle();
            ally.ApplyDamage(10);

            // Act — heal 90 > missing 10
            ResolveAndComplete(CombatAction.Repair());

            // Assert
            Assert.AreEqual(ally.MaxHHP, ally.CurrentHHP);
        }

        // ====================================================================
        // HABILIDAD NAVAL (AC 8)
        // ====================================================================

        [Test]
        public void test_naval_ability_consumes_mp_and_damages_hull()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();
            var ability = MakeAbility("andanada", power: 2.0f, mpCost: 15);
            int mpBefore = ally.CurrentMP;

            // Act — (100×1.8 − effHDF 83×1.0) × 2.0 = 194 base
            ResolveAndComplete(CombatAction.FromAbility(ability, enemy));

            // Assert
            Assert.AreEqual(mpBefore - 15, ally.CurrentMP);
            int damage = enemy.MaxHHP - enemy.CurrentHHP;
            AssertDamageInVarianceRange(damage, 194f);
        }

        [Test]
        public void test_naval_ability_heal_restores_hull()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();
            ally.ApplyDamage(250);
            var heal = MakeAbility("marea_viva", power: 0f, mpCost: 15,
                category: AbilityCategory.Heal, healPower: 2.0f,
                targetType: TargetType.Self);

            // Act — floor(MST 60 × 2.0) = 120 to the hull
            ResolveAndComplete(CombatAction.FromAbility(heal, ally));

            // Assert
            Assert.AreEqual(ally.MaxHHP - 250 + 120, ally.CurrentHHP);
        }

        // ====================================================================
        // PASAR TURNO + DOT SPLIT (AC 14, 21-24; edge case 16)
        // ====================================================================

        [Test]
        public void test_naval_pass_does_not_trigger_burn()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();
            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Quemadura, RemainingTurns = 3, Param = 0.05f
            });

            // Act
            ResolveAndComplete(CombatAction.PassTurn());

            // Assert — AC 14
            Assert.AreEqual(ally.MaxHHP, ally.CurrentHHP);
        }

        [Test]
        public void test_naval_burn_damages_hull_after_acting()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();
            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Quemadura, RemainingTurns = 3, Param = 0.05f
            });

            // Act — acted → burn fires: floor(500 × 0.05) = 25 to HHP
            ResolveAndComplete(CombatAction.Cannonball(enemy));

            // Assert — AC 21: hull takes it, crew untouched
            Assert.AreEqual(ally.MaxHHP - 25, ally.CurrentHHP);
        }

        [Test]
        public void test_naval_poison_damages_random_living_crew_not_hull()
        {
            // Arrange — ally with 2 crew members
            var ally = MakeAllyShip(withCrew: true);
            var (_, enemy) = StartSimpleBattle(ally: ally);
            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Veneno, RemainingTurns = 3, Param = 0.05f
            });

            // Act
            ResolveAndComplete(CombatAction.Cannonball(enemy));

            // Assert — AC 22: exactly one living crew member took 5% MaxHP
            Assert.AreEqual(ally.MaxHHP, ally.CurrentHHP, "poison must not damage hull");
            int damagedCount = 0;
            foreach (var crew in ally.Crew)
            {
                if (crew.CurrentHP < crew.MaxHP)
                {
                    damagedCount++;
                    Assert.AreEqual(crew.MaxHP - Mathf.FloorToInt(crew.MaxHP * 0.05f),
                        crew.CurrentHP);
                }
            }
            Assert.AreEqual(1, damagedCount);
        }

        [Test]
        public void test_naval_bleed_damages_random_living_crew_at_turn_start()
        {
            // Arrange — bleed ticks when the ship's turn begins (AdvanceTurn)
            var ally = MakeAllyShip(withCrew: true);
            var enemy = MakeEnemyShip();
            _manager.StartBattle(MakeNavalBattle(ally, new[] { enemy }));
            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Sangrado, RemainingTurns = 3, Param = 0.05f
            });
            _manager.BeginRound();

            // Act — ally's turn starts
            _manager.AdvanceTurn();

            // Assert — AC 23
            Assert.AreEqual(ally.MaxHHP, ally.CurrentHHP, "bleed must not damage hull");
            int damagedCount = 0;
            foreach (var crew in ally.Crew)
                if (crew.CurrentHP < crew.MaxHP)
                    damagedCount++;
            Assert.AreEqual(1, damagedCount);
        }

        [Test]
        public void test_naval_crew_dots_without_living_crew_do_no_damage()
        {
            // Arrange — crewless ship (creature-like ally for simplicity)
            var (ally, enemy) = StartSimpleBattle();
            Assert.AreEqual(0, ally.GetLivingCrew().Count);
            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Veneno, RemainingTurns = 3, Param = 0.05f
            });

            // Act
            ResolveAndComplete(CombatAction.Cannonball(enemy));

            // Assert — AC 24: no living crew → poison fizzles, hull untouched
            Assert.AreEqual(ally.MaxHHP, ally.CurrentHHP);
        }

        [Test]
        public void test_naval_bleed_kill_recalculates_stats_before_acting()
        {
            // Arrange — single crew member at 1 HP; bleed will kill at turn start
            var ally = MakeAllyShip(withCrew: true);
            var enemy = MakeEnemyShip();
            _manager.StartBattle(MakeNavalBattle(ally, new[] { enemy }));

            var artillero = ally.Crew[1];
            ally.DamageCrewMember(ally.Crew[0], ally.Crew[0].MaxHP, out _); // kill captain first
            ally.DamageCrewMember(artillero, artillero.CurrentHP - 1, out _);
            float fpwBefore = ally.GetEffectiveShipStat(ShipStatType.FPW);

            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Sangrado, RemainingTurns = 3, Param = 0.05f
            });
            _manager.BeginRound();

            // Act — turn start bleed kills the last crew member
            _manager.AdvanceTurn();

            // Assert — edge case 16: recalculated before the ship acts
            Assert.IsTrue(artillero.IsDead);
            Assert.Less(ally.GetEffectiveShipStat(ShipStatType.FPW), fpwBefore);
        }

        // ====================================================================
        // STATUS IMMUNITY PIPELINE (AC 28)
        // ====================================================================

        [Test]
        public void test_naval_ship_immune_to_stun_and_sleep_still_acts()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();
            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Aturdimiento, RemainingTurns = 2, Param = 0f
            });
            ally.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Sueno, RemainingTurns = 2, Param = 0f
            });

            // Assert — AC 28: immunity swallows the status entirely
            Assert.IsFalse(ally.HasStatus(StatusEffect.Aturdimiento));
            Assert.IsFalse(ally.HasStatus(StatusEffect.Sueno));

            // Act — ship still resolves an action normally
            ResolveAndComplete(CombatAction.Cannonball(enemy));
            Assert.Less(enemy.CurrentHHP, enemy.MaxHHP);
        }

        // ====================================================================
        // LIMIT BREAK NAVAL (AC 33, 34)
        // ====================================================================

        private CombatAction MakeLBCrewAbility(ShipCombatant targetShip,
            CrewMemberState targetCrew, out AbilityData ability)
        {
            ability = MakeAbility("abordaje_fantasmal", power: 1.0f, mpCost: 10,
                targetType: TargetType.SingleCrewEnemy);
            var entry = new AbilityEntry
            {
                Ability = ability,
                CanLimitBreak = true,
                LBCondition = LBCondition.OnKill,
                LBConditionParam = -1f
            };
            return new CombatAction
            {
                Type = ActionType.Ability,
                AbilityPower = ability.AbilityPower,
                Element = ability.Element,
                IsPhysical = true,
                TargetType = TargetType.SingleCrewEnemy,
                Target = targetShip,
                TargetCrew = targetCrew,
                ActionName = ability.DisplayName,
                AbilityEntry = entry,
                AbilityData = ability
            };
        }

        [Test]
        public void test_naval_lb_on_kill_grants_extra_ship_turn()
        {
            // Arrange — crew member at 1 HP so the LB ability's kill triggers OnKill
            var (ally, enemy) = StartSimpleBattle();
            var targetCrew = enemy.Crew[1];
            enemy.DamageCrewMember(targetCrew, targetCrew.CurrentHP - 1, out _);
            var action = MakeLBCrewAbility(enemy, targetCrew, out _);

            // Act
            ResolveAndComplete(action);

            // Assert — AC 33: the SHIP gets the extra turn, inserted next
            Assert.IsTrue(targetCrew.IsDead);
            Assert.IsTrue(ally.LBUsedThisRound);
            var next = _manager.AdvanceTurn();
            Assert.AreSame(ally, next.Combatant);
            Assert.IsTrue(next.IsLimitBreak);
        }

        [Test]
        public void test_naval_lb_max_once_per_ship_per_round()
        {
            // Arrange — two kills in the same round, only one LB
            var crew = new Dictionary<int, CharacterData>
            {
                [0] = MakeUnit("e_cap"),
                [1] = MakeUnit("e_art1"),
                [2] = MakeUnit("e_art2")
            };
            var enemy = MakeEnemyShip(crewOverride: crew);
            var (ally, _) = StartSimpleBattle(enemy: enemy);

            var first = enemy.Crew[1];
            var second = enemy.Crew[2];
            enemy.DamageCrewMember(first, first.CurrentHP - 1, out _);
            enemy.DamageCrewMember(second, second.CurrentHP - 1, out _);

            // Act — first LB kill grants the extra turn
            ResolveAndComplete(MakeLBCrewAbility(enemy, first, out _));
            var extra = _manager.AdvanceTurn();
            Assert.IsTrue(extra.IsLimitBreak);

            // Second kill during the extra turn must NOT grant another
            ResolveAndComplete(MakeLBCrewAbility(enemy, second, out _));

            // Assert — AC 34: next actor is the enemy, not another ally LB turn
            var next = _manager.AdvanceTurn();
            Assert.AreSame(enemy, next.Combatant);
        }

        // ====================================================================
        // OLEADAS Y PERSISTENCIA (AC 3, 4, 5, 19, 36)
        // ====================================================================

        [Test]
        public void test_naval_victory_when_last_wave_sunk()
        {
            // Arrange — weak enemy that one cannonball sinks
            var ally = MakeAllyShip();
            var enemy = MakeEnemyShip(hhp: 50f);
            _manager.StartBattle(MakeNavalBattle(ally, new[] { enemy }));
            _manager.BeginRound();
            _manager.AdvanceTurn();

            // Act
            ResolveAndComplete(CombatAction.Cannonball(enemy));

            // Assert — AC 5
            Assert.IsTrue(enemy.IsKO);
            Assert.AreEqual(BattlePhase.Victory, _manager.Phase);
        }

        [Test]
        public void test_naval_defeat_when_ally_ship_sinks()
        {
            // Arrange — ally hull at 30; enemy cannonball (base 100) sinks it
            var ally = MakeAllyShip();
            var enemy = MakeEnemyShip(spd: 200f); // enemy acts first
            ally.ApplyDamage(ally.MaxHHP - 30);
            _manager.StartBattle(MakeNavalBattle(ally, new[] { enemy }));
            _manager.BeginRound();
            var entry = _manager.AdvanceTurn();
            Assert.AreSame(enemy, entry.Combatant);

            // Act
            ResolveAndComplete(CombatAction.Cannonball(ally));

            // Assert — AC 5
            Assert.IsTrue(ally.IsKO);
            Assert.AreEqual(BattlePhase.Defeat, _manager.Phase);
        }

        [Test]
        public void test_naval_ship_state_persists_between_waves()
        {
            // Arrange — 2 waves; ally takes hull damage, spends MP and loses crew in wave 1
            var ally = MakeAllyShip(withCrew: true);
            var wave1Enemy = MakeEnemyShip(hhp: 50f);
            var wave2Enemy = MakeEnemyShip();
            _manager.StartBattle(MakeNavalBattle(ally,
                new[] { wave1Enemy }, new[] { wave2Enemy }));

            ally.ApplyDamage(150);
            ally.ConsumeMP(20);
            ally.DamageCrewMember(ally.Crew[1], ally.Crew[1].MaxHP, out _);

            _manager.BeginRound();
            _manager.AdvanceTurn();
            ResolveAndComplete(CombatAction.Cannonball(wave1Enemy));
            Assert.IsTrue(wave1Enemy.IsKO);
            Assert.IsTrue(_manager.IsCurrentWaveCleared);
            Assert.AreEqual(BattlePhase.InRound, _manager.Phase, "more waves remain");

            // Act — AC 4: wave transition
            _manager.TransitionToNextWave();

            // Assert — AC 36/19: hull, MP and dead crew persist into wave 2
            Assert.AreEqual(BattlePhase.InRound, _manager.Phase);
            Assert.AreEqual(ally.MaxHHP - 150, ally.CurrentHHP);
            Assert.AreEqual(ally.MaxMP - 20, ally.CurrentMP);
            Assert.IsTrue(ally.Crew[1].IsDead, "dead crew must not be restored between waves");
            Assert.AreSame(wave2Enemy, _manager.Enemies[0]);
        }

        // ====================================================================
        // EVENTOS NAVALES (ADR-004 §4 — UI integration surface)
        // ====================================================================

        [Test]
        public void test_naval_crew_death_publishes_crew_died_and_stats_recalculated()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();
            var targetCrew = enemy.Crew[1];
            enemy.DamageCrewMember(targetCrew, targetCrew.CurrentHP - 1, out _);

            CrewDiedEvent? died = null;
            ShipCombatant recalculated = null;
            GameEvents.OnCrewDied += e => died = e;
            GameEvents.OnShipStatsRecalculated += s => recalculated = s;

            // Act
            ResolveAndComplete(CombatAction.Boarding(enemy, targetCrew));

            // Assert
            Assert.IsNotNull(died);
            Assert.AreSame(targetCrew, died.Value.Crew);
            Assert.AreSame(enemy, died.Value.Ship);
            Assert.AreSame(enemy, recalculated);
        }

        [Test]
        public void test_naval_boarding_publishes_crew_damaged_event()
        {
            // Arrange
            var (ally, enemy) = StartSimpleBattle();
            var targetCrew = enemy.Crew[1];
            CrewDamageEvent? received = null;
            GameEvents.OnCrewDamaged += e => received = e;

            // Act
            ResolveAndComplete(CombatAction.Boarding(enemy, targetCrew));

            // Assert
            Assert.IsNotNull(received);
            Assert.AreSame(targetCrew, received.Value.Crew);
            Assert.AreEqual(DamageSource.Boarding, received.Value.Source);
            Assert.Greater(received.Value.ActualDamage, 0);
        }
    }
}
