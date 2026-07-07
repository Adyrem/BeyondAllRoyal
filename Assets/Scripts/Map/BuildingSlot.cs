using UnityEngine;

public class BuildingSlot : MonoBehaviour
{
    public Vector2Int GridPosition { get; set; }
    public Owner Side { get; set; }
    public Building OccupyingBuilding { get; private set; }
    public bool IsOccupied => OccupyingBuilding != null;

    public void Occupy(Building building) => OccupyingBuilding = building;
    public void Vacate() => OccupyingBuilding = null;
}
