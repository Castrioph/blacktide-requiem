using UnityEngine;
using BlacktideRequiem.Runtime.Flow;

public class SimulateReturnMenu
{
    public static void Execute()
    {
        var gfm = Object.FindFirstObjectByType<GameFlowManager>();
        if (gfm == null) { Debug.LogError("[Sim] GFM not found"); return; }
        Debug.Log("[Sim] Loading MainMenu from Results");
        gfm.LoadMainMenu();
    }
}
