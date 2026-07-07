using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "BeyondAllRoyal/Unit Data")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public EntityType entityType;
    public float maxHealth;
    public float damage;
    public float attackRange;
    public float attacksPerSecond;
    public float moveSpeed;
    public float metalCostPerUnit;
    public float energyCostPerUnit;
    public GameObject prefab;

    [Header("Visuals")]
    public Sprite idleSprite;
    public Sprite shootingSprite;
}
