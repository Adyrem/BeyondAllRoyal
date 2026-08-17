using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Part of ProjectSetup (see ProjectSetup.cs) — everything WireScene() calls
// behind "BeyondAllRoyal → 2 - Setup Scenes": the categorized shop panel,
// minimum-reserve slider, Cancel/Demolish/Pause-Resume-Production/Main Menu
// buttons, the Pause button + pause panel, and the NPC building pool.
public static partial class ProjectSetup
{
    // -------------------------------------------------------------------------
    // Step 2a — rebuilds the shop panel as a categorized vertical list (Economy /
    // Units / Defense, per ShopCategories below), each building shown as a row
    // with its icon, name, and cost. Always fully rebuilds from ShopCategories
    // rather than incrementally adding, since the panel's whole layout can
    // change (e.g. grid -> categorized list) — unlike a simple add-if-missing
    // list, there's no sensible way to patch that in place. Sprite/cost/name
    // values are set at runtime by BuildingShopPanel.Start(), not baked in here,
    // so they stay in sync with the underlying BuildingData without re-running
    // this step. Requires BuildingShopPanel in the open scene.
    // -------------------------------------------------------------------------

    static readonly (string Header, string[] Names)[] ShopCategories =
    {
        ("Economy", new[] { "MetalFactory", "TeslaTower" }),
        ("Units",   new[] { "Barracks", "GunRange", "Laboratory", "SkimmerPad", "IronWorks" }),
        ("Defense", new[] { "MachinegunTurret", "RailgunTurret" }),
    };

    static void PopulateShopPanel()
    {
        var panel = Object.FindAnyObjectByType<BuildingShopPanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No BuildingShopPanel found in the open scene.");
            return;
        }

        for (int i = panel.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);

        var oldGrid = panel.GetComponent<GridLayoutGroup>();
        if (oldGrid != null) Object.DestroyImmediate(oldGrid);

