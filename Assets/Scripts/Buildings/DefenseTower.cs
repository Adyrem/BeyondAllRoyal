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

        attackCooldown -= Time.deltaTime;

        if (currentTarget == null || !InRange(currentTarget.transform.position))
            currentTarget = FindNearestEnemyUnit();

        if (currentTarget != null && InRange(currentTarget.transform.position) && attackCooldown <= 0f)
            TryFire();
    }

    private void TryFire()
    {
        if (!TryConsumeEnergy(towerData.energyCostPerShot)) return;

        float multiplier = CounterSystem.GetDamageMultiplier(towerData.entityType, currentTarget.EntityType);
        currentTarget.TakeDamage(towerData.damage * multiplier);
        attackCooldown = 1f / towerData.attacksPerSecond;
    }

    private bool InRange(Vector3 pos) =>
        Vector2.Distance(transform.position, pos) <= towerData.attackRange;

    private Unit FindNearestEnemyUnit()
    {
        Unit nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var u in UnitRegistry.All)
        {
            if (u.Owner == Owner) continue;
            float dist = Vector2.Distance(transform.position, u.transform.position);
            if (dist <= towerData.attackRange && dist < nearestDist)
            {
                nearest = u;
                nearestDist = dist;
            }
        }
        return nearest;
    }
}
