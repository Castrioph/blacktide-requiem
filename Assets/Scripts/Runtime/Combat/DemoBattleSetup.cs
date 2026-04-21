using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.AI;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.UI.Combat;

namespace BlacktideRequiem.Runtime.Combat
{
    public class DemoBattleSetup : MonoBehaviour
    {
        [SerializeField] private CombatRunner _runner;
        [SerializeField] private CombatHUDCanvas _hud;

        [Header("Allies")]
        [SerializeField] private CharacterData _elena;
        [SerializeField] private CharacterData _kael;
        [SerializeField] private CharacterData _mirra;

        [Header("Enemies Wave 1")]
        [SerializeField] private CharacterData _pirateGrunt;
        [SerializeField] private CharacterData _pirateBrute;

        [Header("Enemies Wave 2")]
        [SerializeField] private CharacterData _corsair;
        [SerializeField] private CharacterData _hexer;

        private PlayerCombatInput _playerInput;

        private void Start()
        {
            _playerInput = new PlayerCombatInput();
            _hud.Bind(_playerInput);

            var config = BuildDemoConfig();

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
            return new BattleConfig
            {
                Allies = new List<InitiativeEntry>
                {
                    new InitiativeEntry(MakeState(_elena), CombatTeam.Ally, 0),
                    new InitiativeEntry(MakeState(_kael), CombatTeam.Ally, 1),
                    new InitiativeEntry(MakeState(_mirra), CombatTeam.Ally, 2)
                },
                Waves = new List<WaveConfig>
                {
                    new WaveConfig
                    {
                        Enemies = new List<InitiativeEntry>
                        {
                            new InitiativeEntry(MakeState(_pirateGrunt), CombatTeam.Enemy, 0),
                            new InitiativeEntry(MakeState(_pirateBrute), CombatTeam.Enemy, 1)
                        }
                    },
                    new WaveConfig
                    {
                        Enemies = new List<InitiativeEntry>
                        {
                            new InitiativeEntry(MakeState(_corsair), CombatTeam.Enemy, 0),
                            new InitiativeEntry(MakeState(_hexer), CombatTeam.Enemy, 1)
                        }
                    }
                }
            };
        }

        private static CombatantState MakeState(CharacterData data) =>
            new CombatantState(data, data.BaseStats, 1);
    }
}
