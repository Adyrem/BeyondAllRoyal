using UnityEngine;

public class UnitAI : MonoBehaviour
{
    // Deciding "who's the best target" is a full O(n) scan over every enemy
    // unit/building — doing that every single frame for every unit scales
    // badly once a battle has many units on screen. Retargeting on this
    // interval instead (each unit's timer phase-shifted randomly at spawn so
    // they don't all rescan on the same frame) is imperceptible to the
    // player, since "no micro required" autonomous units don't need
    // frame-perfect target switches, but cuts the scanning cost roughly
    // 9x at 60fps. Movement and attacking against whatever the last scan
    // picked still run every frame in Act(), so combat itself stays fully
    // responsive — only the target *selection* is throttled, and an early
    // rescan still fires immediately if the current target dies.
    private const float RetargetInterval = 0.15f;

    private enum TargetKind { None, Unit, Building }

    private Unit unit;
    private float attackCooldown;
    private float retargetTimer;

    private TargetKind targetKind;
    private Unit     currentUnitTarget;
    private Building currentBuildingTarget;

    private void Awake()
    {
        unit = GetComponent<Unit>();
        retargetTimer = Random.Range(0f, RetargetInterval);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.InGame) return;

        attackCooldown -= Time.deltaTime;
        retargetTimer  -= Time.deltaTime;

        if (retargetTimer <= 0f || !HasValidTarget())
        {
            retargetTimer = RetargetInterval;
            PickTarget();
        }

