using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixScrollContentPivot
{
    public static void Execute()
    {
        FixScene("Assets/StageSelect.unity", "Canvas/StageScrollView/Viewport/Content");
        FixScene("Assets/TeamSelect.unity",  "Canvas/RosterScrollView/Viewport/Content");

        // BtnSelect default text on StageEntryUI prefab
        FixPrefabBtnText();

        Debug.Log("[FixPivot] Done.");
    }

    static void FixScene(string scenePath, string contentPath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var go = GameObject.Find(contentPath);
        if (go == null) { Debug.LogWarning("[FixPivot] NOT FOUND: " + contentPath); return; }

        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            EditorUtility.SetDirty(rt.gameObject);
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixPivot] Saved: " + scenePath);
    }

    static void FixPrefabBtnText()
    {
        const string path = "Assets/Prefabs/UI/StageEntryUI.prefab";
        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            var root = scope.prefabContentsRoot;
            var textGO = root.transform.Find("BtnSelect/Text");
            if (textGO == null) { Debug.LogWarning("[FixPivot] BtnSelect/Text not found"); return; }
            var txt = textGO.GetComponent<Text>();
            if (txt == null) return;
            txt.text = "→";
            txt.fontSize = 20;
            EditorUtility.SetDirty(txt.gameObject);
        }
        Debug.Log("[FixPivot] BtnSelect text set to →");
    }
}
