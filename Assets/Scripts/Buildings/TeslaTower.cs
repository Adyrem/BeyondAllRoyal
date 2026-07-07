using UnityEngine;

public class TeslaTower : Building
{
    private TeslaTowerData teslaData;

    protected override void Awake()
    {
        base.Awake();
        teslaData = RequireData<TeslaTowerData>();
    }

    protected override void Update()
    {
        base.Update();
        if (!IsConstructed || !IsGameActive) return;
        InjectEnergyIntoNearby(teslaData.injectionRatePerBuilding, teslaData.injectionRange);
    }
}
