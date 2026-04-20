using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using BlacktideRequiem.Core.Events;
using BlacktideRequiem.Core.Flow;

namespace BlacktideRequiem.Runtime.Flow
{
    /// <summary>
    /// Manages game screen flow: MainMenu → Combat → Results → MainMenu.
    /// Persists across scenes via DontDestroyOnLoad. Handles scene transitions
    /// and stores battle result data for the Results screen.
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        /// <summary>Singleton instance. Set on Awake, cleared on destroy.</summary>
        public static GameFlowManager Instance { get; private set; }

        /// <summary>Current flow state.</summary>
        public GameFlowState CurrentState { get; private set; } = GameFlowState.None;

        /// <summary>Last battle result, available after combat ends.</summary>
        public BattleEndEvent? LastBattleResult { get; private set; }

        /// <summary>Fired when flow state changes.</summary>
        public event Action<GameFlowState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameEvents.OnBattleEnd += HandleBattleEnd;
        }

        private void OnDisable()
        {
            GameEvents.OnBattleEnd -= HandleBattleEnd;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Transition to MainMenu scene.
        /// </summary>
        public void LoadMainMenu()
        {
            LastBattleResult = null;
            TransitionTo(GameFlowState.MainMenu, SceneRegistry.MainMenu);
        }

        /// <summary>
        /// Transition to Combat scene.
        /// </summary>
        public void LoadCombat()
        {
            LastBattleResult = null;
            TransitionTo(GameFlowState.Combat, SceneRegistry.Combat);
        }

        /// <summary>
        /// Transition to Results scene. Requires LastBattleResult to be set.
        /// </summary>
        public void LoadResults()
        {
            TransitionTo(GameFlowState.Results, SceneRegistry.Results);
        }

        private void TransitionTo(GameFlowState newState, string sceneName)
        {
            GameEvents.ClearAll();

            // Re-subscribe after clear so we catch battle end in combat
            GameEvents.OnBattleEnd += HandleBattleEnd;

            CurrentState = newState;
            OnStateChanged?.Invoke(newState);

            SceneManager.LoadScene(sceneName);
        }

        private void HandleBattleEnd(BattleEndEvent e)
        {
            LastBattleResult = e;
        }
    }
}
