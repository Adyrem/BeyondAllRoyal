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

        // The passive trickle every building gets (Building.Update) fills this
        // buffer even though a plain MetalFactory otherwise never spends
        // energy on anything — burst on full instead of leaving it sitting
        // there unused, so a nearby Tesla Tower (which fills the buffer
        // faster) meaningfully increases how often a factory bursts.
        if (EnergyBuffer >= EnergyBufferCapacity && TryConsumeEnergy(EnergyBufferCapacity))
            ResourceManager.Instance.AddMetal(Owner, factoryData.burstMetalAmount);
    }
}
