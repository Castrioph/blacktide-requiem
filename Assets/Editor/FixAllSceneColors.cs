using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Sets functional (visible) colors for StageSelect and TeamSelect.
/// Dark background, gold/cream text — same palette as MainMenu.
/// </summary>
public class FixAllSceneColors
{
    static readonly Color BgDark    = new Color(0.08f, 0.06f, 0.14f, 1f);
    static readonly Color PanelDark = new Color(0.12f, 0.09f, 0.18f, 1f);
    static readonly Color Gold      = new Color(0.95f, 0.78f, 0.25f, 1f);
    static readonly Color Cream     = new Color(0.92f, 0.88f, 0.72f, 1f);
    static readonly Color BtnActive = new Color(0.60f, 0.42f, 0.10f, 1f);
    static readonly Color BtnOff    = new Color(0.24f, 0.19f, 0.13f, 1f);

    public static void Execute()
    {
        FixScene("Assets/StageSelect.unity");
        FixScene("Assets/TeamSelect.unity");
        Debug.Log("[FixColors] Done.");
    }

    static void FixScene(string path)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        SetImageColor("Canvas/Background", BgDark);
        SetImageColor("Canvas/Header", PanelDark);
        SetImageColor("Canvas/Footer", PanelDark);
        SetTextColor("Canvas/Header/TitleText", Gold);
        SetImageColor("Canvas/Header/BtnBack", BtnActive);
        SetTextColor("Canvas/Header/BtnBack/Text", Cream);
        SetImageColor("Canvas/Footer/BtnLaunch", BtnOff);    // disabled by default
        SetTextColor("Canvas/Footer/BtnLaunch/Text", Cream);
        SetImageColor("Canvas/Footer/BtnConfirm", BtnOff);   // TeamSelect
        SetTextColor("Canvas/Footer/BtnConfirm/Text", Cream);
        SetTextColor("Canvas/EmptyStateText", Cream);

        // TeamSelect slots
        for (int i = 0; i < 3; i++)
        {
            string slot = $"Canvas/SlotsPanel/Slot{i}";
            SetImageColor(slot, PanelDark);
            SetTextColor(slot + "/SlotNameText", Cream);
            SetImageColor(slot + "/BtnClear", BtnActive);
            SetTextColor(slot + "/BtnClear/Text", Cream);
        }
        SetImageColor("Canvas/SlotsPanel", new Color(0.06f, 0.04f, 0.10f, 1f));

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixColors] Saved: " + path);
    }

    static void SetImageColor(string path, Color c)
    {
        var go = GameObject.Find(path); if (go == null) return;
        var img = go.GetComponent<Image>(); if (img == null) return;
        img.color = c; EditorUtility.SetDirty(go);
    }

    static void SetTextColor(string path, Color c)
    {
        var go = GameObject.Find(path); if (go == null) return;
        var txt = go.GetComponent<Text>(); if (txt == null) return;
        txt.color = c; EditorUtility.SetDirty(go);
    }
}
