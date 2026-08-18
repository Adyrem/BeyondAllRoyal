using System.Collections.Generic;
using UnityEngine;

public class MapGrid : MonoBehaviour
{
    public static MapGrid Instance { get; private set; }

    [SerializeField] private MapLayoutData layout;      // Medium / fallback if no per-difficulty override is assigned
    [SerializeField] private MapLayoutData easyLayout;  // optional smaller-map override for AIDifficulty.Easy
    [SerializeField] private MapLayoutData hardLayout;  // optional larger-map override for AIDifficulty.Hard
    [SerializeField] private Camera        gameCamera;
    [SerializeField] private GameObject    slotPrefab;
    [SerializeField] private GameObject    playerHQPrefab;
    [SerializeField] private GameObject    npcHQPrefab;

    public float SlotVisualSize { get; private set; }
    public bool  IsReady        { get; private set; }
    public int   Rows           => layout.rows;

    // Cached layout values set once in Start()
    private float halfH, halfW;
    private float xLeft, xSpacing;
    private float innerY, outerY;

    private readonly Dictionary<Vector2Int, BuildingSlot> slots = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Overwrites the serialized `layout` field itself (in memory only —
        // doesn't touch the asset) so every method below that already reads
        // `layout` picks up the difficulty-appropriate one for free, without
        // needing its own resolution logic. OnDrawGizmos runs in the Editor
        // before Start() has ever executed, so it still shows the Medium/
        // fallback layout there, which is the right thing to preview anyway.
        layout = ResolveLayout();

        var cam = gameCamera != null ? gameCamera : Camera.main;
        ComputeLayout(cam.orthographicSize, cam.aspect);
        GenerateSlots();
        PlaceHQs();
        IsReady = true;
    }

    // Easy plays on a smaller map, Hard on a larger one (see CLAUDE.md/NPC
    // section) — falls back to the Medium/default `layout` if no override is
    // assigned for the current difficulty (e.g. testing PlayScene directly
    // without going through the main menu, where GameSetup.Difficulty
    // defaults to Medium anyway).
    private MapLayoutData ResolveLayout()
    {
        return GameSetup.Difficulty switch
        {
            AIDifficulty.Easy => easyLayout != null ? easyLayout : layout,
            AIDifficulty.Hard => hardLayout != null ? hardLayout : layout,
            _                 => layout,
        };
    }

    // -------------------------------------------------------------------------
    // Layout computation
    // -------------------------------------------------------------------------

    private void ComputeLayout(float camHalfH, float camAspect)
    {
        halfH = camHalfH;
        halfW = camHalfH * camAspect;

        var m = ComputeGridLayout(camHalfH, camAspect, layout);
        xLeft          = m.xLeft;
        xSpacing       = m.xSpacing;
        innerY         = m.innerY;
        outerY         = m.outerY;
        SlotVisualSize = m.visualSlotSize;
    }

    // Result of the pure grid-math derivation below.
    private readonly struct GridLayoutMath
    {
        public readonly float xLeft, xRight, xSpacing, innerY, outerY, visualSlotSize;

        public GridLayoutMath(float xLeft, float xRight, float xSpacing, float innerY, float outerY, float visualSlotSize)
        {
            this.xLeft = xLeft;
            this.xRight = xRight;
            this.xSpacing = xSpacing;
            this.innerY = innerY;
            this.outerY = outerY;
            this.visualSlotSize = visualSlotSize;
        }
    }

    // Shared by ComputeLayout (Play Mode, via Start()) and OnDrawGizmos (Edit Mode,
    // which needs to draw before Start() has ever run) so the two can't silently
    // drift apart from independently-edited copies of the same formula.
    private static GridLayoutMath ComputeGridLayout(float camHalfH, float camAspect, MapLayoutData layout)
    {
        float halfW  = camHalfH * camAspect;
        float xRight = halfW * layout.widthFraction;
        float xLeft  = -xRight;
        float innerY = camHalfH * layout.innerFraction;
        float outerY = camHalfH * layout.outerFraction;

        float xs = layout.columns > 1 ? (xRight - xLeft) / (layout.columns - 1) : xRight - xLeft;
        float ys = layout.rows    > 1 ? (outerY - innerY) / (layout.rows    - 1) : outerY - innerY;
        float visualSlotSize = Mathf.Min(xs, ys) * 0.85f;

        return new GridLayoutMath(xLeft, xRight, xs, innerY, outerY, visualSlotSize);
    }

    // -------------------------------------------------------------------------
    // Slot generation
    // -------------------------------------------------------------------------

    private void GenerateSlots()
    {
        for (int row = 0; row < layout.rows; row++)
        for (int col = 0; col < layout.columns; col++)
        {
            float tx = layout.columns > 1 ? (float)col / (layout.columns - 1) : 0.5f;
            float ty = layout.rows    > 1 ? (float)row / (layout.rows    - 1) : 0f;
            float x  = Mathf.Lerp(xLeft, xLeft + xSpacing * (layout.columns - 1), tx);

            SpawnSlot(new Vector2Int(col, row),               Owner.Player, x, Mathf.Lerp(-innerY, -outerY, ty));
            SpawnSlot(new Vector2Int(col, layout.rows + row), Owner.NPC,    x, Mathf.Lerp( innerY,  outerY, ty));
        }
    }

