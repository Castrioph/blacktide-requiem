using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BlacktideRequiem.UI.StageSelect;
using BlacktideRequiem.UI.TeamSelect;

/// <summary>
/// S3-11 UI Review & Polish — applies every approved P0/P1/P2 fix from
/// docs/art/ui-s311-ux-audit.md per docs/art/ui-s311-visual-design.md.
/// Idempotent: safe to run twice. Run via:
/// Unity.exe -batchmode -projectPath . -executeMethod ApplyS311UIPolish.Execute -quit
/// </summary>
public static class ApplyS311UIPolish
{
    // --- Canonical palette (visual design §1.2) ---
    private static readonly Color BgDark        = Hex("#140F24");
    private static readonly Color HeaderBase    = Hex("#1A0D00");
    private static readonly Color Gold          = Hex("#D4A017");
    private static readonly Color GoldBright    = Hex("#FFD700");
    private static readonly Color Cream         = Hex("#EDD9A3");
    private static readonly Color CreamMuted    = Hex("#F5E6C8");
    private static readonly Color LabelDark     = Hex("#1A0D00");
    private static readonly Color WoodBase      = Hex("#3D2810");
    private static readonly Color WoodBorder    = Hex("#5C3D1E");
    private static readonly Color SlotEmpty     = Hex("#1F172E");
    private static readonly Color SlotBorderEmp = Hex("#3A2A50");
    private static readonly Color RewardLabelC  = Hex("#8B5E3C");

    private const string GradientPath = "Assets/Sprites/UI/ui_bg_ocean_gradient.png";
    private const string LogoPath     = "Assets/Sprites/UI/ui_logo_placeholder.png";

    private const string FontPirataPath     = "Assets/Fonts/PirataOne-Regular.ttf";
    private const string FontNotoPath       = "Assets/Fonts/NotoSans-Regular.ttf";
    private const string FontNotoBoldPath   = "Assets/Fonts/NotoSans-Bold.ttf";
    private const string FontNotoItalicPath = "Assets/Fonts/NotoSans-Italic.ttf";

    private static Font _pirata, _noto, _notoBold, _notoItalic;

    public static void Execute()
    {
        try
        {
            Debug.Log("[S311] Starting S3-11 UI polish pass");

            LoadFonts();
            GenerateSprites();
            MoveScenes();
            FixStageEntryPrefab();
            FixRosterEntryPrefab();
            FixMainMenuScene();
            FixStageSelectScene();
            FixTeamSelectScene();

            AssetDatabase.SaveAssets();
            Debug.Log("[S311] DONE — all fixes applied");
        }
        catch (Exception e)
        {
            Debug.LogError("[S311] FAILED: " + e);
            throw;
        }
    }

    // ------------------------------------------------------------------
    // Fonts (P2-01) — legacy UI.Text keeps working with raw TTF Font assets.
    // TMP migration deliberately deferred (see active.md S3-11 notes).
    // ------------------------------------------------------------------

    private static void LoadFonts()
    {
        _pirata     = AssetDatabase.LoadAssetAtPath<Font>(FontPirataPath);
        _noto       = AssetDatabase.LoadAssetAtPath<Font>(FontNotoPath);
        _notoBold   = AssetDatabase.LoadAssetAtPath<Font>(FontNotoBoldPath);
        _notoItalic = AssetDatabase.LoadAssetAtPath<Font>(FontNotoItalicPath);

        if (_pirata == null || _noto == null)
            Debug.LogWarning("[S311] Fonts missing under Assets/Fonts/ — text will keep current font");
        else
            Debug.Log("[S311] Fonts loaded (PirataOne + NotoSans family)");
    }

    // ------------------------------------------------------------------
    // Sprites (P2-02 / asset list §5.1)
    // ------------------------------------------------------------------

