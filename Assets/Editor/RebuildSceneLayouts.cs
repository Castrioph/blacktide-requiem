using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Rebuilds RectTransform layout for StageSelect and TeamSelect scenes.
/// Fixes anchors and sizes that were corrupted by the scene-creation scripts.
/// </summary>
public class RebuildSceneLayouts
{
    public static void Execute()
    {
        RebuildStageSelect();
        RebuildTeamSelect();
        Debug.Log("[RebuildLayouts] All done.");
    }

    // ── helpers ──────────────────────────────────────────────────────────

    static RectTransform RT(string path)
    {
        var go = GameObject.Find(path);
        if (go == null) { Debug.LogWarning("[Layout] NOT FOUND: " + path); return null; }
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) Debug.LogWarning("[Layout] No RectTransform: " + path);
        return rt;
    }

    // Stretch to fill parent
    static void Stretch(string path)
    {
        var rt = RT(path); if (rt == null) return;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(rt.gameObject);
    }

    // Top bar: full width, fixed height from top
    static void TopBar(string path, float height)
    {
        var rt = RT(path); if (rt == null) return;
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, -height); rt.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(rt.gameObject);
    }

    // Bottom bar: full width, fixed height from bottom
    static void BottomBar(string path, float height)
    {
        var rt = RT(path); if (rt == null) return;
        rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1, 0);
        rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(0, height);
        EditorUtility.SetDirty(rt.gameObject);
    }

    // Fill between two offsets from top/bottom
    static void MiddleFill(string path, float topOffset, float bottomOffset)
    {
        var rt = RT(path); if (rt == null) return;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0, bottomOffset);
        rt.offsetMax = new Vector2(0, -topOffset);
        EditorUtility.SetDirty(rt.gameObject);
    }

    // Fixed size centered
    static void Centered(string path, float w, float h, float offsetX = 0, float offsetY = 0)
    {
        var rt = RT(path); if (rt == null) return;
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(offsetX, offsetY);
        EditorUtility.SetDirty(rt.gameObject);
    }

    // Left-anchored inside parent
    static void LeftInParent(string path, float w, float h, float padX = 10)
    {
        var rt = RT(path); if (rt == null) return;
        rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(padX + w * 0.5f, 0);
        EditorUtility.SetDirty(rt.gameObject);
    }

    // ── StageSelect ───────────────────────────────────────────────────────

    static void RebuildStageSelect()
    {
        var scene = EditorSceneManager.OpenScene("Assets/StageSelect.unity", OpenSceneMode.Single);

        Stretch("Canvas/Background");
        TopBar("Canvas/Header", 80);
        Stretch("Canvas/Header/TitleText");
        LeftInParent("Canvas/Header/BtnBack", 100, 50);
        Stretch("Canvas/Header/BtnBack/Text");
        MiddleFill("Canvas/StageScrollView", 80, 80);
        Stretch("Canvas/StageScrollView/Viewport");
        // Content uses VerticalLayoutGroup — just set anchors to top-stretch
        var contentRT = RT("Canvas/StageScrollView/Viewport/Content");
        if (contentRT != null)
        {
            contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
            contentRT.offsetMin = Vector2.zero; contentRT.offsetMax = Vector2.zero;
            contentRT.anchoredPosition = Vector2.zero;
            EditorUtility.SetDirty(contentRT.gameObject);
        }
        BottomBar("Canvas/Footer", 80);
        Centered("Canvas/Footer/BtnLaunch", 220, 55);
        Stretch("Canvas/Footer/BtnLaunch/Text");
        Centered("Canvas/EmptyStateText", 500, 60);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[RebuildLayouts] StageSelect saved.");
    }

    // ── TeamSelect ────────────────────────────────────────────────────────

    static void RebuildTeamSelect()
    {
        var scene = EditorSceneManager.OpenScene("Assets/TeamSelect.unity", OpenSceneMode.Single);

        Stretch("Canvas/Background");
        TopBar("Canvas/Header", 80);
        Stretch("Canvas/Header/TitleText");
        LeftInParent("Canvas/Header/BtnBack", 100, 50);
        Stretch("Canvas/Header/BtnBack/Text");

        // Slots panel: below header, 120px tall
        var slotsRT = RT("Canvas/SlotsPanel"); if (slotsRT != null)
        {
            slotsRT.anchorMin = new Vector2(0, 1); slotsRT.anchorMax = new Vector2(1, 1);
            slotsRT.offsetMin = new Vector2(0, -200); slotsRT.offsetMax = new Vector2(0, -80);
            EditorUtility.SetDirty(slotsRT.gameObject);
        }

        // Each slot: 1/3 of parent width
        string[] slots = { "Canvas/SlotsPanel/Slot0", "Canvas/SlotsPanel/Slot1", "Canvas/SlotsPanel/Slot2" };
        float[] anchorsX = { 0f, 1f/3f, 2f/3f };
        for (int i = 0; i < 3; i++)
        {
            var rt = RT(slots[i]); if (rt == null) continue;
            rt.anchorMin = new Vector2(anchorsX[i], 0); rt.anchorMax = new Vector2(anchorsX[i] + 1f/3f, 1);
            rt.offsetMin = new Vector2(4, 4); rt.offsetMax = new Vector2(-4, -4);
            EditorUtility.SetDirty(rt.gameObject);

            Centered(slots[i] + "/SlotNameText", 0, 0);
            var nameRT = RT(slots[i] + "/SlotNameText"); if (nameRT != null)
            {
                nameRT.anchorMin = new Vector2(0,0.5f); nameRT.anchorMax = new Vector2(1,1);
                nameRT.offsetMin = new Vector2(4,0); nameRT.offsetMax = new Vector2(-4,0);
                EditorUtility.SetDirty(nameRT.gameObject);
            }
            Centered(slots[i] + "/BtnClear", 80, 30, 0, -16);
            Stretch(slots[i] + "/BtnClear/Text");
        }

        MiddleFill("Canvas/RosterScrollView", 200, 80);
        Stretch("Canvas/RosterScrollView/Viewport");
        var rosterContent = RT("Canvas/RosterScrollView/Viewport/Content");
        if (rosterContent != null)
        {
            rosterContent.anchorMin = new Vector2(0, 1); rosterContent.anchorMax = new Vector2(1, 1);
            rosterContent.offsetMin = Vector2.zero; rosterContent.offsetMax = Vector2.zero;
            rosterContent.anchoredPosition = Vector2.zero;
            EditorUtility.SetDirty(rosterContent.gameObject);
        }

        BottomBar("Canvas/Footer", 80);
        Centered("Canvas/Footer/BtnConfirm", 220, 55);
        Stretch("Canvas/Footer/BtnConfirm/Text");

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[RebuildLayouts] TeamSelect saved.");
    }
}
