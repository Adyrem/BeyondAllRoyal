using UnityEngine;
using UnityEngine.UI;

// Shared dark-purple palette. Lives outside Editor/ (unlike most of this
// project's UI-setup code) because runtime scripts need it too — e.g.
// BuildingShopPanel dynamically recoloring a shop row when a building
// becomes unaffordable — not just the Editor scripts that build the UI once
// at design time (MainMenuSetup for MainMenu, ThemeSetup for PlayScene).
public static class UITheme
{
    public static readonly Color Background   = new Color(0.071f, 0.055f, 0.098f); // near-black purple (camera clear color)
    public static readonly Color Panel        = new Color(0.157f, 0.114f, 0.212f); // panel/dropdown/slider backgrounds
    public static readonly Color Accent       = new Color(0.545f, 0.361f, 0.965f); // vivid violet (primary actions, highlights)
    public static readonly Color Disabled     = new Color(0.176f, 0.157f, 0.204f); // muted dark gray-purple
    public static readonly Color DisabledText = new Color(0.494f, 0.463f, 0.549f); // muted gray-lavender
    public static readonly Color MutedText    = new Color(0.729f, 0.671f, 0.827f); // lavender-gray secondary text
    public static readonly Color Warning      = new Color(0.976f, 0.443f, 0.376f); // coral-red, e.g. a cost the player can't currently afford
    public static readonly Color Text         = Color.white;

    public static Color Hover(Color fill)   => Color.Lerp(fill, Color.white, 0.15f);
    public static Color Pressed(Color fill) => Color.Lerp(fill, Color.black, 0.15f);

    // A flat "disabled" color reads as barely different against an
    // already-dark fill (e.g. shop rows, which use Panel) — blending toward
    // Background instead gives a consistently strong dim regardless of the
    // button's own base color.
    public static Color Dim(Color fill) => Color.Lerp(fill, Background, 0.6f);

    // Buttons are styled via Selectable.colors (ColorBlock), not by setting the
    // Image color directly — Unity's ColorTint transition overwrites the
    // Image's color with colors.normalColor on the first state transition, so
    // setting Image.color alone gets silently clobbered at runtime.
    public static void ApplyButtonColors(Button button, Color fillColor)
    {
        var colors = button.colors;
        colors.normalColor      = fillColor;
        colors.highlightedColor = Hover(fillColor);
        colors.pressedColor     = Pressed(fillColor);
        colors.selectedColor    = fillColor;
        // Previously set equal to fillColor, which meant a non-interactable
        // button (e.g. a shop row for a building the player can't currently
        // afford) looked completely identical to an interactable one.
        colors.disabledColor    = Dim(fillColor);
        button.colors = colors;
    }
}
