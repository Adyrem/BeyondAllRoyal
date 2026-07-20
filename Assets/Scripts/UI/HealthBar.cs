using UnityEngine;

// Attach to a GameObject that has a SpriteRenderer as background.
// Give it a child GameObject with a SpriteRenderer as the fill (also assign to 'fill').
// The fill scales and shifts on the x-axis to simulate a left-anchored bar.
// An optional second child ('indicator') can mark a fixed threshold along the
// bar (e.g. the production bar's "energy needed for one unit" tick).
public class HealthBar : MonoBehaviour
{
    [SerializeField] private SpriteRenderer fill;
    [SerializeField] private SpriteRenderer indicator;

    private Color overrideColor = Color.clear; // clear = use health gradient

    // Call once to lock the bar to a fixed colour instead of the red-green gradient
    public void SetColor(Color color) => overrideColor = color;

    public void SetFraction(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);

        // Shift so the bar shrinks from the right (compensates for centre pivot)
        var pos = fill.transform.localPosition;
        pos.x   = (fraction - 1f) * 0.5f;
        fill.transform.localPosition = pos;

        var scale = fill.transform.localScale;
        scale.x   = fraction;
        fill.transform.localScale = scale;

        fill.color = overrideColor == Color.clear
            ? Color.Lerp(Color.red, Color.green, fraction)
            : overrideColor;
    }

    // Positions the threshold marker at the given fraction along the bar. No-op
    // if this bar wasn't given an indicator child (e.g. plain health bars).
    public void SetIndicator(float fraction)
    {
        if (indicator == null) return;

        fraction = Mathf.Clamp01(fraction);
        var pos = indicator.transform.localPosition;
        pos.x   = fraction - 0.5f;
        indicator.transform.localPosition = pos;
    }
}
