using UnityEngine;

[CreateAssetMenu(fileName = "HQData", menuName = "BeyondAllRoyal/HQ Data")]
public class HQData : BuildingData
{
    public float metalPerSecond;
    [Tooltip("Energy per second injected into each adjacent non-full building")]
    public float injectionRatePerBuilding;
    public float injectionRange;

    [Header("Self-defense")]
    [Tooltip("Lets the HQ fend off units that reach it, so a fast rush can't end the game instantly")]
    public float attackDamage;
    public float attackRange;
    public float attacksPerSecond;
    public float energyCostPerShot;
}
