using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach to the shop panel GameObject.
// Populate shopEntries in the Inspector with one entry per placeable building.
// Each button calls BuildingPlacer.SelectBuilding when tapped.
public class BuildingShopPanel : MonoBehaviour
{
    [System.Serializable]
    public struct ShopEntry
    {
        public BuildingData data;
        public GameObject   prefab;
        public Button       button;        // assign the UI Button in Inspector
        public Image        icon;          // optional icon Image; set to data.spriteFrameA
        public TextMeshProUGUI nameLabel;  // optional building-name label
        public TextMeshProUGUI costLabel;  // optional cost label
    }

    // Applied to the icon when a building can't currently be afforded, on top
    // of the button's own dimmed background (UITheme.ApplyButtonColors) —
    // that alone read as too subtle against the shop row's already-dark
    // background, so icon opacity and text color shift too, making
    // affordability obvious at a glance instead of a faint tint change.
    private const float UnaffordableIconAlpha = 0.35f;

    [SerializeField] private ShopEntry[] shopEntries;

    private void Start()
    {
        foreach (var entry in shopEntries)
        {
            if (entry.button == null) continue;

            var d = entry.data;
            var p = entry.prefab;

            if (entry.icon != null)
            {
                entry.icon.sprite = d.spriteFrameA;

                // The generated sprite itself is a colorless badge shape — in
                // the world, each building's own color comes from its
                // prefab's SpriteRenderer tint, not the sprite pixels. Read
                // that same tint here so the shop icon matches instead of
                // rendering plain white.
                var prefabRenderer = p != null ? p.GetComponent<SpriteRenderer>() : null;
                if (prefabRenderer != null)
                    entry.icon.color = prefabRenderer.color;
            }

            if (entry.nameLabel != null)
                entry.nameLabel.text = d.buildingName;

            if (entry.costLabel != null)
                entry.costLabel.text = $"{d.metalCostToBuild:F0}";

            entry.button.onClick.AddListener(() =>
            {
                if (BuildingPlacer.Instance.IsPlacing)
                {
                    BuildingPlacer.Instance.CancelPlacement();
                }
                else
                {
                    BuildingPlacer.Instance.SelectBuilding(d, p);
                    // Close the shop so it doesn't block the view while placing on the map.
                    gameObject.SetActive(false);
                }
            });
        }
    }

    // Only runs while the panel is active (i.e. the shop is open), so this
    // doesn't cost anything the rest of the time. Unity's Button already
    // blocks onClick and dims its background while non-interactable, and
    // SetAffordabilityVisuals below layers icon/text changes on top so
    // affordability reads clearly at a glance.
    private void Update()
    {
        if (ResourceManager.Instance == null) return;
        float metal = ResourceManager.Instance.PlayerMetal;

        foreach (var entry in shopEntries)
        {
            if (entry.button == null || entry.data == null) continue;
            bool affordable = metal >= entry.data.metalCostToBuild;
            entry.button.interactable = affordable;
            SetAffordabilityVisuals(entry, affordable);
        }
    }

    private static void SetAffordabilityVisuals(ShopEntry entry, bool affordable)
    {
        if (entry.icon != null)
        {
            // Preserve the building's own tint (see Start above) — only alpha changes.
            var c = entry.icon.color;
            c.a = affordable ? 1f : UnaffordableIconAlpha;
            entry.icon.color = c;
        }

        if (entry.nameLabel != null)
            entry.nameLabel.color = affordable ? UITheme.Text : UITheme.DisabledText;

        if (entry.costLabel != null)
            entry.costLabel.color = affordable ? UITheme.MutedText : UITheme.Warning;
    }
}
