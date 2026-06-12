using System.Text;
using UnityEngine;
using UnityEngine.UI;
using BlacktideRequiem.Core.Economy;
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

            var details = new StringBuilder($"Rounds: {result.RoundsElapsed}");

            // Rewards del stage (pagadas por RewardDispatcher en victoria)
            var rewards = GameFlowManager.Instance.SelectedStage?.Rewards;
            if (result.Result == BattleResult.Victory &&
                rewards != null && rewards.Entries != null && rewards.Entries.Count > 0)
            {
                details.Append("\nRecompensas: ");
                for (int i = 0; i < rewards.Entries.Count; i++)
                {
                    if (i > 0) details.Append(", ");
                    details.Append(rewards.Entries[i].Amount)
                           .Append(' ')
                           .Append(CurrencyLabel(rewards.Entries[i].Currency));
                }
            }

            _resultDetails.text = details.ToString();
        }

        private static string CurrencyLabel(CurrencyType type)
        {
            return type switch
            {
                CurrencyType.Doblones => "Doblones",
                CurrencyType.GemasDeCalavera => "Gemas de Calavera",
                _ => type.ToString()
            };
        }

        private void OnReturnToMenu()
        {
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.LoadMainMenu();
        }
    }
}
