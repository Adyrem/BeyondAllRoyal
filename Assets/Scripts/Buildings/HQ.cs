using UnityEngine;

public class HQ : Building
{
    private HQData hqData;
    private float attackCooldown;
    private Unit currentTarget;

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

        // The HQ defends itself with a flat-damage attack (no counter multiplier —
        // it isn't part of the EntityType counter chart) so a quick rush can't end
        // the game before the player gets any real defenses up.
        TickAutoAttack(hqData.attackRange, ref currentTarget, ref attackCooldown, TryFire);
    }

    private void TryFire()
    {
        if (!TryConsumeEnergy(hqData.energyCostPerShot)) return;

        currentTarget.TakeDamage(hqData.attackDamage);
        AttackBeamSpawner.Spawn(transform.position, currentTarget.transform.position, Owner);
        attackCooldown = 1f / hqData.attacksPerSecond;
    }

    protected override void OnDestroyed()
    {
        GameManager.Instance.OnHQDestroyed(Owner);
        base.OnDestroyed();
    }

    public override void Demolish()
    {
        Debug.LogWarning($"[HQ] '{name}': the HQ cannot be demolished.");
    }
}