    private static void GenerateSprites()
    {
        Directory.CreateDirectory("Assets/Sprites/UI");

        if (!File.Exists(GradientPath))
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color top = Hex("#1A2744"), bot = Hex("#050D14");
            // SetPixels: bottom row first
            tex.SetPixels(new[] { bot, bot, top, top });
            tex.Apply();
            File.WriteAllBytes(GradientPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(GradientPath);
            Debug.Log("[S311] Created " + GradientPath);
        }
        ConfigureSpriteImport(GradientPath, FilterMode.Bilinear);

        if (!File.Exists(LogoPath))
        {
            const int w = 600, h = 300, b = 2;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color fill = Hex("#1F172E"), border = Gold;
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = (x < b || x >= w - b || y < b || y >= h - b) ? border : fill;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(LogoPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(LogoPath);
            Debug.Log("[S311] Created " + LogoPath);
        }
        ConfigureSpriteImport(LogoPath, FilterMode.Bilinear);
    }

    private static void ConfigureSpriteImport(string path, FilterMode filter)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        bool dirty = importer.textureType != TextureImporterType.Sprite
                  || importer.filterMode != filter
                  || importer.mipmapEnabled;
        if (!dirty) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = filter;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    // ------------------------------------------------------------------
    // Scene relocation (P0-03 / P2-11) + Build Settings rewrite
    // ------------------------------------------------------------------

    private static void MoveScenes()
    {
        // Stale duplicates in Assets/Scenes/ are only deleted while the real
        // (build-referenced) copy still lives at Assets/ root.
        DeleteStaleDuplicate("Assets/MainMenu.unity", "Assets/Scenes/MainMenu.unity");
        DeleteStaleDuplicate("Assets/StageSelect.unity", "Assets/Scenes/StageSelect.unity");

        MoveScene("Assets/MainMenu.unity", "Assets/Scenes/MainMenu.unity");
        MoveScene("Assets/StageSelect.unity", "Assets/Scenes/StageSelect.unity");
        MoveScene("Assets/TeamSelect.unity", "Assets/Scenes/TeamSelect.unity");

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/StageSelect.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/TeamSelect.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/CombatDemo.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Results.unity", true),
        };
        Debug.Log("[S311] Build Settings rewritten (5 scenes, all under Assets/Scenes/)");
    }

    private static void DeleteStaleDuplicate(string activeRootCopy, string staleCopy)
    {
        if (File.Exists(activeRootCopy) && File.Exists(staleCopy))
        {
            AssetDatabase.DeleteAsset(staleCopy);
            Debug.Log("[S311] Deleted stale duplicate " + staleCopy);
        }
    }

    private static void MoveScene(string from, string to)
    {
        if (!File.Exists(from)) return; // already moved (idempotent)
        string error = AssetDatabase.MoveAsset(from, to);
        if (string.IsNullOrEmpty(error))
            Debug.Log($"[S311] Moved {from} -> {to}");
        else
            Debug.LogError($"[S311] Move failed {from}: {error}");
    }

    // ------------------------------------------------------------------
    // StageEntryUI prefab (P1-10, P2-03, P2-04)
    // ------------------------------------------------------------------

