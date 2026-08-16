using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Run from the Unity menu, in order:
//   BeyondAllRoyal → 1 - Setup Project Assets (no scene needed)
//   BeyondAllRoyal → 2 - Setup Scenes         (run once GameManager/HUD/MapGrid/BuildingShopPanel/NPCController
//                                               exist in PlayScene and it's the open scene; re-runnable any time)
// Everything scene-related — wiring PlayScene, theming it (UITheme.cs),
// creating MainMenu (MainMenuSetup.cs), and creating/refreshing TestScene
// (TestSceneSetup.cs) — lives behind that one second step instead of each
// having its own menu item, so there's just the one asset step and one scene
// step to run/re-run. Those other files keep their own logic for readability,
// they just aren't independently runnable from the menu anymore.
public static class ProjectSetup
{
    // Keep MenuItem strings short and slash-free: Unity treats every "/" in
    // the path as a submenu separator, so a descriptive suffix like
    // "(... Cancel/Demolish/Restart ...)" silently explodes into a chain of
    // nested submenus instead of one clickable item. Put details in comments
    // (here and in each step's own Debug.Log) instead of the menu string.

    [MenuItem("BeyondAllRoyal/1 - Setup Project Assets")]
    public static void SetupProjectAssets()
    {
        CreateScriptableObjects();
        CreatePrefabs();
        ImportAndAssignSprites();
        Debug.Log("[BeyondAllRoyal] Project assets set up. Run 2 - Setup Scenes once PlayScene's GameObjects exist.");
    }

    [MenuItem("BeyondAllRoyal/2 - Setup Scenes")]
    public static void SetupScenes()
    {
        string originalScenePath = SceneManager.GetActiveScene().path;

        WireScene();
        ThemeSetup.ApplyPlaySceneTheme();

        var activeScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(activeScene.path))
            EditorSceneManager.SaveScene(activeScene);

        MainMenuSetup.CreateMainMenuScene();
        TestSceneSetup.CreateTestScene();

        // Land back on whatever was open when this started (PlayScene, per
        // the documented workflow) instead of leaving TestScene open, since
        // that's just an implementation detail of this step, not the point of it.
        if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            EditorSceneManager.OpenScene(originalScenePath);

