using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Called by ProjectSetup.SetupScenes() (BeyondAllRoyal → 2 - Setup Scenes).
// Unlike the rest of that step (which only creates missing UI and skips
// anything already wired), this force-reapplies UITheme's colors to whatever
// HUD/BuildingShopPanel already reference — safe to re-run any time (e.g.
// after tweaking UITheme) without deleting/recreating anything first.
// Requires HUD (and, for shop styling, BuildingShopPanel) in the open scene.
public static class ThemeSetup
{
    public static void ApplyPlaySceneTheme()
    {
        var hud = Object.FindAnyObjectByType<HUD>(FindObjectsInactive.Include);
        if (hud == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No HUD found in the open scene.");
            return;
        }

        var so = new SerializedObject(hud);

        StyleText(so, "metalText");
        StyleText(so, "endScreenText");
        StyleText(so, "productionBuildingName");
        StyleText(so, "toggleProductionLabel");
        StyleText(so, "placementBuildingName");
        StyleText(so, "placementCostText");
        StyleText(so, "buildingInfoName");
        StyleText(so, "energyLabel");
        StyleText(so, "minimumReserveLabel");

        StyleButton(so, "shopToggleButton");
        StyleButton(so, "toggleProductionButton");
        StyleButton(so, "cancelPlacementButton");
        StyleButton(so, "demolishButton");
        StyleButton(so, "mainMenuButton");

        StylePanelBackground(so, "endScreen");
        StylePanelBackground(so, "buildingInfoPanel");
        StylePanelBackground(so, "placementInfoPanel");
        StylePanelBackground(so, "productionPanel");

        StyleSlider(so, "minimumReserveSlider");
        StyleSlider(so, "energyBar");

        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);

        StyleShopPanel();

        Debug.Log("[BeyondAllRoyal] Applied the dark purple theme to HUD and the shop panel. Save the scene.");
    }

    private static void StyleText(SerializedObject hudSo, string fieldName)
    {
        var text = hudSo.FindProperty(fieldName)?.objectReferenceValue as TextMeshProUGUI;
        if (text != null) text.color = UITheme.Text;
    }

    private static void StyleButton(SerializedObject hudSo, string fieldName)
    {
        var button = hudSo.FindProperty(fieldName)?.objectReferenceValue as Button;
        if (button != null) UITheme.ApplyButtonColors(button, UITheme.Accent);
    }

    // Recolors the panel's own background Image, if it has one, preserving its
    // existing alpha rather than assuming it should become fully opaque.
    private static void StylePanelBackground(SerializedObject hudSo, string fieldName)
    {
        var panelGO = hudSo.FindProperty(fieldName)?.objectReferenceValue as GameObject;
        var image = panelGO != null ? panelGO.GetComponent<Image>() : null;
        if (image == null) return;

        var color = UITheme.Panel;
        color.a = image.color.a;
        image.color = color;
    }

    private static void StyleSlider(SerializedObject hudSo, string fieldName)
    {
        var slider = hudSo.FindProperty(fieldName)?.objectReferenceValue as Slider;
        if (slider == null) return;

        var background = slider.transform.Find("Background")?.GetComponent<Image>();
        if (background != null) background.color = UITheme.Panel;

        var fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (fill != null) fill.color = UITheme.Accent;

        var handle = slider.handleRect != null ? slider.handleRect.GetComponent<Image>() : null;
        if (handle != null) handle.color = UITheme.Accent;
    }

    // Recolors the panel's own background (if any) and each entry's cost
    // label. Deliberately leaves each entry's icon/button Image alone — that
    // Image displays the building's own sprite (see BuildingShopPanel.Start),
    // so tinting it would recolor the building icons themselves.
    private static void StyleShopPanel()
    {
        var panel = Object.FindAnyObjectByType<BuildingShopPanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No BuildingShopPanel found in the open scene — skipping shop styling.");
            return;
        }

        var panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            var color = UITheme.Panel;
            color.a = panelImage.color.a;
            panelImage.color = color;
        }

        var so = new SerializedObject(panel);
        var entries = so.FindProperty("shopEntries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            var costLabel = entries.GetArrayElementAtIndex(i)
                .FindPropertyRelative("costLabel").objectReferenceValue as TextMeshProUGUI;
            if (costLabel != null) costLabel.color = UITheme.Text;
        }

        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
    }
}
