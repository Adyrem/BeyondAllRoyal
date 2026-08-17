using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Called by ProjectSetup.SetupScenes() (BeyondAllRoyal → 2 - Setup Scenes).
// Builds a new MainMenu scene from scratch (Camera, EventSystem, Canvas, title,
// AI difficulty dropdown, Singleplayer/Multiplayer buttons) wired to a
// MainMenuController, saves it to Assets/Scenes/MainMenu.unity, and registers
// it as build index 0 — PlayScene is loaded from it once Singleplayer is picked.
// Re-running this once MainMenu.unity already exists rebuilds its contents
// fresh from the code below instead of skipping, so a re-run always reflects
// the current sizes/positions/colors without deleting the file by hand first.
public static class MainMenuSetup
{
    private const string ScenePath     = "Assets/Scenes/MainMenu.unity";
    private const string PlayScenePath = "Assets/Scenes/PlayScene.unity";

    public static void CreateMainMenuScene()
    {
        // Prompts to save the currently open scene if it has unsaved changes,
        // rather than silently discarding them when we switch scenes below.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        bool alreadyExisted = File.Exists(ScenePath);
        Scene scene;

        if (alreadyExisted)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                Object.DestroyImmediate(root);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        BuildCameraAndEventSystem();
        var canvas       = BuildCanvas();
        var controllerGO = new GameObject("MainMenuController");
        var controller   = controllerGO.AddComponent<MainMenuController>();

        BuildTitle(canvas.transform);
        var dropdown            = BuildDifficultyDropdown(canvas.transform);
        // Left enough headroom below the dropdown for its opened option list
        // (3 rows below the box itself, see BuildDifficultyDropdown) to not
        // overlap the Singleplayer button.
        var singleplayerButton  = BuildButton(canvas.transform, "SingleplayerButton", "Singleplayer",
            new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(460f, 110f), UITheme.Accent, Color.white);
        var multiplayerButton   = BuildButton(canvas.transform, "MultiplayerButton", "Multiplayer (Coming Soon)",
            new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(460f, 110f), UITheme.Disabled, UITheme.DisabledText);
        // Below Multiplayer, with headroom to spare on a 1920-tall reference canvas.
        var (volumeSlider, volumeLabel) = BuildVolumeSlider(canvas.transform);

        var so = new SerializedObject(controller);
        so.FindProperty("difficultyDropdown").objectReferenceValue = dropdown;
        so.FindProperty("singleplayerButton").objectReferenceValue = singleplayerButton;
        so.FindProperty("multiplayerButton").objectReferenceValue  = multiplayerButton;
        so.FindProperty("volumeSlider").objectReferenceValue       = volumeSlider;
        so.FindProperty("volumeLabel").objectReferenceValue        = volumeLabel;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        RegisterScenesInBuildSettings();

        string verb = alreadyExisted ? "Refreshed" : "Created";
        Debug.Log($"[BeyondAllRoyal] {verb} MainMenu scene at '{ScenePath}' (build index 0). " +
                  "Reposition/style the UI as needed, then save the scene.");
    }

    private static void BuildCameraAndEventSystem()
    {
        var camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor  = UITheme.Background;
        camGO.transform.position = new Vector3(0f, 0f, -10f);

        // The project uses the new Input System exclusively (see InputHelper.cs),
        // so the UI event system needs InputSystemUIInputModule, not the legacy one.
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static Canvas BuildCanvas()
    {
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f); // portrait, mobile-first per CLAUDE.md
        scaler.matchWidthOrHeight  = 0.5f;

        return canvas;
    }

    private static void BuildTitle(Transform parent)
    {
        var go = new GameObject("Title", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot     = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -220f);
        rect.sizeDelta = new Vector2(1000f, 200f);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text      = "BeyondAllRoyal";
        text.fontSize  = 100f;
        text.alignment = TextAlignmentOptions.Center;
        text.color     = UITheme.Accent;
    }

    private static TMP_Dropdown BuildDifficultyDropdown(Transform parent)
    {
        var label = new GameObject("DifficultyLabel", typeof(RectTransform));
        label.transform.SetParent(parent, false);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot     = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 330f);
        labelRect.sizeDelta = new Vector2(460f, 60f);
        var labelText = label.AddComponent<TextMeshProUGUI>();
        labelText.text      = "AI Difficulty";
        labelText.fontSize  = 46f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color     = UITheme.MutedText;

