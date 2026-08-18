using UnityEngine;

[CreateAssetMenu(fileName = "TowerData", menuName = "BeyondAllRoyal/Tower Data")]
public class TowerData : BuildingData
{
    public EntityType entityType;
    public float damage;
    public float attackRange;
    public float attacksPerSecond;
    public float energyCostPerShot;
    [Tooltip("Passive health regeneration, so a tower isn't permanently crippled by chip damage between fights.")]
    public float healthRegenPerSecond;
}
