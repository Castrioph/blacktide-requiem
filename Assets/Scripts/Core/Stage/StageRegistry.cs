using System.Collections.Generic;
using UnityEngine;

namespace BlacktideRequiem.Core.Stage
{
    [CreateAssetMenu(fileName = "StageRegistry", menuName = "Blacktide/Stage Registry")]
    public class StageRegistry : ScriptableObject
    {
        public List<StageData> Stages = new List<StageData>();

        public StageData GetById(string id)
        {
            if (id == null) return null;
            foreach (var stage in Stages)
                if (stage != null && stage.Id == id) return stage;
            return null;
        }
    }
}
