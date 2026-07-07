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

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        healthBar?.SetFraction(HealthFraction);
        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        UnitRegistry.Unregister(this);
        Destroy(gameObject);
    }
}
