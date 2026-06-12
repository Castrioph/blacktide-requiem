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

        // S3-11 — card background + element accent stripe (assigned in prefab by ApplyS311UIPolish)
        [SerializeField] private Image _background;
        [SerializeField] private Image _accentStripe;

        private static readonly Color BorderDefault  = new Color(0.227f, 0.165f, 0.310f);  // #3A2A50
        private static readonly Color BorderInTeam   = new Color(0.831f, 0.627f, 0.090f);  // #D4A017
        private static readonly Color BorderDisabled = new Color(0.180f, 0.118f, 0.059f);
        private static readonly Color BgDefault      = new Color(0.122f, 0.090f, 0.180f, 0.902f); // #1F172E a230
        private static readonly Color BgInTeam       = new Color(0.165f, 0.118f, 0.063f);  // #2A1E10

        private CharacterData _data;
        private Action<CharacterData> _onSelected;

        public CharacterData BoundCharacter => _data;

        /// <summary>Focus target for gamepad/keyboard navigation.</summary>
        public GameObject SelectableObject => _btnSelect != null ? _btnSelect.gameObject : gameObject;

        public void Initialize(CharacterData data, Action<CharacterData> onSelected)
        {
            _data = data;
            _onSelected = onSelected;

            _charName.text    = data.DisplayName;
            _charElement.text = ElementDisplayName(data.Element);

            Color accent = ElementAccentColor(data.Element);
            _charElement.color = accent;
            if (_accentStripe != null)
                _accentStripe.color = accent;

            _btnSelect.onClick.AddListener(OnSelectClicked);
            SetState(false, false);
        }

        /// <param name="inTeam">Character already assigned to a slot.</param>
        /// <param name="teamFull">All 3 slots filled and this character is not assigned.</param>
        public void SetState(bool inTeam, bool teamFull)
        {
            if (_border != null)
                _border.color = inTeam ? BorderInTeam : (teamFull ? BorderDisabled : BorderDefault);

            if (_background != null)
                _background.color = inTeam ? BgInTeam : BgDefault;

            _btnSelect.interactable = inTeam || !teamFull;
        }

        private void OnDestroy()
        {
            if (_btnSelect != null)
                _btnSelect.onClick.RemoveListener(OnSelectClicked);
        }

        private void OnSelectClicked() => _onSelected?.Invoke(_data);

        // P2-07: enum names carry no diacritics; map to proper Spanish display strings.
        private static string ElementDisplayName(Element element)
        {
            switch (element)
            {
                case Element.Polvora:   return "Pólvora";
                case Element.Maldicion: return "Maldición";
                default:                return element.ToString();
            }
        }

        private static Color ElementAccentColor(Element element)
        {
            switch (element)
            {
                case Element.Polvora:   return new Color(0.749f, 0.212f, 0.047f); // Temple Ember
                case Element.Tormenta:  return new Color(0.118f, 0.533f, 0.898f); // Corsair Blue
                case Element.Maldicion: return new Color(0.416f, 0.106f, 0.604f); // Voodoo Violet
                case Element.Bestia:    return new Color(0.180f, 0.490f, 0.196f);
                case Element.Acero:     return new Color(0.471f, 0.565f, 0.612f);
                case Element.Luz:       return new Color(0.910f, 0.706f, 0.125f); // Gold Mid
                case Element.Sombra:    return new Color(0.290f, 0.078f, 0.549f);
                default:                return new Color(0.545f, 0.369f, 0.235f); // WoodCatch neutral
            }
        }
    }
}
