using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [Header("Resource display")]
    [SerializeField] private TextMeshProUGUI metalText;

    [Header("End screen")]
    [SerializeField] private GameObject      endScreen;
    [SerializeField] private TextMeshProUGUI endScreenText;

    [Header("Building shop")]
    [SerializeField] private BuildingShopPanel shopPanel;
    [SerializeField] private Button            shopToggleButton;

    [Header("Production control")]
    [SerializeField] private GameObject      productionPanel;
    [SerializeField] private TextMeshProUGUI productionBuildingName;
    [SerializeField] private Button          toggleProductionButton;
    [SerializeField] private TextMeshProUGUI toggleProductionLabel;

    [Header("Placement info")]
    [SerializeField] private GameObject      placementInfoPanel;
    [SerializeField] private TextMeshProUGUI placementBuildingName;
    [SerializeField] private TextMeshProUGUI placementCostText;

    [Header("Selected building info")]
    [SerializeField] private GameObject      buildingInfoPanel;
    [SerializeField] private TextMeshProUGUI buildingInfoName;
    [SerializeField] private Slider          energyBar;
    [SerializeField] private TextMeshProUGUI energyLabel;

    [Header("Minimum metal reserve")]
    [SerializeField] private Slider          minimumReserveSlider;
    [SerializeField] private TextMeshProUGUI minimumReserveLabel;

    private ProductionBuilding selectedProduction;
    private Building           selectedBuilding;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        endScreen?.SetActive(false);
        productionPanel?.SetActive(false);
        if (buildingInfoPanel  != null) buildingInfoPanel.SetActive(false);
        if (placementInfoPanel != null) placementInfoPanel.SetActive(false);
        if (shopPanel != null) shopPanel.gameObject.SetActive(false);

        toggleProductionButton?.onClick.AddListener(OnToggleProduction);
        shopToggleButton?.onClick.AddListener(ToggleShop);
        minimumReserveSlider?.onValueChanged.AddListener(OnMinimumReserveChanged);
    }

    private void Start()
    {
        if (BuildingSelector.Instance != null)
            BuildingSelector.Instance.OnSelectionChanged += OnBuildingSelected;

        if (minimumReserveSlider != null && ResourceManager.Instance != null)
        {
            minimumReserveSlider.SetValueWithoutNotify(ResourceManager.Instance.MinimumMetalReserve);
            UpdateMinimumReserveLabel(ResourceManager.Instance.MinimumMetalReserve);
        }
    }

    private void OnDestroy()
    {
        if (BuildingSelector.Instance != null)
            BuildingSelector.Instance.OnSelectionChanged -= OnBuildingSelected;
    }

    private void Update()
    {
        if (ResourceManager.Instance != null)
            metalText.text = $"Metal: {ResourceManager.Instance.PlayerMetal:F0}";

        if (selectedProduction != null)
            toggleProductionLabel.text = selectedProduction.IsProducing ? "Pause Production" : "Resume Production";

        if (selectedBuilding != null && buildingInfoPanel != null && buildingInfoPanel.activeSelf)
        {
            float fill = selectedBuilding.EnergyBufferCapacity > 0f
                ? selectedBuilding.EnergyBuffer / selectedBuilding.EnergyBufferCapacity
                : 0f;
            if (energyBar   != null) energyBar.value  = fill;
            if (energyLabel != null) energyLabel.text = $"Energy: {selectedBuilding.EnergyBuffer:F0} / {selectedBuilding.EnergyBufferCapacity:F0}";
        }
    }

    public void ShowPlacementInfo(BuildingData data)
    {
        if (placementInfoPanel == null) return;
        placementInfoPanel.SetActive(true);
        if (placementBuildingName != null) placementBuildingName.text = data.buildingName;
        if (placementCostText     != null) placementCostText.text     = $"{data.metalCostToBuild:F0} Metal";
    }

    public void HidePlacementInfo()
    {
        if (placementInfoPanel != null) placementInfoPanel.SetActive(false);
    }

    public void ShowEndScreen(GameState result)
    {
        endScreen.SetActive(true);
        endScreenText.text = result == GameState.Victory ? "Victory!" : "Defeat";
    }

    // -------------------------------------------------------------------------
    // Building selection
    // -------------------------------------------------------------------------

    private void OnBuildingSelected(Building building)
    {
        selectedBuilding   = building;
        selectedProduction = building as ProductionBuilding;

        bool hasBuilding = building != null;
        if (buildingInfoPanel != null)
        {
            buildingInfoPanel.SetActive(hasBuilding);
            if (hasBuilding && buildingInfoName != null)
                buildingInfoName.text = building.Data.buildingName;
        }

        bool hasProd = selectedProduction != null;
        productionPanel?.SetActive(hasProd);
        if (hasProd && productionBuildingName != null)
            productionBuildingName.text = building.Data.buildingName;
    }

    private void OnToggleProduction()
    {
        if (selectedProduction == null) return;
        selectedProduction.SetProducing(!selectedProduction.IsProducing);
    }

    private void ToggleShop()
    {
        if (shopPanel == null) return;
        bool show = !shopPanel.gameObject.activeSelf;
        shopPanel.gameObject.SetActive(show);
        if (!show)
            BuildingPlacer.Instance?.CancelPlacement();
    }

    // -------------------------------------------------------------------------
    // Minimum metal reserve
    // -------------------------------------------------------------------------

    private void OnMinimumReserveChanged(float value)
    {
        ResourceManager.Instance.SetMinimumMetalReserve(value);
        UpdateMinimumReserveLabel(value);
    }

    private void UpdateMinimumReserveLabel(float value)
    {
        if (minimumReserveLabel != null)
            minimumReserveLabel.text = $"Min Reserve: {value:F0}";
    }
}
