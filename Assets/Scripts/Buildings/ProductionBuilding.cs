using UnityEngine;

public class ProductionBuilding : Building
{
    [SerializeField] private HealthBar productionBar;

    private ProductionBuildingData productionData;

    public bool IsProducing { get; private set; } = true;
    public float UnitMetalCost => productionData.unitToProduced.metalCostPerUnit;
    public EntityType ProducedEntityType => productionData.unitToProduced.entityType;

    public void SetProducing(bool producing) => IsProducing = producing;

    protected override void Awake()
    {
        base.Awake();
        productionData = RequireData<ProductionBuildingData>();
    }

    protected override void Update()
    {
        base.Update();
        if (!IsGameActive) return;

        // Reflects the live energy buffer from the moment the building exists —
        // including during construction, which spends from the same buffer —
        // not just once producing, so it doesn't sit frozen at a stale default
        // and then jump the instant construction happens to finish.
        UpdateProductionBar();

        if (IsConstructed && IsProducing)
            TickProduction();
    }

    private void UpdateProductionBar()
    {
        if (productionBar == null || EnergyBufferCapacity <= 0f) return;
        productionBar.SetFraction(EnergyBuffer / EnergyBufferCapacity);
        productionBar.SetIndicator(productionData.unitToProduced.energyCostPerUnit / EnergyBufferCapacity);
    }

    private void TickProduction()
    {
        // Let the buffer accumulate from the passive trickle (Building.Update)
        // up to the per-unit threshold before spending anything — previously
        // this drained EnergyBuffer by a little bit every single frame as soon
        // as it arrived, which kept the buffer (and its progress bar) pinned
        // near zero the whole time instead of visibly filling toward the
        // Indicator tick.
        float required = productionData.unitToProduced.energyCostPerUnit;
        if (EnergyBuffer < required) return;

        // Metal is only spent once a unit is actually ready to spawn, not
        // reserved the moment production starts — if the pool can't afford it
        // right now, production just stalls with a full energy buffer (this
        // ticks again next frame, so it spawns the instant metal frees up)
        // instead of reserving metal early and losing it with no refund path
        // if the building is demolished or paused before it fires.
        if (!ResourceManager.Instance.TrySpendMetalAboveReserve(Owner, productionData.unitToProduced.metalCostPerUnit))
            return;

        TryConsumeEnergy(required);
        SpawnUnit();
    }

    private void SpawnUnit()
    {
        var unitData = productionData.unitToProduced;
        if (unitData.prefab == null)
        {
            Debug.LogError($"[ProductionBuilding] '{name}': UnitData '{unitData.name}' has no prefab assigned.");
            return;
        }
        var go = Instantiate(unitData.prefab);
        go.GetComponent<Unit>().Initialize(unitData, Owner);

        // Spawn at the edge of the building that faces the map centre so units
        // don't overlap the building sprite before moving away.
        float halfH   = productionData.slotSize.y * MapGrid.Instance.SlotVisualSize * 0.5f;
        float offsetY = Owner == Owner.Player ? halfH : -halfH;
        go.transform.position = transform.position + new Vector3(0f, offsetY, 0f);
    }
}
