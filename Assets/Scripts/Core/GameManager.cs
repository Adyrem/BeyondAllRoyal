using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameSettings settings;

    public GameSettings Settings => settings;
    public GameState CurrentState { get; private set; } = GameState.Pregame;

    private HQ playerHQ;
    private HQ npcHQ;

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

    public void RegisterHQ(HQ hq, Owner owner)
    {
        if (owner == Owner.Player) playerHQ = hq;
        else npcHQ = hq;
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
}
