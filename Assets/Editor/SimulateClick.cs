using UnityEngine;
using UnityEngine.SceneManagement;
using BlacktideRequiem.Runtime.Flow;

public class SimulateClick
{
    public static void Execute()
    {
        var gfm = Object.FindObjectOfType<GameFlowManager>();
        if (gfm == null)
        {
            Debug.LogError("[Sim] GameFlowManager component not found via FindObjectOfType");
            return;
        }
        Debug.Log($"[Sim] Found GFM on GO={gfm.gameObject.name}, calling LoadStageSelect");
        gfm.LoadStageSelect();
    }
}
