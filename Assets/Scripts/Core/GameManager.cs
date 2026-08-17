using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameSettings settings;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public GameSettings Settings => settings;
    public GameState CurrentState { get; private set; } = GameState.Pregame;
    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Defensive: Time.timeScale is a global engine static that survives
        // scene loads within the same session, so a match that starts mid-way
        // through a previous match's pause (e.g. jumping straight into
        // PlayScene/TestScene in the Editor) doesn't start frozen.
        Time.timeScale = 1f;
        SoundSettings.Apply();
    }

    // Every gameplay system (units, buildings, NPC, effects) drives its own
    // timing off Time.deltaTime, so zeroing timeScale freezes the whole
    // simulation for free instead of needing an IsPaused check threaded
    // through each one individually. UI (button clicks) is unaffected, since
    // uGUI's input handling doesn't run on scaled time.
    public void Pause()
    {
        if (CurrentState != GameState.InGame || IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
    }

    private IEnumerator Start()
    {
        // Wait until MapGrid has finished generating slots and placing HQs
        yield return new WaitUntil(() => MapGrid.Instance != null && MapGrid.Instance.IsReady);
        // One additional frame for NPCController's own Start coroutine to run
        yield return null;
        StartGame();
    }

    public void StartGame()
    {
        CurrentState = GameState.InGame;
    }

    public void OnHQDestroyed(Owner destroyedOwner)
    {
        CurrentState = destroyedOwner == Owner.Player ? GameState.Defeat : GameState.Victory;
        HUD.Instance?.ShowEndScreen(CurrentState);
    }

    // Loading MainMenu resets every MonoBehaviour singleton in this scene
    // (GameManager, ResourceManager, MapGrid, HUD, ...) for free since none are
    // marked DontDestroyOnLoad. The two plain-static registries aren't scene
    // objects though, so they need clearing explicitly or stale entries from
    // this match would linger into the next one.
    public void ReturnToMainMenu()
    {
        // Time.timeScale persists across the scene load otherwise, which
        // would leave MainMenu's own UI/animations frozen if this was called
        // while paused.
        Time.timeScale = 1f;
        BuildingRegistry.Reset();
        UnitRegistry.Reset();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