    private static void FixStageEntryPrefab()
    {
        const string path = "Assets/Prefabs/UI/StageEntryUI.prefab";
        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            GameObject root = scope.prefabContentsRoot;

            var le = root.GetComponent<LayoutElement>();
            if (le == null) le = root.AddComponent<LayoutElement>();
            le.preferredHeight = 340f;

            // Border child becomes a 3px ring: enlarged behind an exact-fit CardBg
            RectTransform border = (RectTransform)root.transform.Find("Border");
            SetRect(border, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(6, 6));
            border.SetSiblingIndex(0);
            border.GetComponent<Image>().raycastTarget = false;

            GameObject cardBg = EnsureChild(root.transform, "CardBg");
            var cardBgImg = EnsureImage(cardBg, new Color(WoodBase.r, WoodBase.g, WoodBase.b, 0.902f));
            cardBgImg.raycastTarget = false;
            SetRect((RectTransform)cardBg.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            cardBg.transform.SetSiblingIndex(1);

            GameObject stripe = EnsureChild(root.transform, "AccentStripe");
            var stripeImg = EnsureImage(stripe, Color.white);
            stripeImg.raycastTarget = false;
            SetRect((RectTransform)stripe.transform, new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(7, 0), new Vector2(8, 0));
            stripe.transform.SetSiblingIndex(2);

            var name = (RectTransform)root.transform.Find("StageName");
            SetRectOffsets(name, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(28, -76), new Vector2(-28, -20));
            StyleText(name.GetComponent<Text>(), _pirata, 30, CreamMuted, TextAnchor.MiddleLeft);
            name.SetSiblingIndex(3);

            var diff = (RectTransform)root.transform.Find("StageDifficulty");
            SetRectOffsets(diff, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(28, -130), new Vector2(-28, -86));
            StyleText(diff.GetComponent<Text>(), _noto, 24, Cream, TextAnchor.MiddleLeft);
            diff.SetSiblingIndex(4);

            // Reward strip (visual design §3.6)
            GameObject strip = EnsureChild(root.transform, "RewardStrip");
            var stripImg = EnsureImage(strip, new Color(HeaderBase.r, HeaderBase.g, HeaderBase.b, 0.706f));
            stripImg.raycastTarget = false;
            var stripRT = (RectTransform)strip.transform;
            stripRT.pivot = new Vector2(0.5f, 0f);
            SetRect(stripRT, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 3), new Vector2(-6, 48));

            GameObject divider = EnsureChild(strip.transform, "Divider");
            EnsureImage(divider, new Color(WoodBorder.r, WoodBorder.g, WoodBorder.b, 0.784f)).raycastTarget = false;
            SetRect((RectTransform)divider.transform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(0, 1));

            GameObject rLabel = EnsureChild(strip.transform, "RewardLabel");
            var rLabelText = EnsureText(rLabel);
            rLabelText.text = "Botín:";
            StyleText(rLabelText, _noto, 18, new Color(RewardLabelC.r, RewardLabelC.g, RewardLabelC.b, 0.863f),
                TextAnchor.MiddleLeft);
            SetRectOffsets((RectTransform)rLabel.transform, new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(16, 0), new Vector2(110, 0));

            GameObject rValue = EnsureChild(strip.transform, "RewardValue");
            var rValueText = EnsureText(rValue);
            if (string.IsNullOrEmpty(rValueText.text)) rValueText.text = "???";
            StyleText(rValueText, _notoBold, 19, GoldBright, TextAnchor.MiddleLeft);
            SetRectOffsets((RectTransform)rValue.transform, new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(118, 0), new Vector2(-16, 0));

            // Click target covers the whole card and stays on top
            var btn = root.transform.Find("BtnSelect");
            SetRect((RectTransform)btn, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var btnImg = btn.GetComponent<Image>();
            if (btnImg != null) btnImg.color = new Color(1, 1, 1, 0.004f);
            var btnLabel = btn.GetComponentInChildren<Text>(true);
            if (btnLabel != null) btnLabel.text = string.Empty;
            btn.SetAsLastSibling();

            // Wire new serialized fields
            var ui = root.GetComponent<StageEntryUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("_accentStripe").objectReferenceValue = stripeImg;
            so.FindProperty("_rewardValue").objectReferenceValue = rValueText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Debug.Log("[S311] StageEntryUI prefab restructured (340px card, stripe, reward strip)");
    }

    // ------------------------------------------------------------------
    // TeamRosterEntryUI prefab (visual design §4.5)
    // ------------------------------------------------------------------

    private static void FixRosterEntryPrefab()
    {
        const string path = "Assets/Prefabs/UI/TeamRosterEntryUI.prefab";
        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            GameObject root = scope.prefabContentsRoot;

            var le = root.GetComponent<LayoutElement>();
            if (le == null) le = root.AddComponent<LayoutElement>();
            le.preferredHeight = 100f;

            RectTransform border = (RectTransform)root.transform.Find("Border");
            SetRect(border, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(4, 4));
            border.SetSiblingIndex(0);
            var borderImg = border.GetComponent<Image>();
            borderImg.raycastTarget = false;

            GameObject cardBg = EnsureChild(root.transform, "CardBg");
            var cardBgImg = EnsureImage(cardBg, new Color(SlotEmpty.r, SlotEmpty.g, SlotEmpty.b, 0.902f));
            cardBgImg.raycastTarget = false;
            SetRect((RectTransform)cardBg.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            cardBg.transform.SetSiblingIndex(1);

            GameObject stripe = EnsureChild(root.transform, "AccentStripe");
            var stripeImg = EnsureImage(stripe, Color.white);
            stripeImg.raycastTarget = false;
            SetRect((RectTransform)stripe.transform, new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(6, 0), new Vector2(8, 0));
            stripe.transform.SetSiblingIndex(2);

            var charName = (RectTransform)root.transform.Find("CharName");
            SetRectOffsets(charName, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(28, -52), new Vector2(-100, -10));
            StyleText(charName.GetComponent<Text>(), _notoBold, 24, CreamMuted, TextAnchor.MiddleLeft);

            var charElem = (RectTransform)root.transform.Find("CharElement");
            SetRectOffsets(charElem, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(28, 10), new Vector2(-100, 44));
            StyleText(charElem.GetComponent<Text>(), _noto, 19, Cream, TextAnchor.MiddleLeft);

            var btn = root.transform.Find("BtnSelect");
            SetRect((RectTransform)btn, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var btnImg = btn.GetComponent<Image>();
            if (btnImg != null) btnImg.color = new Color(1, 1, 1, 0.004f);
            btn.SetAsLastSibling();

            var ui = root.GetComponent<TeamRosterEntryUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("_background").objectReferenceValue = cardBgImg;
            so.FindProperty("_accentStripe").objectReferenceValue = stripeImg;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Debug.Log("[S311] TeamRosterEntryUI prefab restructured (100px card, stripe, bg state)");
    }

    // ------------------------------------------------------------------
    // MainMenu scene
    // ------------------------------------------------------------------

    private static void FixMainMenuScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        FixCanvasScaler();

        GameObject menuPanel = MustFind("MenuPanel");
        var panelImg = menuPanel.GetComponent<Image>();
        if (panelImg != null) panelImg.color = BgDark;

        AddGradientLayer(menuPanel.transform, 0);

        // Logo placeholder slot (inactive until real art exists)
        GameObject logo = EnsureChild(menuPanel.transform, "LogoSlot");
        var logoImg = EnsureImage(logo, new Color(1, 1, 1, 0.784f));
        logoImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        logoImg.preserveAspect = true;
        logoImg.raycastTarget = false;
        SetRect((RectTransform)logo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -210), new Vector2(600, 300));
        logo.transform.SetSiblingIndex(1);
        logo.SetActive(false);

        // Title: top-anchored branding zone (P1-09)
        GameObject title = MustFind("TitleText");
        SetRect((RectTransform)title.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -460), new Vector2(900, 90));
        StyleText(title.GetComponent<Text>(), _pirata, 56, Gold, TextAnchor.MiddleCenter);

        GameObject subtitle = EnsureChild(menuPanel.transform, "SubtitleText");
        var subText = EnsureText(subtitle);
        subText.text = "El mar cobra lo suyo.";
        StyleText(subText, _notoItalic, 18, new Color(Cream.r, Cream.g, Cream.b, 0.706f), TextAnchor.MiddleCenter);
        SetRect((RectTransform)subtitle.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -560), new Vector2(700, 40));

