using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// El proyecto usa exclusivamente el Input System package: cualquier
/// StandaloneInputModule (legacy) lanza InvalidOperationException cada frame
/// y deja la UI sin input real. Sustituye el módulo en todas las escenas
/// del build por InputSystemUIInputModule.
/// </summary>
public static class FixEventSystemModules
{
    public static string Execute()
    {
        var report = new StringBuilder();
        var setup = EditorSceneManager.GetSceneManagerSetup();

        foreach (var buildScene in EditorBuildSettings.scenes)
        {
            var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            bool dirty = false;

            foreach (var legacy in Object.FindObjectsByType<StandaloneInputModule>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var go = legacy.gameObject;
                Object.DestroyImmediate(legacy);
                if (go.GetComponent<InputSystemUIInputModule>() == null)
                    go.AddComponent<InputSystemUIInputModule>();
                dirty = true;
                report.AppendLine($"{scene.name}: StandaloneInputModule → InputSystemUIInputModule");
            }

            if (dirty)
                EditorSceneManager.SaveScene(scene);
        }

        if (setup.Length > 0)
            EditorSceneManager.RestoreSceneManagerSetup(setup);

        return report.Length > 0 ? report.ToString() : "Ninguna escena necesitaba fix";
    }
}
