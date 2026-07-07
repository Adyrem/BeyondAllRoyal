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
        if (!IsConstructed || !IsProducing || !IsGameActive) return;
        TickProduction();
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
        productionBar?.SetFraction(energySpentOnCurrentUnit / required);

        if (energySpentOnCurrentUnit >= required)
        {
            SpawnUnit();
            energySpentOnCurrentUnit = 0f;
            metalReserved = false;
            productionBar?.SetFraction(0f);
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
