using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Team;
using BlacktideRequiem.Runtime.Flow;

namespace BlacktideRequiem.UI.TeamSelect
{
    public class TeamSelectController : MonoBehaviour
    {
        [SerializeField] private CharacterData[] _roster;

        [Header("Slots (3 exactly)")]
        [SerializeField] private Text[]   _slotNameTexts;
        [SerializeField] private Button[] _slotClearButtons;

        [Header("Roster list")]
        [SerializeField] private Transform         _rosterContainer;
        [SerializeField] private TeamRosterEntryUI _rosterEntryPrefab;

        [Header("Navigation")]
        [SerializeField] private Button _btnBack;
        [SerializeField] private Button _btnConfirm;

        private static readonly Color ConfirmColorEnabled  = new Color(0.831f, 0.627f, 0.090f);
        private static readonly Color ConfirmColorDisabled = new Color(0.239f, 0.188f, 0.125f);
        private static readonly Color ConfirmTextEnabled   = new Color(0.102f, 0.051f, 0f);
        private static readonly Color ConfirmTextDisabled  = new Color(0.420f, 0.353f, 0.188f);

        private static readonly string EmptySlotLabel = "— Vacío —";

        private TeamComposition _composition;
        private readonly List<TeamRosterEntryUI> _entries = new List<TeamRosterEntryUI>();

        private void Start()
        {
            if (_roster == null || _roster.Length == 0)
            {
                Debug.LogError("[TeamSelectController] Roster not assigned.", this);
                return;
            }

            _composition = new TeamComposition(_roster);

            _btnBack?.onClick.AddListener(OnBackClicked);
            _btnConfirm?.onClick.AddListener(OnConfirmClicked);

            for (int i = 0; i < TeamComposition.MaxSlots; i++)
            {
                int captured = i;
                _slotClearButtons?[captured]?.onClick.AddListener(() => OnSlotClearClicked(captured));
            }

            BuildRosterList();
            RefreshSlotDisplays();
            SetConfirmInteractable(false);
        }

        private void OnDestroy()
        {
            if (_btnBack != null)    _btnBack.onClick.RemoveListener(OnBackClicked);
            if (_btnConfirm != null) _btnConfirm.onClick.RemoveListener(OnConfirmClicked);

            if (_slotClearButtons != null)
                for (int i = 0; i < _slotClearButtons.Length; i++)
                    _slotClearButtons[i]?.onClick.RemoveAllListeners();
        }

        private void BuildRosterList()
        {
            if (_rosterEntryPrefab == null || _rosterContainer == null)
            {
                Debug.LogError("[TeamSelectController] Roster prefab or container not assigned.", this);
                return;
            }

            foreach (CharacterData data in _roster)
            {
                if (data == null) continue;
                TeamRosterEntryUI entry = Instantiate(_rosterEntryPrefab, _rosterContainer);
                entry.Initialize(data, OnRosterEntryClicked);
                _entries.Add(entry);
            }
        }

        private void OnRosterEntryClicked(CharacterData data)
        {
            // If already in team → remove from its slot
            for (int i = 0; i < TeamComposition.MaxSlots; i++)
            {
                if (_composition.GetSlot(i) == data)
                {
                    _composition.ClearSlot(i);
                    RefreshAll();
                    return;
                }
            }

            // Find first empty slot and assign
            for (int i = 0; i < TeamComposition.MaxSlots; i++)
            {
                if (_composition.GetSlot(i) == null)
                {
                    _composition.SelectCharacter(i, data);
                    RefreshAll();
                    return;
                }
            }
        }

        private void OnSlotClearClicked(int slotIndex)
        {
            _composition.ClearSlot(slotIndex);
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshSlotDisplays();
            RefreshRosterEntryStates();
            SetConfirmInteractable(_composition.IsValid);
        }

        private void RefreshSlotDisplays()
        {
            if (_slotNameTexts == null) return;
            for (int i = 0; i < TeamComposition.MaxSlots && i < _slotNameTexts.Length; i++)
            {
                if (_slotNameTexts[i] == null) continue;
                CharacterData inSlot = _composition.GetSlot(i);
                _slotNameTexts[i].text = inSlot != null ? inSlot.DisplayName : EmptySlotLabel;
            }
        }

        private void RefreshRosterEntryStates()
        {
            bool teamFull = _composition.FilledSlotCount >= TeamComposition.MaxSlots;
            foreach (TeamRosterEntryUI entry in _entries)
            {
                bool inTeam = IsInTeam(entry.BoundCharacter);
                entry.SetState(inTeam, teamFull && !inTeam);
            }
        }

        private bool IsInTeam(CharacterData data)
        {
            for (int i = 0; i < TeamComposition.MaxSlots; i++)
                if (_composition.GetSlot(i) == data) return true;
            return false;
        }

        private void OnConfirmClicked()
        {
            if (!_composition.IsValid) return;

            if (GameFlowManager.Instance == null)
            {
                Debug.LogError("[TeamSelectController] GameFlowManager not found.", this);
                return;
            }

            GameFlowManager.Instance.SelectedTeam = _composition;
            GameFlowManager.Instance.LoadCombat();
        }

        private void OnBackClicked()
        {
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.LoadStageSelect();
        }

        private void SetConfirmInteractable(bool interactable)
        {
            if (_btnConfirm == null) return;
            _btnConfirm.interactable = interactable;
            _btnConfirm.image.color = interactable ? ConfirmColorEnabled : ConfirmColorDisabled;

            Text label = _btnConfirm.GetComponentInChildren<Text>();
            if (label != null)
                label.color = interactable ? ConfirmTextEnabled : ConfirmTextDisabled;
        }
    }
}
