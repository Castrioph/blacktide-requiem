using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Play-mode interaction helpers for verifying the S4-06 naval HUD via Coplay.
/// Each method simulates one player input step; capture_ui_canvas between calls.
/// </summary>
public static class SimNavalActions
{
    public static string ClickCannon() => ClickButton("BtnCannon");
    public static string ClickAbility() => ClickButton("BtnAbility");
    public static string ClickManeuver() => ClickButton("BtnManeuver");
    public static string ClickBoarding() => ClickButton("BtnBoarding");
    public static string ClickRepair() => ClickButton("BtnRepair");
    public static string ClickPass() => ClickButton("BtnPass");

    private static string ClickButton(string name)
    {
        var go = GameObject.Find($"NavalCombatCanvas/NavalHUD/ActionPanel/{name}");
        if (go == null) return $"{name} no encontrado (¿ActionPanel inactivo?)";
        var btn = go.GetComponent<Button>();
        if (!btn.interactable) return $"{name} está disabled";
        btn.onClick.Invoke();
        return $"{name} pulsado";
    }

    /// <summary>Clicks the sprite of the first living enemy ship view.</summary>
    public static string ClickFirstEnemy()
    {
        var sprite = FindFirstEnemySprite();
        if (sprite == null) return "Sprite enemigo no encontrado";
        SimulateClick(sprite);
        return $"Click en {sprite.transform.parent.name}";
    }

    /// <summary>Clicks the first living crew chip deployed on an enemy ship.</summary>
    public static string ClickFirstCrewChip()
    {
        var enemyColumn = GameObject.Find("NavalCombatCanvas/NavalHUD/Battlefield/EnemyColumn");
        if (enemyColumn == null) return "EnemyColumn no encontrada";

        foreach (var trigger in enemyColumn.GetComponentsInChildren<EventTrigger>())
        {
            if (!trigger.name.StartsWith("Chip_")) continue;
            SimulateClick(trigger.gameObject);
            return $"Click en chip {trigger.name}";
        }
        return "Sin chips desplegados (¿modo Abordaje activo?)";
    }

    private static GameObject FindFirstEnemySprite()
    {
        var enemyColumn = GameObject.Find("NavalCombatCanvas/NavalHUD/Battlefield/EnemyColumn");
        if (enemyColumn == null) return null;
        foreach (Transform child in enemyColumn.transform)
        {
            var sprite = child.Find("Sprite");
            if (sprite != null) return sprite.gameObject;
        }
        return null;
    }

    private static void SimulateClick(GameObject target)
    {
        var pointer = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
    }
}
