using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Economy;
using BlacktideRequiem.Core.Stage;
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
        CreateNavalStageAssets();

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
        so.FindProperty("_demoStage").objectReferenceValue =
            Load<NavalStageData>(NavalStagePath);
        so.FindProperty("_wallet").objectReferenceValue =
            Load<CurrencyWallet>(WalletPath);

        // Fallback crew (escena abierta sin flujo): protagonistas demo
        SetList(so, "_demoCrew", new[]
        {
            "Assets/Data/Characters/elena_tempestad.asset",
            "Assets/Data/Characters/kael_polvora.asset",
            "Assets/Data/Characters/mirra_mareamadre.asset"
        });

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetList(SerializedObject so, string property, string[] paths)
    {
        var prop = so.FindProperty(property);
        prop.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = Load<CharacterData>(paths[i]);
    }

    // ====================================================================
    // ASSETS S4-07: stage naval + reward + wallet + registro
    // ====================================================================

    private const string NavalStagePath = "Assets/Data/Stages/stage_004_mar_de_los_lamentos.asset";
    private const string RewardPath = "Assets/Data/Rewards/reward_stage_004.asset";
    private const string WalletPath = "Assets/Data/Economy/player_wallet.asset";
    private const string RegistryPath = "Assets/Data/Stages/StageRegistry.asset";

    public static void CreateNavalStageAssets()
    {
        // Wallet compartida (runtime; sin save hasta S5)
        if (AssetDatabase.LoadAssetAtPath<CurrencyWallet>(WalletPath) == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data/Economy"))
                AssetDatabase.CreateFolder("Assets/Data", "Economy");
            AssetDatabase.CreateAsset(
                ScriptableObject.CreateInstance<CurrencyWallet>(), WalletPath);
        }

        // RewardTable del stage naval
        var reward = AssetDatabase.LoadAssetAtPath<RewardTable>(RewardPath);
        if (reward == null)
        {
            reward = ScriptableObject.CreateInstance<RewardTable>();
            reward.Entries = new List<RewardEntry>
            {
                new RewardEntry { Currency = CurrencyType.Doblones, Amount = 150 },
                new RewardEntry { Currency = CurrencyType.GemasDeCalavera, Amount = 5 }
            };
            AssetDatabase.CreateAsset(reward, RewardPath);
        }

        // Stage naval
        var stage = AssetDatabase.LoadAssetAtPath<NavalStageData>(NavalStagePath);
        if (stage == null)
        {
            stage = ScriptableObject.CreateInstance<NavalStageData>();
            stage.Id = "stage_004_mar_de_los_lamentos";
            stage.DisplayName = "Mar de los Lamentos";
            stage.Description = "Aguas malditas donde el Requiem patrulla. " +
                "Combate naval: tu barco contra tres oleadas corsarias.";
            stage.DifficultyLevel = 3;
            stage.Rewards = reward;
            stage.PlayerShip = Load<ShipData>("Assets/Data/Ships/ship_marea_espectral.asset");
            stage.NavalWaves = new List<NavalWaveDefinition>
            {
                new NavalWaveDefinition { Ships = new List<ShipData>
                    { Load<ShipData>("Assets/Data/Ships/ship_balandra_corsaria.asset") } },
                new NavalWaveDefinition { Ships = new List<ShipData>
                    { Load<ShipData>("Assets/Data/Ships/ship_bergantin_maldito.asset"),
                      Load<ShipData>("Assets/Data/Ships/creature_serpiente_abisal.asset") } },
                new NavalWaveDefinition { Ships = new List<ShipData>
                    { Load<ShipData>("Assets/Data/Ships/ship_galeon_del_requiem.asset") } }
            };
            stage.EnemyCrewPool = new List<CharacterData>
            {
                Load<CharacterData>("Assets/Data/Characters/pirate_grunt_1.asset"),
                Load<CharacterData>("Assets/Data/Characters/pirate_brute_1.asset"),
                Load<CharacterData>("Assets/Data/Characters/corsair_1.asset"),
                Load<CharacterData>("Assets/Data/Characters/hexer_1.asset")
            };
            AssetDatabase.CreateAsset(stage, NavalStagePath);
        }

        // Registro en StageRegistry (visible en StageSelect)
        var registry = AssetDatabase.LoadAssetAtPath<StageRegistry>(RegistryPath);
        if (registry != null && !registry.Stages.Contains(stage))
        {
            registry.Stages.Add(stage);
            EditorUtility.SetDirty(registry);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[S4-07] Assets navales creados/verificados (stage_004 + reward + wallet)");
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
