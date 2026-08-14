using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Called by ProjectSetup.SetupScenes() (BeyondAllRoyal → 2 - Setup Scenes).
// Duplicates PlayScene into a new TestScene (so it inherits all of PlayScene's
// manually-wired GameObjects/HUD/MapGrid references for free instead of
// rebuilding them from scratch) and adds a TestSceneBootstrap that pre-places
// a starter loadout of buildings for both sides once the match starts — handy
// for testing without building an economy up every time. Re-running this once
// TestScene already exists just refreshes the starter-building loadout below
// (e.g. after tuning StarterBuildingNames) rather than recreating the scene.
public static class TestSceneSetup
{
    private const string PlayScenePath = "Assets/Scenes/PlayScene.unity";
    private const string TestScenePath = "Assets/Scenes/TestScene.unity";
    private const string SOBuildings   = "Assets/ScriptableObjects/Buildings";
    private const string PrefabBuildings = "Assets/Prefabs/Buildings";

    // Heavy on economy (income + energy) so testing doesn't stall waiting on
    // metal, plus a couple of production buildings for some military presence.
    private static readonly string[] StarterBuildingNames =
    {
        "MetalFactory", "MetalFactory", "MetalFactory", "MetalFactory", "MetalFactory",
        "TeslaTower", "TeslaTower", "TeslaTower",
        "Barracks", "GunRange"
    };

    public static void CreateTestScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene;
        bool alreadyExisted = File.Exists(TestScenePath);

        if (alreadyExisted)
        {
            scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        }
        else
        {
            if (!File.Exists(PlayScenePath))
            {
                Debug.LogWarning($"[BeyondAllRoyal] '{PlayScenePath}' doesn't exist yet — set up and save PlayScene first.");
                return;
            }

            if (!AssetDatabase.CopyAsset(PlayScenePath, TestScenePath))
            {
                Debug.LogError($"[BeyondAllRoyal] Failed to copy '{PlayScenePath}' to '{TestScenePath}'.");
                return;
            }
            AssetDatabase.Refresh();

            scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        }

        var bootstrap = Object.FindAnyObjectByType<TestSceneBootstrap>(FindObjectsInactive.Include);
        if (bootstrap == null)
        {
            var bootstrapGO = new GameObject("TestSceneBootstrap");
            bootstrap = bootstrapGO.AddComponent<TestSceneBootstrap>();
        }

        int wired = WireStarterBuildings(bootstrap);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EnsureRegisteredInBuildSettings();

        string verb = alreadyExisted ? "Refreshed" : "Created";
        Debug.Log($"[BeyondAllRoyal] {verb} '{TestScenePath}' with {wired}/{StarterBuildingNames.Length} " +
                  "starter buildings wired. Open it directly and hit Play to test with both sides pre-stocked.");
    }

    // Always overwrites the bootstrap's array to exactly match
    // StarterBuildingNames, so shrinking/growing/reordering the list above and
    // re-running this menu item keeps the scene in sync.
    private static int WireStarterBuildings(TestSceneBootstrap bootstrap)
    {
        var so = new SerializedObject(bootstrap);
        var buildingsProp = so.FindProperty("starterBuildings");
        buildingsProp.arraySize = StarterBuildingNames.Length;

        int wired = 0;
        for (int i = 0; i < StarterBuildingNames.Length; i++)
        {
            var name    = StarterBuildingNames[i];
            var data    = AssetDatabase.LoadAssetAtPath<BuildingData>($"{SOBuildings}/{name}.asset");
            var prefab  = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBuildings}/{name}.prefab");
            var element = buildingsProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("data").objectReferenceValue   = data;
            element.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            if (data != null && prefab != null) wired++;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        return wired;
    }

    private static void EnsureRegisteredInBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(s => s.path != TestScenePath))
            scenes.Add(new EditorBuildSettingsScene(TestScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
