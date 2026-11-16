using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LittleKeep.Game
{
    /// Hold a finger (or the mouse) anywhere on the lane to aim the Keeper. Presses that start on a button are ignored,
    /// so one thumb can hold the aim while the other taps Rally.
    public sealed class AimInput
    {
        readonly Camera cam;
        readonly Dictionary<int, bool> touchStartedOnUi = new Dictionary<int, bool>();
        bool mouseStartedOnUi;
        readonly List<RaycastResult> hits = new List<RaycastResult>();

        public AimInput(Camera cam) { this.cam = cam; }

        /// The world point being held, or null if nothing is held on the lane.
        public Vector2? Read()
        {
            Vector2? screen = null;
            var touch = Touchscreen.current;
            if (touch != null)
            {
                foreach (var t in touch.touches)
                {
                    int id = t.touchId.ReadValue();
                    if (!t.press.isPressed) { touchStartedOnUi.Remove(id); continue; }
                    if (!touchStartedOnUi.TryGetValue(id, out bool onUi))
                    {
                        onUi = IsOverUi(t.startPosition.ReadValue());
                        touchStartedOnUi[id] = onUi;
                    }
                    if (!onUi) { screen = t.position.ReadValue(); break; }
                }
            }
            var mouse = Mouse.current;
            if (screen == null && mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame) mouseStartedOnUi = IsOverUi(mouse.position.ReadValue());
                if (mouse.leftButton.isPressed && !mouseStartedOnUi) screen = mouse.position.ReadValue();
            }
            if (screen == null) return null;
            return cam.ScreenToWorldPoint(new Vector3(screen.Value.x, screen.Value.y, -cam.transform.position.z));
        }

        bool IsOverUi(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            hits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screenPos }, hits);
            return hits.Count > 0;
        }
    }
}
