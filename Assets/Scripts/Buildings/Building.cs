using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Building : MonoBehaviour
{
    [SerializeField] protected BuildingData data;

    [SerializeField] private Owner owner;

    [SerializeField] private HealthBar healthBar;

    public Owner Owner => owner;
    public Vector2Int GridOrigin { get; private set; }
    public bool IsConstructed { get; protected set; }
    public float EnergyBuffer { get; private set; }
    public float EnergyBufferCapacity => data.energyBufferCapacity;
    public BuildingData Data => data;
    public float HealthFraction => data != null ? currentHealth / data.maxHealth : 1f;
    protected bool IsGameActive => GameManager.Instance?.CurrentState == GameState.InGame;

    private float currentHealth;
    private float energySpentOnConstruction;
    private bool isDestroyed;

    private SpriteRenderer spriteRenderer;
    private float spriteCycleTimer;
    private bool showingFrameB;

    protected virtual void Awake()
    {
        currentHealth = data.maxHealth;
        IsConstructed = data.energyCostToBuild <= 0f;
        energySpentOnConstruction = IsConstructed ? data.energyCostToBuild : 0f;
        BuildingRegistry.Register(this);

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && data.spriteFrameA != null)
            spriteRenderer.sprite = data.spriteFrameA;
    }

    // Called by ProductionBuilding when spawning a unit-produced building at runtime
    public void Initialize(Owner ownerValue)
    {
        owner = ownerValue;
    }

    // Called by MapGrid once the building is placed, so it can vacate its own slots on destruction
    public void SetGridOrigin(Vector2Int origin)
    {
        GridOrigin = origin;
    }

    protected virtual void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.InGame) return;

        AddEnergy(GameManager.Instance.Settings.buildingPassiveTrickleRate * Time.deltaTime);

        if (!IsConstructed)
            TickConstruction();

        TickSpriteCycle();
    }

    private void TickSpriteCycle()
    {
        if (spriteRenderer == null || data.spriteFrameA == null || data.spriteFrameB == null) return;

        spriteCycleTimer += Time.deltaTime;
        if (spriteCycleTimer < data.spriteCycleInterval) return;

        spriteCycleTimer = 0f;
        showingFrameB = !showingFrameB;
        spriteRenderer.sprite = showingFrameB ? data.spriteFrameB : data.spriteFrameA;
    }

    private void TickConstruction()
    {
        float needed = data.energyCostToBuild - energySpentOnConstruction;
        float use = Mathf.Min(EnergyBuffer, needed);
        EnergyBuffer -= use;
        energySpentOnConstruction += use;

        if (energySpentOnConstruction >= data.energyCostToBuild)
        {
            IsConstructed = true;
            OnConstructionComplete();
        }
    }

    public void AddEnergy(float amount)
    {
        EnergyBuffer = Mathf.Min(EnergyBuffer + amount, data.energyBufferCapacity);
    }

    public bool TryConsumeEnergy(float amount)
    {
        if (EnergyBuffer < amount) return false;
        EnergyBuffer -= amount;
        return true;
    }

    public void TakeDamage(float damage)
    {
        if (isDestroyed) return; // already dying — ignore further hits (e.g. from a chained explosion)

        currentHealth -= damage;
        healthBar?.SetFraction(HealthFraction);
        if (currentHealth <= 0f) DestroyBuilding();
    }

    // Override in DefenseTower to expose the tower's EntityType for counter lookups
    public virtual EntityType? GetEntityType() => null;

    // Squared 2D distance — matches Vector2.Distance's semantics (Z is
    // dropped) without its sqrt, which the comparisons below never actually
    // need since they only compare against another distance/range.
    private static float SqrDistance2D(Vector3 a, Vector3 b) => ((Vector2)a - (Vector2)b).sqrMagnitude;

    // Shared by TeslaTower and HQ: injects energy into nearby friendly buildings that aren't full.
    protected void InjectEnergyIntoNearby(float ratePerBuilding, float range)
    {
        float sqrRange = range * range;
        foreach (var building in BuildingRegistry.All)
        {
            if (building == this || building.Owner != Owner) continue;
            if (building.EnergyBuffer >= building.EnergyBufferCapacity) continue;
            if (SqrDistance2D(transform.position, building.transform.position) > sqrRange) continue;

            building.AddEnergy(ratePerBuilding * Time.deltaTime);
        }
    }

    // Shared by DefenseTower and HQ: finds the nearest enemy unit within range.
    protected Unit FindNearestEnemyUnitInRange(float range)
    {
        Unit nearest = null;
        float sqrRange = range * range;
        float nearestSqrDist = float.MaxValue;

        foreach (var u in UnitRegistry.All)
        {
            if (u.Owner == Owner) continue;
            float sqrDist = SqrDistance2D(transform.position, u.transform.position);
            if (sqrDist <= sqrRange && sqrDist < nearestSqrDist) { nearest = u; nearestSqrDist = sqrDist; }
        }
        return nearest;
    }

    protected bool IsWithinRange(Vector3 pos, float range) =>
        SqrDistance2D(transform.position, pos) <= range * range;

    // Shared by DefenseTower and HQ: re-targets the nearest enemy unit in range
    // if needed, then invokes fire() once the cooldown allows.
    protected void TickAutoAttack(float range, ref Unit currentTarget, ref float attackCooldown, System.Action fire)
    {
        attackCooldown -= Time.deltaTime;

        if (currentTarget == null || !IsWithinRange(currentTarget.transform.position, range))
            currentTarget = FindNearestEnemyUnitInRange(range);

        if (currentTarget != null && IsWithinRange(currentTarget.transform.position, range) && attackCooldown <= 0f)
            fire();
    }

    // Validates that the serialized data field is the expected subtype.
    // Disables the component and logs an error if the cast fails.
    protected T RequireData<T>() where T : BuildingData
    {
        var typed = data as T;
        if (typed == null)
        {
            Debug.LogError($"[{GetType().Name}] '{name}' expects a {typeof(T).Name} asset but got " +
                           $"{(data == null ? "null" : data.GetType().Name)}. Disabling component.", this);
            enabled = false;
        }
        return typed;
    }

    protected virtual void OnConstructionComplete() { }

    // Voluntarily removes this building, freeing its grid slot(s). Used by the
    // player's Demolish action; overridden by HQ to refuse, since the HQ can
    // only be lost in combat, not demolished by choice.
    public virtual void Demolish() => DestroyBuilding();

    // Single non-virtual entry point for both death paths (combat via
    // TakeDamage, voluntary via Demolish) so the isDestroyed guard applies
    // before OnDestroyed() — including subclass overrides like HQ's, which
    // run extra logic (GameManager.OnHQDestroyed) ahead of the base call —
    // rather than only guarding inside Building's own base implementation,
    // which a subclass's pre-base-call logic would still slip past. Mirrors
    // Unit's isDead guard against the same re-entrant-death class of bug
    // (e.g. a double-tapped Demolish, or a chained explosion's splash damage
    // hitting an already-dying building again before Destroy() has actually
    // removed it).
    private void DestroyBuilding()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        OnDestroyed();
    }

    protected virtual void OnDestroyed()
    {
        // Combat deaths (unlike a voluntary Demolish) don't go through HUD's own
        // deselect call, so without this the info panel would stay open showing
        // stale data for a building that no longer exists.
        if (BuildingSelector.Instance != null && BuildingSelector.Instance.SelectedBuilding == this)
            BuildingSelector.Instance.Deselect();

        MapGrid.Instance?.RemoveBuilding(this);
        BuildingRegistry.Unregister(this);
        Destroy(gameObject);
    }
}
