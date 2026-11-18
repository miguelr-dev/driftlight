using UnityEngine;
using UnityEngine.InputSystem;

namespace Driftlight
{
    /// <summary>
    /// Reads keyboard and mouse and flies the ship. The mouse moves a "virtual stick" that slowly
    /// re-centres, so small mouse movements give gentle turns and big ones give hard turns.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(ShipController))]
    public class ShipPlayerInput : MonoBehaviour
    {
        [Tooltip("How far one pixel of mouse movement pushes the virtual stick.")]
        public float mouseSensitivity = 0.0032f;
        [Tooltip("How fast the virtual stick drifts back to centre (per second).")]
        public float stickRecenter = 1.6f;
        public bool invertY;

        /// <summary>Current virtual stick position (-1..1); the HUD draws it as the aim cursor.</summary>
        public Vector2 Stick { get; private set; }

        ShipController ship;
        Blaster[] blasters;
        WarpDrive warpDrive;

        void Awake()
        {
            ship = GetComponent<ShipController>();
            blasters = GetComponentsInChildren<Blaster>();
            warpDrive = GetComponent<WarpDrive>();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null) return;

            bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;
            Vector2 delta = cursorLocked && mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
            if (invertY) delta.y = -delta.y;

            Vector2 stick = Stick + delta * mouseSensitivity;
            stick = Vector2.ClampMagnitude(stick, 1f);
            stick = Vector2.MoveTowards(stick, Vector2.zero, stickRecenter * Time.deltaTime);
            Stick = stick;

            ship.steer = stick;
            ship.thrust = Axis(keyboard.wKey.isPressed, keyboard.sKey.isPressed);
            ship.strafe = Axis(keyboard.dKey.isPressed, keyboard.aKey.isPressed);
            ship.lift = Axis(keyboard.spaceKey.isPressed, keyboard.leftCtrlKey.isPressed);
            ship.roll = Axis(keyboard.eKey.isPressed, keyboard.qKey.isPressed);
            ship.boost = keyboard.leftShiftKey.isPressed;

            bool fire = cursorLocked && ship.ControlsEnabled && mouse != null && mouse.leftButton.isPressed;
            foreach (var blaster in blasters) blaster.triggerHeld = fire;

            if (warpDrive) warpDrive.chargeHeld = keyboard.fKey.isPressed;
        }

        static float Axis(bool positive, bool negative) => (positive ? 1f : 0f) - (negative ? 1f : 0f);
    }
}
