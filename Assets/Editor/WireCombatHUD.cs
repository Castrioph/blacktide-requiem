using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Reflection;

public static class WireCombatHUD
{
    public static string Execute()
    {
        var hud = GameObject.Find("CombatUI")?.GetComponent<BlacktideRequiem.UI.Combat.CombatHUDCanvas>();
        if (hud == null) return "ERROR: CombatHUDCanvas not found";

        var so = new SerializedObject(hud);

        // Initiative Bar
        SetRef(so, "_iconsContainer", "CombatUI/InitiativeBar/IconsContainer");
        // Round Info
        SetTextRef(so, "_roundLabel", "CombatUI/RoundInfo/RoundLabel");
        SetTextRef(so, "_waveLabel", "CombatUI/RoundInfo/WaveLabel");
        // Battlefield
        SetRef(so, "_allyColumn", "CombatUI/Battlefield/AllyColumn");
        SetRef(so, "_enemyColumn", "CombatUI/Battlefield/EnemyColumn");
        // Unit Info
        SetTextRef(so, "_unitName", "CombatUI/UnitInfo/UnitName");
        SetTextRef(so, "_unitHpText", "CombatUI/UnitInfo/UnitHP");
        SetTextRef(so, "_unitMpText", "CombatUI/UnitInfo/UnitMP");
        SetTextRef(so, "_unitAtkText", "CombatUI/UnitInfo/UnitATK");
        // Action Panel
        SetGameObjectRef(so, "_mainActions", "CombatUI/ActionPanel/MainActions");
        SetButtonRef(so, "_btnAttack", "CombatUI/ActionPanel/MainActions/BtnAttack");
        SetButtonRef(so, "_btnAbilities", "CombatUI/ActionPanel/MainActions/BtnAbilities");
        SetButtonRef(so, "_btnGuard", "CombatUI/ActionPanel/MainActions/BtnGuard");
        SetButtonRef(so, "_btnPass", "CombatUI/ActionPanel/MainActions/BtnPass");
        // Ability Menu
        SetGameObjectRef(so, "_abilityMenu", "CombatUI/ActionPanel/AbilityMenu");
        // AbilityList content — inside ScrollRect > Viewport > Content
        var abilityListContent = GameObject.Find("CombatUI/ActionPanel/AbilityMenu/AbilityList/Viewport/Content");
        if (abilityListContent != null)
            so.FindProperty("_abilityListContent").objectReferenceValue = abilityListContent.transform;
        SetButtonRef(so, "_btnBack", "CombatUI/ActionPanel/AbilityMenu/BtnBack");
        // Target Hint
        SetGameObjectRef(so, "_targetHintObj", "CombatUI/ActionPanel/TargetHint");
        var targetHintGo = GameObject.Find("CombatUI/ActionPanel/TargetHint");
        if (targetHintGo != null)
            so.FindProperty("_targetHintText").objectReferenceValue = targetHintGo.GetComponent<Text>();
        // Battle Log
        var logContent = GameObject.Find("CombatUI/BattleLogPanel/BattleLog/Viewport/Content");
        if (logContent != null)
            so.FindProperty("_battleLogContent").objectReferenceValue = logContent.transform;
        var logScroll = GameObject.Find("CombatUI/BattleLogPanel/BattleLog");
        if (logScroll != null)
            so.FindProperty("_battleLogScroll").objectReferenceValue = logScroll.GetComponent<ScrollRect>();
        // Result Overlay
        SetGameObjectRef(so, "_resultOverlay", "CombatUI/ResultOverlay");
        // ResultOverlay children need to be found even if inactive
        var overlayGo = GameObject.Find("CombatUI")?.transform.Find("ResultOverlay");
        if (overlayGo != null)
        {
            var resultText = overlayGo.Find("ResultText");
            var resultDetails = overlayGo.Find("ResultDetails");
            if (resultText != null)
                so.FindProperty("_resultText").objectReferenceValue = resultText.GetComponent<Text>();
            if (resultDetails != null)
                so.FindProperty("_resultDetails").objectReferenceValue = resultDetails.GetComponent<Text>();
        }

        // DemoBattleSetup._hud
        var setup = GameObject.Find("CombatSystem")?.GetComponent<BlacktideRequiem.Runtime.Combat.DemoBattleSetup>();
        if (setup != null)
        {
            var setupSO = new SerializedObject(setup);
            setupSO.FindProperty("_hud").objectReferenceValue = hud;
            setupSO.ApplyModifiedProperties();
        }

        so.ApplyModifiedProperties();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        return "All CombatHUDCanvas references wired successfully";
    }

    private static void SetRef(SerializedObject so, string propName, string path)
    {
        var go = GameObject.Find(path);
        if (go != null)
            so.FindProperty(propName).objectReferenceValue = go.transform;
    }

    private static void SetTextRef(SerializedObject so, string propName, string path)
    {
        var go = GameObject.Find(path);
        if (go != null)
            so.FindProperty(propName).objectReferenceValue = go.GetComponent<Text>();
    }

    private static void SetButtonRef(SerializedObject so, string propName, string path)
    {
        var go = GameObject.Find(path);
        if (go != null)
            so.FindProperty(propName).objectReferenceValue = go.GetComponent<Button>();
    }

    private static void SetGameObjectRef(SerializedObject so, string propName, string path)
    {
        var go = GameObject.Find(path);
        if (go != null)
            so.FindProperty(propName).objectReferenceValue = go;
    }
}
