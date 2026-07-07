using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "BeyondAllRoyal/Building Data")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public float maxHealth;
    public float metalCostToBuild;
    [Tooltip("Energy that must accumulate in the buffer before construction completes. 0 = starts pre-built.")]
    public float energyCostToBuild;
    public Vector2Int slotSize;
    public float energyBufferCapacity;
    public GameObject prefab;

    [Header("Visuals")]
    [Tooltip("Also used as the build-menu icon")]
    public Sprite spriteFrameA;
    public Sprite spriteFrameB;
    [Tooltip("Seconds between swapping spriteFrameA and spriteFrameB")]
    public float spriteCycleInterval = 1f;
}
