using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [SerializeField] private float playerMetalPerSecond = 5f;
    [SerializeField] private float npcMetalPerSecond = 5f;

    private float playerMetal;
    private float npcMetal;

    public float PlayerMetal => playerMetal;
    public float NPCMetal => npcMetal;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.InGame) return;
        playerMetal += playerMetalPerSecond * Time.deltaTime;
        npcMetal += npcMetalPerSecond * Time.deltaTime;
    }

    public bool TrySpendMetal(Owner owner, float amount)
    {
        if (owner == Owner.Player)
        {
            if (playerMetal < amount) return false;
            playerMetal -= amount;
        }
        else
        {
            if (npcMetal < amount) return false;
            npcMetal -= amount;
        }
        return true;
    }

    public void AddMetal(Owner owner, float amount)
    {
        if (owner == Owner.Player) playerMetal += amount;
        else npcMetal += amount;
    }
}
