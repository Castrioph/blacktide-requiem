using System;
using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Stage
{
    [CreateAssetMenu(fileName = "stage_", menuName = "Blacktide/Stage Data")]
    public class StageData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        [Range(1, 5)] public int DifficultyLevel;
        public List<WaveDefinition> Waves = new List<WaveDefinition>();
    }

    [Serializable]
    public class WaveDefinition
    {
        public List<EnemySlot> Enemies = new List<EnemySlot>();
        public int EnemyCaptainIndex = -1;
    }

    [Serializable]
    public class EnemySlot
    {
        public CharacterData Enemy;
        public int SlotIndex;
    }
}
