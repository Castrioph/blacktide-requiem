using System;
using UnityEngine;
using UnityEngine.UI;
using BlacktideRequiem.Core.Economy;
using BlacktideRequiem.Core.Stage;

namespace BlacktideRequiem.UI.StageSelect
{
    public class StageEntryUI : MonoBehaviour
    {
        [SerializeField] private Text  _stageName;
        [SerializeField] private Text  _stageDifficulty;
        [SerializeField] private Image _border;
        [SerializeField] private Button _btnSelect;

        // P2-04 — left accent stripe (assigned in prefab by ApplyS311UIPolish)
        [SerializeField] private Image _accentStripe;

        // P2-03 — reward preview value (assigned in prefab by ApplyS311UIPolish)
        [SerializeField] private Text _rewardValue;

        private static readonly Color BorderUnselected = new Color(0.361f, 0.239f, 0.118f); // #5C3D1E
        private static readonly Color BorderSelected   = new Color(0.831f, 0.627f, 0.090f); // #D4A017
        private static readonly Vector3 ScaleSelected   = new Vector3(1.02f, 1.02f, 1f);
        private static readonly Vector3 ScaleUnselected = Vector3.one;

        private const string EmptyDotHex = "#2A3A4A"; // NeutralDot

        private StageData _stageData;
        private Action<StageData> _onSelected;
        private string _filledDotHex = "#F2C740"; // gold fallback until SetAccent

        public StageData BoundStage => _stageData;

        /// <summary>Focus target for gamepad/keyboard navigation.</summary>
        public GameObject SelectableObject => _btnSelect != null ? _btnSelect.gameObject : gameObject;

        public void Initialize(StageData stageData, Action<StageData> onSelected)
        {
            _stageData = stageData;
            _onSelected = onSelected;

            _stageName.text = stageData.DisplayName;
            RefreshDifficulty();

            if (_rewardValue != null)
                _rewardValue.text = BuildRewardString(stageData);

            _btnSelect.onClick.AddListener(OnSelectClicked);

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_border != null)
                _border.color = selected ? BorderSelected : BorderUnselected;

            transform.localScale = selected ? ScaleSelected : ScaleUnselected;
        }

        /// <summary>
        /// Applies per-stage identity colors: primary on the left stripe,
        /// secondary on the filled difficulty dots (P2-04 / P2-05).
        /// </summary>
        public void SetAccent(StageAccentPalette.StageAccent accent)
        {
            if (_accentStripe != null)
                _accentStripe.color = accent.Primary;

            _filledDotHex = "#" + ColorUtility.ToHtmlStringRGB(accent.Secondary);
            RefreshDifficulty();
        }

        private void OnDestroy()
        {
            if (_btnSelect != null)
                _btnSelect.onClick.RemoveListener(OnSelectClicked);
        }

        private void OnSelectClicked() => _onSelected?.Invoke(_stageData);

        private void RefreshDifficulty()
        {
            if (_stageData != null && _stageDifficulty != null)
                _stageDifficulty.text = BuildDifficultyString(_stageData.DifficultyLevel, _filledDotHex);
        }

        // P2-05: rich-text colored dots — legacy UI.Text supports <color> tags.
        private static string BuildDifficultyString(int level, string filledHex)
        {
            int clamped = Mathf.Clamp(level, 1, 5);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(i < clamped
                    ? $"<color={filledHex}>●</color>"
                    : $"<color={EmptyDotHex}>○</color>");
            }
            return sb.ToString();
        }

        private static string BuildRewardString(StageData stage)
        {
            if (stage.Rewards == null || stage.Rewards.Entries == null || stage.Rewards.Entries.Count == 0)
                return "???";

            RewardEntry first = stage.Rewards.Entries[0];
            return $"{first.Amount} {CurrencyLabel(first.Currency)}";
        }

        private static string CurrencyLabel(CurrencyType currency) =>
            currency == CurrencyType.GemasDeCalavera ? "Gemas de Calavera" : "Doblones";
    }
}
