using UnityEngine;

public class TeslaTower : Building, IEnergyInjector
{
    private TeslaTowerData teslaData;

    public float InjectionRange => teslaData.injectionRange;

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
