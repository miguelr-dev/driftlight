using UnityEngine;
using UnityEngine.InputSystem;

namespace Driftlight
{
    /// <summary>Locks the mouse while playing. Esc frees it; clicking the game locks it again.</summary>
    public class CursorLock : MonoBehaviour
    {
        public bool lockOnStart = true;

        void Start()
        {
            if (lockOnStart) SetLocked(true);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                SetLocked(false);
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
                SetLocked(true);
        }

        public static void SetLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
