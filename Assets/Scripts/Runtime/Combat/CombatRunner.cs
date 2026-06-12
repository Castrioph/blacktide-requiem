using System;
using System.Collections;
using UnityEngine;
using BlacktideRequiem.Core.AI;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.Runtime.Combat
{
    /// <summary>
    /// Thin MonoBehaviour wrapper that drives CombatManager via coroutines.
    /// Owns the battle coroutine lifecycle — CombatManager stays pure C#.
    /// See ADR-003 §1.
    /// </summary>
    public class CombatRunner : MonoBehaviour
    {
        [Tooltip("Delay between turns for readability")]
        [SerializeField] private float _turnDelay = 0.5f;

        [Tooltip("Delay after enemy action")]
        [SerializeField] private float _enemyActionDelay = 0.8f;

        [Tooltip("Delay for wave transition")]
        [SerializeField] private float _waveTransitionDelay = 1.5f;

        private CombatManager _manager;
        private ICombatInput _playerInput;
        private EnemyAI _defaultEnemyAI;
        private Func<ICombatant, ICombatInput> _enemyInputSelector;

        /// <summary>The CombatManager being driven. Set after StartBattle.</summary>
        public CombatManager Manager => _manager;

        /// <summary>
        /// Initializes and starts a battle with the given config and player input source.
        /// Naval battles pass a NavalTurnResolver and a per-enemy input selector
        /// (each naval enemy owns its NavalEnemyAI — bosses carry phase state).
        /// </summary>
        public void StartBattle(BattleConfig config, ICombatInput playerInput,
            EnemyAI enemyAI = null, ITurnResolver resolver = null,
            Func<ICombatant, ICombatInput> enemyInputSelector = null)
        {
            _playerInput = playerInput;
            _defaultEnemyAI = enemyAI ?? new EnemyAI(AIProfileType.Agresivo);
            _enemyInputSelector = enemyInputSelector;

            var bar = new InitiativeBar();
            _manager = new CombatManager(bar, resolver);
            _manager.StartBattle(config);

            StartCoroutine(BattleLoop());
        }

        private IEnumerator BattleLoop()
        {
            while (_manager.Phase == BattlePhase.InRound)
            {
                _manager.BeginRound();

                while (true)
                {
                    var entry = _manager.AdvanceTurn();
                    if (entry == null) break; // round over

                    if (_manager.Phase == BattlePhase.Victory ||
                        _manager.Phase == BattlePhase.Defeat)
                        break;

                    yield return new WaitForSeconds(_turnDelay);

                    // Determine input source
                    var context = _manager.GetCurrentContext();
                    CombatAction? chosenAction = null;

                    if (entry.Team == CombatTeam.Ally)
                    {
                        _playerInput.RequestAction(context, action => chosenAction = action);

                        // Wait for player input (PlayerCombatInput sets this via callback)
                        while (chosenAction == null)
                            yield return null;
                    }
                    else
                    {
                        var enemyInput = _enemyInputSelector?.Invoke(entry.Combatant)
                            ?? (ICombatInput)_defaultEnemyAI;
                        enemyInput.RequestAction(context, action => chosenAction = action);
                        yield return new WaitForSeconds(_enemyActionDelay);
                    }

                    _manager.ResolveAction(chosenAction.Value);
                    _manager.CompleteTurn();

                    if (_manager.Phase == BattlePhase.Victory ||
                        _manager.Phase == BattlePhase.Defeat)
                        break;

                    // Wave cleared mid-round — skip remaining turns, transition
                    if (_manager.Phase == BattlePhase.InRound && _manager.IsCurrentWaveCleared)
                        break;
                }

                // Check wave transition
                if (_manager.Phase == BattlePhase.InRound && _manager.IsCurrentWaveCleared)
                {
                    yield return new WaitForSeconds(_waveTransitionDelay);
                    _manager.TransitionToNextWave();
                }
            }

            // Battle ended — events already fired by CombatManager
        }
    }
}
