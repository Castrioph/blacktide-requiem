using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public static class SetupCombatScene
{
    public static string Execute()
    {
        // Remove all layout groups from ALL children — we set anchors manually
        RemoveAllLayoutGroups("CombatUI");

        // --- Zone anchors (portrait 1080x1920) ---
        SetAnchorsStretch("CombatUI/InitiativeBar",      0f, 0.93f, 1f, 1f);
        SetAnchorsStretch("CombatUI/RoundInfo",           0f, 0.90f, 1f, 0.93f);
        SetAnchorsStretch("CombatUI/Battlefield",         0f, 0.50f, 1f, 0.90f);
        SetAnchorsStretch("CombatUI/UnitInfo",            0f, 0.44f, 1f, 0.50f);
        SetAnchorsStretch("CombatUI/ActionPanel",         0f, 0.18f, 1f, 0.44f);
        SetAnchorsStretch("CombatUI/BattleLogPanel",      0f, 0f,    1f, 0.18f);
        SetAnchorsStretch("CombatUI/ResultOverlay",       0f, 0f,    1f, 1f);

        // --- Battlefield columns ---
        SetAnchorsStretch("CombatUI/Battlefield/AllyColumn",  0f,    0f, 0.48f, 1f);
        SetAnchorsStretch("CombatUI/Battlefield/EnemyColumn", 0.52f, 0f, 1f,    1f);

        // --- Initiative bar children ---
        SetAnchorsStretch("CombatUI/InitiativeBar/TurnOrderLabel", 0f, 0.5f, 1f, 1f);
        SetAnchorsStretch("CombatUI/InitiativeBar/IconsContainer", 0f, 0f, 1f, 0.5f);

        // --- Round info children ---
        SetAnchorsStretch("CombatUI/RoundInfo/RoundLabel", 0f, 0f, 0.5f, 1f);
        SetAnchorsStretch("CombatUI/RoundInfo/WaveLabel",  0.5f, 0f, 1f, 1f);

        // --- Unit info children ---
        SetAnchorsStretch("CombatUI/UnitInfo/UnitName", 0f,   0f, 0.3f, 1f);
        SetAnchorsStretch("CombatUI/UnitInfo/UnitHP",   0.3f, 0f, 0.5f, 1f);
        SetAnchorsStretch("CombatUI/UnitInfo/UnitMP",   0.5f, 0f, 0.7f, 1f);
        SetAnchorsStretch("CombatUI/UnitInfo/UnitATK",  0.7f, 0f, 1f,   1f);

        // --- Action panel children ---
        SetAnchorsStretch("CombatUI/ActionPanel/MainActions",  0f, 0f, 1f, 1f);
        SetAnchorsStretch("CombatUI/ActionPanel/AbilityMenu",  0f, 0f, 1f, 1f);
        SetAnchorsStretch("CombatUI/ActionPanel/TargetHint",   0f, 0f, 1f, 0.2f);

        // --- Add GridLayoutGroup to MainActions ---
        var mainActions = GameObject.Find("CombatUI/ActionPanel/MainActions");
        if (mainActions != null)
        {
            var grid = mainActions.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = mainActions.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(480, 90);
            grid.spacing = new Vector2(12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(16, 16, 16, 16);
        }

        // --- Add VerticalLayoutGroup to AllyColumn and EnemyColumn ---
        AddVerticalLayout("CombatUI/Battlefield/AllyColumn", 4);
        AddVerticalLayout("CombatUI/Battlefield/EnemyColumn", 4);

        // --- Add HorizontalLayoutGroup to IconsContainer ---
        var icons = GameObject.Find("CombatUI/InitiativeBar/IconsContainer");
        if (icons != null)
        {
            var hlg = icons.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = icons.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
        }

        // --- Battle log ---
        SetAnchorsStretch("CombatUI/BattleLogPanel/BattleLog", 0f, 0f, 1f, 1f);
        AddVerticalLayout("CombatUI/BattleLogPanel/BattleLog/Viewport/Content", 2);

        // --- Result overlay children ---
        SetAnchorsStretch("CombatUI/ResultOverlay/ResultText",    0.1f, 0.45f, 0.9f, 0.65f);
        SetAnchorsStretch("CombatUI/ResultOverlay/ResultDetails", 0.1f, 0.35f, 0.9f, 0.45f);

        // --- Hide panels that start hidden ---
        SetActive("CombatUI/ResultOverlay", false);
        SetActive("CombatUI/ActionPanel/AbilityMenu", false);
        SetActive("CombatUI/ActionPanel/TargetHint", false);

        // Mark dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        return "Scene setup v3: full anchor layout + grid + vertical layouts";
    }

    private static void SetAnchorsStretch(string path, float xMin, float yMin, float xMax, float yMax)
    {
        var go = GameObject.Find(path);
        if (go == null) { Debug.LogWarning($"SetAnchorsStretch: {path} not found"); return; }
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AddVerticalLayout(string path, float spacing)
    {
        var go = GameObject.Find(path);
        if (go == null) return;
        var vlg = go.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
    }

    private static void RemoveAllLayoutGroups(string rootPath)
    {
        var root = GameObject.Find(rootPath);
        if (root == null) return;
        foreach (var lg in root.GetComponentsInChildren<LayoutGroup>(true))
        {
            // Keep GridLayoutGroup on MainActions if re-running
            Object.DestroyImmediate(lg);
        }
        foreach (var csf in root.GetComponentsInChildren<ContentSizeFitter>(true))
        {
            Object.DestroyImmediate(csf);
        }
    }

    private static void SetActive(string path, bool active)
    {
        var go = GameObject.Find(path);
        if (go != null) go.SetActive(active);
    }
}
