using System;
using UnityEngine;
using UnityEngine.UI;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.UI.TeamSelect
{
    public class TeamRosterEntryUI : MonoBehaviour
    {
        [SerializeField] private Text _charName;
        [SerializeField] private Text _charElement;
        [SerializeField] private Image _border;
        [SerializeField] private Button _btnSelect;

        private static readonly Color BorderDefault  = new Color(0.361f, 0.239f, 0.118f);
        private static readonly Color BorderInTeam   = new Color(0.831f, 0.627f, 0.090f);
        private static readonly Color BorderDisabled = new Color(0.180f, 0.118f, 0.059f);

        private CharacterData _data;
        private Action<CharacterData> _onSelected;

        public CharacterData BoundCharacter => _data;

        public void Initialize(CharacterData data, Action<CharacterData> onSelected)
        {
            _data = data;
            _onSelected = onSelected;

            _charName.text    = data.DisplayName;
            _charElement.text = data.Element.ToString();

            _btnSelect.onClick.AddListener(OnSelectClicked);
            SetState(false, false);
        }

        /// <param name="inTeam">Character already assigned to a slot.</param>
        /// <param name="teamFull">All 3 slots filled and this character is not assigned.</param>
        public void SetState(bool inTeam, bool teamFull)
        {
            if (_border != null)
                _border.color = inTeam ? BorderInTeam : (teamFull ? BorderDisabled : BorderDefault);

            _btnSelect.interactable = inTeam || !teamFull;
        }

        private void OnDestroy()
        {
            if (_btnSelect != null)
                _btnSelect.onClick.RemoveListener(OnSelectClicked);
        }

        private void OnSelectClicked() => _onSelected?.Invoke(_data);
    }
}
