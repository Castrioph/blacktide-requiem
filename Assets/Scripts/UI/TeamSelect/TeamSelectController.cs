using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [SerializeField] private Text[]  _slotNameTexts;
        [SerializeField] private Image[] _slotBackgrounds;
        [SerializeField] private Image[] _slotBorders;

        [Header("Roster list")]
        [SerializeField] private Transform         _rosterContainer;
        [SerializeField] private TeamRosterEntryUI _rosterEntryPrefab;

        [Header("Navigation")]
        [SerializeField] private Button _btnBack;
        [SerializeField] private Button _btnConfirm;

        // Button background states are driven by the Button ColorBlock (scene-serialized).
        // The controller only adjusts the label for WCAG-compliant disabled contrast.
        private static readonly Color ConfirmTextEnabled  = new Color(0.102f, 0.051f, 0f);            // #1A0D00
        private static readonly Color ConfirmTextDisabled = new Color(0.627f, 0.502f, 0.251f, 0.7f);  // #A08040 a180

        // Slot visual states — docs/art/ui-s311-visual-design.md §4.4
        private static readonly Color SlotBgEmpty      = new Color(0.122f, 0.090f, 0.180f);  // #1F172E
        private static readonly Color SlotBgFilled     = new Color(0.165f, 0.118f, 0.063f);  // #2A1E10
        private static readonly Color SlotBorderEmpty  = new Color(0.227f, 0.165f, 0.310f);  // #3A2A50
        private static readonly Color SlotBorderFilled = new Color(0.831f, 0.627f, 0.090f);  // #D4A017
        private static readonly Color SlotNameEmpty    = new Color(0.929f, 0.851f, 0.639f, 0.55f); // cream a140
        private static readonly Color SlotNameFilled   = new Color(0.961f, 0.902f, 0.784f);  // #F5E6C8

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

            BuildRosterList();
            RefreshSlotDisplays();
            SetConfirmInteractable(false);
            FocusFirstEntry();
        }

        private void OnDestroy()
        {
            if (_btnBack != null)    _btnBack.onClick.RemoveListener(OnBackClicked);
            if (_btnConfirm != null) _btnConfirm.onClick.RemoveListener(OnConfirmClicked);
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

        // P0-02: runtime-spawned entries cannot be serialized as firstSelected,
        // so give gamepad/keyboard focus to the first roster card here.
        private void FocusFirstEntry()
        {
            if (_entries.Count > 0 && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_entries[0].SelectableObject);
        }

        // Tap toggles membership: in team → remove, not in team → first empty slot.
        // BtnClear was removed in S3-11 (user decision) — this is the only clear path.
        private void OnRosterEntryClicked(CharacterData data)
        {
            for (int i = 0; i < TeamComposition.MaxSlots; i++)
            {
                if (_composition.GetSlot(i) == data)
                {
                    _composition.ClearSlot(i);
                    RefreshAll();
                    return;
                }
            }

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

        private void RefreshAll()
        {
            RefreshSlotDisplays();
            RefreshRosterEntryStates();
            SetConfirmInteractable(_composition.IsValid);
        }

        private void RefreshSlotDisplays()
        {
            for (int i = 0; i < TeamComposition.MaxSlots; i++)
            {
                CharacterData inSlot = _composition.GetSlot(i);
                bool filled = inSlot != null;

                if (_slotNameTexts != null && i < _slotNameTexts.Length && _slotNameTexts[i] != null)
                {
                    _slotNameTexts[i].text  = filled ? inSlot.DisplayName : EmptySlotLabel;
                    _slotNameTexts[i].color = filled ? SlotNameFilled : SlotNameEmpty;
                }

                if (_slotBackgrounds != null && i < _slotBackgrounds.Length && _slotBackgrounds[i] != null)
                    _slotBackgrounds[i].color = filled ? SlotBgFilled : SlotBgEmpty;

                if (_slotBorders != null && i < _slotBorders.Length && _slotBorders[i] != null)
                    _slotBorders[i].color = filled ? SlotBorderFilled : SlotBorderEmpty;
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

            Text label = _btnConfirm.GetComponentInChildren<Text>();
            if (label != null)
                label.color = interactable ? ConfirmTextEnabled : ConfirmTextDisabled;
        }
    }
}
