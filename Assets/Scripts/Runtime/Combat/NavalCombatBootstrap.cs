using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Economy;
using BlacktideRequiem.Core.Stage;
using BlacktideRequiem.Runtime.Flow;
using BlacktideRequiem.UI.Combat.Naval;

namespace BlacktideRequiem.Runtime.Combat
{
    /// <summary>
    /// Arranca la batalla naval en la escena NavalCombat. Con flujo activo
    /// (GameFlowManager con NavalStageData seleccionado) usa el stage y el
    /// equipo elegido como tripulación; sin flujo (escena abierta directa)
    /// cae al stage de demo serializado. Cablea HUD, AI por enemigo y el
    /// payout de rewards (RewardDispatcher) en victoria.
    /// </summary>
    public class NavalCombatBootstrap : MonoBehaviour
    {
        [Header("Fallback demo (escena abierta sin flujo)")]
        [SerializeField] private NavalStageData _demoStage;
        [Tooltip("Crew aliada del fallback demo")]
        [SerializeField] private List<CharacterData> _demoCrew = new();

        [Header("Economy")]
        [SerializeField] private CurrencyWallet _wallet;

        [Header("Scene refs")]
        [SerializeField] private CombatRunner _runner;
        [SerializeField] private NavalCombatHUD _hud;

        private RewardDispatcher _rewards;

        private void Start()
        {
            var flow = GameFlowManager.Instance;
            var stage = flow?.SelectedStage as NavalStageData ?? _demoStage;

            IReadOnlyList<CharacterData> crew =
                flow?.SelectedTeam != null && flow.SelectedTeam.IsValid
                    ? flow.SelectedTeam.GetTeam()
                    : _demoCrew;

            if (stage == null || _runner == null || _hud == null)
            {
                Debug.LogError("[NavalBootstrap] Faltan referencias (stage/runner/hud)");
                return;
            }

            var setup = NavalStageController.BuildNavalBattle(stage, crew);

            if (stage.Rewards != null && _wallet != null)
            {
                _rewards = new RewardDispatcher(stage.Rewards, _wallet);
                _rewards.Connect();
            }

            var playerInput = new NavalPlayerCombatInput();
            _hud.Bind(playerInput);

            _runner.StartBattle(setup.Config, playerInput,
                resolver: new NavalTurnResolver(),
                enemyInputSelector: c =>
                    setup.EnemyInputs.TryGetValue(c, out var ai) ? ai : null);

            var firstWaveEnemies = new List<ICombatant>();
            foreach (var entry in setup.Config.Waves[0].Enemies)
                firstWaveEnemies.Add(entry.Combatant);
            _hud.BuildBattlefield(setup.AllyShip, firstWaveEnemies);
        }

        private void OnDestroy()
        {
            _rewards?.Disconnect();
        }
    }
}