        var dropdownGO = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        dropdownGO.name = "DifficultyDropdown";
        dropdownGO.transform.SetParent(parent, false);
        var rect = dropdownGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, 260f);
        rect.sizeDelta = new Vector2(460f, 110f);

        // TMP_DefaultControls sizes its caption/item text for the small default
        // rect and enables auto-sizing on both — which silently overrides a
        // plain .fontSize assignment, so the bump below wouldn't actually take
        // effect without also turning auto-sizing off first.
        var captionText = dropdownGO.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (captionText != null)
        {
            captionText.enableAutoSizing = false;
            captionText.fontSize = 44f;
            captionText.color    = Color.white;
        }

        var itemTransform = dropdownGO.transform.Find("Template/Viewport/Content/Item");
        var itemText = itemTransform?.Find("Item Label")?.GetComponent<TextMeshProUGUI>();
        if (itemText != null)
        {
            itemText.enableAutoSizing = false;
            itemText.fontSize = 40f;
            itemText.color    = Color.white;
        }

        // The option rows default to a height sized for TMP_DefaultControls' own
        // small caption font (~14pt) — at the 40pt used here that's way too
        // short, so the three options render squished/overlapping each other
        // instead of as separate rows. Resize the row (and the template that
        // contains all of them) to fit.
        const float itemHeight = 70f;
        var itemRect = itemTransform?.GetComponent<RectTransform>();
        if (itemRect != null) itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, itemHeight);
        var itemLayout = itemTransform?.GetComponent<LayoutElement>();
        if (itemLayout != null) itemLayout.preferredHeight = itemHeight;

        var templateRect = dropdownGO.transform.Find("Template")?.GetComponent<RectTransform>();
        if (templateRect != null)
            templateRect.sizeDelta = new Vector2(templateRect.sizeDelta.x, itemHeight * 3f + 10f); // Easy/Medium/Hard

        // The dropdown box itself, and the opened list's viewport background —
        // both default to a plain white Image, tinted here to match the theme.
        var tmpDropdown = dropdownGO.GetComponent<TMP_Dropdown>();
        var dropdownColors = tmpDropdown.colors;
        dropdownColors.normalColor      = UITheme.Panel;
        dropdownColors.highlightedColor = UITheme.Hover(UITheme.Panel);
        dropdownColors.pressedColor     = UITheme.Pressed(UITheme.Panel);
        dropdownColors.selectedColor    = UITheme.Panel;
        tmpDropdown.colors = dropdownColors;

        var viewportImage = dropdownGO.transform.Find("Template/Viewport")?.GetComponent<Image>();
        if (viewportImage != null) viewportImage.color = UITheme.Panel;

        var itemBackground = dropdownGO.transform.Find("Template/Viewport/Content/Item/Item Background")?.GetComponent<Image>();
        if (itemBackground != null) itemBackground.color = UITheme.Panel;

        var itemCheckmark = dropdownGO.transform.Find("Template/Viewport/Content/Item/Item Checkmark")?.GetComponent<Image>();
        if (itemCheckmark != null) itemCheckmark.color = UITheme.Accent;

        var arrow = dropdownGO.transform.Find("Arrow")?.GetComponent<Image>();
        if (arrow != null) arrow.color = UITheme.Accent;

        // MainMenuController.Awake() populates Easy/Medium/Hard at runtime; no
        // need to author options here.
        return tmpDropdown;
    }

    // Master volume, applied globally via SoundSettings/AudioListener.volume.
    // MainMenuController.Awake() initializes the slider from the persisted
    // value and updates the label text live as it's dragged.
    private static (Slider slider, TextMeshProUGUI label) BuildVolumeSlider(Transform parent)
    {
        var labelGO = new GameObject("VolumeLabel", typeof(RectTransform));
        labelGO.transform.SetParent(parent, false);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot     = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, -435f);
        labelRect.sizeDelta = new Vector2(460f, 60f);
        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text      = "Volume: 50%";
        labelText.fontSize  = 46f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color     = UITheme.MutedText;

        var sliderGO = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderGO.name = "VolumeSlider";
        sliderGO.transform.SetParent(parent, false);
        var rect = sliderGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -445f);
        rect.sizeDelta = new Vector2(460f, 70f);

        var slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = 0.5f;

        // Colored inline (like the dropdown above) rather than deferred to
        // ThemeSetup, since ThemeSetup only styles PlayScene's HUD.
        var background = sliderGO.transform.Find("Background")?.GetComponent<Image>();
        if (background != null) background.color = UITheme.Panel;
        var fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (fill != null) fill.color = UITheme.Accent;
        var handle = slider.handleRect != null ? slider.handleRect.GetComponent<Image>() : null;
        if (handle != null) handle.color = UITheme.Accent;

        return (slider, labelText);
    }

    private static Button BuildButton(Transform parent, string goName, string label,
        Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta, Color fillColor, Color textColor)
    {
        var buttonGO = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonGO.name = goName;
        buttonGO.transform.SetParent(parent, false);

        var rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot     = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var button = buttonGO.GetComponent<Button>();
        UITheme.ApplyButtonColors(button, fillColor);

        // The default label child is legacy Text ("Button") — replace with TMP,
        // matching the rest of the project's UI (see ProjectSetup.CreateHudChildButton).
        var legacyText = buttonGO.transform.Find("Text (Legacy)");
        if (legacyText != null) Object.DestroyImmediate(legacyText.gameObject);

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(buttonGO.transform, false);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 18f;
        labelText.fontSizeMax = 34f;
        labelText.text        = label;
        labelText.color       = textColor;

        return button;
    }

    // MainMenu becomes build index 0 (the scene the game actually launches
    // with); PlayScene is appended if it isn't registered yet, otherwise left
    // wherever it already was (just shifted down since MainMenu is inserted
    // at the front).
    private static void RegisterScenesInBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(s => s.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));

        if (File.Exists(PlayScenePath) && scenes.All(s => s.path != PlayScenePath))
            scenes.Add(new EditorBuildSettingsScene(PlayScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
