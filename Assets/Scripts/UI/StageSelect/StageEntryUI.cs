using System;
using UnityEngine;
using UnityEngine.UI;
using BlacktideRequiem.Core.Stage;

namespace BlacktideRequiem.UI.StageSelect
{
    public class StageEntryUI : MonoBehaviour
    {
        [SerializeField] private Text _stageName;
        [SerializeField] private Text _stageDifficulty;
        [SerializeField] private Image _border;
        [SerializeField] private Button _btnSelect;

        private static readonly Color BorderUnselected = new Color(0.361f, 0.239f, 0.118f); // #5C3D1E
        private static readonly Color BorderSelected   = new Color(0.831f, 0.627f, 0.090f); // #D4A017
        private static readonly Vector3 ScaleSelected   = new Vector3(1.02f, 1.02f, 1f);
        private static readonly Vector3 ScaleUnselected = Vector3.one;

        private StageData _stageData;
        private Action<StageData> _onSelected;

        public StageData BoundStage => _stageData;

        public void Initialize(StageData stageData, Action<StageData> onSelected)
        {
            _stageData = stageData;
            _onSelected = onSelected;

            _stageName.text = stageData.DisplayName;
            _stageDifficulty.text = BuildDifficultyString(stageData.DifficultyLevel);

            _btnSelect.onClick.AddListener(OnSelectClicked);

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_border != null)
                _border.color = selected ? BorderSelected : BorderUnselected;

            transform.localScale = selected ? ScaleSelected : ScaleUnselected;
        }

        private void OnDestroy()
        {
            if (_btnSelect != null)
                _btnSelect.onClick.RemoveListener(OnSelectClicked);
        }

        private void OnSelectClicked() => _onSelected?.Invoke(_stageData);

        private static string BuildDifficultyString(int level)
        {
            return new string('★', level) + new string('☆', 5 - level);
        }
    }
}
