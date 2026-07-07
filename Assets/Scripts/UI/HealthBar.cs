using UnityEngine;

// Attach to a GameObject that has a SpriteRenderer as background.
// Give it a child GameObject with a SpriteRenderer as the fill (also assign to 'fill').
// The fill scales and shifts on the x-axis to simulate a left-anchored bar.
public class HealthBar : MonoBehaviour
{
    [SerializeField] private SpriteRenderer fill;

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
}