        Act();
    }

    private bool HasValidTarget() => targetKind switch
    {
        TargetKind.Unit     => currentUnitTarget != null,
        TargetKind.Building => currentBuildingTarget != null,
        _                   => false,
    };

    // -------------------------------------------------------------------------
    // Target selection (staggered — see RetargetInterval)
    // -------------------------------------------------------------------------

    private void PickTarget()
    {
        // Defending home turf comes first: an enemy unit that has pushed onto our
        // own side of the map takes priority over everything else below — but
        // only pursued if it's ahead (or already in range). A home intruder
        // that's behind is left alone rather than chased, consistent with
        // units never moving backward — see IsAheadOrLevel and the ahead-only
        // fallback searches below.
        var nearestIntruder = FindNearestEnemyUnit(u => MapGrid.Instance.IsOnSide(u.transform.position, unit.Owner));
        if (nearestIntruder != null &&
            (InRange(nearestIntruder.transform.position) || IsAheadOrLevel(nearestIntruder.transform.position)))
        {
            targetKind = TargetKind.Unit;
            currentUnitTarget = nearestIntruder;
            return;
        }

        var nearestBuilding = FindNearestEnemyBuilding();
        var nearestUnit     = FindNearestEnemyUnit();

        bool buildingInRange = nearestBuilding != null && InRange(nearestBuilding.transform.position);
        bool unitInRange     = nearestUnit != null && InRange(nearestUnit.transform.position);

        // Priority: home intruders > buildings in range > units in range > nearest building ahead > nearest unit ahead.
        // A target already in range can always be fought regardless of direction
        // (see HandleUnitCombat/HandleBuildingAssault) — the "ahead" restriction
        // only applies to picking something to chase down while out of range, so
        // a closer enemy that's behind is skipped in favor of a farther one that's
        // actually reachable without reversing course.
        if (buildingInRange)
        {
            targetKind = TargetKind.Building;
            currentBuildingTarget = nearestBuilding;
            return;
        }
        if (unitInRange)
        {
            targetKind = TargetKind.Unit;
            currentUnitTarget = nearestUnit;
            return;
        }

        var buildingAhead = FindNearestEnemyBuilding(b => IsAheadOrLevel(b.transform.position));
        if (buildingAhead != null)
        {
            targetKind = TargetKind.Building;
            currentBuildingTarget = buildingAhead;
            return;
        }

        var unitAhead = FindNearestEnemyUnit(u => IsAheadOrLevel(u.transform.position));
        if (unitAhead != null)
        {
            targetKind = TargetKind.Unit;
            currentUnitTarget = unitAhead;
            return;
        }

        targetKind = TargetKind.None;
    }

    private bool InRange(Vector3 pos) => SqrDistance2D(transform.position, pos) <= unit.AttackRange * unit.AttackRange;

    // True if pos doesn't require moving backward to reach — Player units
    // advance toward +Y (the NPC's side), NPC units toward -Y. Units should
    // only ever push forward; something behind can still be fought if it's
    // already in range, but is never picked as something to chase.
    private bool IsAheadOrLevel(Vector3 pos)
    {
        float forward = unit.Owner == Owner.Player ? 1f : -1f;
        return (pos.y - transform.position.y) * forward >= 0f;
    }

    private static float SqrDistance2D(Vector3 a, Vector3 b) => ((Vector2)a - (Vector2)b).sqrMagnitude;

    // -------------------------------------------------------------------------
    // Acting on the current target (every frame)
    // -------------------------------------------------------------------------

    private void Act()
    {
        switch (targetKind)
        {
            case TargetKind.Unit:
                HandleUnitCombat();
                break;
            case TargetKind.Building:
                HandleBuildingAssault(currentBuildingTarget);
                break;
        }
    }

    private void HandleUnitCombat()
    {
        float sqrDist = SqrDistance2D(transform.position, currentUnitTarget.transform.position);
        if (sqrDist <= unit.AttackRange * unit.AttackRange)
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
        float sqrDist = SqrDistance2D(transform.position, target.transform.position);
        float range   = unit.AttackRange;

        if (sqrDist <= range * range)
        {
            if (attackCooldown <= 0f) AttackBuilding(target);

            // Buildings don't move, so keep pressing the attack between shots
            // instead of just holding at the range boundary, until right up
            // against the building's edge.
            float stoppingDist = BuildingStoppingDistance(target);
            if (sqrDist > stoppingDist * stoppingDist)
                MoveToward(target.transform.position);
        }
        else
        {
            MoveToward(target.transform.position);
        }
    }

    // Approximates the building's footprint edge as a circle, so units stop
    // just outside it instead of walking into/through its sprite.
    private float BuildingStoppingDistance(Building target)
    {
        var size = target.Data.slotSize;
        return Mathf.Max(size.x, size.y) * MapGrid.Instance.SlotVisualSize * 0.5f;
    }

    private void AttackUnit(Unit target)
    {
        float multiplier = CounterSystem.GetDamageMultiplier(unit.EntityType, target.EntityType);
        target.TakeDamage(unit.Damage * multiplier);
        unit.FlashShootingSprite();
        unit.PlayShootSfx();
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
        unit.PlayShootSfx();
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

    private Unit FindNearestEnemyUnit(System.Func<Unit, bool> filter = null)
    {
        Unit nearest = null;
        float nearestSqrDist = float.MaxValue;
        foreach (var u in UnitRegistry.All)
        {
            if (u.Owner == unit.Owner || u == unit) continue;
            if (filter != null && !filter(u)) continue;
            float sqrDist = SqrDistance2D(transform.position, u.transform.position);
            if (sqrDist < nearestSqrDist) { nearest = u; nearestSqrDist = sqrDist; }
        }
        return nearest;
    }

    private Building FindNearestEnemyBuilding(System.Func<Building, bool> filter = null)
    {
        Building nearest = null;
        float nearestSqrDist = float.MaxValue;
        foreach (var b in BuildingRegistry.All)
        {
            if (b.Owner == unit.Owner) continue;
            if (filter != null && !filter(b)) continue;
            float sqrDist = SqrDistance2D(transform.position, b.transform.position);
            if (sqrDist < nearestSqrDist) { nearest = b; nearestSqrDist = sqrDist; }
        }
        return nearest;
    }
}
