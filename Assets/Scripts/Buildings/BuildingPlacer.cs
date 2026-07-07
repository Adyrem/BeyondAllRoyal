using UnityEngine;

// Attach to a scene GameObject (e.g. "BuildingPlacer").
// Call SelectBuilding() from the building shop UI to begin placement mode.
// The player then taps a valid slot to place the building.
public class BuildingPlacer : MonoBehaviour
{
    public static BuildingPlacer Instance { get; private set; }

    [SerializeField] private BuildingGhost ghost;

    public bool IsPlacing => selectedData != null;

    private BuildingData selectedData;
    private GameObject   selectedPrefab;
    private Camera       cam;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        cam = Camera.main;
    }

    public void SelectBuilding(BuildingData data, GameObject prefab)
    {
        selectedData   = data;
        selectedPrefab = prefab;
        ghost.Show(data);
        HUD.Instance?.ShowPlacementInfo(data);
    }

    public void CancelPlacement()
    {
        selectedData   = null;
        selectedPrefab = null;
        ghost.Hide();
        HUD.Instance?.HidePlacementInfo();
    }

    private void Update()
    {
        if (selectedData == null || !MapGrid.Instance.IsReady) return;

        if (InputHelper.CancelPressed())
        {
            CancelPlacement();
            return;
        }

        Vector3    worldPos = ScreenToWorld(InputHelper.TapPosition());
        Vector2Int gridPos  = MapGrid.Instance.GetPlacementOrigin(worldPos, Owner.Player, selectedData.slotSize);
        bool       valid    = MapGrid.Instance.CanPlace(gridPos, selectedData.slotSize, Owner.Player);
        Vector3    snapPos  = MapGrid.Instance.GetBuildingCenterPosition(gridPos, selectedData.slotSize);

        ghost.UpdateState(snapPos, valid, selectedData);

        if (valid && InputHelper.TapBegan())
            Place(gridPos);
    }

    private void Place(Vector2Int gridPos)
    {
        if (!ResourceManager.Instance.TrySpendMetal(Owner.Player, selectedData.metalCostToBuild))
            return;

        var go       = Instantiate(selectedPrefab);
        var building = go.GetComponent<Building>();

        if (building == null)
        {
            Debug.LogError($"[BuildingPlacer] Prefab '{selectedPrefab.name}' has no Building component.");
            Destroy(go);
            ResourceManager.Instance.AddMetal(Owner.Player, selectedData.metalCostToBuild);
            return;
        }

        building.Initialize(Owner.Player);

        if (!MapGrid.Instance.TryPlaceBuilding(building, gridPos))
        {
            ResourceManager.Instance.AddMetal(Owner.Player, selectedData.metalCostToBuild);
            Destroy(go);
            return;
        }

        CancelPlacement();
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        var pos = new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z);
        return cam.ScreenToWorldPoint(pos);
    }
}
