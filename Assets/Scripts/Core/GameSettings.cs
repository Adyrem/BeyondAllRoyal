using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "BeyondAllRoyal/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Tooltip("Energy added to every building's buffer per second before Tesla Tower bonuses")]
    public float buildingPassiveTrickleRate = 1f;

    [Tooltip("Damage multiplier applied when attacker has a STRONG counter against the defender")]
    public float strongMultiplier = 1.5f;

    [Tooltip("Damage multiplier applied when attacker has a WEAK counter against the defender")]
    public float weakMultiplier = 0.5f;

    public CounterChartData counterChart;

    [Header("Unit Death Explosion")]
    [Tooltip("Explosion damage when a unit dies = the unit's own maxHealth * this fraction")]
    public float explosionDamageFraction = 0.3f;

    [Tooltip("Explosion radius when a unit dies = the unit's own maxHealth * this factor")]
    public float explosionRadiusPerHealth = 0.01f;

    [Tooltip("Sound effect played when a unit explodes on death")]
    public AudioClip explosionSfx;

    [Header("Unit Shoot Sound")]
    [Tooltip("Sound effect played whenever a unit fires (same clip for every unit type; pitch varies by size — see below)")]
    public AudioClip unitShootSfx;

    [Tooltip("Units at or below this maxHealth get the highest shoot pitch (shootPitchForMinHealth)")]
    public float shootPitchReferenceMinHealth = 50f;

    [Tooltip("Units at or above this maxHealth get the lowest shoot pitch (shootPitchForMaxHealth)")]
    public float shootPitchReferenceMaxHealth = 350f;

    [Tooltip("Pitch played for the smallest units (at or below shootPitchReferenceMinHealth)")]
    public float shootPitchForMinHealth = 1.3f;

    [Tooltip("Pitch played for the biggest units (at or above shootPitchReferenceMaxHealth)")]
    public float shootPitchForMaxHealth = 0.7f;
}
