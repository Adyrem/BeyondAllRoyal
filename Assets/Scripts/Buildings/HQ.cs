using UnityEngine;

public class HQ : Building
{
    private HQData hqData;

    protected override void Awake()
    {
        base.Awake();
        hqData = RequireData<HQData>();
    }

    private void Start()
    {
        GameManager.Instance.RegisterHQ(this, Owner);
    }

    protected override void Update()
    {
        base.Update();
        if (!IsConstructed || !IsGameActive) return;

        ResourceManager.Instance.AddMetal(Owner, hqData.metalPerSecond * Time.deltaTime);
        InjectEnergyIntoNearby(hqData.injectionRatePerBuilding, hqData.injectionRange);
    }

    protected override void OnDestroyed()
    {
        GameManager.Instance.OnHQDestroyed(Owner);
        base.OnDestroyed();
    }
}
