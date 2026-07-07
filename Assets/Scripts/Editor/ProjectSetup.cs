using System.IO;
using UnityEditor;
using UnityEngine;

// Run from the Unity menu: BeyondAllRoyal → 1 - Create ScriptableObjects, then → 2 - Create Prefabs
public static class ProjectSetup
{
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
    static readonly string[] BuildingNames = {
        "Barracks", "GunRange", "Laboratory", "SkimmerPad", "IronWorks",
        "MachinegunTurret", "RailgunTurret", "TeslaTower", "MetalFactory", "HQ"
    };

    // -------------------------------------------------------------------------
    // Step 1
    // -------------------------------------------------------------------------

    [MenuItem("BeyondAllRoyal/1 - Create ScriptableObjects")]
    public static void CreateScriptableObjects()
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
        CreateUnitData(EntityType.Soldier,             "Soldier",              50f,  10f, 1.5f, 2.0f, 2.5f, 20f,  10f);
        CreateUnitData(EntityType.HeavyGunner,         "HeavyGunner",         120f,  18f, 4.0f, 1.5f, 1.5f, 50f,  25f);
        CreateUnitData(EntityType.ExplosiveSpecialist, "ExplosiveSpecialist", 100f,  35f, 3.5f, 0.5f, 2.5f, 50f,  25f);
        CreateUnitData(EntityType.Hovercraft,          "Hovercraft",          200f,  25f, 2.5f, 1.0f, 4.0f, 100f, 50f);
        CreateUnitData(EntityType.HeavyTank,           "HeavyTank",           350f,  40f, 4.5f, 0.5f, 1.5f, 100f, 50f);

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
        hq.slotSize                    = new Vector2Int(5, 5);
        hq.metalPerSecond              = 2f;
        hq.injectionRatePerBuilding    = 5f;
        hq.injectionRange              = 8f;
        EditorUtility.SetDirty(hq);

        CreateDefaultMapLayout();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BeyondAllRoyal] ScriptableObjects created. Run step 2 to create prefabs.");
    }

    // -------------------------------------------------------------------------
    // Step 2
    // -------------------------------------------------------------------------

    [MenuItem("BeyondAllRoyal/2 - Create Prefabs (run step 1 first)")]
    public static void CreatePrefabs()
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
    // Step 3 — assumes gen_sprites.py (or equivalent) has already written the
    // PNGs to Assets/Sprites/Units and Assets/Sprites/Buildings.
    // -------------------------------------------------------------------------

    [MenuItem("BeyondAllRoyal/3 - Import Sprites and Assign to Data + Prefabs")]
    public static void ImportAndAssignSprites()
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
        layout.columns       = 8;
        layout.rows          = 8; // back 5 rows reserved for HQ, front 3 rows free for buildings
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
        string[] names = { "Soldier", "HeavyGunner", "ExplosiveSpecialist", "Hovercraft", "HeavyTank" };
        foreach (var name in names)
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
        => AddBar(parent, sprite, yOffset, Color.green, "HealthBar");

    static HealthBar AddProgressBar(GameObject parent, Sprite sprite, float yOffset)
        => AddBar(parent, sprite, yOffset, new Color(0.2f, 0.6f, 1f), "ProductionBar");

    static HealthBar AddBar(GameObject parent, Sprite sprite, float yOffset, Color fillColor, string barName)
    {
        // Background
        var bg = new GameObject(barName);
        bg.transform.SetParent(parent.transform, false);
        bg.transform.localPosition = new Vector3(0f, yOffset, 0f);
        bg.transform.localScale    = new Vector3(0.9f, 0.08f, 1f);
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
        so.ApplyModifiedPropertiesWithoutUndo();

        return hb;
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
