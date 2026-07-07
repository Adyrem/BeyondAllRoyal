using UnityEngine;
using UnityEngine.InputSystem;

// Centralised input helpers for the new Input System.
// Primary input = touch (mobile). Mouse = editor/desktop fallback.
public static class InputHelper
{
    // True on the frame a finger touches down or left mouse button is pressed
    public static bool TapBegan()
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    // Screen position of the active touch or mouse cursor
    public static Vector2 TapPosition()
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.isPressed)
            return touch.primaryTouch.position.ReadValue();
        var mouse = Mouse.current;
        return mouse?.position.ReadValue() ?? Vector2.zero;
    }

    // True on the frame Escape is pressed or right mouse button is clicked
    public static bool CancelPressed()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
        var mouse = Mouse.current;
        return mouse != null && mouse.rightButton.wasPressedThisFrame;
    }
}
