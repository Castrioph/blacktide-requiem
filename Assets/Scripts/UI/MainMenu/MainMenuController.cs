using UnityEngine;
using UnityEngine.UI;
using BlacktideRequiem.Runtime.Flow;

namespace BlacktideRequiem.UI.MainMenu
{
    /// <summary>
    /// Controls the Main Menu screen. Wires the Start Battle button
    /// to GameFlowManager scene transition.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _btnStartBattle;

        private void Start()
        {
            _btnStartBattle.onClick.AddListener(OnStartBattle);
        }

        private void OnDestroy()
        {
            if (_btnStartBattle != null)
                _btnStartBattle.onClick.RemoveListener(OnStartBattle);
        }

        private void OnStartBattle()
        {
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.LoadCombat();
        }
    }
}