    private void SpawnSlot(Vector2Int gridPos, Owner side, float x, float y)
    {
        var go   = Instantiate(slotPrefab, new Vector3(x, y, 0f), Quaternion.identity, transform);
        go.transform.localScale = new Vector3(SlotVisualSize, SlotVisualSize, 1f);
        var slot = go.GetComponent<BuildingSlot>();
        slot.GridPosition = gridPos;
        slot.Side         = side;
        slots[gridPos]    = slot;
    }

    // -------------------------------------------------------------------------
    // HQ placement — occupies the back rows of the regular grid
    // -------------------------------------------------------------------------

    private void PlaceHQs()
    {
        var prefab = playerHQPrefab != null ? playerHQPrefab : npcHQPrefab;
        if (prefab == null) return;

        int hqSize   = prefab.GetComponent<Building>().Data.slotSize.x;
        int startCol = (layout.columns - hqSize) / 2;
        int startRow = layout.rows - hqSize; // back rows of the regular grid

        // World position: centre of the HQ slot footprint
        float cx = layout.columns > 1 ? (startCol + (hqSize - 1) * 0.5f) / (layout.columns - 1) : 0.5f;
        float ry = layout.rows    > 1 ? (startRow + (hqSize - 1) * 0.5f) / (layout.rows    - 1) : 0.5f;

        float centerX       = Mathf.Lerp(xLeft, xLeft + xSpacing * (layout.columns - 1), cx);
        float playerCenterY = Mathf.Lerp(-innerY, -outerY, ry);
        float npcCenterY    = Mathf.Lerp( innerY,  outerY, ry);

        SpawnHQ(playerHQPrefab, new Vector3(centerX, playerCenterY, 0f), Owner.Player, new Vector2Int(startCol, startRow));
        SpawnHQ(npcHQPrefab,    new Vector3(centerX, npcCenterY,    0f), Owner.NPC,    new Vector2Int(startCol, layout.rows + startRow));
    }

