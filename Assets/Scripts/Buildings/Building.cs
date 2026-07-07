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
        currentHealth -= damage;
        healthBar?.SetFraction(HealthFraction);
        if (currentHealth <= 0f) OnDestroyed();
    }

    // Override in DefenseTower to expose the tower's EntityType for counter lookups
    public virtual EntityType? GetEntityType() => null;

    // Shared by TeslaTower and HQ: injects energy into nearby friendly buildings that aren't full.
    protected void InjectEnergyIntoNearby(float ratePerBuilding, float range)
    {
        foreach (var building in BuildingRegistry.All)
        {
            if (building == this || building.Owner != Owner) continue;
            if (building.EnergyBuffer >= building.EnergyBufferCapacity) continue;
            if (Vector2.Distance(transform.position, building.transform.position) > range) continue;

            building.AddEnergy(ratePerBuilding * Time.deltaTime);
        }
    }

    // Shared by DefenseTower and HQ: finds the nearest enemy unit within range.
    protected Unit FindNearestEnemyUnitInRange(float range)
    {
        Unit nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var u in UnitRegistry.All)
        {
            if (u.Owner == Owner) continue;
            float dist = Vector2.Distance(transform.position, u.transform.position);
            if (dist <= range && dist < nearestDist) { nearest = u; nearestDist = dist; }
        }
        return nearest;
    }

    protected bool IsWithinRange(Vector3 pos, float range) =>
        Vector2.Distance(transform.position, pos) <= range;

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

    protected virtual void OnDestroyed()
    {
        MapGrid.Instance?.RemoveBuilding(GridOrigin, data.slotSize);
        BuildingRegistry.Unregister(this);
        Destroy(gameObject);
    }
}
