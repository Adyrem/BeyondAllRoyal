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
    // renders non-interactable buttons with a dimmed/disabled tint and blocks
    // their onClick, so this alone both greys out and disables selection for
    // anything the player can't currently afford.
    private void Update()
    {
        if (ResourceManager.Instance == null) return;
        float metal = ResourceManager.Instance.PlayerMetal;

        foreach (var entry in shopEntries)
        {
            if (entry.button == null || entry.data == null) continue;
            entry.button.interactable = metal >= entry.data.metalCostToBuild;
        }
    }
}
