using UnityEngine;

public class DefenseTower : Building
{
    private TowerData towerData;
    private float attackCooldown;
    private Unit currentTarget;

    protected override void Awake()
    {
        base.Awake();
        towerData = RequireData<TowerData>();
    }

    public override EntityType? GetEntityType() => towerData.entityType;

    protected override void Update()
    {
        base.Update();
        if (!IsConstructed || !IsGameActive) return;

        if (towerData.healthRegenPerSecond > 0f)
            Heal(towerData.healthRegenPerSecond * Time.deltaTime);

        TickAutoAttack(towerData.attackRange, ref currentTarget, ref attackCooldown, TryFire);
    }

    private void TryFire()
    {
        if (!TryConsumeEnergy(towerData.energyCostPerShot)) return;

        float multiplier = CounterSystem.GetDamageMultiplier(towerData.entityType, currentTarget.EntityType);
        currentTarget.TakeDamage(towerData.damage * multiplier);
        AttackBeamSpawner.Spawn(transform.position, currentTarget.transform.position, Owner);
        attackCooldown = 1f / towerData.attacksPerSecond;
    }
}