    private void SpawnHQ(GameObject prefab, Vector3 pos, Owner owner, Vector2Int gridOrigin)
    {
        if (prefab == null) return;
        var go       = Instantiate(prefab, pos, Quaternion.identity);
        var building = go.GetComponent<Building>();
        if (building == null)
        {
            Debug.LogError($"[MapGrid] HQ prefab '{prefab.name}' has no Building component. Destroying it.");
            Destroy(go);
            return;
        }
        building.Initialize(owner);

        // A failure here means the match has no functioning win-condition
        // target for this side (GameManager.OnHQDestroyed could never fire
        // for it) — almost certainly a misconfigured map layout (grid too
        // small for the HQ's footprint), loud enough to not go unnoticed.
        if (!TryPlaceBuilding(building, gridOrigin))
        {
            Debug.LogError($"[MapGrid] Failed to place {owner}'s HQ at {gridOrigin} — map layout may be " +
                            "misconfigured (grid too small for the HQ's footprint, or the slot is already " +
                            "occupied). Destroying the unplaced HQ.");
            Destroy(go);
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public bool CanPlace(Vector2Int origin, Vector2Int size, Owner owner)
    {
        for (int x = origin.x; x < origin.x + size.x; x++)
        for (int y = origin.y; y < origin.y + size.y; y++)
        {
            var pos = new Vector2Int(x, y);
            if (!slots.TryGetValue(pos, out var slot)) return false;
            if (slot.IsOccupied || slot.Side != owner)  return false;
        }
        return true;
    }

    public bool TryPlaceBuilding(Building building, Vector2Int origin)
    {
        var size = building.Data.slotSize;
        if (!CanPlace(origin, size, building.Owner)) return false;

        for (int x = origin.x; x < origin.x + size.x; x++)
        for (int y = origin.y; y < origin.y + size.y; y++)
            slots[new Vector2Int(x, y)].Occupy(building);

        building.SetGridOrigin(origin);
        building.transform.position   = GetBuildingCenterPosition(origin, size);
        building.transform.localScale = new Vector3(size.x * SlotVisualSize, size.y * SlotVisualSize, 1f);
        return true;
    }

    public Vector3 GetBuildingCenterPosition(Vector2Int origin, Vector2Int size)
    {
        if (!slots.TryGetValue(origin, out var a)) return Vector3.zero;
        var far = new Vector2Int(origin.x + size.x - 1, origin.y + size.y - 1);
        if (!slots.TryGetValue(far, out var b)) return a.transform.position;
        return (a.transform.position + b.transform.position) * 0.5f;
    }

    // Converts a screen/world touch position to the grid origin for a footprint of
    // the given size, centered as closely as possible on worldPos (not anchored at
    // one corner), clamped to stay within the owner's rows and the map's columns.
    public Vector2Int GetPlacementOrigin(Vector3 worldPos, Owner owner, Vector2Int size)
    {
        float colF = (worldPos.x - xLeft) / xSpacing;

        float rowF;
        int rowMin, rowMax;
        if (owner == Owner.Player)
        {
            float ty = (worldPos.y + innerY) / (-outerY + innerY);
            rowF = ty * (layout.rows - 1);
            rowMin = 0;
            rowMax = layout.rows - size.y;
        }
        else
        {
            float ty = (worldPos.y - innerY) / (outerY - innerY);
            rowF = ty * (layout.rows - 1) + layout.rows;
            rowMin = layout.rows;
            rowMax = layout.rows * 2 - size.y;
        }

        int col = Mathf.RoundToInt(colF - (size.x - 1) / 2f);
        int row = Mathf.RoundToInt(rowF - (size.y - 1) / 2f);

        col = Mathf.Clamp(col, 0, layout.columns - size.x);
        row = Mathf.Clamp(row, rowMin, rowMax);

        return new Vector2Int(col, row);
    }

    // Only vacates slots this building actually occupies, rather than
    // blindly clearing whatever's at its GridOrigin — a building that never
    // successfully placed (TryPlaceBuilding returned false but the caller
    // kept it alive anyway) still defaults to GridOrigin (0,0), and without
    // this check, that building dying later would incorrectly vacate
    // whatever real building occupies (0,0).
    public void RemoveBuilding(Building building)
    {
        var origin = building.GridOrigin;
        var size   = building.Data.slotSize;
        for (int x = origin.x; x < origin.x + size.x; x++)
        for (int y = origin.y; y < origin.y + size.y; y++)
            if (slots.TryGetValue(new Vector2Int(x, y), out var slot) && slot.OccupyingBuilding == building)
                slot.Vacate();
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos) =>
        slots.TryGetValue(gridPos, out var s) ? s.transform.position : Vector3.zero;

    // True if worldPos lies on the given owner's half of the map (Player rows sit
    // at negative Y, NPC rows at positive Y — see GenerateSlots). Used by units to
    // detect an enemy incursion onto their own side.
    public bool IsOnSide(Vector3 worldPos, Owner owner) =>
        owner == Owner.Player ? worldPos.y < 0f : worldPos.y > 0f;

    // Finds the first unoccupied origin that fits the given size for the given owner.
    // Rows near the HQ (the back of the owner's row range) are searched first, so
    // auto-placed buildings fill in behind existing structures instead of exposing
    // themselves at the front line closest to the enemy.
    public bool TryGetFreeSlot(Vector2Int size, Owner owner, out Vector2Int origin)
    {
        int rowStart = owner == Owner.Player ? 0            : layout.rows;
        int rowEnd   = owner == Owner.Player ? layout.rows  : layout.rows * 2;

        for (int row = rowEnd - size.y; row >= rowStart; row--)
        for (int col = 0; col <= layout.columns - size.x; col++)
        {
            var candidate = new Vector2Int(col, row);
            if (CanPlace(candidate, size, owner))
            {
                origin = candidate;
                return true;
            }
        }
        origin = Vector2Int.zero;
        return false;
    }

    // -------------------------------------------------------------------------
    // Editor gizmos
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (layout == null || gameCamera == null) return;

        var m    = ComputeGridLayout(gameCamera.orthographicSize, gameCamera.aspect, layout);
        var cube = new Vector3(m.visualSlotSize, m.visualSlotSize, 0f);

        // Determine HQ footprint for highlight
        int hqSize   = 0;
        int hqStartC = 0;
        int hqStartR = 0;
        var hqPrefab = playerHQPrefab != null ? playerHQPrefab : npcHQPrefab;
        if (hqPrefab != null)
        {
            var b = hqPrefab.GetComponent<Building>();
            if (b?.Data != null)
            {
                hqSize   = b.Data.slotSize.x;
                hqStartC = (layout.columns - hqSize) / 2;
                hqStartR = layout.rows - hqSize;
            }
        }

        for (int row = 0; row < layout.rows; row++)
        for (int col = 0; col < layout.columns; col++)
        {
            float tx = layout.columns > 1 ? (float)col / (layout.columns - 1) : 0.5f;
            float ty = layout.rows    > 1 ? (float)row / (layout.rows    - 1) : 0f;
            float x  = Mathf.Lerp(m.xLeft, m.xRight, tx);

            bool isHQ = hqSize > 0
                && col >= hqStartC && col < hqStartC + hqSize
                && row >= hqStartR && row < hqStartR + hqSize;

            Gizmos.color = isHQ
                ? new Color(0.3f, 0.7f, 1.0f, 0.9f)
                : new Color(0.3f, 0.7f, 1.0f, 0.4f);
            Gizmos.DrawWireCube(new Vector3(x, Mathf.Lerp(-m.innerY, -m.outerY, ty), 0f), cube);

            Gizmos.color = isHQ
                ? new Color(1.0f, 0.4f, 0.4f, 0.9f)
                : new Color(1.0f, 0.4f, 0.4f, 0.4f);
            Gizmos.DrawWireCube(new Vector3(x, Mathf.Lerp( m.innerY,  m.outerY, ty), 0f), cube);
        }
    }
#endif
}
