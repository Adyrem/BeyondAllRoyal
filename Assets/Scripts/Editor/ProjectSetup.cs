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
//   BeyondAllRoyal → 1 - Setup Project Assets   (no scene needed)
//   BeyondAllRoyal → 2 - Wire Scene             (run once GameManager/HUD/MapGrid/BuildingShopPanel/NPCController exist in-scene)
//   BeyondAllRoyal → 3 - Create Main Menu Scene (see MainMenuSetup.cs; standalone, can run any time after Step 1)
//   BeyondAllRoyal → 4 - Apply Dark Purple Theme to Play Scene (see ThemeSetup.cs; re-runnable any time, unlike Step 2)
public static class ProjectSetup
{
    // -------------------------------------------------------------------------
    // Consolidated entry points — everything below is grouped into these two
    // menu items so there's only one asset step and one scene step to run.
    // Keep these MenuItem strings short and slash-free: Unity treats every "/"
    // in the path as a submenu separator, so a descriptive suffix like
    // "(... Cancel/Demolish/Restart ...)" silently explodes into a chain of
    // nested submenus instead of one clickable item. Put details in comments
    // (here and in WireScene's own Debug.Log) instead of the menu string.
    // -------------------------------------------------------------------------

    [MenuItem("BeyondAllRoyal/1 - Setup Project Assets")]
    public static void SetupProjectAssets()
    {
        CreateScriptableObjects();
        CreatePrefabs();
        ImportAndAssignSprites();
        Debug.Log("[BeyondAllRoyal] Project assets set up. Run 2 - Wire Scene once the scene's GameObjects exist.");
    }

    // Populates the shop panel, backfills shop icons, creates the minimum-reserve
    // slider, adds Cancel/Demolish/Restart buttons, and populates the NPC's
    // production building pool.
    [MenuItem("BeyondAllRoyal/2 - Wire Scene")]
    public static void WireScene()
    {
        PopulateShopPanel();
        AutoWireShopIcons();
        CreateMinimumReserveSlider();
        CreateCancelPlacementButton();
        CreateDemolishButton();
        CreateRestartButton();
        AutoWireNPCBuildingTypes();
        Debug.Log("[BeyondAllRoyal] Scene wiring done. Reposition/style the new UI as needed, then save the scene.");
    }

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
        hq.attacksPerSecond            = 5f;
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
    // Step 2a — creates one button per player-placeable building type (skips any
    // BuildingData already present in shopEntries) and appends a wired ShopEntry
    // for each. Adds a GridLayoutGroup to the panel so buttons don't overlap.
    // Requires BuildingShopPanel in the open scene.
    // -------------------------------------------------------------------------

    // Every BuildingNames entry except HQ — it's pre-placed by MapGrid, never player-built.
    static string[] PlaceableBuildingNames => BuildingNames.Where(n => n != "HQ").ToArray();

    static void PopulateShopPanel()
    {
        var panel = Object.FindAnyObjectByType<BuildingShopPanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No BuildingShopPanel found in the open scene.");
            return;
        }

        if (panel.GetComponent<GridLayoutGroup>() == null)
        {
            var grid = panel.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(160f, 160f);
            grid.spacing  = new Vector2(12f, 12f);
        }

        var so = new SerializedObject(panel);
        var entries = so.FindProperty("shopEntries");

        var existingData = new HashSet<Object>();
        for (int i = 0; i < entries.arraySize; i++)
        {
            var d = entries.GetArrayElementAtIndex(i).FindPropertyRelative("data").objectReferenceValue;
            if (d != null) existingData.Add(d);
        }

        int added = 0;
        foreach (var name in PlaceableBuildingNames)
        {
            var data   = AssetDatabase.LoadAssetAtPath<BuildingData>($"{SOBuildings}/{name}.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBuildings}/{name}.prefab");
            if (data == null || prefab == null || existingData.Contains(data)) continue;

            var buttonGO = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonGO.name = $"{name}Button";
            buttonGO.transform.SetParent(panel.transform, false);

            // The default label child just says "Button" and would sit on top of
            // the building icon (the button's own Image, set by BuildingShopPanel
            // at runtime) — replace it with a small cost label instead.
            var defaultLabel = buttonGO.transform.Find("Text (Legacy)");
            if (defaultLabel != null) Object.DestroyImmediate(defaultLabel.gameObject);

            var labelGO = new GameObject("CostLabel", typeof(RectTransform));
            labelGO.transform.SetParent(buttonGO.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0.3f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize  = 20f;
            label.text      = $"{data.metalCostToBuild:F0}";

            int idx = entries.arraySize;
            entries.arraySize++;
            var entry = entries.GetArrayElementAtIndex(idx);
            entry.FindPropertyRelative("data").objectReferenceValue      = data;
            entry.FindPropertyRelative("prefab").objectReferenceValue    = prefab;
            entry.FindPropertyRelative("button").objectReferenceValue    = buttonGO.GetComponent<Button>();
            entry.FindPropertyRelative("icon").objectReferenceValue      = buttonGO.GetComponent<Image>();
            entry.FindPropertyRelative("costLabel").objectReferenceValue = label;

            added++;
        }

        so.ApplyModifiedProperties();

        if (added > 0)
        {
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Debug.Log($"[BeyondAllRoyal] Added {added} shop button(s). Icons populate from spriteFrameA at runtime. " +
                      "Resize the panel/grid cells as needed, then save the scene.");
        }
        else
        {
            Debug.Log("[BeyondAllRoyal] Nothing to add — every placeable building already has a shop entry.");
        }
    }

    // -------------------------------------------------------------------------
    // Step 2b — backfills the icon on any ShopEntry that doesn't have one yet
    // (PopulateShopPanel already sets it for entries it creates; this covers
    // entries added by hand). Requires BuildingShopPanel in the open scene.
    // -------------------------------------------------------------------------

