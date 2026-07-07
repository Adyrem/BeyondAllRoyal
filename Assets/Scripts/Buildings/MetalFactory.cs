using UnityEngine;

public class MetalFactory : Building
{
    private MetalFactoryData factoryData;

    protected override void Awake()
    {
        base.Awake();
        factoryData = RequireData<MetalFactoryData>();
    }

    protected override void Update()
    {
        base.Update();
        if (!IsConstructed || !IsGameActive) return;
        ResourceManager.Instance.AddMetal(Owner, factoryData.metalPerSecond * Time.deltaTime);
    }
}
