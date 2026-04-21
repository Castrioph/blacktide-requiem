using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class FixStageEntryUIPrefab
{
    static readonly Color PanelDark = new Color(0.12f, 0.09f, 0.18f, 1f);
    static readonly Color BtnActive = new Color(0.60f, 0.42f, 0.10f, 1f);
    static readonly Color Cream     = new Color(0.92f, 0.88f, 0.72f, 1f);
    static readonly Color Gold      = new Color(0.95f, 0.78f, 0.25f, 1f);

    public static void Execute()
    {
        const string path = "Assets/Prefabs/UI/StageEntryUI.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogError("[FixEntry] Prefab not found: " + path); return; }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            var root = scope.prefabContentsRoot;

            // Root: dark panel background
            var rootImg = root.GetComponent<Image>();
            if (rootImg != null) { rootImg.color = PanelDark; EditorUtility.SetDirty(rootImg); }

            // Root: fixed height for VLG
            var le = root.GetComponent<LayoutElement>() ?? root.AddComponent<LayoutElement>();
            le.preferredHeight = 90f;
            EditorUtility.SetDirty(le);

            // Border: stretch to fill (color managed by StageEntryUI script)
            FixRT(root, "Border",
                ancMin: Vector2.zero, ancMax: Vector2.one,
                offMin: Vector2.zero, offMax: Vector2.zero);

            // StageName: left side, top half
            FixRT(root, "StageName",
                ancMin: new Vector2(0f, 0.5f), ancMax: new Vector2(0.75f, 1f),
                offMin: new Vector2(12f, 4f), offMax: new Vector2(-4f, -4f));
            SetTextColor(root, "StageName", Cream);
            SetTextAlign(root, "StageName", TextAnchor.MiddleLeft, 16);

            // StageDifficulty: left side, bottom half
            FixRT(root, "StageDifficulty",
                ancMin: new Vector2(0f, 0f), ancMax: new Vector2(0.75f, 0.5f),
                offMin: new Vector2(12f, 4f), offMax: new Vector2(-4f, -4f));
            SetTextColor(root, "StageDifficulty", Gold);
            SetTextAlign(root, "StageDifficulty", TextAnchor.MiddleLeft, 14);

            // BtnSelect: right side, centered
            FixRTCentered(root, "BtnSelect", 90f, 50f, 0f);
            var btnRT = root.transform.Find("BtnSelect")?.GetComponent<RectTransform>();
            if (btnRT != null)
            {
                btnRT.anchorMin = new Vector2(1f, 0.5f);
                btnRT.anchorMax = new Vector2(1f, 0.5f);
                btnRT.sizeDelta = new Vector2(90f, 50f);
                btnRT.anchoredPosition = new Vector2(-55f, 0f);
                EditorUtility.SetDirty(btnRT.gameObject);
            }
            var btnImg = root.transform.Find("BtnSelect")?.GetComponent<Image>();
            if (btnImg != null) { btnImg.color = BtnActive; EditorUtility.SetDirty(btnImg.gameObject); }

            // BtnSelect/Text if it exists
            var btnTextGO = root.transform.Find("BtnSelect/Text");
            if (btnTextGO != null)
            {
                var txt = btnTextGO.GetComponent<Text>();
                if (txt != null) { txt.color = Cream; txt.fontSize = 13; EditorUtility.SetDirty(txt.gameObject); }
                var rt = btnTextGO.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                    EditorUtility.SetDirty(rt.gameObject);
                }
            }
        }

        Debug.Log("[FixEntry] StageEntryUI prefab fixed.");
    }

    static void FixRT(GameObject root, string name, Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax)
    {
        var t = root.transform.Find(name);
        if (t == null) { Debug.LogWarning("[FixEntry] NOT FOUND: " + name); return; }
        var rt = t.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
        EditorUtility.SetDirty(rt.gameObject);
    }

    static void FixRTCentered(GameObject root, string name, float w, float h, float offsetX)
    {
        // placeholder — overridden for BtnSelect above
    }

    static void SetTextColor(GameObject root, string name, Color c)
    {
        var t = root.transform.Find(name); if (t == null) return;
        var txt = t.GetComponent<Text>(); if (txt == null) return;
        txt.color = c; EditorUtility.SetDirty(t.gameObject);
    }

    static void SetTextAlign(GameObject root, string name, TextAnchor align, int size)
    {
        var t = root.transform.Find(name); if (t == null) return;
        var txt = t.GetComponent<Text>(); if (txt == null) return;
        txt.alignment = align; txt.fontSize = size; EditorUtility.SetDirty(t.gameObject);
    }
}
