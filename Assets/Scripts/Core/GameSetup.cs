// Carries the player's MainMenu selections across the scene load into PlayScene.
// A plain static class (like BuildingRegistry/UnitRegistry) rather than a
// DontDestroyOnLoad singleton, since it only needs to survive the one scene
// transition, not persist indefinitely. Defaults let PlayScene be tested
// directly in the Editor without going through MainMenu first.
public static class GameSetup
{
    public static GameMode Mode { get; set; } = GameMode.Singleplayer;
    public static AIDifficulty Difficulty { get; set; } = AIDifficulty.Medium;
}
