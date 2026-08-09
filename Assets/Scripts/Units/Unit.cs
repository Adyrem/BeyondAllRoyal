using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitAI))]
[RequireComponent(typeof(SpriteRenderer))]
public class Unit : MonoBehaviour
{
    private const float ShootSpriteDuration = 0.15f;

    [SerializeField] private UnitData data;
    [SerializeField] private HealthBar healthBar;

    private float currentHealth;
    private SpriteRenderer spriteRenderer;
    private float shootSpriteTimer;
    private bool isDead;

    public Owner Owner { get; private set; }
    public EntityType EntityType => data.entityType;
    public float AttackRange => data.attackRange;
    public float AttacksPerSecond => data.attacksPerSecond;
    public float MoveSpeed => data.moveSpeed;
    public float Damage => data.damage;
    public float HealthFraction => data != null ? currentHealth / data.maxHealth : 1f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(UnitData unitData, Owner owner)
    {
        data = unitData;
        Owner = owner;
        currentHealth = data.maxHealth;
        UnitRegistry.Register(this);

        if (spriteRenderer != null && data.idleSprite != null)
            spriteRenderer.sprite = data.idleSprite;
    }

    private void Update()
    {
        if (shootSpriteTimer <= 0f) return;

        shootSpriteTimer -= Time.deltaTime;
        if (shootSpriteTimer <= 0f && spriteRenderer != null && data.idleSprite != null)
            spriteRenderer.sprite = data.idleSprite;
    }

    // Called by UnitAI whenever this unit fires, to briefly show the shooting sprite
    public void FlashShootingSprite()
    {
        if (spriteRenderer == null || data.shootingSprite == null) return;
        spriteRenderer.sprite = data.shootingSprite;
        shootSpriteTimer = ShootSpriteDuration;
    }

    // Called by UnitAI whenever this unit fires. Same clip for every unit,
    // but pitched by this unit's own maxHealth so bigger units sound deeper
    // and smaller ones sound higher — same "based on maxHealth" pattern as
    // the death explosion's damage/radius.
    public void PlayShootSfx()
    {
        var settings = GameManager.Instance.Settings;
        float t = Mathf.InverseLerp(settings.shootPitchReferenceMinHealth, settings.shootPitchReferenceMaxHealth, data.maxHealth);
        float pitch = Mathf.Lerp(settings.shootPitchForMinHealth, settings.shootPitchForMaxHealth, t);
        ShootSfxSpawner.Play(transform.position, settings.unitShootSfx, pitch);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return; // already dying — ignore further hits (e.g. from a chained explosion)

        currentHealth -= amount;
        healthBar?.SetFraction(HealthFraction);
        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        // Explode() can deal damage to other units close enough to explode in
        // turn, which can loop back and hit this same unit again before
        // Destroy(gameObject) has actually removed it — without this guard
        // that re-enters Die() -> Explode() -> ... indefinitely and crashes
        // Unity with a stack overflow instead of just ending the match.
        if (isDead) return;
        isDead = true;

        Explode();
        UnitRegistry.Unregister(this);
        Destroy(gameObject);
    }

    // Splash damage on death: hits units on BOTH sides (friendly and enemy)
    // but only enemy buildings, so a unit's death can't damage its own team's
    // structures. Flat damage, no counter multiplier — consistent with HQ's
    // self-defense attack (neither is part of the EntityType counter chart).
    private void Explode()
    {
        var settings = GameManager.Instance.Settings;
        float damage = data.maxHealth * settings.explosionDamageFraction;
        float radius = data.maxHealth * settings.explosionRadiusPerHealth;
        if (damage <= 0f || radius <= 0f) return;

        // Snapshot both registries first — TakeDamage below can kill other
        // units/buildings, which unregister themselves and would otherwise
        // mutate the very lists we're iterating (e.g. a chain of explosions).
        foreach (var u in new List<Unit>(UnitRegistry.All))
        {
            if (u == this) continue;
            if (Vector2.Distance(transform.position, u.transform.position) <= radius)
                u.TakeDamage(damage);
        }

        foreach (var b in new List<Building>(BuildingRegistry.All))
        {
            if (b.Owner == Owner) continue; // only enemy buildings take explosion damage
            if (Vector2.Distance(transform.position, b.transform.position) <= radius)
                b.TakeDamage(damage);
        }

        ExplosionSpawner.Spawn(transform.position, radius, settings.explosionSfx);
    }
}
