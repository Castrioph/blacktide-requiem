using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.AI;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.UI.Combat;

namespace BlacktideRequiem.Runtime.Combat
{
    /// <summary>
    /// Hardcoded demo battle setup for S2-05 verification.
    /// Creates 3 allies vs 2 enemies (2 waves), wires CombatRunner + CombatHUD.
    /// Temporary — will be replaced by Stage System data loading.
    /// </summary>
    public class DemoBattleSetup : MonoBehaviour
    {
        [SerializeField] private CombatRunner _runner;
        [SerializeField] private CombatHUDCanvas _hud;

        private PlayerCombatInput _playerInput;

        private void Start()
        {
            _playerInput = new PlayerCombatInput();
            _hud.Bind(_playerInput);

            var config = BuildDemoConfig();

            // Build initial UI cards before starting the battle loop
            var allAllies = new List<CombatantState>();
            foreach (var entry in config.Allies)
                allAllies.Add(entry.Combatant);

            var firstWaveEnemies = new List<CombatantState>();
            foreach (var entry in config.Waves[0].Enemies)
                firstWaveEnemies.Add(entry.Combatant);

            _hud.BuildCombatantCards(allAllies, firstWaveEnemies, config.Waves.Count);

            var enemyAI = new EnemyAI(AIProfileType.Agresivo);
            _runner.StartBattle(config, _playerInput, enemyAI);
        }

        private BattleConfig BuildDemoConfig()
        {
            // --- Abilities ---
            var stormBolt = CreateAbility("storm_bolt", "Storm Bolt",
                power: 1.4f, Element.Tormenta, isPhysical: false,
                TargetType.SingleEnemy, AbilityCategory.Damage, mpCost: 8, cooldown: 0);

            var healingTide = CreateAbility("healing_tide", "Healing Tide",
                power: 0f, Element.Neutral, isPhysical: false,
                TargetType.SingleAlly, AbilityCategory.Heal, mpCost: 12, cooldown: 1,
                healPower: 1.2f);

            var powderBlast = CreateAbility("powder_blast", "Powder Blast",
                power: 1.6f, Element.Polvora, isPhysical: true,
                TargetType.SingleEnemy, AbilityCategory.Damage, mpCost: 10, cooldown: 2);

            // --- Allies ---
            var elena = CreateCharacter("elena_storm", "Elena", Element.Tormenta,
                hp: 320, mp: 80, atk: 45, def: 30, mst: 65, spr: 40, spd: 72,
                new List<AbilityEntry>
                {
                    new AbilityEntry { Ability = stormBolt, UnlockLevel = 1 },
                    new AbilityEntry { Ability = healingTide, UnlockLevel = 1 }
                });

            var kael = CreateCharacter("kael_cannon", "Kael", Element.Polvora,
                hp: 400, mp: 50, atk: 70, def: 45, mst: 30, spr: 25, spd: 55,
                new List<AbilityEntry>
                {
                    new AbilityEntry { Ability = powderBlast, UnlockLevel = 1 }
                });

            var mirra = CreateCharacter("mirra_tide", "Mirra", Element.Neutral,
                hp: 280, mp: 100, atk: 35, def: 25, mst: 55, spr: 50, spd: 65,
                new List<AbilityEntry>
                {
                    new AbilityEntry { Ability = healingTide, UnlockLevel = 1 }
                });

            // --- Enemies Wave 1 ---
            var pirate1 = CreateCharacter("pirate_grunt_1", "Pirate Grunt", Element.Neutral,
                hp: 180, mp: 0, atk: 40, def: 20, mst: 15, spr: 15, spd: 50);

            var pirate2 = CreateCharacter("pirate_brute_1", "Pirate Brute", Element.Acero,
                hp: 260, mp: 0, atk: 55, def: 35, mst: 10, spr: 20, spd: 35);

            // --- Enemies Wave 2 ---
            var corsair = CreateCharacter("corsair_1", "Corsair", Element.Tormenta,
                hp: 220, mp: 0, atk: 50, def: 25, mst: 40, spr: 30, spd: 60);

            var hexer = CreateCharacter("hexer_1", "Voodoo Hexer", Element.Maldicion,
                hp: 160, mp: 0, atk: 25, def: 15, mst: 60, spr: 35, spd: 68);

            // --- Build config ---
            return new BattleConfig
            {
                Allies = new List<InitiativeEntry>
                {
                    new InitiativeEntry(elena, CombatTeam.Ally, 0),
                    new InitiativeEntry(kael, CombatTeam.Ally, 1),
                    new InitiativeEntry(mirra, CombatTeam.Ally, 2)
                },
                Waves = new List<WaveConfig>
                {
                    new WaveConfig
                    {
                        Enemies = new List<InitiativeEntry>
                        {
                            new InitiativeEntry(pirate1, CombatTeam.Enemy, 0),
                            new InitiativeEntry(pirate2, CombatTeam.Enemy, 1)
                        }
                    },
                    new WaveConfig
                    {
                        Enemies = new List<InitiativeEntry>
                        {
                            new InitiativeEntry(corsair, CombatTeam.Enemy, 0),
                            new InitiativeEntry(hexer, CombatTeam.Enemy, 1)
                        }
                    }
                }
            };
        }

        private static CombatantState CreateCharacter(string id, string displayName,
            Element element, float hp, float mp, float atk, float def,
            float mst, float spr, float spd, List<AbilityEntry> abilities = null)
        {
            var data = ScriptableObject.CreateInstance<CharacterData>();
            data.Id = id;
            data.DisplayName = displayName;
            data.Element = element;
            data.BaseStats = new StatBlock { HP = hp, MP = mp, ATK = atk, DEF = def, MST = mst, SPR = spr, SPD = spd };
            data.SecondaryStats = new SecondaryStatBlock { CRI = 5f, LCK = 5f };
            data.LandAbilities = abilities ?? new List<AbilityEntry>();

            var stats = new StatBlock { HP = hp, MP = mp, ATK = atk, DEF = def, MST = mst, SPR = spr, SPD = spd };
            return new CombatantState(data, stats, 1);
        }

        private static AbilityData CreateAbility(string id, string displayName,
            float power, Element element, bool isPhysical, TargetType targetType,
            AbilityCategory category, int mpCost, int cooldown, float healPower = 0f)
        {
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            ability.Id = id;
            ability.DisplayName = displayName;
            ability.AbilityPower = power;
            ability.Element = element;
            ability.IsPhysical = isPhysical;
            ability.TargetType = targetType;
            ability.Category = category;
            ability.MPCost = mpCost;
            ability.Cooldown = cooldown;
            ability.HealPower = healPower;
            return ability;
        }
    }
}
