using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
//
// Split across three files as a partial class, since it had grown to over
// 1300 lines in one file: this file holds just the two menu entry points and
// the constants/name lists shared across the other two —
// ProjectSetup.Assets.cs (Step 1 — ScriptableObjects/prefabs/sprites) and
// ProjectSetup.SceneWiring.cs (Step 2 — PlayScene's HUD/shop panel/buttons).
public static partial class ProjectSetup
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
    // slider, adds Cancel/Demolish/Pause-Resume-Production/Main Menu buttons,
    // the Pause button + pause panel (Resume/Main Menu), and populates the
    // NPC's production building pool. Requires PlayScene's GameManager/HUD/
    // MapGrid/BuildingShopPanel/NPCController to already exist in the open scene.
    private static void WireScene()
    {
        PopulateShopPanel();
        CreateMinimumReserveSlider();
        CreateCancelPlacementButton();
        CreateDemolishButton();
        CreateToggleProductionButton();
        CreateMainMenuButton();
        CreatePauseButton();
        CreatePausePanel();
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
}