    static void AutoWireShopIcons()
    {
        var panel = Object.FindAnyObjectByType<BuildingShopPanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No BuildingShopPanel found in the open scene.");
            return;
        }

        var so = new SerializedObject(panel);
        var entries = so.FindProperty("shopEntries");
        int wired = 0, skipped = 0;

        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            var iconProp = entry.FindPropertyRelative("icon");

            if (iconProp.objectReferenceValue != null) continue; // don't clobber a manual choice

            var button = entry.FindPropertyRelative("button").objectReferenceValue as Button;
            var image  = button != null ? button.GetComponent<Image>() : null;
            if (image == null) { skipped++; continue; }

            iconProp.objectReferenceValue = image;
            wired++;
        }

        so.ApplyModifiedProperties();

        if (wired > 0)
        {
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Debug.Log($"[BeyondAllRoyal] Wired {wired} shop icon(s) to their button's own Image component. " +
                      $"Save the scene (Ctrl+S) to persist this.{(skipped > 0 ? $" ({skipped} entries had no button/Image and were skipped.)" : "")}");
        }
        else
        {
            Debug.Log("[BeyondAllRoyal] Nothing to wire — icons already assigned, or entries have no button with an Image component.");
        }
    }

    // -------------------------------------------------------------------------
    // Step 2c — builds a default (unstyled) Slider + label under the same Canvas
    // as HUD and wires them into HUD.minimumReserveSlider/minimumReserveLabel.
    // Requires HUD in the open scene.
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

        if (sliderProp.objectReferenceValue != null)
        {
            Debug.Log("[BeyondAllRoyal] HUD already has a minimumReserveSlider assigned — skipping.");
            return;
        }

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

        var rect = sliderGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot     = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -100f);
        rect.sizeDelta = new Vector2(240f, 32f);

        var slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 500f;
        slider.value    = 50f;

        var labelGO = new GameObject("MinimumReserveLabel", typeof(RectTransform));
        labelGO.transform.SetParent(canvas.transform, false);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot     = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(276f, -100f);
        labelRect.sizeDelta = new Vector2(220f, 32f);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.fontSize = 20f;
        label.text = "Min Reserve: 50";

        sliderProp.objectReferenceValue = slider;
        labelProp.objectReferenceValue  = label;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Debug.Log("[BeyondAllRoyal] Created and wired the minimum-reserve slider (unstyled placeholder). " +
                  "Reposition/style it as needed, then save the scene.");
    }

    // -------------------------------------------------------------------------
    // Step 2d — adds a Cancel button as a child of HUD.placementInfoPanel, wired
    // to HUD.cancelPlacementButton. Touch devices have no Escape key or right
    // click, so BuildingPlacer.CancelPlacement() was otherwise unreachable on
    // mobile. Requires HUD (with placementInfoPanel assigned) in the open scene.
    // -------------------------------------------------------------------------

    static void CreateCancelPlacementButton()
    {
        CreateHudChildButton("cancelPlacementButton", "placementInfoPanel", "CancelPlacementButton", "Cancel",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 16f), new Vector2(130f, 50f));
    }

    // -------------------------------------------------------------------------
    // Step 2e — adds a Demolish button as a child of HUD.buildingInfoPanel, wired
    // to HUD.demolishButton. Lets the player free up a slot by voluntarily
    // destroying a building they own (HUD.OnDemolishClicked excludes the HQ, and
    // HQ.Demolish() refuses too, as a second line of defense).
    // Requires HUD (with buildingInfoPanel assigned) in the open scene.
    // -------------------------------------------------------------------------

    static void CreateDemolishButton()
    {
        CreateHudChildButton("demolishButton", "buildingInfoPanel", "DemolishButton", "Demolish",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 16f), new Vector2(140f, 50f));
    }

    // -------------------------------------------------------------------------
    // Step 2f — adds a Restart button as a child of HUD.endScreen, wired to
    // HUD.restartButton, which calls GameManager.RestartGame(). Also registers
    // the current scene in Build Settings, since SceneManager.LoadScene (used
    // by RestartGame) silently fails on a scene that isn't listed there.
    // Requires HUD (with endScreen assigned) in the open scene.
    // -------------------------------------------------------------------------

    static void CreateRestartButton()
    {
        EnsureSceneInBuildSettings();
        CreateHudChildButton("restartButton", "endScreen", "RestartButton", "Restart",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(220f, 64f));
    }

    static void EnsureSceneInBuildSettings()
    {
        var scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path)) return; // scene was never saved — nothing to register yet

        if (EditorBuildSettings.scenes.Any(s => s.path == scene.path)) return;

        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.Add(new EditorBuildSettingsScene(scene.path, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log($"[BeyondAllRoyal] Added '{scene.path}' to Build Settings (required for GameManager.RestartGame()'s scene reload).");
    }

    // -------------------------------------------------------------------------
    // Step 2g — populates NPCController.allProductionBuildingTypes with all 5
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

    // Shared by CreateCancelPlacementButton/CreateDemolishButton/CreateRestartButton:
    // creates a button as a child of the GameObject referenced by HUD's
    // parentFieldName, wires it into HUD's buttonFieldName, and skips if already assigned.
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

        if (buttonProp.objectReferenceValue != null)
        {
            Debug.Log($"[BeyondAllRoyal] HUD already has a {buttonFieldName} assigned — skipping.");
            return;
        }

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
        labelText.fontSize  = 22f;
        labelText.text      = label;

        buttonProp.objectReferenceValue = buttonGO.GetComponent<Button>();
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Debug.Log($"[BeyondAllRoyal] Created and wired a {label} button under {parentFieldName}. " +
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
