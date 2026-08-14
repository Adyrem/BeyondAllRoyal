using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    // True on the frame a finger lifts or the left mouse button is released
    public static bool TapEnded()
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasReleasedThisFrame) return true;
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasReleasedThisFrame;
    }

    // True for every frame a finger/button is held down (not just the first)
    public static bool IsPressed()
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.isPressed) return true;
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.isPressed;
    }

    // Screen position of the active touch or mouse cursor. Also valid on the
    // exact frame a touch ends (press.isPressed is already false by then, but
    // the touch's last position is still readable) — callers that react to
    // TapEnded() need this same frame's position, not a stale fallback.
    public static Vector2 TapPosition()
    {
        var touch = Touchscreen.current;
        if (touch != null && (touch.primaryTouch.press.isPressed || touch.primaryTouch.press.wasReleasedThisFrame))
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

    // Converts a screen-space position to a world position on the plane the
    // camera is looking at. Takes the camera as a parameter (rather than doing
    // its own Camera.main lookup) so callers keep whatever caching they already do.
    public static Vector3 ScreenToWorld(Camera cam, Vector2 screenPos)
    {
        var pos = new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z);
        return cam.ScreenToWorldPoint(pos);
    }

    // True if the current tap/click hit an interactive UI element (Button,
    // Slider, ...). World-space tap handling (selecting/placing/deselecting
    // buildings) should skip when this is true, so tapping a HUD button doesn't
    // also register as a world tap — but a tap on a passive panel background
    // (no Selectable underneath) still reaches the world, so e.g. tapping near
    // an info panel to deselect a building still works.
    public static bool TapHitInteractiveUI()
    {
        if (EventSystem.current == null) return false;

        var pointerData = new PointerEventData(EventSystem.current) { position = TapPosition() };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
            if (result.gameObject.GetComponentInParent<Selectable>() != null)
                return true;

        return false;
    }
}
