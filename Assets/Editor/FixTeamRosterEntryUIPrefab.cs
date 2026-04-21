using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class FixTeamRosterEntryUIPrefab
{
    static readonly Color Gold  = new Color(0.95f, 0.78f, 0.25f, 1f);
    static readonly Color Cream = new Color(0.92f, 0.88f, 0.72f, 1f);

    public static void Execute()
    {
        const string path = "Assets/Prefabs/UI/TeamRosterEntryUI.prefab";
        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            var root = scope.prefabContentsRoot;

            // LayoutElement so VLG respects the 80px height
            var le = root.GetComponent<LayoutElement>() ?? root.AddComponent<LayoutElement>();
            le.preferredHeight = 80f;
            EditorUtility.SetDirty(le);

            // CharName: cream, readable size
            SetText(root, "CharName", Cream, 15, TextAnchor.MiddleLeft);

            // CharElement: gold (was dark brown, unreadable on dark bg)
            SetText(root, "CharElement", Gold, 13, TextAnchor.MiddleLeft);
        }
        Debug.Log("[FixRosterEntry] TeamRosterEntryUI prefab fixed.");
    }

    static void SetText(GameObject root, string child, Color c, int size, TextAnchor align)
    {
        var t = root.transform.Find(child); if (t == null) return;
        var txt = t.GetComponent<Text>(); if (txt == null) return;
        txt.color = c; txt.fontSize = size; txt.alignment = align;
        EditorUtility.SetDirty(t.gameObject);
    }
}
