using UnityEngine;
using UnityEngine.UI;
using BlacktideRequiem.Core.Events;
using BlacktideRequiem.Runtime.Flow;

namespace BlacktideRequiem.UI.Results
{
    /// <summary>
    /// Controls the Results screen shown after combat.
    /// Reads battle outcome from GameFlowManager and displays it.
    /// </summary>
    public class ResultScreenController : MonoBehaviour
    {
        [SerializeField] private Text _resultTitle;
        [SerializeField] private Text _resultDetails;
        [SerializeField] private Button _btnReturnToMenu;

        private void Start()
        {
            _btnReturnToMenu.onClick.AddListener(OnReturnToMenu);
            DisplayResult();
        }

        private void OnDestroy()
        {
            if (_btnReturnToMenu != null)
                _btnReturnToMenu.onClick.RemoveListener(OnReturnToMenu);
        }

        private void DisplayResult()
        {
            if (GameFlowManager.Instance == null || !GameFlowManager.Instance.LastBattleResult.HasValue)
            {
                _resultTitle.text = "No Battle Data";
                _resultDetails.text = "";
                return;
            }

            var result = GameFlowManager.Instance.LastBattleResult.Value;

            _resultTitle.text = result.Result == BattleResult.Victory
                ? "VICTORY"
                : "DEFEAT";

            _resultDetails.text = $"Rounds: {result.RoundsElapsed}";
        }

        private void OnReturnToMenu()
        {
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.LoadMainMenu();
        }
    }
}
