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
}