        GameObject separator = EnsureChild(menuPanel.transform, "Separator");
        EnsureImage(separator, new Color(Gold.r, Gold.g, Gold.b, 0.47f)).raycastTarget = false;
        SetRect((RectTransform)separator.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -620), new Vector2(400, 2));

        // Primary button (P0-05, P0-06, P1-01)
        GameObject btn = MustFind("BtnStartBattle");
        SetRect((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -220), new Vector2(540, 88));
        var button = btn.GetComponent<Button>();
        ApplyPrimaryColorBlock(button);
        AddButtonBevels(btn.transform);

        // Delete ghost empty Text children; restyle the real label
        foreach (var t in btn.GetComponentsInChildren<Text>(true))
        {
            if (string.IsNullOrEmpty(t.text))
            {
                UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
            else
            {
                t.text = "¡INICIAR MISIÓN!";
                StyleText(t, _pirata, 26, LabelDark, TextAnchor.MiddleCenter);
                t.raycastTarget = false;
                SetRect((RectTransform)t.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }

        SetFirstSelected(btn);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[S311] MainMenu scene fixed");
    }

    // ------------------------------------------------------------------
    // StageSelect scene
    // ------------------------------------------------------------------

    private static void FixStageSelectScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/StageSelect.unity");
        FixCanvasScaler();

        GameObject background = MustFind("Background");
        background.GetComponent<Image>().color = BgDark;
        AddGradientLayer(background.transform.parent, background.transform.GetSiblingIndex() + 1);

        FixHeader("Seleccionar Misión", "Elige tu destino, corsario.");

        // Scroll area between 180px header and 160px footer (P0-04, P1-03)
        GameObject scroll = MustFind("StageScrollView");
        var scrollRT = (RectTransform)scroll.transform;
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(0, 160);
        scrollRT.offsetMax = new Vector2(0, -180);
        var scrollRect = scroll.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.elasticity = 0.08f;

        FixFooter();

        GameObject btnLaunch = MustFind("BtnLaunch");
        SetRect((RectTransform)btnLaunch.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(540, 120));
        var launch = btnLaunch.GetComponent<Button>();
        launch.interactable = false; // P0-07
        ApplyPrimaryColorBlock(launch);
        AddButtonBevels(btnLaunch.transform);
        var launchLabel = btnLaunch.GetComponentInChildren<Text>(true);
        if (launchLabel != null)
        {
            StyleText(launchLabel, _pirata, 26, LabelDark, TextAnchor.MiddleCenter);
            launchLabel.raycastTarget = false;
        }

        GameObject empty = MustFind("EmptyStateText");
        var emptyText = empty.GetComponent<Text>();
        emptyText.text = "No hay misiones disponibles. Vuelve más tarde."; // P2-06
        StyleText(emptyText, _noto, 22, Cream, TextAnchor.MiddleCenter);

        SetFirstSelected(MustFind("BtnBack")); // runtime focus moves to first card

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[S311] StageSelect scene fixed");
    }

    // ------------------------------------------------------------------
    // TeamSelect scene
    // ------------------------------------------------------------------

    private static void FixTeamSelectScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TeamSelect.unity");
        FixCanvasScaler();

        GameObject background = MustFind("Background");
        background.GetComponent<Image>().color = BgDark; // P2-08
        AddGradientLayer(background.transform.parent, background.transform.GetSiblingIndex() + 1);

        FixHeader("Selección de Equipo", "Elige a tus corsarios.");

        Transform canvasT = background.transform.parent;

        // Slots section (visual design §4.4)
        GameObject slotsLabel = EnsureChild(canvasT, "SlotsLabel");
        var slotsLabelText = EnsureText(slotsLabel);
        slotsLabelText.text = "EQUIPO SELECCIONADO:";
        StyleText(slotsLabelText, _noto, 18, new Color(Cream.r, Cream.g, Cream.b, 0.706f), TextAnchor.MiddleLeft);
        SetRectOffsets((RectTransform)slotsLabel.transform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(24, -222), new Vector2(-24, -190));

        GameObject slotsPanel = MustFind("SlotsPanel");
        var slotsPanelRT = (RectTransform)slotsPanel.transform;
        slotsPanelRT.anchorMin = new Vector2(0, 1);
        slotsPanelRT.anchorMax = new Vector2(1, 1);
        slotsPanelRT.anchoredPosition = new Vector2(0, -305);
        slotsPanelRT.sizeDelta = new Vector2(0, 150);
        var slotsPanelImg = slotsPanel.GetComponent<Image>();
        if (slotsPanelImg != null) slotsPanelImg.color = new Color(0, 0, 0, 0); // container only

        var slotBorders = new Image[3];
        var slotFills = new Image[3];
        var slotNames = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject slot = MustFind("Slot" + i);

            // Root image acts as the state border; exact-fit fill child above it
            var slotImg = slot.GetComponent<Image>();
            slotImg.color = SlotBorderEmp;
            slotBorders[i] = slotImg;

            GameObject fill = EnsureChild(slot.transform, "SlotFill");
            var fillImg = EnsureImage(fill, SlotEmpty);
            fillImg.raycastTarget = false;
            SetRect((RectTransform)fill.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-6, -6));
            fill.transform.SetSiblingIndex(0);
            slotFills[i] = fillImg;

            // BtnClear removed by user decision — second tap on roster toggles instead
            var btnClear = slot.transform.Find("BtnClear");
            if (btnClear != null) UnityEngine.Object.DestroyImmediate(btnClear.gameObject);

            var nameT = slot.transform.Find("SlotNameText");
            if (nameT != null)
            {
                var nameText = nameT.GetComponent<Text>();
                StyleText(nameText, _noto, 20, new Color(Cream.r, Cream.g, Cream.b, 0.55f), TextAnchor.MiddleCenter);
                SetRect((RectTransform)nameT, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-12, -12));
                nameT.SetAsLastSibling();
                slotNames[i] = nameText;
            }
        }

        GameObject rosterLabel = EnsureChild(canvasT, "RosterLabel");
        var rosterLabelText = EnsureText(rosterLabel);
        rosterLabelText.text = "PERSONAJES DISPONIBLES:";
        StyleText(rosterLabelText, _noto, 18, new Color(Cream.r, Cream.g, Cream.b, 0.706f), TextAnchor.MiddleLeft);
        SetRectOffsets((RectTransform)rosterLabel.transform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(24, -424), new Vector2(-24, -392));

        GameObject scroll = MustFind("RosterScrollView");
        var scrollRT = (RectTransform)scroll.transform;
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(0, 160);
        scrollRT.offsetMax = new Vector2(0, -434);
        var scrollRect = scroll.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.elasticity = 0.08f;
        var viewport = scroll.transform.Find("Viewport");
        if (viewport != null)
        {
            var vpImg = viewport.GetComponent<Image>();
            if (vpImg != null) vpImg.color = new Color(1, 1, 1, 0.004f);
        }

        FixFooter();

        GameObject btnConfirm = MustFind("BtnConfirm");
        SetRect((RectTransform)btnConfirm.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(540, 88));
        var confirm = btnConfirm.GetComponent<Button>();
        confirm.interactable = false; // P0-08
        ApplyPrimaryColorBlock(confirm);
        AddButtonBevels(btnConfirm.transform);
        var confirmLabel = btnConfirm.GetComponentInChildren<Text>(true);
        if (confirmLabel != null)
        {
            confirmLabel.text = "¡CONFIRMAR EQUIPO!";
            StyleText(confirmLabel, _pirata, 24, LabelDark, TextAnchor.MiddleCenter);
            confirmLabel.raycastTarget = false;
        }

        // Wire new TeamSelectController fields
        var controller = UnityEngine.Object.FindFirstObjectByType<TeamSelectController>();
        if (controller != null)
        {
            var so = new SerializedObject(controller);
            WireArray(so, "_slotBackgrounds", slotFills);
            WireArray(so, "_slotBorders", slotBorders);
            WireArray(so, "_slotNameTexts", slotNames);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogError("[S311] TeamSelectController not found in TeamSelect scene");
        }

        SetFirstSelected(MustFind("BtnBack")); // runtime focus moves to first roster card

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[S311] TeamSelect scene fixed");
    }

    // ------------------------------------------------------------------
    // Shared scene helpers
    // ------------------------------------------------------------------

    private static void FixCanvasScaler()
    {
        var scaler = UnityEngine.Object.FindFirstObjectByType<CanvasScaler>();
        if (scaler == null) { Debug.LogError("[S311] CanvasScaler not found"); return; }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f; // match height, portrait (P0-01)
    }

    private static void SetFirstSelected(GameObject target)
    {
        var es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        if (es == null) { Debug.LogError("[S311] EventSystem not found"); return; }
        es.firstSelectedGameObject = target; // P0-02
    }

    private static void AddGradientLayer(Transform parent, int siblingIndex)
    {
        GameObject gradient = EnsureChild(parent, "BgGradient");
        var img = EnsureImage(gradient, new Color(1, 1, 1, 0.706f));
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GradientPath);
        img.raycastTarget = false;
        SetRect((RectTransform)gradient.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        gradient.transform.SetSiblingIndex(siblingIndex);
    }

    // Header 180px + gold divider + title/subtitle + text-only BtnBack (P1-03, P1-04)
    private static void FixHeader(string title, string subtitle)
    {
        GameObject header = MustFind("Header");
        var headerRT = (RectTransform)header.transform;
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.anchoredPosition = new Vector2(0, -90);
        headerRT.sizeDelta = new Vector2(0, 180);
        header.GetComponent<Image>().color = HeaderBase;

        GameObject divider = EnsureChild(header.transform, "HeaderDivider");
        EnsureImage(divider, new Color(Gold.r, Gold.g, Gold.b, 0.863f)).raycastTarget = false;
        SetRect((RectTransform)divider.transform, new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 2), new Vector2(0, 4));

        var titleT = header.transform.Find("TitleText");
        if (titleT != null)
        {
            var titleText = titleT.GetComponent<Text>();
            titleText.text = title;
            StyleText(titleText, _pirata, 38, Gold, TextAnchor.MiddleCenter);
            SetRect((RectTransform)titleT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -22), new Vector2(700, 52));
        }

        GameObject sub = EnsureChild(header.transform, "SubtitleText");
        var subText = EnsureText(sub);
        subText.text = subtitle;
        StyleText(subText, _notoItalic, 17, new Color(Cream.r, Cream.g, Cream.b, 0.706f), TextAnchor.MiddleCenter);
        SetRect((RectTransform)sub.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -62), new Vector2(700, 30));

        var back = header.transform.Find("BtnBack");
        if (back != null)
        {
            var backImg = back.GetComponent<Image>();
            if (backImg != null) backImg.color = new Color(1, 1, 1, 0); // text-only, keeps raycast
            SetRect((RectTransform)back, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(84, -22), new Vector2(120, 64));
            var backText = back.GetComponentInChildren<Text>(true);
            if (backText != null)
            {
                StyleText(backText, _noto, 20, new Color(Cream.r, Cream.g, Cream.b, 0.784f), TextAnchor.MiddleCenter);
                backText.raycastTarget = false;
            }
        }
    }

    // Footer 160px, HeaderBase fill, gold top divider
    private static void FixFooter()
    {
        GameObject footer = MustFind("Footer");
        var footerRT = (RectTransform)footer.transform;
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.anchoredPosition = new Vector2(0, 80);
        footerRT.sizeDelta = new Vector2(0, 160);
        footer.GetComponent<Image>().color = HeaderBase;

        GameObject divider = EnsureChild(footer.transform, "FooterDivider");
        EnsureImage(divider, new Color(Gold.r, Gold.g, Gold.b, 0.863f)).raycastTarget = false;
        SetRect((RectTransform)divider.transform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -2), new Vector2(0, 4));
    }

    private static void ApplyPrimaryColorBlock(Button button)
    {
        var cb = button.colors;
        cb.normalColor      = Gold;
        cb.highlightedColor = Hex("#E8B420");
        cb.pressedColor     = Hex("#B8880F");
        cb.selectedColor    = Hex("#E8B420");
        cb.disabledColor    = Hex("#3D3020");
        cb.colorMultiplier  = 1f;
        cb.fadeDuration     = 0.1f;
        button.colors = cb;
        // ColorBlock tints the target graphic — base image must be white
        if (button.image != null) button.image.color = Color.white;
    }

    private static void AddButtonBevels(Transform button)
    {
        GameObject top = EnsureChild(button, "BevelTop");
        EnsureImage(top, new Color(GoldBright.r, GoldBright.g, GoldBright.b, 0.784f)).raycastTarget = false;
        SetRect((RectTransform)top.transform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -2), new Vector2(0, 4));

        GameObject bot = EnsureChild(button, "BevelBot");
        EnsureImage(bot, new Color(0.545f, 0.412f, 0.078f, 0.47f)).raycastTarget = false; // #8B6914
        SetRect((RectTransform)bot.transform, new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 2), new Vector2(0, 4));
    }

    // ------------------------------------------------------------------
    // Low-level helpers
    // ------------------------------------------------------------------

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    private static GameObject MustFind(string name)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                    return t.gameObject;
        throw new InvalidOperationException($"[S311] GameObject '{name}' not found in scene");
    }

    private static GameObject EnsureChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Image EnsureImage(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static Text EnsureText(GameObject go)
    {
        var text = go.GetComponent<Text>();
        if (text == null) text = go.AddComponent<Text>();
        return text;
    }

    private static void StyleText(Text text, Font font, int size, Color color, TextAnchor anchor)
    {
        if (text == null) return;
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = anchor;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    private static void SetRectOffsets(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static void WireArray(SerializedObject so, string propertyName, UnityEngine.Object[] values)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null) { Debug.LogError("[S311] Property not found: " + propertyName); return; }
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
