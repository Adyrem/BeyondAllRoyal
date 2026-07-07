using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [SerializeField] private float playerMetalPerSecond = 5f;
    [SerializeField] private float npcMetalPerSecond = 5f;

    [Tooltip("Shared minimum metal reserve, adjustable via the HUD slider. Used as a floor by the " +
             "NPC's own building-placement reserve, and enforced against unit-production spending " +
             "for both sides via TrySpendMetalAboveReserve.")]
    [SerializeField] private float minimumMetalReserve = 50f;

    private float playerMetal;
    private float npcMetal;

    public float PlayerMetal => playerMetal;
    public float NPCMetal => npcMetal;
    public float MinimumMetalReserve => minimumMetalReserve;

    public void SetMinimumMetalReserve(float value)
    {
        minimumMetalReserve = Mathf.Max(0f, value);
    }

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

    // Like TrySpendMetal, but also refuses to spend if doing so would drop the
    // owner's metal below minimumMetalReserve. Used for unit production so the
    // reserve slider actually holds back ongoing spending, not just new builds.
    public bool TrySpendMetalAboveReserve(Owner owner, float amount)
    {
        float current = owner == Owner.Player ? playerMetal : npcMetal;
        if (current - amount < minimumMetalReserve) return false;
        return TrySpendMetal(owner, amount);
    }

    public void AddMetal(Owner owner, float amount)
    {
        if (owner == Owner.Player) playerMetal += amount;
        else npcMetal += amount;
    }
}
