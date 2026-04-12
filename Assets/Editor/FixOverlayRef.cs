using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class FixOverlayRef
{
    public static string Execute()
    {
        // First re-enable overlay so we can find it, then assign, then disable
        var combatUI = GameObject.Find("CombatUI");
        if (combatUI == null) return "ERROR: CombatUI not found";

        var overlayTransform = combatUI.transform.Find("ResultOverlay");
        if (overlayTransform == null) return "ERROR: ResultOverlay not found";

        var overlayGo = overlayTransform.gameObject;

        var hud = combatUI.GetComponent<BlacktideRequiem.UI.Combat.CombatHUDCanvas>();
        if (hud == null) return "ERROR: CombatHUDCanvas not found";

        var so = new SerializedObject(hud);

        // Assign overlay GO
        so.FindProperty("_resultOverlay").objectReferenceValue = overlayGo;

        // Assign child texts (they're inactive but accessible via transform.Find)
        var resultText = overlayTransform.Find("ResultText");
        var resultDetails = overlayTransform.Find("ResultDetails");

        if (resultText != null)
            so.FindProperty("_resultText").objectReferenceValue = resultText.GetComponent<Text>();
        else
            return "ERROR: ResultText not found under overlay";

        if (resultDetails != null)
            so.FindProperty("_resultDetails").objectReferenceValue = resultDetails.GetComponent<Text>();
        else
            return "ERROR: ResultDetails not found under overlay";

        so.ApplyModifiedProperties();

        // Verify
        var check = so.FindProperty("_resultOverlay").objectReferenceValue;
        var check2 = so.FindProperty("_resultText").objectReferenceValue;
        var check3 = so.FindProperty("_resultDetails").objectReferenceValue;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        return $"Fixed: overlay={check != null}, text={check2 != null}, details={check3 != null}";
    }
}