        Debug.Log("[BeyondAllRoyal] Scene setup complete: wired + themed PlayScene, created/refreshed MainMenu and TestScene.");
    }

    // Populates the categorized shop panel, creates the minimum-reserve
    // slider, adds Cancel/Demolish/Main Menu buttons, and populates the NPC's
    // production building pool. Requires PlayScene's GameManager/HUD/MapGrid/
    // BuildingShopPanel/NPCController to already exist in the open scene.
    private static void WireScene()
    {
        PopulateShopPanel();
        CreateMinimumReserveSlider();
        CreateCancelPlacementButton();
        CreateDemolishButton();
        CreateMainMenuButton();
        AutoWireNPCBuildingTypes();
    }

    const string AudioPath   = "Assets/Resources";
    const string SOUnits     = "Assets/ScriptableObjects/Units";
    const string SOBuildings = "Assets/ScriptableObjects/Buildings";
    const string SOMaps      = "Assets/ScriptableObjects/Maps";
    const string SORoot      = "Assets/ScriptableObjects";
    const string PrefabUnits     = "Assets/Prefabs/Units";
    const string PrefabBuildings = "Assets/Prefabs/Buildings";
    const string SpritePath      = "Assets/Sprites/Placeholder";
    const string SpriteUnitsPath     = "Assets/Sprites/Units";
    const string SpriteBuildingsPath = "Assets/Sprites/Buildings";

    static readonly string[] UnitNames = { "Soldier", "HeavyGunner", "ExplosiveSpecialist", "Hovercraft", "HeavyTank" };
    static readonly string[] ProductionBuildingNames = { "Barracks", "GunRange", "Laboratory", "SkimmerPad", "IronWorks" };
    static readonly string[] BuildingNames = {
        "Barracks", "GunRange", "Laboratory", "SkimmerPad", "IronWorks",
        "MachinegunTurret", "RailgunTurret", "TeslaTower", "MetalFactory", "HQ"
    };

    // -------------------------------------------------------------------------
    // Step 1a — ScriptableObjects
    // -------------------------------------------------------------------------

    static void CreateScriptableObjects()
    {
        EnsureFolders();

        // Counter chart & game settings
        var chart = CreateSO<CounterChartData>($"{SORoot}/CounterChart.asset");
        chart.InitializeDefaults();
        EditorUtility.SetDirty(chart);

        var settings = CreateSO<GameSettings>($"{SORoot}/GameSettings.asset");
        settings.buildingPassiveTrickleRate = 1f;
        settings.strongMultiplier           = 1.5f;
        settings.weakMultiplier             = 0.5f;
        settings.counterChart               = chart;
        settings.explosionDamageFraction    = 0.3f;
        settings.explosionRadiusPerHealth   = 0.01f;
        settings.shootPitchReferenceMinHealth = 50f;
        settings.shootPitchReferenceMaxHealth = 350f;
        settings.shootPitchForMinHealth        = 1.3f;
        settings.shootPitchForMaxHealth        = 0.7f;

        // Sound effects can't be procedurally generated like sprites — auto-wired
        // by convention if present at these paths, left alone otherwise so a
        // manual Inspector assignment elsewhere isn't clobbered.
        var explosionClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioPath}/splosion.mp3");
        if (explosionClip != null) settings.explosionSfx = explosionClip;

        var shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioPath}/shot.mp3");
        if (shootClip != null) settings.unitShootSfx = shootClip;

        EditorUtility.SetDirty(settings);

        // Units                        type                              name                    hp     dmg  range  atkSpd  spd  metal  nrg
        // Move speed values are 1/4 of the original design (0.625, 0.375, 0.625, 1.0, 0.375)
        // Metal lowered and energy (build time) raised a bit across the board —
        // metal was the tighter bottleneck given it's a shared pool across all
        // production buildings, while energy is per-building and doesn't compete.
        CreateUnitData(EntityType.Soldier,             "Soldier",              50f,  10f, 1.5f, 2.0f, 0.625f, 22f,  35f);
        CreateUnitData(EntityType.HeavyGunner,         "HeavyGunner",         120f,  18f, 4.0f, 1.5f, 0.375f, 35f,  40f);
        CreateUnitData(EntityType.ExplosiveSpecialist, "ExplosiveSpecialist", 100f,  35f, 3.5f, 0.5f, 0.625f, 35f,  40f);
        CreateUnitData(EntityType.Hovercraft,          "Hovercraft",          200f,  25f, 2.5f, 1.0f, 1.0f,   70f,  75f);
        CreateUnitData(EntityType.HeavyTank,           "HeavyTank",           350f,  40f, 4.5f, 0.5f, 0.375f, 70f,  75f);

        // Production buildings         name              slot          metalBuild  nrgBuild  buffer  unitSO name
        CreateProductionBuildingData("Barracks",    new Vector2Int(1,1), 30f, 20f,  60f, "Soldier");
        CreateProductionBuildingData("GunRange",    new Vector2Int(2,2), 20f, 30f,  60f, "HeavyGunner");
        CreateProductionBuildingData("Laboratory",  new Vector2Int(1,1), 20f, 30f,  60f, "ExplosiveSpecialist");
        CreateProductionBuildingData("SkimmerPad", new Vector2Int(2,2), 50f, 50f, 100f, "Hovercraft");
        CreateProductionBuildingData("IronWorks",  new Vector2Int(3,3), 80f, 80f, 100f, "HeavyTank");

        // Towers                     name                 entityType                        hp    dmg  range  atkSpd  nrgShot  slot              metalBuild  nrgBuild  buffer
        CreateTowerData("MachinegunTurret", EntityType.MachinegunTurret, 200f, 15f, 5f, 2.0f,  5f, new Vector2Int(2,2),  60f,  40f,  80f);
        CreateTowerData("RailgunTurret",    EntityType.RailgunTurret,    300f, 60f, 8f, 0.5f, 20f, new Vector2Int(3,3), 100f,  80f, 150f);

        // Tesla Tower
        var tesla = CreateSO<TeslaTowerData>($"{SOBuildings}/TeslaTower.asset");
        tesla.buildingName          = "Tesla Tower";
        tesla.maxHealth             = 150f;
        tesla.metalCostToBuild      = 40f;
        tesla.energyCostToBuild     = 30f;
        tesla.energyBufferCapacity  = 50f;
        tesla.slotSize              = new Vector2Int(1, 1);
        tesla.injectionRatePerBuilding = 15f;
        tesla.injectionRange        = 6f;
        EditorUtility.SetDirty(tesla);

        // Metal Factory
        var factory = CreateSO<MetalFactoryData>($"{SOBuildings}/MetalFactory.asset");
        factory.buildingName         = "Metal Factory";
        factory.maxHealth            = 150f;
        factory.metalCostToBuild     = 30f;
        factory.energyCostToBuild    = 40f;
        factory.energyBufferCapacity = 80f;
        factory.slotSize             = new Vector2Int(1, 1);
        factory.metalPerSecond       = 3f;
        EditorUtility.SetDirty(factory);

        // HQ — energyCostToBuild = 0 so it starts pre-constructed
        var hq = CreateSO<HQData>($"{SOBuildings}/HQ.asset");
        hq.buildingName                = "HQ";
        hq.maxHealth                   = 1000f;
        hq.metalCostToBuild            = 0f;
        hq.energyCostToBuild           = 0f;
        hq.energyBufferCapacity        = 200f;
        hq.slotSize                    = new Vector2Int(3, 3);
        hq.metalPerSecond              = 2f;
        hq.injectionRatePerBuilding    = 5f;
        hq.injectionRange              = 8f;
        hq.attackDamage                = 20f;
        hq.attackRange                 = 6f;
        hq.attacksPerSecond            = 5f / 3f; // was 5 — cut to a third, it was firing way too fast
        hq.energyCostPerShot           = 10f;
        EditorUtility.SetDirty(hq);

        CreateDefaultMapLayout();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BeyondAllRoyal] ScriptableObjects created.");
    }

    // -------------------------------------------------------------------------
    // Step 1b — Prefabs
    // -------------------------------------------------------------------------

    static void CreatePrefabs()
    {
        EnsureFolders();
        var sprite = GetOrCreatePlaceholderSprite();

        // Units — colored squares, scale 0.5 world units
        CreateUnitPrefab("Soldier",             sprite, new Color(0.40f, 0.80f, 1.00f));
        CreateUnitPrefab("HeavyGunner",         sprite, new Color(0.20f, 0.40f, 1.00f));
        CreateUnitPrefab("ExplosiveSpecialist", sprite, new Color(1.00f, 0.60f, 0.10f));
        CreateUnitPrefab("Hovercraft",          sprite, new Color(0.20f, 0.80f, 0.40f));
        CreateUnitPrefab("HeavyTank",           sprite, new Color(0.55f, 0.55f, 0.55f));

        // Production buildings
        CreateBuildingPrefab<ProductionBuilding>("Barracks",    sprite, new Color(0.60f, 0.90f, 1.00f));
        CreateBuildingPrefab<ProductionBuilding>("GunRange",    sprite, new Color(0.40f, 0.60f, 1.00f));
        CreateBuildingPrefab<ProductionBuilding>("Laboratory",  sprite, new Color(1.00f, 0.80f, 0.40f));
        CreateBuildingPrefab<ProductionBuilding>("SkimmerPad", sprite, new Color(0.40f, 1.00f, 0.60f));
        CreateBuildingPrefab<ProductionBuilding>("IronWorks",  sprite, new Color(0.70f, 0.70f, 0.70f));

        // Towers
        CreateBuildingPrefab<DefenseTower>("MachinegunTurret", sprite, new Color(1.00f, 0.30f, 0.30f));
        CreateBuildingPrefab<DefenseTower>("RailgunTurret",    sprite, new Color(0.65f, 0.10f, 0.10f));

        // Support
        CreateBuildingPrefab<TeslaTower> ("TeslaTower",   sprite, new Color(1.00f, 1.00f, 0.20f));
        CreateBuildingPrefab<MetalFactory>("MetalFactory", sprite, new Color(0.80f, 0.60f, 0.20f));
        CreateBuildingPrefab<HQ>          ("HQ",          sprite, new Color(1.00f, 1.00f, 1.00f));

        // Slot visual
        CreateBuildingSlotPrefab(sprite);

        // Wire unit prefabs back into UnitData.prefab
        AssignUnitPrefabsToData();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BeyondAllRoyal] Prefabs created. Wire MapGrid.slotPrefab and scene managers manually.");
    }

    // -------------------------------------------------------------------------
    // Step 1c — Sprites. Assumes gen_sprites.py (or equivalent) has already
    // written the PNGs to Assets/Sprites/Units and Assets/Sprites/Buildings.
    // -------------------------------------------------------------------------

    static void ImportAndAssignSprites()
    {
        AssetDatabase.Refresh();

        foreach (var name in UnitNames) AssignUnitSprites(name);
        foreach (var name in BuildingNames) AssignBuildingSprites(name);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BeyondAllRoyal] Sprites imported and assigned to UnitData/BuildingData assets and prefabs.");
    }

    static void AssignUnitSprites(string name)
    {
        var idle  = LoadSpriteAsset($"{SpriteUnitsPath}/{name}_Idle.png");
        var shoot = LoadSpriteAsset($"{SpriteUnitsPath}/{name}_Shoot.png");
        if (idle == null || shoot == null)
        {
            Debug.LogWarning($"[BeyondAllRoyal] Missing generated sprites for unit '{name}'.");
            return;
        }

        var data = AssetDatabase.LoadAssetAtPath<UnitData>($"{SOUnits}/{name}.asset");
        if (data != null)
        {
            data.idleSprite    = idle;
            data.shootingSprite = shoot;
            EditorUtility.SetDirty(data);
        }

        PatchPrefabSprite($"{PrefabUnits}/{name}.prefab", idle);
    }

    static void AssignBuildingSprites(string name)
    {
        var frameA = LoadSpriteAsset($"{SpriteBuildingsPath}/{name}_A.png");
        var frameB = LoadSpriteAsset($"{SpriteBuildingsPath}/{name}_B.png");
        if (frameA == null || frameB == null)
        {
            Debug.LogWarning($"[BeyondAllRoyal] Missing generated sprites for building '{name}'.");
            return;
        }

        var data = AssetDatabase.LoadAssetAtPath<BuildingData>($"{SOBuildings}/{name}.asset");
        if (data != null)
        {
            data.spriteFrameA = frameA;
            data.spriteFrameB = frameB;
            EditorUtility.SetDirty(data);
        }

        PatchPrefabSprite($"{PrefabBuildings}/{name}.prefab", frameA);
    }

    static void PatchPrefabSprite(string prefabPath, Sprite sprite)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) return;

        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        var sr = scope.prefabContentsRoot.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = sprite;
    }

    static Sprite LoadSpriteAsset(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;

        // MapGrid/BuildingGhost scale buildings assuming each sprite's native size is
        // exactly 1x1 world unit (slotSize * SlotVisualSize). Bigger buildings use
        // higher-resolution source art to stay crisp, so pixelsPerUnit must match each
        // texture's actual resolution — a fixed value would over- or under-scale them.
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = Mathf.Max(width, height);
        importer.filterMode          = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

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
        layout.preferredHeight = 56f;
        layout.minHeight       = 56f;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text      = title;
        text.fontSize  = 32f;
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
        rowLayout.preferredHeight = 104f;
        rowLayout.minHeight       = 104f;

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
        iconLayout.preferredWidth = 84f;
        iconLayout.minWidth       = 84f;
        var icon = iconGO.AddComponent<Image>();
        icon.preserveAspect = true;

        var nameGO = new GameObject("Name", typeof(RectTransform));
        nameGO.transform.SetParent(rowGO.transform, false);
        var nameLayout = nameGO.AddComponent<LayoutElement>();
        nameLayout.flexibleWidth = 1f;
        var nameLabel = nameGO.AddComponent<TextMeshProUGUI>();
        nameLabel.fontSize          = 30f;
        nameLabel.alignment         = TextAlignmentOptions.MidlineLeft;
        nameLabel.color             = UITheme.Text;
        nameLabel.enableAutoSizing  = true;
        nameLabel.fontSizeMin       = 18f;
        nameLabel.fontSizeMax       = 30f;

        var costGO = new GameObject("Cost", typeof(RectTransform));
        costGO.transform.SetParent(rowGO.transform, false);
        var costLayout = costGO.AddComponent<LayoutElement>();
        costLayout.preferredWidth = 90f;
        costLayout.minWidth       = 90f;
        var costLabel = costGO.AddComponent<TextMeshProUGUI>();
        costLabel.fontSize          = 26f;
        costLabel.alignment         = TextAlignmentOptions.MidlineRight;
        costLabel.color             = UITheme.MutedText;
        costLabel.enableAutoSizing  = true;
        costLabel.fontSizeMin       = 16f;
        costLabel.fontSizeMax       = 26f;

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
        rect.sizeDelta = new Vector2(320f, 44f);

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
        labelRect.anchoredPosition = new Vector2(360f, -130f);
        labelRect.sizeDelta = new Vector2(280f, 44f);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 28f;
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
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(180f, 72f));
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
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(190f, 72f));
    }

    // -------------------------------------------------------------------------
    // Step 2e — adds a Main Menu button as a child of HUD.endScreen, wired to
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
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(280f, 84f));
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
    // Step 2f — populates NPCController.allProductionBuildingTypes with all 5
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
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
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
        labelText.fontSizeMin = 16f;
        labelText.fontSizeMax = 30f;
        labelText.text        = label;

        buttonProp.objectReferenceValue = buttonGO.GetComponent<Button>();
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Debug.Log($"[BeyondAllRoyal] Created/refreshed the {label} button under {parentFieldName}. " +
                  "Reposition/style it as needed, then save the scene.");
    }

    // -------------------------------------------------------------------------
    // ScriptableObject helpers
    // -------------------------------------------------------------------------

    static T CreateSO<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void CreateUnitData(EntityType type, string name,
        float hp, float dmg, float range, float atkSpd, float spd, float metal, float energy)
    {
        var d = CreateSO<UnitData>($"{SOUnits}/{name}.asset");
        d.unitName          = name;
        d.entityType        = type;
        d.maxHealth         = hp;
        d.damage            = dmg;
        d.attackRange       = range;
        d.attacksPerSecond  = atkSpd;
        d.moveSpeed         = spd;
        d.metalCostPerUnit  = metal;
        d.energyCostPerUnit = energy;
        EditorUtility.SetDirty(d);
    }

    static void CreateProductionBuildingData(string name, Vector2Int slot,
        float metalBuild, float nrgBuild, float buffer, string unitName)
    {
        var d = CreateSO<ProductionBuildingData>($"{SOBuildings}/{name}.asset");
        d.buildingName         = name;
        d.maxHealth            = 200f;
        d.metalCostToBuild     = metalBuild;
        d.energyCostToBuild    = nrgBuild;
        d.energyBufferCapacity = buffer;
        d.slotSize             = slot;
        d.unitToProduced       = AssetDatabase.LoadAssetAtPath<UnitData>($"{SOUnits}/{unitName}.asset");
        EditorUtility.SetDirty(d);
    }

    static void CreateTowerData(string name, EntityType type,
        float hp, float dmg, float range, float atkSpd, float nrgShot,
        Vector2Int slot, float metalBuild, float nrgBuild, float buffer)
    {
        var d = CreateSO<TowerData>($"{SOBuildings}/{name}.asset");
        d.buildingName         = name;
        d.entityType           = type;
        d.maxHealth            = hp;
        d.damage               = dmg;
        d.attackRange          = range;
        d.attacksPerSecond     = atkSpd;
        d.energyCostPerShot    = nrgShot;
        d.slotSize             = slot;
        d.metalCostToBuild     = metalBuild;
        d.energyCostToBuild    = nrgBuild;
        d.energyBufferCapacity = buffer;
        EditorUtility.SetDirty(d);
    }

    static void CreateDefaultMapLayout()
    {
        var layout = CreateSO<MapLayoutData>($"{SOMaps}/DefaultMap.asset");
        layout.layoutName    = "Default (Two-Lane)";
        layout.columns       = 9; // odd width so the HQ's 3-wide footprint centers exactly (see MapGrid.PlaceHQs)
        layout.rows          = 8; // back 3 rows reserved for HQ, front 5 rows free for buildings
        layout.innerFraction = 0.08f;
        layout.outerFraction = 0.90f;
        layout.widthFraction = 0.90f;
        EditorUtility.SetDirty(layout);
    }

    // -------------------------------------------------------------------------
    // Prefab helpers
    // -------------------------------------------------------------------------

    static Sprite GetOrCreatePlaceholderSprite()
    {
        const string assetPath = SpritePath + "/Placeholder.png";

        if (!File.Exists(Path.Combine(Application.dataPath, "Sprites/Placeholder/Placeholder.png")))
        {
            var tex = new Texture2D(32, 32);
            var pixels = new Color32[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();

            File.WriteAllBytes(
                Path.Combine(Application.dataPath, "Sprites/Placeholder/Placeholder.png"),
                tex.EncodeToPNG());
            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType       = TextureImporterType.Sprite;
                importer.spriteImportMode  = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static void CreateUnitPrefab(string unitName, Sprite sprite, Color color)
    {
        string path = $"{PrefabUnits}/{unitName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var go = new GameObject(unitName);
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        var sr   = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color  = color;

        var unit = go.AddComponent<Unit>();
        go.AddComponent<UnitAI>();

        var unitData = AssetDatabase.LoadAssetAtPath<UnitData>($"{SOUnits}/{unitName}.asset");
        if (unitData != null)
        {
            var so = new SerializedObject(unit);
            so.FindProperty("data").objectReferenceValue = unitData;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var hb = AddHealthBar(go, sprite, yOffset: 0.35f);
        var unitSO = new SerializedObject(unit);
        unitSO.FindProperty("healthBar").objectReferenceValue = hb;
        unitSO.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static void CreateBuildingPrefab<T>(string name, Sprite sprite, Color color) where T : Building
    {
        string path = $"{PrefabBuildings}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            // Prefab already exists — patch any missing components rather than skipping
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = scope.prefabContentsRoot;
                if (root.GetComponent<BoxCollider2D>() == null)
                    root.AddComponent<BoxCollider2D>().size = Vector2.one;

                if (typeof(T) == typeof(ProductionBuilding))
                    EnsureProductionBarIndicator(root, sprite);
            }
            return;
        }

        var go = new GameObject(name);

        var buildingData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{SOBuildings}/{name}.asset");
        if (buildingData != null)
        {
            // Scale is a placeholder; MapGrid overrides it at runtime based on camera-derived slot size
            go.transform.localScale = new Vector3(buildingData.slotSize.x, buildingData.slotSize.y, 1f);
        }

        var sr    = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color  = color;

        // BoxCollider2D sized to one slot; TryPlaceBuilding rescales the transform at runtime
        var col  = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        var building = go.AddComponent<T>();
        if (buildingData != null)
        {
            var bso = new SerializedObject(building);
            bso.FindProperty("data").objectReferenceValue = buildingData;
            bso.ApplyModifiedPropertiesWithoutUndo();
        }

        var hb = AddHealthBar(go, sprite, yOffset: 0.6f);
        var buildingSO = new SerializedObject(building);
        buildingSO.FindProperty("healthBar").objectReferenceValue = hb;
        buildingSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire production bar for ProductionBuilding subclass
        if (building is ProductionBuilding prod)
        {
            var progressBar = AddProgressBar(go, sprite, yOffset: 0.5f);
            var prodSO = new SerializedObject(prod);
            prodSO.FindProperty("productionBar").objectReferenceValue = progressBar;
            prodSO.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    // Backfills the indicator tick onto a ProductionBuilding prefab's existing
    // ProductionBar, for prefabs created before the indicator existed.
    static void EnsureProductionBarIndicator(GameObject root, Sprite sprite)
    {
        var bar = root.transform.Find("ProductionBar");
        if (bar == null) return;

        var hb = bar.GetComponent<HealthBar>();
        if (hb == null) return;

        var so = new SerializedObject(hb);
        var indicatorProp = so.FindProperty("indicator");
        if (indicatorProp.objectReferenceValue != null) return; // already has one

        indicatorProp.objectReferenceValue = AddBarIndicator(bar.gameObject, sprite);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateBuildingSlotPrefab(Sprite sprite)
    {
        string path = $"{PrefabBuildings}/BuildingSlot.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var go = new GameObject("BuildingSlot");
        go.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        var sr         = go.AddComponent<SpriteRenderer>();
        sr.sprite      = sprite;
        sr.color       = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        sr.sortingOrder = -10;

        go.AddComponent<BuildingSlot>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static void AssignUnitPrefabsToData()
    {
        foreach (var name in UnitNames)
        {
            var data   = AssetDatabase.LoadAssetAtPath<UnitData>($"{SOUnits}/{name}.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabUnits}/{name}.prefab");
            if (data == null || prefab == null) continue;
            data.prefab = prefab;
            EditorUtility.SetDirty(data);
        }
    }

    // -------------------------------------------------------------------------
    // Bar helpers
    // -------------------------------------------------------------------------

    static HealthBar AddHealthBar(GameObject parent, Sprite sprite, float yOffset)
        => AddBar(parent, sprite, yOffset, Color.green, "HealthBar", withIndicator: false);

    // The production bar gets an extra indicator tick marking the energy
    // threshold for one unit, since it shows the raw energy buffer (not just
    // progress toward the current unit) and capacity is usually well above
    // that per-unit cost.
    static HealthBar AddProgressBar(GameObject parent, Sprite sprite, float yOffset)
        => AddBar(parent, sprite, yOffset, new Color(0.2f, 0.6f, 1f), "ProductionBar", withIndicator: true);

    static HealthBar AddBar(GameObject parent, Sprite sprite, float yOffset, Color fillColor, string barName, bool withIndicator)
    {
        // Background
        var bg = new GameObject(barName);
        bg.transform.SetParent(parent.transform, false);
        bg.transform.localPosition = new Vector3(0f, yOffset, 0f);
        bg.transform.localScale    = new Vector3(0.9f, 0.12f, 1f);
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite       = sprite;
        bgSr.color        = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        bgSr.sortingOrder = 10;

        // Fill
        var fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        fill.transform.localPosition = Vector3.zero;
        fill.transform.localScale    = Vector3.one;
        var fillSr = fill.AddComponent<SpriteRenderer>();
        fillSr.sprite       = sprite;
        fillSr.color        = fillColor;
        fillSr.sortingOrder = 11;

        // HealthBar component
        var hb = bg.AddComponent<HealthBar>();
        var so = new SerializedObject(hb);
        so.FindProperty("fill").objectReferenceValue = fillSr;

        if (withIndicator)
            so.FindProperty("indicator").objectReferenceValue = AddBarIndicator(bg, sprite);

        so.ApplyModifiedPropertiesWithoutUndo();

        return hb;
    }

    // A thin marker tick, slightly taller than the bar, positioned later via
    // HealthBar.SetIndicator(fraction). Defaults to the far right (100%) until then.
    static SpriteRenderer AddBarIndicator(GameObject bar, Sprite sprite)
    {
        var indicator = new GameObject("Indicator");
        indicator.transform.SetParent(bar.transform, false);
        indicator.transform.localPosition = new Vector3(0.5f, 0f, 0f);
        indicator.transform.localScale    = new Vector3(0.06f, 1.4f, 1f);
        var indicatorSr = indicator.AddComponent<SpriteRenderer>();
        indicatorSr.sprite       = sprite;
        indicatorSr.color        = Color.yellow;
        indicatorSr.sortingOrder = 12;
        return indicatorSr;
    }

    // -------------------------------------------------------------------------
    // Folder utility
    // -------------------------------------------------------------------------

    static void EnsureFolders()
    {
        AssetDatabase.Refresh();
        string[] paths = { SOUnits, SOBuildings, SOMaps, PrefabUnits, PrefabBuildings, SpritePath };
        foreach (var folderPath in paths)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) continue;
            var parts   = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
