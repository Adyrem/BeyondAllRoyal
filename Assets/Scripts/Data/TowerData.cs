using UnityEngine;

[CreateAssetMenu(fileName = "TowerData", menuName = "BeyondAllRoyal/Tower Data")]
public class TowerData : BuildingData
{
    public EntityType entityType;
    public float damage;
    public float attackRange;
    public float attacksPerSecond;
    public float energyCostPerShot;
}
