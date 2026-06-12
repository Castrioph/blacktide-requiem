using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Fija la Game view a la resolución del proyecto (portrait 1080×1920).
/// La demo usa CanvasScaler 1080×1920 match-height: en Free Aspect apaisado
/// la UI se deforma en barras gigantes. API interna GameViewSizes vía reflection.
/// </summary>
public static class SetPortraitGameView
{
    private const string SizeLabel = "Blacktide 1080x1920";

    public static string Execute()
    {
        var sizesType = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
        var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instance = singleType.GetProperty("instance",
            BindingFlags.Public | BindingFlags.Static).GetValue(null);
        var group = sizesType.GetMethod("GetGroup").Invoke(instance,
            new object[] { (int)GameViewSizeGroupType.Standalone });

        // ¿Ya existe el tamaño custom?
        int index = FindSizeIndex(group);
        if (index < 0)
        {
            var sizeType = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            var enumType = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");
            var size = Activator.CreateInstance(sizeType,
                Enum.Parse(enumType, "FixedResolution"), 1080, 1920, SizeLabel);
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            index = FindSizeIndex(group);
        }
        if (index < 0) return "No se pudo registrar el tamaño custom";

        var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        var gameView = EditorWindow.GetWindow(gameViewType);
        gameViewType.GetMethod("SizeSelectionCallback",
            BindingFlags.Public | BindingFlags.Instance)
            .Invoke(gameView, new object[] { index, null });
        gameView.Repaint();

        return $"Game view fijada a {SizeLabel} (índice {index})";
    }

    private static int FindSizeIndex(object group)
    {
        int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        for (int i = 0; i < total; i++)
        {
            var size = group.GetType().GetMethod("GetGameViewSize")
                .Invoke(group, new object[] { i });
            var text = (string)size.GetType().GetProperty("baseText").GetValue(size);
            if (text == SizeLabel) return i;
        }
        return -1;
    }
}
