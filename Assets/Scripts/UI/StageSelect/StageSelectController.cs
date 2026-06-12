using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BlacktideRequiem.Core.Stage;
using BlacktideRequiem.Runtime.Flow;

namespace BlacktideRequiem.UI.StageSelect
{
    public class StageSelectController : MonoBehaviour
    {
        [SerializeField] private StageRegistry _stageRegistry;
        [SerializeField] private Transform _entryContainer;
        [SerializeField] private StageEntryUI _entryPrefab;
        [SerializeField] private Button _btnBack;
        [SerializeField] private Button _btnLaunch;
        [SerializeField] private Text _emptyStateText;

        // Button background states are driven by the Button ColorBlock (scene-serialized).
        // The controller only adjusts the label for WCAG-compliant disabled contrast.
        private static readonly Color LaunchTextEnabled  = new Color(0.102f, 0.051f, 0f);            // #1A0D00
        private static readonly Color LaunchTextDisabled = new Color(0.627f, 0.502f, 0.251f, 0.7f);  // #A08040 a180

        private readonly List<StageEntryUI> _entries = new List<StageEntryUI>();
        private StageEntryUI _selectedEntry;
        private StageData _selectedStage;

        private void Start()
        {
            if (_btnBack == null)   Debug.LogWarning("[StageSelectController] Back button not assigned.", this);
            if (_btnLaunch == null) Debug.LogWarning("[StageSelectController] Launch button not assigned.", this);

            _btnBack?.onClick.AddListener(OnBackClicked);
            _btnLaunch?.onClick.AddListener(OnLaunchClicked);

            SetLaunchInteractable(false);
            PopulateStageList();
            FocusFirstEntry();
        }

        private void OnDestroy()
        {
            if (_btnBack != null)   _btnBack.onClick.RemoveListener(OnBackClicked);
            if (_btnLaunch != null) _btnLaunch.onClick.RemoveListener(OnLaunchClicked);
        }

        private void PopulateStageList()
        {
            bool hasStages = _stageRegistry != null
                && _stageRegistry.Stages != null
                && _stageRegistry.Stages.Count > 0;

            if (_emptyStateText != null)
                _emptyStateText.gameObject.SetActive(!hasStages);

            if (!hasStages)
            {
                if (_stageRegistry == null)
                    Debug.LogError("[StageSelectController] StageRegistry not assigned.", this);
                return;
            }

            if (_entryPrefab == null)
            {
                Debug.LogError("[StageSelectController] Entry prefab not assigned.", this);
                return;
            }

            foreach (StageData stage in _stageRegistry.Stages)
            {
                if (stage == null) continue;
                StageEntryUI entry = Instantiate(_entryPrefab, _entryContainer);
                entry.Initialize(stage, OnStageSelected);
                entry.SetAccent(StageAccentPalette.Get(stage.Id));
                _entries.Add(entry);
            }
        }

        // P0-02: runtime-spawned entries cannot be serialized as firstSelected,
        // so give gamepad/keyboard focus to the first stage card here.
        private void FocusFirstEntry()
        {
            if (_entries.Count > 0 && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_entries[0].SelectableObject);
        }

        private void OnStageSelected(StageData stage)
        {
            foreach (StageEntryUI entry in _entries)
                entry.SetSelected(false);

            _selectedStage = stage;

            // Find the entry bound to this stage to apply selected visual
            foreach (StageEntryUI entry in _entries)
            {
                if (entry.BoundStage == stage)
                {
                    _selectedEntry = entry;
                    entry.SetSelected(true);
                    break;
                }
            }

            SetLaunchInteractable(true);
        }

        private void OnLaunchClicked()
        {
            if (_selectedStage == null) return;

            if (GameFlowManager.Instance == null)
            {
                Debug.LogError("[StageSelectController] GameFlowManager not found.", this);
                return;
            }

            GameFlowManager.Instance.SelectedStage = _selectedStage;
            GameFlowManager.Instance.LoadTeamSelect();
        }

        private void OnBackClicked()
        {
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.LoadMainMenu();
        }

        private void SetLaunchInteractable(bool interactable)
        {
            if (_btnLaunch == null) return;
            _btnLaunch.interactable = interactable;

            Text label = _btnLaunch.GetComponentInChildren<Text>();
            if (label != null)
                label.color = interactable ? LaunchTextEnabled : LaunchTextDisabled;
        }
    }
}
