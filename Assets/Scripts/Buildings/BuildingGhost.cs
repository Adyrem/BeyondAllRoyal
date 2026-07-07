using UnityEngine;

// Attach to a scene GameObject with a SpriteRenderer.
// BuildingPlacer drives this to show a snapped placement preview.
[RequireComponent(typeof(SpriteRenderer))]
public class BuildingGhost : MonoBehaviour
{
    private static readonly Color ValidColor   = new Color(0f, 1f, 0f, 0.45f);
    private static readonly Color InvalidColor = new Color(1f, 0f, 0f, 0.45f);

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        gameObject.SetActive(false);
    }

    public void Show(BuildingData data)
    {
        gameObject.SetActive(true);
        UpdateScale(data);
    }

    public void Hide() => gameObject.SetActive(false);

    public void UpdateState(Vector3 worldPos, bool isValid, BuildingData data)
    {
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
