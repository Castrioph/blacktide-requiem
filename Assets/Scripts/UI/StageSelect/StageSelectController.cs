using System.Collections.Generic;
using UnityEngine;
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

        private static readonly Color LaunchColorEnabled  = new Color(0.831f, 0.627f, 0.090f);
        private static readonly Color LaunchColorDisabled = new Color(0.239f, 0.188f, 0.125f);
        private static readonly Color LaunchTextEnabled   = new Color(0.102f, 0.051f, 0f);
        private static readonly Color LaunchTextDisabled  = new Color(0.420f, 0.353f, 0.188f);

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
                _entries.Add(entry);
            }
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
            _btnLaunch.image.color = interactable ? LaunchColorEnabled : LaunchColorDisabled;

            Text label = _btnLaunch.GetComponentInChildren<Text>();
            if (label != null)
                label.color = interactable ? LaunchTextEnabled : LaunchTextDisabled;
        }
    }
}
