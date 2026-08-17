using UnityEngine;

// Attach to the scene alongside BuildingPlacer.
// Handles tapping an already-placed building to select it and show production controls.
// Requires building prefabs to have a BoxCollider2D (added by ProjectSetup).
public class BuildingSelector : MonoBehaviour
{
    public static BuildingSelector Instance { get; private set; }

    public Building SelectedBuilding { get; private set; }

    public delegate void SelectionChanged(Building building);
    public event SelectionChanged OnSelectionChanged;

    private Camera cam;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        cam = Camera.main;
    }

    private void Update()
    {
        if (!InputHelper.TapBegan()) return;

        // Don't steal input from BuildingPlacer when it's active
        if (BuildingPlacer.Instance != null && BuildingPlacer.Instance.IsPlacing) return;

        // Belt-and-suspenders alongside the pause overlay's own full-screen
        // raycast blocker (see BuildingPlacer.Update) — an explicit guard
        // that doesn't depend on HUD's pausePanel being wired correctly.
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        // A tap that hits an interactive UI element (e.g. the Demolish/Pause
        // Production buttons) must not also be treated as a world-space tap, or
        // this would immediately deselect the very building those buttons just
        // acted on. A tap on a passive panel background still reaches here, so
        // tapping near an open info panel to deselect still works.
        if (InputHelper.TapHitInteractiveUI()) return;

        Vector3 worldPos = InputHelper.ScreenToWorld(cam, InputHelper.TapPosition());
        var hit = Physics2D.OverlapPoint(worldPos);

        if (hit != null)
        {
            var building = hit.GetComponent<Building>();
            if (building != null && building.Owner == Owner.Player)
            {
                Select(building);
                return;
            }
        }

        Deselect();
    }

    public void Select(Building building)
    {
        SelectedBuilding = building;
        OnSelectionChanged?.Invoke(building);
    }

    public void Deselect()
    {
        SelectedBuilding = null;
        OnSelectionChanged?.Invoke(null);
    }
}
