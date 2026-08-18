using System.Collections.Generic;
using UnityEngine;

// Attach to a scene GameObject (e.g. "BuildingPlacer").
// Call SelectBuilding() from the building shop UI to begin placement mode.
// The player then touches and drags to preview a slot, releasing to place it
// there — there's no such thing as "hovering" without touching on mobile.
public class BuildingPlacer : MonoBehaviour
{
    public static BuildingPlacer Instance { get; private set; }

    [SerializeField] private BuildingGhost ghost;

    public bool IsPlacing => selectedData != null;

    private BuildingData selectedData;
    private GameObject   selectedPrefab;
    private Camera       cam;

    // True from the moment a building is selected (from the shop) until the
    // player begins a brand-new press. The release that triggered the shop
    // button's own click is otherwise indistinguishable from a "place here"
    // release arriving on the very same frame — without this guard, selecting
    // a building from the shop would instantly place it whatever the release
    // happened to be over, since closing the shop panel (part of that same
    // click) removes the UI element that would otherwise have blocked it.
    private bool awaitingFreshPress;

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
        awaitingFreshPress = true;
        HUD.Instance?.ShowPlacementInfo(data);
    }

    public void CancelPlacement()
    {
        selectedData   = null;
        selectedPrefab = null;
        ghost.Hide();
        HUD.Instance?.HidePlacementInfo();
        ClearEnergyCoverage();
    }

    private void Update()
    {
        if (selectedData == null || MapGrid.Instance == null || !MapGrid.Instance.IsReady) return;
        // The pause overlay's own full-screen raycast blocker already stops
        // this via TapHitInteractiveUI() below in the normal case, but that
        // depends on HUD's pausePanel being wired correctly — this is an
        // explicit, code-visible guarantee that doesn't rely on UI wiring.
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        if (InputHelper.CancelPressed())
        {
            CancelPlacement();
            return;
        }

        if (awaitingFreshPress)
        {
            ghost.Hide();
            UpdateEnergyCoverage(null);
            if (InputHelper.TapBegan()) awaitingFreshPress = false;
            return;
        }

        // The ghost only exists while a finger/mouse button is actually down.
        // Previously it updated every frame regardless, and InputHelper.TapPosition()
        // falls back to screen (0,0) when nothing is pressed and there's no mouse
        // hardware (the normal case on mobile) — so between touches the ghost
        // snapped to the bottom-left slot instead of just not being there.
        bool released = InputHelper.TapEnded();
        if (!InputHelper.IsPressed() && !released)
        {
            ghost.Hide();
            UpdateEnergyCoverage(null);
            return;
        }

        // A press/release over an interactive UI element (Cancel button, shop
        // toggle, reserve slider, ...) is never a world-space placement
        // attempt, no matter what world position happens to be underneath it —
        // checked before the enemy-side test below, since HUD elements are
        // screen-anchored and can easily land over the NPC's half of the map
        // in a vertical 2-lane layout (e.g. anything docked near the top).
        // Skips this touch only; the current selection is left alone so the
        // player can keep dragging afterward.
        if (InputHelper.TapHitInteractiveUI())
        {
            ghost.Hide();
            UpdateEnergyCoverage(null);
            return;
        }

        Vector3 worldPos = InputHelper.ScreenToWorld(cam, InputHelper.TapPosition());

        // The player's own side only — dragging onto the enemy's half doesn't
        // show a ghost at all, and releasing there just cancels the whole
        // placement instead of silently doing nothing.
        if (!MapGrid.Instance.IsOnSide(worldPos, Owner.Player))
        {
            ghost.Hide();
            UpdateEnergyCoverage(null);
            if (released) CancelPlacement();
            return;
        }

        Vector2Int gridPos  = MapGrid.Instance.GetPlacementOrigin(worldPos, Owner.Player, selectedData.slotSize);
        bool       valid    = MapGrid.Instance.CanPlace(gridPos, selectedData.slotSize, Owner.Player);
        Vector3    snapPos  = MapGrid.Instance.GetBuildingCenterPosition(gridPos, selectedData.slotSize);

        ghost.UpdateState(snapPos, valid, selectedData);
        UpdateEnergyCoverage(snapPos);

        if (valid && released)
            Place(gridPos);
    }

    // Tints every one of the player's own slots to show how many energy
    // injectors (HQ, Tesla Tower) currently reach it, so placing a new Tesla
    // Tower can be planned to actually extend coverage instead of stacking on
    // top of what's already there. Recomputed every frame while placement is
    // active (cheap enough — this only runs during the relatively rare
    // placement flow, not the main game loop) so a dragged ghost's own
    // prospective range is reflected live via ghostPos.
    private void UpdateEnergyCoverage(Vector3? ghostPos)
    {
        var injectors = new List<(Vector3 pos, float range)>();
        foreach (var b in BuildingRegistry.All)
        {
            if (b.Owner != Owner.Player) continue;
            if (b is IEnergyInjector injector)
                injectors.Add((b.transform.position, injector.InjectionRange));
        }

        // The building currently being placed counts too, if it's itself an
        // injector (only Tesla Tower is player-placeable right now), so the
        // player sees the resulting coverage before committing to a spot.
        if (ghostPos.HasValue && selectedData is TeslaTowerData teslaData)
            injectors.Add((ghostPos.Value, teslaData.injectionRange));

        foreach (var slot in MapGrid.Instance.GetSlotsForOwner(Owner.Player))
        {
            int count = 0;
            foreach (var (pos, range) in injectors)
                if (((Vector2)slot.transform.position - (Vector2)pos).sqrMagnitude <= range * range)
                    count++;
            slot.SetEnergyCoverage(count);
        }
    }

    private void ClearEnergyCoverage()
    {
        if (MapGrid.Instance == null) return;
        foreach (var slot in MapGrid.Instance.GetSlotsForOwner(Owner.Player))
            slot.SetEnergyCoverage(0);
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
}
