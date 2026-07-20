using UnityEngine;

public class ProductionBuilding : Building
{
    [SerializeField] private HealthBar productionBar;

    private ProductionBuildingData productionData;
    private float energySpentOnCurrentUnit;
    private bool metalReserved;

    public bool IsProducing { get; private set; } = true;
    public float UnitMetalCost => productionData.unitToProduced.metalCostPerUnit;

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
        if (!metalReserved)
        {
            if (!ResourceManager.Instance.TrySpendMetalAboveReserve(Owner, productionData.unitToProduced.metalCostPerUnit))
                return;
            metalReserved = true;
        }

        float needed = productionData.unitToProduced.energyCostPerUnit - energySpentOnCurrentUnit;
        float use = Mathf.Min(EnergyBuffer, needed);
        TryConsumeEnergy(use);
        energySpentOnCurrentUnit += use;

        float required = productionData.unitToProduced.energyCostPerUnit;

        if (energySpentOnCurrentUnit >= required)
        {
            SpawnUnit();
            energySpentOnCurrentUnit = 0f;
            metalReserved = false;
        }
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
