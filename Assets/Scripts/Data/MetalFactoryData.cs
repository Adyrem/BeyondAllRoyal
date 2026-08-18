using UnityEngine;

[CreateAssetMenu(fileName = "MetalFactoryData", menuName = "BeyondAllRoyal/Metal Factory Data")]
public class MetalFactoryData : BuildingData
{
    [Tooltip("Passive baseline income, added every frame regardless of the energy buffer")]
    public float metalPerSecond;
    [Tooltip("Metal granted in one go whenever the energy buffer (energyBufferCapacity) fills — " +
             "the buffer is drained back to 0 immediately after. Tesla Tower support fills the " +
             "buffer faster, so a supported factory bursts more often than one running on the " +
             "shared passive trickle alone.")]
    public float burstMetalAmount;
}
