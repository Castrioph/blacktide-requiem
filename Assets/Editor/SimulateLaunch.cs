using UnityEngine;
using BlacktideRequiem.Runtime.Flow;
using BlacktideRequiem.Core.Stage;

public class SimulateLaunch
{
    public static void Execute()
    {
        var gfm = Object.FindFirstObjectByType<GameFlowManager>();
        if (gfm == null) { Debug.LogError("[Sim] GFM not found"); return; }

        // Load first StageData directly from assets
        var stage = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>(
            "Assets/Data/Stages/stage_001_bahia_corsaria.asset");

        if (stage == null) { Debug.LogError("[Sim] No StageData found"); return; }

        gfm.SelectedStage = stage;
        Debug.Log($"[Sim] Set SelectedStage={stage.DisplayName}, loading TeamSelect");
        gfm.LoadTeamSelect();
    }
}
