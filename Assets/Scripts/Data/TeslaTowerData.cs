using UnityEngine;

[CreateAssetMenu(fileName = "TeslaTowerData", menuName = "BeyondAllRoyal/Tesla Tower Data")]
public class TeslaTowerData : BuildingData
{
    [Tooltip("Energy per second injected into each adjacent non-full building")]
    public float injectionRatePerBuilding;
    [Tooltip("World-unit radius within which buildings receive energy injection")]
    public float injectionRange;
}
