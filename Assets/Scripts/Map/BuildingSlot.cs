using UnityEngine;

public class BuildingSlot : MonoBehaviour
{
    // Divisor for the stack-count-to-tint ramp below — chosen so at least 10
    // distinct overlap levels are visually distinguishable (1 stack should
    // read as a faint hint, not already-bright) before capping out. Slots
    // covered by more than this many overlapping injectors all look equally
    // maxed-out rather than continuing to intensify indefinitely.
    private const int   MaxStackForFullIntensity = 10;
    private static readonly Color EnergyCoverageColor = new Color(1f, 0.85f, 0.2f, 0.9f); // warm yellow, Tesla-ish

    public Vector2Int GridPosition { get; set; }
    public Owner Side { get; set; }
    public Building OccupyingBuilding { get; private set; }
    public bool IsOccupied => OccupyingBuilding != null;

    private SpriteRenderer spriteRenderer;
    private Color baseColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;
    }

    public void Occupy(Building building) => OccupyingBuilding = building;
    public void Vacate() => OccupyingBuilding = null;

    // Tints the slot to show how many energy injectors (HQ/Tesla Tower) reach
    // it — 0 clears back to the plain slot color, higher counts blend further
    // toward a warm "energy boosted" tint. Driven by BuildingPlacer while a
    // building is being placed (see BuildingPlacer.UpdateEnergyCoverage).
    public void SetEnergyCoverage(int stackCount)
    {
        if (spriteRenderer == null) return;

        if (stackCount <= 0)
        {
            spriteRenderer.color = baseColor;
            return;
        }

        float t = Mathf.Clamp01(stackCount / (float)MaxStackForFullIntensity);
        spriteRenderer.color = Color.Lerp(baseColor, EnergyCoverageColor, t);
    }
}
