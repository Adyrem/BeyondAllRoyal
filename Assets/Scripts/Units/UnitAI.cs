using UnityEngine;

public class UnitAI : MonoBehaviour
{
    private Unit unit;
    private float attackCooldown;
    private Unit currentUnitTarget;

    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.InGame) return;

        attackCooldown -= Time.deltaTime;
        currentUnitTarget = FindNearestEnemyUnit();

        if (currentUnitTarget != null)
            HandleUnitCombat();
        else
            HandleBuildingAssault();
    }

    // -------------------------------------------------------------------------
    // Combat handlers
    // -------------------------------------------------------------------------

    private void HandleUnitCombat()
    {
        float dist = Vector2.Distance(transform.position, currentUnitTarget.transform.position);
        if (dist <= unit.AttackRange)
        {
            if (attackCooldown <= 0f) AttackUnit(currentUnitTarget);
        }
        else
        {
            MoveToward(currentUnitTarget.transform.position);
        }
    }

    // When no enemy units are visible, target the nearest enemy building.
    // Units will destroy towers and production buildings on the way to the HQ;
    // the HQ is naturally the last building standing.
    private void HandleBuildingAssault()
    {
        var target = FindNearestEnemyBuilding();
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.transform.position);
        if (dist <= unit.AttackRange)
        {
            if (attackCooldown <= 0f) AttackBuilding(target);
        }
        else
        {
            MoveToward(target.transform.position);
        }
    }

    private void AttackUnit(Unit target)
    {
        float multiplier = CounterSystem.GetDamageMultiplier(unit.EntityType, target.EntityType);
        target.TakeDamage(unit.Damage * multiplier);
        unit.FlashShootingSprite();
        attackCooldown = 1f / unit.AttacksPerSecond;
    }

    private void AttackBuilding(Building building)
    {
        float damage = unit.Damage;
        var entityType = building.GetEntityType();
        if (entityType.HasValue)
            damage *= CounterSystem.GetDamageMultiplier(unit.EntityType, entityType.Value);
        building.TakeDamage(damage);
        unit.FlashShootingSprite();
        attackCooldown = 1f / unit.AttacksPerSecond;
    }

    private void MoveToward(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        transform.position += dir * unit.MoveSpeed * Time.deltaTime;
    }

    // -------------------------------------------------------------------------
    // Target finding
    // -------------------------------------------------------------------------

    private Unit FindNearestEnemyUnit()
    {
        Unit nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var u in UnitRegistry.All)
        {
            if (u.Owner == unit.Owner || u == unit) continue;
            float dist = Vector2.Distance(transform.position, u.transform.position);
            if (dist < nearestDist) { nearest = u; nearestDist = dist; }
        }
        return nearest;
    }

    private Building FindNearestEnemyBuilding()
    {
        Building nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var b in BuildingRegistry.All)
        {
            if (b.Owner == unit.Owner) continue;
            float dist = Vector2.Distance(transform.position, b.transform.position);
            if (dist < nearestDist) { nearest = b; nearestDist = dist; }
        }
        return nearest;
    }
}
