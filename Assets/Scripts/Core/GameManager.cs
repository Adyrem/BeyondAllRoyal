using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameSettings settings;

    public GameSettings Settings => settings;
    public GameState CurrentState { get; private set; } = GameState.Pregame;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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

    // Reloading the scene resets every MonoBehaviour singleton (GameManager,
    // ResourceManager, MapGrid, HUD, ...) for free since none are marked
    // DontDestroyOnLoad. The two plain-static registries aren't scene objects
    // though, so they need clearing explicitly or stale entries would linger.
    public void RestartGame()
    {
        BuildingRegistry.Reset();
        UnitRegistry.Reset();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
