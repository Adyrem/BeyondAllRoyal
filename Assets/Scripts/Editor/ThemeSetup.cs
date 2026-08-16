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
        if (text == null) return;

        text.color = UITheme.Text;

        // These were all hand-created at whatever default size the Inspector
        // starts new Text objects at (~14-18pt), which reads as tiny on a
        // phone screen. Auto-sizing (rather than a fixed size) so a label
        // stuck in a small manually-sized panel shrinks to fit instead of
        // overflowing it.
        text.enableAutoSizing = true;
        text.fontSizeMin = 24f;
        text.fontSizeMax = 44f;
    }

    private static void StyleButton(SerializedObject hudSo, string fieldName)
    {
        var button = hudSo.FindProperty(fieldName)?.objectReferenceValue as Button;
        if (button != null) UITheme.ApplyButtonColors(button, UITheme.Accent);
    }

    // A panel sized to just barely fit one small text label reads as "no
    // panel at all" against the busy game-world background behind it.
    private const float MinPanelWidth  = 480f;
    private const float MinPanelHeight = 200f;

    // Recolors the panel's own background Image (bumping its alpha to at
    // least NearOpaqueAlpha, since a near-transparent panel is exactly the
    // "poor visibility" this exists to fix) and enforces a minimum size on
    // the panel itself. Adds a background Image first if the panel doesn't
    // have one yet — a panel with only text/buttons and no backing Image
    // renders as text floating directly over the game world.
    private const float NearOpaqueAlpha = 0.95f;

    private static void StylePanelBackground(SerializedObject hudSo, string fieldName)
    {
        var panelGO = hudSo.FindProperty(fieldName)?.objectReferenceValue as GameObject;
        if (panelGO == null) return;

        // Only safe to force a minimum size when the panel is point-anchored
        // (a fixed size) — a stretch-anchored panel's sizeDelta means
        // something else entirely (an offset from the stretched size), so
        // forcing it here could distort a deliberately full-width/height panel.
        var rect = panelGO.GetComponent<RectTransform>();
        if (rect != null && Vector2.Distance(rect.anchorMin, rect.anchorMax) < 0.01f)
        {
            var size = rect.sizeDelta;
            rect.sizeDelta = new Vector2(Mathf.Max(size.x, MinPanelWidth), Mathf.Max(size.y, MinPanelHeight));
        }

        var image = panelGO.GetComponent<Image>();
        if (image == null)
        {
            // Added directly on the panel itself (not a new child), so it
            // naturally renders behind the panel's existing child text/buttons
            // without needing any sibling-order changes.
            image = panelGO.AddComponent<Image>();
            image.color = new Color(UITheme.Panel.r, UITheme.Panel.g, UITheme.Panel.b, NearOpaqueAlpha);
            return;
        }

        var color = UITheme.Panel;
        color.a = Mathf.Max(image.color.a, NearOpaqueAlpha);
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

    // Recolors the panel's own background (if any) and each entry's name/cost
    // labels. Deliberately leaves each entry's icon Image alone — that Image
    // displays the building's own sprite (see BuildingShopPanel.Start), so
    // tinting it would recolor the building icons themselves.
    private static void StyleShopPanel()
    {
        var panel = Object.FindAnyObjectByType<BuildingShopPanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            Debug.LogWarning("[BeyondAllRoyal] No BuildingShopPanel found in the open scene — skipping shop styling.");
            return;
        }

        var panelImage = panel.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(UITheme.Panel.r, UITheme.Panel.g, UITheme.Panel.b, NearOpaqueAlpha);
        }
        else
        {
            var color = UITheme.Panel;
            color.a = Mathf.Max(panelImage.color.a, NearOpaqueAlpha);
            panelImage.color = color;
        }

        var so = new SerializedObject(panel);
        var entries = so.FindProperty("shopEntries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);

            var nameLabel = entry.FindPropertyRelative("nameLabel").objectReferenceValue as TextMeshProUGUI;
            if (nameLabel != null) nameLabel.color = UITheme.Text;

            var costLabel = entry.FindPropertyRelative("costLabel").objectReferenceValue as TextMeshProUGUI;
            if (costLabel != null) costLabel.color = UITheme.MutedText;
        }

        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
    }
}
