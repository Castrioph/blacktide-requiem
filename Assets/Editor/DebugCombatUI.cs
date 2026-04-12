using UnityEngine;
using UnityEngine.UI;

public static class DebugCombatUI
{
    public static string Execute()
    {
        var result = "";

        // Check Canvas
        var canvas = GameObject.Find("CombatUI")?.GetComponent<Canvas>();
        result += $"Canvas: {(canvas != null ? canvas.renderMode.ToString() : "NULL")}\n";

        var scaler = GameObject.Find("CombatUI")?.GetComponent<CanvasScaler>();
        if (scaler != null)
            result += $"Scaler: mode={scaler.uiScaleMode}, ref={scaler.referenceResolution}, match={scaler.matchWidthOrHeight}\n";

        // Check root layout groups
        var rootVLG = GameObject.Find("CombatUI")?.GetComponent<VerticalLayoutGroup>();
        result += $"Root VLG: {(rootVLG != null ? "EXISTS" : "removed")}\n";

        // Check MainActions grid
        var mainActions = GameObject.Find("CombatUI/ActionPanel/MainActions");
        if (mainActions != null)
        {
            var grid = mainActions.GetComponent<GridLayoutGroup>();
            if (grid != null)
                result += $"Grid: cellSize={grid.cellSize}, cols={grid.constraintCount}, spacing={grid.spacing}\n";
            else
                result += "Grid: NULL\n";

            var rt = mainActions.GetComponent<RectTransform>();
            result += $"MainActions rect: anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}, sizeDelta={rt.sizeDelta}, rect={rt.rect}\n";
        }

        // Check button count
        var btnAttack = GameObject.Find("CombatUI/ActionPanel/MainActions/BtnAttack");
        var btnAbilities = GameObject.Find("CombatUI/ActionPanel/MainActions/BtnAbilities");
        var btnGuard = GameObject.Find("CombatUI/ActionPanel/MainActions/BtnGuard");
        var btnPass = GameObject.Find("CombatUI/ActionPanel/MainActions/BtnPass");
        result += $"Buttons: Atk={btnAttack != null} Abi={btnAbilities != null} Grd={btnGuard != null} Pas={btnPass != null}\n";

        // Check ActionPanel rect
        var actionPanel = GameObject.Find("CombatUI/ActionPanel");
        if (actionPanel != null)
        {
            var rt = actionPanel.GetComponent<RectTransform>();
            result += $"ActionPanel rect: anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}, sizeDelta={rt.sizeDelta}, rect={rt.rect}\n";
        }

        return result;
    }
}
