using UnityEngine;

// Attach to a scene GameObject with a SpriteRenderer.
// BuildingPlacer drives this to show a snapped placement preview.
[RequireComponent(typeof(SpriteRenderer))]
public class BuildingGhost : MonoBehaviour
{
    private static readonly Color ValidColor   = new Color(0f, 1f, 0f, 0.45f);
    private static readonly Color InvalidColor = new Color(1f, 0f, 0f, 0.45f);

    private static Sprite fullBleedSprite;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Force a full-bleed solid sprite (not whatever sprite happened to be
        // assigned in the Inspector — the building/unit icon sprites are "badge"
        // style with a lot of transparent padding, which made the ghost look
        // tiny relative to its actual footprint) and Simple draw mode, and
        // detach from any scene parent, so the footprint scaling below always
        // maps directly to world-space size regardless of scene setup.
        sr.sprite     = GetFullBleedSprite();
        sr.drawMode   = SpriteDrawMode.Simple;
        transform.SetParent(null, true);

        gameObject.SetActive(false);
    }

    // A 1x1 white sprite built from Unity's built-in white texture — no asset
    // or Inspector wiring required, and guaranteed to have zero padding.
    private static Sprite GetFullBleedSprite()
    {
        if (fullBleedSprite == null)
        {
            var tex = Texture2D.whiteTexture;
            fullBleedSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }
        return fullBleedSprite;
    }

    public void Hide() => gameObject.SetActive(false);

    // Also (re)activates the ghost — callers don't need a separate Show(),
    // since BuildingPlacer only ever wants a state update paired with the
    // ghost becoming visible.
    public void UpdateState(Vector3 worldPos, bool isValid, BuildingData data)
    {
        gameObject.SetActive(true);
        transform.position = worldPos;
        sr.color           = isValid ? ValidColor : InvalidColor;
        UpdateScale(data);
    }

    private void UpdateScale(BuildingData data)
    {
        if (MapGrid.Instance == null) return;
        float s = MapGrid.Instance.SlotVisualSize;
        transform.localScale = new Vector3(data.slotSize.x * s, data.slotSize.y * s, 1f);
    }
}
