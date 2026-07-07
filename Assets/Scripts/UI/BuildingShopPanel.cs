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
        public Button       button;       // assign the UI Button in Inspector
        public Image        icon;         // optional icon Image on the button; set to data.spriteFrameA
        public TextMeshProUGUI costLabel; // optional cost label on the button
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
                entry.icon.sprite = d.spriteFrameA;

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
}
