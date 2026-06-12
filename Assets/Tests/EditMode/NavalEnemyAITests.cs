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
    /// Tests for NavalEnemyAI (S4-05): AI tiers for naval enemies.
    /// Covers Combate Naval GDD AC 25-27 (AC 28 immunities covered in S4-04):
    /// profiles for Normal ships, Elite Profile+ overrides, boss phase behavior
    /// trees with LB, sea creatures never boarding, and the global rule that
    /// enemies never use Maniobra Evasiva or Reparar (GDD §4).
    ///
    /// Boarding damage estimates use DamageCalculator.CalculateDeterministic
    /// with VARIANCE_MIN, so kill-secured boarding asserts are deterministic.
    /// </summary>
    [TestFixture]
    public class NavalEnemyAITests
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
            float mst, float mp, float rsl, float spd, bool withCrewSlots = true,
            Element element = Element.Neutral)
        {
            var ship = ScriptableObject.CreateInstance<ShipData>();
            _toDestroy.Add(ship);
            ship.ShipId = id;
            ship.Element = element;
            ship.BaseStats = new ShipStatBlock
            {
                HHP = hhp, FPW = fpw, HDF = hdf, MST = mst, MP = mp, RSL = rsl, SPD = spd
            };
            ship.RoleSlots = withCrewSlots
                ? new List<RoleSlot>
                {
                    new RoleSlot { SlotIndex = 0, Role = NavalRole.Capitan },
                    new RoleSlot { SlotIndex = 1, Role = NavalRole.Artillero }
                }
                : new List<RoleSlot>();
            ship.BaseAbilities = new List<AbilityData>();
            return ship;
        }

        private UnitData MakeUnit(string id, float atk = 100f, float def = 40f)
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
            return unit;
        }

        private AbilityData MakeAbility(string id, float power = 1.5f, int mpCost = 15,
            AbilityCategory category = AbilityCategory.Damage, float healPower = 0f,
            TargetType targetType = TargetType.SingleEnemy,
            Element element = Element.Neutral)
        {
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            _toDestroy.Add(ability);
            ability.Id = id;
            ability.DisplayName = id;
            ability.AbilityPower = power;
            ability.Element = element;
            ability.IsPhysical = true;
            ability.TargetType = targetType;
            ability.Category = category;
            ability.MPCost = mpCost;
            ability.HealPower = healPower;
            return ability;
        }

        /// <summary>Enemy ship FPW 100, HDF 80, crew Capitán+Artillero (DEF 40).</summary>
        private ShipCombatant MakeEnemyShip(float hhp = 500f, float spd = 50f,
            List<AbilityData> baseAbilities = null, bool withCrew = true,
            Element element = Element.Neutral)
        {
            var data = MakeShipData("enemy_ship", hhp, 100, 80, 60, 50, 70, spd,
                withCrewSlots: withCrew, element: element);
            if (baseAbilities != null)
                data.BaseAbilities = baseAbilities;
            Dictionary<int, CharacterData> crew = null;
            if (withCrew)
            {
                crew = new Dictionary<int, CharacterData>
                {
                    [0] = MakeUnit("enemy_cap"),
                    [1] = MakeUnit("enemy_art")
                };
            }
            return new ShipCombatant(data, default, crew);
        }

        /// <summary>Player ship FPW 100, HDF 80, crew Capitán+Artillero (DEF 40).</summary>
        private ShipCombatant MakePlayerShip(float hhp = 500f, float spd = 90f,
            Element element = Element.Neutral)
        {
            var data = MakeShipData("player_ship", hhp, 100, 80, 60, 50, 70, spd,
                element: element);
            var crew = new Dictionary<int, CharacterData>
            {
                [0] = MakeUnit("player_cap"),
                [1] = MakeUnit("player_art")
            };
            return new ShipCombatant(data, default, crew);
        }

        /// <summary>Sea creature: ship with no role slots (no crew).</summary>
        private ShipCombatant MakeCreature(float hhp = 400f)
        {
            var data = MakeShipData("kraken", hhp, 90, 70, 80, 40, 60, 60,
                withCrewSlots: false);
            return new ShipCombatant(data, default);
        }

        private static CombatContext MakeContext(ShipCombatant actor,
            ShipCombatant[] enemies)
        {
            return new CombatContext
            {
                Actor = actor,
                Allies = new List<ICombatant> { actor },
                Enemies = new List<ICombatant>(enemies)
            };
        }

        private static CombatAction Decide(NavalEnemyAI ai, CombatContext context)
        {
            CombatAction chosen = default;
            ai.RequestAction(context, action => chosen = action);
            return chosen;
        }

        private static CrewMemberState FindCrew(ShipCombatant ship, NavalRole role)
        {
            foreach (var crew in ship.Crew)
                if (crew.Role == role)
                    return crew;
            return null;
        }

        // ====================================================================
        // AGRESIVO (Normal ships — Enemy System GDD §7)
        // ====================================================================

        [Test]
        public void test_naval_ai_aggressive_selects_highest_damage_ability_on_lowest_hhp_target()
        {
            // Arrange — two abilities; two targets with different HHP
            var weak = MakeAbility("weak_shot", power: 1.2f);
            var strong = MakeAbility("strong_shot", power: 2.0f);
            var actor = MakeEnemyShip(baseAbilities: new List<AbilityData> { weak, strong });
            var lowHull = MakePlayerShip(hhp: 300f);
            var highHull = MakePlayerShip(hhp: 900f);
            var ai = new NavalEnemyAI(EnemyTier.Normal, AIProfileType.Agresivo);

            // Act
            var action = Decide(ai, MakeContext(actor, new[] { highHull, lowHull }));

            // Assert
            Assert.AreEqual(ActionType.Ability, action.Type);
            Assert.AreSame(strong, action.AbilityData, "should pick highest power ability");
            Assert.AreSame(lowHull, action.Target, "should target lowest HHP ship");
        }

        [Test]
        public void test_naval_ai_aggressive_falls_back_to_cannonball_without_abilities()
        {
            // Arrange — no abilities, full HP crew (no kill-secured boarding)
            var actor = MakeEnemyShip();
            var target = MakePlayerShip();
            var ai = new NavalEnemyAI(EnemyTier.Normal, AIProfileType.Agresivo);

            // Act
            var action = Decide(ai, MakeContext(actor, new[] { target }));

            // Assert
            Assert.AreEqual(ActionType.Attack, action.Type);
            Assert.AreSame(target, action.Target);
        }

        [Test]
        public void test_naval_ai_aggressive_boards_weakest_crew_when_kill_guaranteed()
        {
            // Arrange — AC 25: enemy ships board player crew. Artillero at 100 HP;
            // min boarding damage = floor((100×1.8 − 40) × 0.8 × 0.95) = 106 ≥ 100.
            var actor = MakeEnemyShip();
            var target = MakePlayerShip();
            var artillero = FindCrew(target, NavalRole.Artillero);
            target.DamageCrewMember(artillero, 300, out _);
            var ai = new NavalEnemyAI(EnemyTier.Normal, AIProfileType.Agresivo);

            // Act
            var action = Decide(ai, MakeContext(actor, new[] { target }));

            // Assert
            Assert.AreEqual(ActionType.Boarding, action.Type);
            Assert.AreSame(target, action.Target);
            Assert.AreSame(artillero, action.TargetCrew, "should board the killable crew member");
        }

        [Test]
        public void test_naval_ai_enemy_boarding_damages_player_crew()
        {
            // Arrange — AC 25 end-to-end: enemy acts first (SPD 120) and boards
            var player = MakePlayerShip(spd: 50f);
            var artillero = FindCrew(player, NavalRole.Artillero);
            player.DamageCrewMember(artillero, 300, out _);
            var enemy = MakeEnemyShip(spd: 120f);

            _manager.StartBattle(new BattleConfig
            {
                Allies = new List<InitiativeEntry>
                {
                    new InitiativeEntry(player, CombatTeam.Ally, 0)
                },
                Waves = new List<WaveConfig>
                {
                    new WaveConfig
                    {
                        Enemies = new List<InitiativeEntry>
                        {
                            new InitiativeEntry(enemy, CombatTeam.Enemy, 0)
                        },
                        EnemyCaptainIndex = -1
                    }
                },
                CaptainIndex = -1
            });
            _manager.BeginRound();
            var entry = _manager.AdvanceTurn();
            Assert.AreSame(enemy, entry.Combatant, "enemy ship (SPD 120) should act first");

            var ai = new NavalEnemyAI(EnemyTier.Normal, AIProfileType.Agresivo);

            // Act
            var action = Decide(ai, _manager.GetCurrentContext());
            _manager.ResolveAction(action);
            _manager.CompleteTurn();

            // Assert — boarding resolved against the player's crew member
            Assert.AreEqual(ActionType.Boarding, action.Type);
            Assert.IsTrue(artillero.IsDead, "kill-secured boarding should kill the crew member");
        }

        // ====================================================================
        // CRIATURAS MARINAS (AC 26)
        // ====================================================================

        [Test]
        public void test_naval_ai_creature_never_boards()
        {
            // Arrange — creature has no crew to send; target crew is killable
            var creature = MakeCreature();
            var target = MakePlayerShip();
            var artillero = FindCrew(target, NavalRole.Artillero);
            target.DamageCrewMember(artillero, 399, out _);
            var ai = new NavalEnemyAI(EnemyTier.Normal, AIProfileType.Agresivo);

            // Act
            var action = Decide(ai, MakeContext(creature, new[] { target }));

            // Assert
            Assert.AreNotEqual(ActionType.Boarding, action.Type,
                "creatures cannot board (no crew to send)");
            Assert.AreEqual(ActionType.Attack, action.Type);
        }

        [Test]
        public void test_naval_ai_never_targets_creature_with_boarding()
        {
            // Arrange — Estratega boards by default, but the only target is a creature
            var actor = MakeEnemyShip();
            var creature = MakeCreature();
            var ai = new NavalEnemyAI(EnemyTier.Normal, AIProfileType.Estratega);

            // Act
            var action = Decide(ai, MakeContext(actor, new[] { creature }));

            // Assert
            Assert.AreNotEqual(ActionType.Boarding, action.Type,
                "creatures have no crew and cannot be boarded");
            Assert.AreEqual(ActionType.Attack, action.Type);
        }

        // ====================================================================
        // GLOBAL RULE: enemies never use Maniobra/Reparar/Guard (GDD §4)
        // ====================================================================

        [Test]
        public void test_naval_ai_never_chooses_maneuver_repair_or_guard()
        {
            // Arrange — Caótico explores the whole action space with seeded RNG
            var ability = MakeAbility("bite", power: 1.5f);
            var actor = MakeEnemyShip(baseAbilities: new List<AbilityData> { ability });
            var target = MakePlayerShip();
            var ai = new NavalEnemyAI(EnemyTier.Normal, AIProfileType.Caotico,
                rng: new System.Random(42));

            // Act + Assert — 50 decisions, none may be a defensive base action
            for (int i = 0; i < 50; i++)
            {
                var action = Decide(ai, MakeContext(actor, new[] { target }));
                Assert.AreNotEqual(ActionType.Maneuver, action.Type, "enemies never use Maniobra");
                Assert.AreNotEqual(ActionType.Repair, action.Type, "enemies never use Reparar");
                Assert.AreNotEqual(ActionType.Guard, action.Type, "Guard is land-only");
            }
        }

        [Test]
        public void test_naval_ai_defensive_without_buff_ability_falls_back_to_cannonball()
        {
            // Arrange — Defensivo with no buffs active and no buff ability:
            // land AI would Guard; naval enemies must attack instead (GDD §4)
            var actor = MakeEnemyShip();
            var target = MakePlayerShip();
            var ai = new NavalEnemyAI(EnemyTier.Normal, AIProfileType.Defensivo);

            // Act
            var action = Decide(ai, MakeContext(actor, new[] { target }));

            // Assert
            Assert.AreEqual(ActionType.Attack, action.Type);
        }

        // ====================================================================
        // ESTRATEGA (Elite profile — Enemy System GDD §7)
        // ====================================================================

        [Test]
        public void test_naval_ai_estratega_prefers_elemental_advantage_ability()
        {
            // Arrange — Luz ability vs Sombra target = 1.25 mod (ElementTable)
            var neutral = MakeAbility("neutral_shot", power: 2.0f);
            var luz = MakeAbility("luz_shot", power: 1.5f, element: Element.Luz);
            var actor = MakeEnemyShip(baseAbilities: new List<AbilityData> { neutral, luz });
            var target = MakePlayerShip(element: Element.Sombra);
            var ai = new NavalEnemyAI(EnemyTier.Elite, AIProfileType.Estratega);

            // Act
            var action = Decide(ai, MakeContext(actor, new[] { target }));

            // Assert
            Assert.AreEqual(ActionType.Ability, action.Type);
            Assert.AreSame(luz, action.AbilityData,
                "should prefer the elemental-advantage ability over raw power");
        }

        [Test]
        public void test_naval_ai_estratega_boards_enemy_captain_without_elemental_advantage()
        {
            // Arrange — no elemental advantage available → tactical boarding
            var actor = MakeEnemyShip();
            var target = MakePlayerShip();
            var captain = FindCrew(target, NavalRole.Capitan);
            var ai = new NavalEnemyAI(EnemyTier.Elite, AIProfileType.Estratega);

            // Act
            var action = Decide(ai, MakeContext(actor, new[] { target }));

            // Assert — AC: elites prioritize boarding the Capitán (synergy kill)
            Assert.AreEqual(ActionType.Boarding, action.Type);
            Assert.AreSame(captain, action.TargetCrew);
        }

        [Test]
        public void test_naval_ai_estratega_targets_highest_threat_ship()
        {
            // Arrange — threat = max(FPW, MST); strong ship has FPW 200
            var weakShip = MakePlayerShip();
            var strongData = MakeShipData("strong_player", 500, 200, 80, 60, 50, 70, 90);
            var strongShip = new ShipCombatant(strongData, default,
                new Dictionary<int, CharacterData> { [0] = MakeUnit("strong_cap") });
            var actor = MakeEnemyShip();
            var ai = new NavalEnemyAI(EnemyTier.Elite, AIProfileType.Estratega);

            // Act
            var action = Decide(ai, MakeContext(actor, new[] { weakShip, strongShip }));

            // Assert
            Assert.AreSame(strongShip, action.Target,
                "Estratega should target the highest FPW/MST ship");
        }

        // ====================================================================
        // ELITE Profile+ override (Enemy System GDD §7, knob 30%)
        // ====================================================================

        [Test]
        public void test_naval_ai_elite_uses_heal_ability_below_emergency_threshold()
        {
            // Arrange — Elite at 20% HHP with a heal ability available
            var heal = MakeAbility("mend_hull", category: AbilityCategory.Heal,
                healPower: 1.5f, targetType: TargetType.Self);
            var attack = MakeAbility("shot", power: 2.0f);
            var actor = MakeEnemyShip(baseAbilities: new List<AbilityData> { heal, attack });
            actor.ApplyDamage(Mathf.FloorToInt(actor.MaxHHP * 0.8f));
            var target = MakePlayerShip();
            var ai = new NavalEnemyAI(EnemyTier.Elite, AIProfileType.Agresivo);

            // Act
            var action = Decide(ai, MakeContext(actor, new[] { target }));

            // Assert
            Assert.AreEqual(ActionType.Ability, action.Type);
            Assert.AreSame(heal, action.AbilityData, "emergency override: heal below 30% HHP");
            Assert.AreSame(actor, action.Target, "heal targets self");
        }

        [Test]
        public void test_naval_ai_elite_ignores_emergency_override_above_threshold()
        {
            // Arrange — same setup but at 80% HHP
            var heal = MakeAbility("mend_hull", category: AbilityCategory.Heal,
                healPower: 1.5f, targetType: TargetType.Self);
            var attack = MakeAbility("shot", power: 2.0f);
            var actor = MakeEnemyShip(baseAbilities: new List<AbilityData> { heal, attack });
            actor.ApplyDamage(Mathf.FloorToInt(actor.MaxHHP * 0.2f));
            var target = MakePlayerShip();
            var ai = new NavalEnemyAI(EnemyTier.Elite, AIProfileType.Agresivo);

            // Act
            var action = Decide(ai, MakeContext(actor, new[] { target }));

            // Assert
            Assert.AreSame(attack, action.AbilityData,
                "no emergency: should use the aggressive damage ability");
        }

        // ====================================================================
        // JEFE: behavior tree by HP phases + LB (AC 27)
        // ====================================================================

        [Test]
        public void test_naval_ai_boss_switches_profile_on_phase_threshold()
        {
            // Arrange — base Agresivo; below 50% HHP switches to Estratega
            var actor = MakeEnemyShip(hhp: 1000f);
            var target = MakePlayerShip();
            var captain = FindCrew(target, NavalRole.Capitan);
            var ai = new NavalEnemyAI(EnemyTier.Jefe, AIProfileType.Agresivo,
                new List<NavalBossPhase>
                {
                    new NavalBossPhase { HPThreshold = 0.5f, Profile = AIProfileType.Estratega }
                });

            // Act 1 — full HHP: aggressive (crew at full HP → no boarding)
            var phase1Action = Decide(ai, MakeContext(actor, new[] { target }));

            // Act 2 — drop below 50%: Estratega boards the captain
            actor.ApplyDamage(Mathf.FloorToInt(actor.MaxHHP * 0.6f));
            var phase2Action = Decide(ai, MakeContext(actor, new[] { target }));

            // Assert
            Assert.AreEqual(ActionType.Attack, phase1Action.Type, "phase 1: aggressive cannonball");
            Assert.AreEqual(ActionType.Boarding, phase2Action.Type, "phase 2: Estratega boards");
            Assert.AreSame(captain, phase2Action.TargetCrew);
        }

        [Test]
        public void test_naval_ai_boss_phase_never_reverts_after_healing()
        {
            // Arrange — boss crosses into phase 2, then heals above the threshold
            var actor = MakeEnemyShip(hhp: 1000f);
            var target = MakePlayerShip();
            var ai = new NavalEnemyAI(EnemyTier.Jefe, AIProfileType.Agresivo,
                new List<NavalBossPhase>
                {
                    new NavalBossPhase { HPThreshold = 0.5f, Profile = AIProfileType.Estratega }
                });
            actor.ApplyDamage(Mathf.FloorToInt(actor.MaxHHP * 0.6f));
            Decide(ai, MakeContext(actor, new[] { target }));

            // Act — heal back to full (phases are one-directional, GDD §3)
            actor.ApplyHealing(actor.MaxHHP);
            var action = Decide(ai, MakeContext(actor, new[] { target }));

            // Assert
            Assert.AreEqual(ActionType.Boarding, action.Type,
                "phase must not revert when healed above the threshold");
        }

        [Test]
        public void test_naval_ai_boss_limit_break_via_lb_sea_ability()
        {
            // Arrange — boss crew captain provides an LB-flagged SeaAbility
            // (OnLowHP < 0.5); boss at 30% HHP chooses it (highest power) and
            // the resolver grants the extra ship turn (AC 27).
            var lbAbility = MakeAbility("requiem_barrage", power: 2.5f);
            var capUnit = MakeUnit("boss_cap");
            capUnit.SeaAbilities = new List<AbilityEntry>
            {
                new AbilityEntry
                {
                    Ability = lbAbility,
                    UnlockLevel = 1,
                    CanLimitBreak = true,
                    LBCondition = LBCondition.OnLowHP,
                    LBConditionParam = 0.5f
                }
            };
            var bossData = MakeShipData("boss_ship", 1000, 100, 80, 60, 50, 70, 120);
            var boss = new ShipCombatant(bossData, default,
                new Dictionary<int, CharacterData> { [0] = capUnit });
            boss.IsBoss = true;
            boss.ApplyDamage(700);

            var player = MakePlayerShip(spd: 50f);
            _manager.StartBattle(new BattleConfig
            {
                Allies = new List<InitiativeEntry>
                {
                    new InitiativeEntry(player, CombatTeam.Ally, 0)
                },
                Waves = new List<WaveConfig>
                {
                    new WaveConfig
                    {
                        Enemies = new List<InitiativeEntry>
                        {
                            new InitiativeEntry(boss, CombatTeam.Enemy, 0)
                        },
                        EnemyCaptainIndex = -1
                    }
                },
                CaptainIndex = -1
            });
            _manager.BeginRound();
            var entry = _manager.AdvanceTurn();
            Assert.AreSame(boss, entry.Combatant, "boss (SPD 120) should act first");

            var ai = new NavalEnemyAI(EnemyTier.Jefe, AIProfileType.Agresivo);

            // Act
            var action = Decide(ai, _manager.GetCurrentContext());
            _manager.ResolveAction(action);

            // Assert — the chosen action carried the LB entry and triggered LB
            Assert.AreSame(lbAbility, action.AbilityData);
            Assert.IsTrue(boss.LBUsedThisRound, "boss LB should have been activated");
        }

        // ====================================================================
        // DATA WIRING (ShipData enemy fields)
        // ====================================================================

        [Test]
        public void test_naval_ai_from_ship_data_reads_enemy_fields()
        {
            // Arrange
            var data = MakeShipData("elite_ship", 500, 100, 80, 60, 50, 70, 50);
            data.Tier = EnemyTier.Elite;
            data.AIProfile = AIProfileType.Estratega;

            // Act
            var ai = NavalEnemyAI.FromShipData(data);

            // Assert
            Assert.AreEqual(EnemyTier.Elite, ai.Tier);
            Assert.AreEqual(AIProfileType.Estratega, ai.Profile);
        }
    }
}