        // Stretch to the full canvas width (minus a small margin) instead of
        // whatever fixed width the panel happened to be given manually —
        // there's plenty of horizontal room on a phone screen and a
        // categorized list with names reads much better wide than narrow.
        // Left as-is vertically: wherever it's anchored/positioned top-to-
        // bottom is left alone, since ContentSizeFitter already grows its
        // height to fit content.
        var panelRect = panel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0f, panelRect.anchorMin.y);
            panelRect.anchorMax = new Vector2(1f, panelRect.anchorMax.y);
            panelRect.offsetMin = new Vector2(16f, panelRect.offsetMin.y);
            panelRect.offsetMax = new Vector2(-16f, panelRect.offsetMax.y);
        }

        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(14, 14, 14, 14);
        vlg.spacing                = 10f;
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.childControlWidth      = true;
        vlg.childForceExpandWidth  = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandHeight = false;

        var fitter = panel.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var so = new SerializedObject(panel);
        var entries = so.FindProperty("shopEntries");
        entries.arraySize = 0;

        int added = 0;
        foreach (var (header, names) in ShopCategories)
        {
            CreateCategoryHeader(panel.transform, header);

            foreach (var name in names)
            {
                var data   = AssetDatabase.LoadAssetAtPath<BuildingData>($"{SOBuildings}/{name}.asset");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBuildings}/{name}.prefab");
                if (data == null || prefab == null) continue;

                var row = CreateShopRow(panel.transform, name);

                int idx = entries.arraySize;
                entries.arraySize++;
                var entry = entries.GetArrayElementAtIndex(idx);
                entry.FindPropertyRelative("data").objectReferenceValue      = data;
                entry.FindPropertyRelative("prefab").objectReferenceValue    = prefab;
                entry.FindPropertyRelative("button").objectReferenceValue    = row.Button;
                entry.FindPropertyRelative("icon").objectReferenceValue      = row.Icon;
                entry.FindPropertyRelative("nameLabel").objectReferenceValue = row.NameLabel;
                entry.FindPropertyRelative("costLabel").objectReferenceValue = row.CostLabel;

                added++;
            }
        }

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        Debug.Log($"[BeyondAllRoyal] Rebuilt the shop panel: {added} building(s) across {ShopCategories.Length} " +
                  "categories (Economy/Units/Defense). Reposition/resize the panel as needed, then save the scene.");
    }

    static void CreateCategoryHeader(Transform parent, string title)
    {
        var go = new GameObject($"{title}Header", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = 64f;
        layout.minHeight       = 64f;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text      = title;
        text.fontSize  = 38f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color     = UITheme.MutedText;
    }

    readonly struct ShopRowRefs
    {
        public readonly Button Button;
        public readonly Image Icon;
        public readonly TextMeshProUGUI NameLabel;
        public readonly TextMeshProUGUI CostLabel;

        public ShopRowRefs(Button button, Image icon, TextMeshProUGUI nameLabel, TextMeshProUGUI costLabel)
        {
            Button = button;
            Icon = icon;
            NameLabel = nameLabel;
            CostLabel = costLabel;
        }
    }

    // One row per building: icon | name (flexible width) | cost, the whole row
    // clickable as a single Button. Sprite/name/cost text is left for
    // BuildingShopPanel.Start() to fill in from the BuildingData at runtime.
    static ShopRowRefs CreateShopRow(Transform parent, string buildingName)
    {
        var rowGO = new GameObject($"{buildingName}Row", typeof(RectTransform));
        rowGO.transform.SetParent(parent, false);

        var rowLayout = rowGO.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 128f;
        rowLayout.minHeight       = 128f;

        var rowImage = rowGO.AddComponent<Image>();
        var button   = rowGO.AddComponent<Button>();
        button.targetGraphic = rowImage;
        UITheme.ApplyButtonColors(button, UITheme.Panel);

        var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.padding               = new RectOffset(14, 14, 8, 8);
        hlg.spacing                = 16f;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlWidth      = true;
        hlg.childForceExpandWidth  = false;
        hlg.childControlHeight     = true;
        hlg.childForceExpandHeight = true;

        var iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(rowGO.transform, false);
        var iconLayout = iconGO.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 100f;
        iconLayout.minWidth       = 100f;
        var icon = iconGO.AddComponent<Image>();
        icon.preserveAspect = true;

        var nameGO = new GameObject("Name", typeof(RectTransform));
        nameGO.transform.SetParent(rowGO.transform, false);
        var nameLayout = nameGO.AddComponent<LayoutElement>();
        nameLayout.flexibleWidth = 1f;
        var nameLabel = nameGO.AddComponent<TextMeshProUGUI>();
        nameLabel.fontSize          = 38f;
        nameLabel.alignment         = TextAlignmentOptions.MidlineLeft;
        nameLabel.color             = UITheme.Text;
        nameLabel.enableAutoSizing  = true;
        nameLabel.fontSizeMin       = 22f;
        nameLabel.fontSizeMax       = 38f;
        nameLabel.textWrappingMode  = TextWrappingModes.NoWrap;

        var costGO = new GameObject("Cost", typeof(RectTransform));
        costGO.transform.SetParent(rowGO.transform, false);
        var costLayout = costGO.AddComponent<LayoutElement>();
        costLayout.preferredWidth = 110f;
        costLayout.minWidth       = 110f;
        var costLabel = costGO.AddComponent<TextMeshProUGUI>();
        costLabel.fontSize          = 32f;
        costLabel.alignment         = TextAlignmentOptions.MidlineRight;
        costLabel.color             = UITheme.MutedText;
        costLabel.enableAutoSizing  = true;
        costLabel.fontSizeMin       = 20f;
        costLabel.fontSizeMax       = 32f;
        costLabel.textWrappingMode  = TextWrappingModes.NoWrap;

        return new ShopRowRefs(button, icon, nameLabel, costLabel);
    }

    // -------------------------------------------------------------------------
    // Step 2b — builds a default (unstyled) Slider + label under the same Canvas
    // as HUD and wires them into HUD.minimumReserveSlider/minimumReserveLabel.
    // Destroys and rebuilds them fresh each time (rather than skipping once
    // created), so a re-run always reflects the current size/position instead
    // of requiring the old ones to be deleted by hand first. Requires HUD in
    // the open scene.
    // -------------------------------------------------------------------------

    static void CreateMinimumReserveSlider()
    {
        var hud = Object.FindAnyObjectByType<HUD>(FindObjectsInactive.Include);
        if (hud == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No HUD found in the open scene.");
            return;
        }

        var so = new SerializedObject(hud);
        var sliderProp = so.FindProperty("minimumReserveSlider");
        var labelProp  = so.FindProperty("minimumReserveLabel");

        var existingSlider = sliderProp.objectReferenceValue as Slider;
        if (existingSlider != null) Object.DestroyImmediate(existingSlider.gameObject);
        var existingLabel = labelProp.objectReferenceValue as TextMeshProUGUI;
        if (existingLabel != null) Object.DestroyImmediate(existingLabel.gameObject);

        var canvas = hud.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No Canvas found to parent the slider under.");
            return;
        }

        var sliderGO = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderGO.name = "MinimumReserveSlider";
        sliderGO.transform.SetParent(canvas.transform, false);
        // Sits behind every other HUD panel (e.g. buildingInfoPanel, which can
        // otherwise end up overlapping it in the same top-left corner) — later
        // siblings draw on top, so first-sibling draws behind everything else.
        sliderGO.transform.SetAsFirstSibling();

        var rect = sliderGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot     = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(30f, -130f);
        rect.sizeDelta = new Vector2(420f, 56f);

        var slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 500f;
        slider.value    = 50f;

        var labelGO = new GameObject("MinimumReserveLabel", typeof(RectTransform));
        labelGO.transform.SetParent(canvas.transform, false);
        labelGO.transform.SetSiblingIndex(1); // right after the slider, still behind every other panel
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot     = new Vector2(0f, 1f);
        // Pushed right to clear the now-wider slider (420 wide, starting at x=30).
        labelRect.anchoredPosition = new Vector2(470f, -130f);
        labelRect.sizeDelta = new Vector2(300f, 56f);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 32f;
        label.text = "Min Reserve: 50";

        sliderProp.objectReferenceValue = slider;
        labelProp.objectReferenceValue  = label;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Debug.Log("[BeyondAllRoyal] Created/refreshed the minimum-reserve slider (unstyled placeholder). " +
                  "Reposition/style it as needed, then save the scene.");
    }

    // -------------------------------------------------------------------------
    // Step 2c — adds a Cancel button as a child of HUD.placementInfoPanel, wired
    // to HUD.cancelPlacementButton. Touch devices have no Escape key or right
    // click, so BuildingPlacer.CancelPlacement() was otherwise unreachable on
    // mobile. Requires HUD (with placementInfoPanel assigned) in the open scene.
    // -------------------------------------------------------------------------

    static void CreateCancelPlacementButton()
    {
        CreateHudChildButton("cancelPlacementButton", "placementInfoPanel", "CancelPlacementButton", "Cancel",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(220f, 88f));
    }

    // -------------------------------------------------------------------------
    // Step 2d — adds a Demolish button as a child of HUD.buildingInfoPanel, wired
    // to HUD.demolishButton. Lets the player free up a slot by voluntarily
    // destroying a building they own (HUD.OnDemolishClicked excludes the HQ, and
    // HQ.Demolish() refuses too, as a second line of defense).
    // Requires HUD (with buildingInfoPanel assigned) in the open scene.
    // -------------------------------------------------------------------------

    static void CreateDemolishButton()
    {
        CreateHudChildButton("demolishButton", "buildingInfoPanel", "DemolishButton", "Demolish",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(220f, 88f));
    }

    // -------------------------------------------------------------------------
    // Step 2e — adds a Pause/Resume Production button as a child of
    // HUD.buildingInfoPanel (bottom-left, mirroring Demolish at bottom-right),
    // wired to HUD.toggleProductionButton/toggleProductionLabel. This used to
    // live in its own standalone panel that covered the play field with
    // largely-redundant info; now it's folded into the info panel that's
    // already shown, and that separate panel is force-hidden permanently
    // (see HUD.Awake). Requires HUD (with buildingInfoPanel assigned) in the
    // open scene.
    // -------------------------------------------------------------------------

    static void CreateToggleProductionButton()
    {
        CreateHudChildButton("toggleProductionButton", "buildingInfoPanel", "ToggleProductionButton", "Pause/Resume Production",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(280f, 88f),
            "toggleProductionLabel");
    }

    // -------------------------------------------------------------------------
    // Step 2f — adds a Main Menu button as a child of HUD.endScreen, wired to
    // HUD.mainMenuButton, which calls GameManager.ReturnToMainMenu(). Also
    // registers the current scene in Build Settings, since SceneManager.LoadScene
    // silently fails on a scene that isn't listed there — needed both for this
    // scene to be reachable from MainMenu and for ReturnToMainMenu's own load.
    // Requires HUD (with endScreen assigned) in the open scene.
    // -------------------------------------------------------------------------

    static void CreateMainMenuButton()
    {
        EnsureSceneInBuildSettings();
        CreateHudChildButton("mainMenuButton", "endScreen", "MainMenuButton", "Main Menu",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(320f, 100f));
    }

    static void EnsureSceneInBuildSettings()
    {
        var scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path)) return; // scene was never saved — nothing to register yet

        if (EditorBuildSettings.scenes.Any(s => s.path == scene.path)) return;

        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.Add(new EditorBuildSettingsScene(scene.path, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log($"[BeyondAllRoyal] Added '{scene.path}' to Build Settings (required for MainMenu/GameManager.ReturnToMainMenu() scene loads).");
    }

    // -------------------------------------------------------------------------
    // Step 2g — adds an always-visible Pause button (top-right corner) parented
    // directly under the Canvas, wired to HUD.pauseButton. Requires HUD in the
    // open scene.
    // -------------------------------------------------------------------------

    static void CreatePauseButton()
    {
        var hud = Object.FindAnyObjectByType<HUD>(FindObjectsInactive.Include);
        if (hud == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No HUD found in the open scene.");
            return;
        }

        var so = new SerializedObject(hud);
        var buttonProp = so.FindProperty("pauseButton");

        var existing = buttonProp.objectReferenceValue as Button;
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var canvas = hud.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No Canvas found to parent the pause button under.");
            return;
        }

        var buttonGO = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonGO.name = "PauseButton";
        buttonGO.transform.SetParent(canvas.transform, false);

        var rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot     = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-30f, -30f);
        rect.sizeDelta = new Vector2(180f, 84f);

        var button = buttonGO.GetComponent<Button>();
        UITheme.ApplyButtonColors(button, UITheme.Accent);

        var legacyText = buttonGO.transform.Find("Text (Legacy)");
        if (legacyText != null) Object.DestroyImmediate(legacyText.gameObject);

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(buttonGO.transform, false);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 20f;
        labelText.fontSizeMax = 34f;
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        labelText.text = "Pause";

        buttonProp.objectReferenceValue = button;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Debug.Log("[BeyondAllRoyal] Created/refreshed the Pause button. Reposition/style it as needed, then save the scene.");
    }

    // -------------------------------------------------------------------------
    // Step 2h — adds a full-screen pause overlay (dimmed background + "Paused"
    // title + Resume/Main Menu buttons) parented directly under the Canvas,
    // wired to HUD.pausePanel/resumeButton/pauseMainMenuButton. The dimmer
    // itself carries a bare Button (no listener) purely so
    // InputHelper.TapHitInteractiveUI() treats any tap on it as hitting a
    // Selectable and skips world-tap handling underneath — otherwise tapping
    // the dimmed background (not directly on a button) would still register
    // as a building select/place tap despite the game being "paused".
    // Requires HUD in the open scene.
    // -------------------------------------------------------------------------

    static void CreatePausePanel()
    {
        var hud = Object.FindAnyObjectByType<HUD>(FindObjectsInactive.Include);
        if (hud == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No HUD found in the open scene.");
            return;
        }

        var so = new SerializedObject(hud);
        var panelProp = so.FindProperty("pausePanel");

        var existing = panelProp.objectReferenceValue as GameObject;
        if (existing != null) Object.DestroyImmediate(existing);

        var canvas = hud.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No Canvas found to parent the pause panel under.");
            return;
        }

        var panelGO = new GameObject("PausePanel", typeof(RectTransform));
        panelGO.transform.SetParent(canvas.transform, false);
        // Rendered last (on top of everything else in the canvas), the
        // opposite of the min-reserve slider, which needs to stay behind.
        panelGO.transform.SetAsLastSibling();

        var rect = panelGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var dimmer = panelGO.AddComponent<Image>();
        dimmer.color = new Color(0f, 0f, 0f, 0.75f);
        // Raycast blocker only, see comment above — no color transition, so
        // tapping the dimmer can't visibly flicker/tint it.
        var dimmerBlocker = panelGO.AddComponent<Button>();
        dimmerBlocker.transition = Selectable.Transition.None;

        var titleGO = new GameObject("PausedTitle", typeof(RectTransform));
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot     = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 160f);
        titleRect.sizeDelta = new Vector2(600f, 120f);
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text      = "Paused";
        titleText.fontSize  = 64f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color     = UITheme.Text;

        panelProp.objectReferenceValue = panelGO;
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);

        // CreateHudChildButton looks up its parent by re-reading HUD's own
        // serialized field, so it'll see the pausePanel just assigned above.
        CreateHudChildButton("resumeButton", "pausePanel", "ResumeButton", "Resume",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(360f, 90f));
        CreateHudChildButton("pauseMainMenuButton", "pausePanel", "PauseMainMenuButton", "Main Menu",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(360f, 90f));

        Debug.Log("[BeyondAllRoyal] Created/refreshed the pause panel. Reposition/style it as needed, then save the scene.");
    }

    // -------------------------------------------------------------------------
    // Step 2i — populates NPCController.allProductionBuildingTypes with all 5
    // production buildings, so it has a full pool to randomly pick from each
    // match (see NPCController.AssignRandomBuildingTypes). Requires NPCController
    // in the open scene.
    // -------------------------------------------------------------------------

    static void AutoWireNPCBuildingTypes()
    {
        var npc = Object.FindAnyObjectByType<NPCController>(FindObjectsInactive.Include);
        if (npc == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No NPCController found in the open scene.");
            return;
        }

        var so = new SerializedObject(npc);
        var typesProp = so.FindProperty("allProductionBuildingTypes");

        if (typesProp.arraySize > 0)
        {
            Debug.Log("[BeyondAllRoyal] NPCController.allProductionBuildingTypes already populated — skipping.");
            return;
        }

        typesProp.arraySize = ProductionBuildingNames.Length;

        int wired = 0;
        for (int i = 0; i < ProductionBuildingNames.Length; i++)
        {
            var name    = ProductionBuildingNames[i];
            var data    = AssetDatabase.LoadAssetAtPath<BuildingData>($"{SOBuildings}/{name}.asset");
            var prefab  = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBuildings}/{name}.prefab");
            var element = typesProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("data").objectReferenceValue   = data;
            element.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            if (data != null && prefab != null) wired++;
        }

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(npc.gameObject.scene);
        Debug.Log($"[BeyondAllRoyal] Wired {wired}/{ProductionBuildingNames.Length} production building types into " +
                  "NPCController.allProductionBuildingTypes. Save the scene.");
    }

    // Shared by CreateCancelPlacementButton/CreateDemolishButton/CreateMainMenuButton:
    // creates a button as a child of the GameObject referenced by HUD's
    // parentFieldName and wires it into HUD's buttonFieldName. Destroys and
    // rebuilds fresh each run rather than skipping if already assigned (see
    // comment above the destroy call below).
    static void CreateHudChildButton(string buttonFieldName, string parentFieldName, string goName, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta,
        string labelFieldName = null)
    {
        var hud = Object.FindAnyObjectByType<HUD>(FindObjectsInactive.Include);
        if (hud == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No HUD found in the open scene.");
            return;
        }

        var so = new SerializedObject(hud);
        var buttonProp = so.FindProperty(buttonFieldName);

        // Destroy and rebuild fresh each time (rather than skipping once
        // created), so a re-run always reflects the current size/position/
        // label instead of requiring the old button to be deleted by hand first.
        var existing = buttonProp.objectReferenceValue as Button;
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var panel = so.FindProperty(parentFieldName).objectReferenceValue as GameObject;
        if (panel == null)
        {
            Debug.LogWarning($"[BeyondAllRoyal] HUD.{parentFieldName} isn't assigned — can't parent the {goName} under it.");
            return;
        }

        var buttonGO = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonGO.name = goName;
        buttonGO.transform.SetParent(panel.transform, false);

        var rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot     = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        // The default label child is legacy Text ("Button") — replace with TMP.
        var legacyText = buttonGO.transform.Find("Text (Legacy)");
        if (legacyText != null) Object.DestroyImmediate(legacyText.gameObject);

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(buttonGO.transform, false);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 20f;
        labelText.fontSizeMax = 38f;
        // Word-wrapping a button label lets auto-sizing shrink it much
        // smaller than the button actually needs, to fit two stacked lines
        // instead of one — force a single line so it only shrinks as far as
        // the button's width actually requires.
        labelText.textWrappingMode  = TextWrappingModes.NoWrap;
        labelText.text        = label;

        buttonProp.objectReferenceValue = buttonGO.GetComponent<Button>();

        // Some callers (e.g. the toggle-production button) need to update the
        // label text at runtime based on state, rather than the static text
        // set above, so also wire the label component into its own HUD field.
        if (labelFieldName != null)
            so.FindProperty(labelFieldName).objectReferenceValue = labelText;

        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Debug.Log($"[BeyondAllRoyal] Created/refreshed the {label} button under {parentFieldName}. " +
                  "Reposition/style it as needed, then save the scene.");
    }
}
