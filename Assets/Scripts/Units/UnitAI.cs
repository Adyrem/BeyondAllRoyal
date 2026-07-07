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

        var nearestBuilding = FindNearestEnemyBuilding();
        var nearestUnit     = FindNearestEnemyUnit();

        bool buildingInRange = nearestBuilding != null && InRange(nearestBuilding.transform.position);
        bool unitInRange     = nearestUnit != null && InRange(nearestUnit.transform.position);

        // Priority: buildings in range > units in range > buildings out of range > units out of range.
        if (buildingInRange)
        {
            HandleBuildingAssault(nearestBuilding);
        }
        else if (unitInRange)
        {
            currentUnitTarget = nearestUnit;
            HandleUnitCombat();
        }
        else if (nearestBuilding != null)
        {
            HandleBuildingAssault(nearestBuilding);
        }
        else if (nearestUnit != null)
        {
            currentUnitTarget = nearestUnit;
            HandleUnitCombat();
        }
    }

    private bool InRange(Vector3 pos) => Vector2.Distance(transform.position, pos) <= unit.AttackRange;

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

    private void HandleBuildingAssault(Building target)
    {
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
        AttackBeamSpawner.Spawn(transform.position, target.transform.position, unit.Owner);
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
        AttackBeamSpawner.Spawn(transform.position, building.transform.position, unit.Owner);
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
