using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Opens the S4-06 naval combat test scene (edit mode). The battle starts
/// automatically on Play via NavalCombatBootstrap.
/// Flow: stop_game → SimulateNavalCombat.Execute → play_game → capture_ui_canvas.
/// </summary>
public static class SimulateNavalCombat
{
    public static string Execute()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/NavalCombat.unity");
        return "Escena NavalCombat abierta. Entra en Play para iniciar la batalla.";
    }
}
