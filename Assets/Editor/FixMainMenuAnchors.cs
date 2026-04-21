using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixMainMenuAnchors
{
    public static void Execute()
    {
        Fix("Canvas/MenuPanel/TitleText",
            anchorMin: new Vector2(0.5f, 0.5f),
            anchorMax: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0f, 100f),
            size: new Vector2(600f, 80f));

        Fix("Canvas/MenuPanel/BtnStartBattle",
            anchorMin: new Vector2(0.5f, 0.5f),
            anchorMax: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0f, -40f),
            size: new Vector2(250f, 60f));

        // Fix button text anchor too
        Fix("Canvas/MenuPanel/BtnStartBattle/Text",
            anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(1f, 1f),
            anchoredPos: new Vector2(0f, 0f),
            size: new Vector2(0f, 0f));  // stretch — size ignored for stretch anchors

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[FixMainMenuAnchors] Done. Anchors corrected and scene saved.");
    }

    static void Fix(string path, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go = GameObject.Find(path);
        if (go == null) { Debug.LogWarning($"[Fix] NOT FOUND: {path}"); return; }
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) { Debug.LogWarning($"[Fix] No RectTransform: {path}"); return; }

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;

        // Only set size if NOT a stretch element (anchorMin != anchorMax)
        if (anchorMin != anchorMax)
            rt.sizeDelta = size;

        EditorUtility.SetDirty(go);
        Debug.Log($"[Fix] OK: {path}");
    }
}
