using UnityEngine;
using UnityEngine.UI;

public class SimulateStageSelect
{
    public static void Execute()
    {
        // Click the first stage entry's BtnSelect
        var entries = Object.FindObjectsByType<BlacktideRequiem.UI.StageSelect.StageEntryUI>(FindObjectsSortMode.None);
        if (entries.Length == 0) { Debug.LogError("[Sim] No StageEntryUI found"); return; }

        var btn = entries[0].GetComponentInChildren<Button>();
        if (btn == null) { Debug.LogError("[Sim] No Button on first entry"); return; }

        btn.onClick.Invoke();
        Debug.Log($"[Sim] Clicked entry: {entries[0].BoundStage?.DisplayName}");
    }
}
