using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Runtime.Combat;
using BlacktideRequiem.UI.Combat.Naval;

/// <summary>
/// Builds Assets/Scenes/NavalCombat.unity for S4-06: Canvas (1080×1920
/// portrait, Scale With Screen Size), EventSystem, CombatRunner, NavalCombatHUD
/// and NavalCombatBootstrap wired to the S4-05 ship assets.
/// Run via: BuildNavalCombatScene.Execute (Coplay execute_script or
/// -executeMethod BuildNavalCombatScene.Execute).
/// </summary>
public static class BuildNavalCombatScene
{
    private const string ScenePath = "Assets/Scenes/NavalCombat.unity";

    public static string Execute()
    {
        ImportNavalSprites();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Camera ---
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.06f, 0.14f);
        cam.orthographic = true;

        // --- EventSystem (proyecto usa Input System package, no legacy) ---
        new GameObject("EventSystem", typeof(EventSystem),
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        // --- Canvas ---
        var canvasGo = new GameObject("NavalCombatCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 1f; // match height (S3-11 P0)

        // --- HUD ---
        var hudGo = new GameObject("NavalHUD", typeof(RectTransform));
        hudGo.transform.SetParent(canvasGo.transform, false);
        var hud = hudGo.AddComponent<NavalCombatHUD>();

        // --- Runner + bootstrap ---
        var systemsGo = new GameObject("CombatSystems");
        var runner = systemsGo.AddComponent<CombatRunner>();
        var bootstrap = systemsGo.AddComponent<NavalCombatBootstrap>();

        WireBootstrap(bootstrap, runner, hud);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        return $"Escena naval creada en {ScenePath}";
    }

    private static void WireBootstrap(NavalCombatBootstrap bootstrap,
        CombatRunner runner, NavalCombatHUD hud)
    {
        var so = new SerializedObject(bootstrap);

        so.FindProperty("_runner").objectReferenceValue = runner;
        so.FindProperty("_hud").objectReferenceValue = hud;
        so.FindProperty("_allyShip").objectReferenceValue =
            Load<ShipData>("Assets/Data/Ships/ship_marea_espectral.asset");

        // Allied crew: demo protagonists
        SetList(so, "_allyCrew", new[]
        {
            "Assets/Data/Characters/elena_tempestad.asset",
            "Assets/Data/Characters/kael_polvora.asset",
            "Assets/Data/Characters/mirra_mareamadre.asset"
        });

        // Enemy crew pool: generic pirate units
        SetList(so, "_enemyCrewPool", new[]
        {
            "Assets/Data/Characters/pirate_grunt_1.asset",
            "Assets/Data/Characters/pirate_brute_1.asset",
            "Assets/Data/Characters/corsair_1.asset",
            "Assets/Data/Characters/hexer_1.asset"
        });

        // Waves: 1) balandra — 2) bergantín + serpiente — 3) galeón (jefe)
        var waves = so.FindProperty("_waves");
        waves.arraySize = 3;
        SetWave(waves.GetArrayElementAtIndex(0),
            "Assets/Data/Ships/ship_balandra_corsaria.asset");
        SetWave(waves.GetArrayElementAtIndex(1),
            "Assets/Data/Ships/ship_bergantin_maldito.asset",
            "Assets/Data/Ships/creature_serpiente_abisal.asset");
        SetWave(waves.GetArrayElementAtIndex(2),
            "Assets/Data/Ships/ship_galeon_del_requiem.asset");

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetWave(SerializedProperty waveEntry, params string[] shipPaths)
    {
        var ships = waveEntry.FindPropertyRelative("Ships");
        ships.arraySize = shipPaths.Length;
        for (int i = 0; i < shipPaths.Length; i++)
            ships.GetArrayElementAtIndex(i).objectReferenceValue = Load<ShipData>(shipPaths[i]);
    }

    private static void SetList(SerializedObject so, string property, string[] paths)
    {
        var prop = so.FindProperty(property);
        prop.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = Load<CharacterData>(paths[i]);
    }

    private static T Load<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            Debug.LogWarning($"[BuildNavalCombatScene] Asset no encontrado: {path}");
        return asset;
    }

    /// <summary>Imports the generated UI PNGs as sprites (Resources folder).</summary>
    public static void ImportNavalSprites()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D",
            new[] { "Assets/Resources/Sprites/UI/Naval" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) continue;
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.alphaIsTransparency)
                continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
        Debug.Log($"[BuildNavalCombatScene] {guids.Length} sprites navales importados");
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
            if (s.path == ScenePath)
                return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
