using UnityEngine;

[CreateAssetMenu(fileName = "HQData", menuName = "BeyondAllRoyal/HQ Data")]
public class HQData : BuildingData
{
    public float metalPerSecond;
    [Tooltip("Energy per second injected into each adjacent non-full building")]
    public float injectionRatePerBuilding;
    public float injectionRange;
}
