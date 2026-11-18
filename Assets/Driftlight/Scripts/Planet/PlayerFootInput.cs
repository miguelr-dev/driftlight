using UnityEngine;
using UnityEngine.InputSystem;

namespace Driftlight
{
    /// <summary>Keyboard and mouse controls for exploring on foot.</summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(PlayerFootController))]
    public class PlayerFootInput : MonoBehaviour
    {
        [Tooltip("Degrees of turn per pixel of mouse movement.")]
        public float mouseSensitivity = 0.09f;
        public bool invertY;

        PlayerFootController player;

        void Awake() => player = GetComponent<PlayerFootController>();

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null) return;

            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Vector2 delta = locked && mouse != null ? mouse.delta.ReadValue() * mouseSensitivity : Vector2.zero;
            if (invertY) delta.y = -delta.y;
            player.lookDelta += delta;

            player.move = new Vector2(Axis(keyboard.dKey.isPressed, keyboard.aKey.isPressed), Axis(keyboard.wKey.isPressed, keyboard.sKey.isPressed));
            player.sprint = keyboard.leftShiftKey.isPressed;
            if (keyboard.spaceKey.wasPressedThisFrame) player.jump = true;
            player.interactPressed = keyboard.eKey.wasPressedThisFrame;
        }

        static float Axis(bool positive, bool negative) => (positive ? 1f : 0f) - (negative ? 1f : 0f);
    }
}
