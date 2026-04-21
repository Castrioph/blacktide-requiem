using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixScrollLayouts
{
    public static void Execute()
    {
        FixScene("Assets/StageSelect.unity", "Canvas/StageScrollView/Viewport/Content");
        FixScene("Assets/TeamSelect.unity",  "Canvas/RosterScrollView/Viewport/Content");
        Debug.Log("[FixScrollLayouts] Done.");
    }

    static void FixScene(string scenePath, string contentPath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var contentGO = GameObject.Find(contentPath);
        if (contentGO == null) { Debug.LogError("[FixScrollLayouts] NOT FOUND: " + contentPath); return; }

        // VLG: let it drive child sizes
        var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing                = 4f;
            vlg.padding                = new RectOffset(8, 8, 4, 4);
            EditorUtility.SetDirty(vlg);
        }

        // ContentSizeFitter so Content grows with entries
        var csf = contentGO.GetComponent<ContentSizeFitter>()
               ?? contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        EditorUtility.SetDirty(csf);

        // Viewport Image: transparent (mask still works, but won't overdraw)
        var viewportGO = contentGO.transform.parent?.gameObject;
        if (viewportGO != null)
        {
            var img = viewportGO.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(1f, 1f, 1f, 0.004f);
                EditorUtility.SetDirty(img);
            }
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixScrollLayouts] Saved: " + scenePath);
    }
}
