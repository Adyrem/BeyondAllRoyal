using System.IO;
using UnityEditor;
using UnityEngine;

// Part of ProjectSetup (see ProjectSetup.cs) — everything behind
// "BeyondAllRoyal → 1 - Setup Project Assets": ScriptableObjects, prefabs,
// and sprite import/assignment.
public static partial class ProjectSetup
{
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

        // Towers — hp/damage bumped and energyCostPerShot cut well below what the earlier
        // values needed: at the shared passive trickle rate (1/sec, GameSettings.
        // buildingPassiveTrickleRate), the old costs (5 and 20) needed a nearby Tesla Tower
        // just to fire at any reasonable rate at all, which made towers feel unusably weak
        // in practice. Also given a slow passive health regen (healthRegenPerSecond) so
        // chip damage doesn't permanently cripple a tower between fights.
        //                 name                 entityType                        hp    dmg  range  atkSpd  nrgShot  slot              metalBuild  nrgBuild  buffer  regen
        CreateTowerData("MachinegunTurret", EntityType.MachinegunTurret, 300f, 22f, 5f, 2.0f, 2f, new Vector2Int(2,2),  60f,  40f,  80f, 3f);
        CreateTowerData("RailgunTurret",    EntityType.RailgunTurret,    450f, 85f, 8f, 0.5f, 8f, new Vector2Int(3,3), 100f,  80f, 150f, 4f);

        // Tesla Tower
        var tesla = CreateSO<TeslaTowerData>($"{SOBuildings}/TeslaTower.asset");
        tesla.buildingName          = "Tesla Tower";
        tesla.maxHealth             = 150f;
        tesla.metalCostToBuild      = 40f;
        tesla.energyCostToBuild     = 30f;
        tesla.energyBufferCapacity  = 50f;
        tesla.slotSize              = new Vector2Int(1, 1);
        tesla.injectionRatePerBuilding = 15f;
        tesla.injectionRange        = 5f;
        EditorUtility.SetDirty(tesla);

        // Metal Factory — metalPerSecond (baseline) + burstMetalAmount (on a full
        // 80-capacity energy buffer) are tuned so that being boosted by one Tesla
        // Tower (injectionRatePerBuilding 15/sec, on top of the 1/sec passive
        // trickle every building gets) reproduces the old flat 3/sec exactly:
        // buffer fills every 80/(1+15) = 5s, so 1 (baseline) + 10/5 (burst) = 3/sec.
        // Unsupported, it only has the 1/sec trickle to fill on (80s/burst), so a
        // lone factory drops to ~1.1/sec — Tesla support now meaningfully matters,
        // and stacks further with more towers in range (each adds another 15/sec
        // to the fill rate, so bursts come proportionally more often).
        var factory = CreateSO<MetalFactoryData>($"{SOBuildings}/MetalFactory.asset");
        factory.buildingName         = "Metal Factory";
        factory.maxHealth            = 150f;
        factory.metalCostToBuild     = 30f;
        factory.energyCostToBuild    = 40f;
        factory.energyBufferCapacity = 80f;
        factory.slotSize             = new Vector2Int(1, 1);
        factory.metalPerSecond       = 1f;
        factory.burstMetalAmount     = 10f;
        EditorUtility.SetDirty(factory);

        // HQ — energyCostToBuild = 0 so it starts pre-constructed
        var hq = CreateSO<HQData>($"{SOBuildings}/HQ.asset");
        hq.buildingName                = "HQ";
        hq.maxHealth                   = 1000f;
        hq.metalCostToBuild            = 0f;
        hq.energyCostToBuild           = 0f;
        hq.energyBufferCapacity        = 200f;
        hq.slotSize                    = new Vector2Int(3, 3);
        hq.metalPerSecond              = 10f; // sole source of passive metal income now — see ResourceManager.cs
        hq.injectionRatePerBuilding    = 5f;
        hq.injectionRange              = 7f;
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
        Vector2Int slot, float metalBuild, float nrgBuild, float buffer, float healthRegen)
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
        d.healthRegenPerSecond = healthRegen;
        EditorUtility.SetDirty(d);
    }

    // DefaultMap is the Medium/fallback layout, wired to MapGrid.layout in the Inspector.
    // SmallMap/LargeMap are optional per-difficulty overrides (MapGrid.easyLayout/
    // hardLayout — see MapGrid.ResolveLayout) — a smaller battlefield for Easy, a
    // larger one for Hard. All three share the same screen-fraction framing
    // (inner/outer/width) since MapGrid's grid math already packs however many
    // columns/rows into that same fixed screen space, so only column/row counts
    // need to differ between sizes.
    static void CreateDefaultMapLayout()
    {
        CreateMapLayout("DefaultMap", "Default (Two-Lane)", columns: 9, rows: 8);
        CreateMapLayout("SmallMap",   "Small (Easy)",        columns: 7, rows: 6);
        CreateMapLayout("LargeMap",   "Large (Hard)",        columns: 11, rows: 10);
    }

    static void CreateMapLayout(string assetName, string layoutName, int columns, int rows)
    {
        var layout = CreateSO<MapLayoutData>($"{SOMaps}/{assetName}.asset");
        layout.layoutName    = layoutName;
        layout.columns       = columns; // odd width so the HQ's 3-wide footprint centers exactly (see MapGrid.PlaceHQs)
        layout.rows          = rows;    // back 3 rows reserved for HQ, the rest free for buildings
        layout.innerFraction = 0.08f;
        layout.outerFraction = 0.78f;
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
