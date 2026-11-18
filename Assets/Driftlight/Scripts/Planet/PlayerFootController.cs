using System;
using UnityEngine;

namespace Driftlight
{
    /// <summary>
    /// First-person movement on planet surfaces. Like the ship, it doesn't read devices itself:
    /// PlayerFootInput (or the demo autopilot) fills in move/look/jump each frame.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerFootController : MonoBehaviour
    {
        public static PlayerFootController Player { get; private set; }

        public Transform cameraPivot;

        [Header("Movement")]
        public float walkSpeed = 5.5f;
        public float sprintSpeed = 9.5f;
        public float acceleration = 40f;
        [Range(0f, 1f)] public float airControl = 0.35f;
        public float jumpHeight = 1.7f;
        public float gravity = -17f;

        [Header("Camera")]
        public float headBobAmount = 0.05f;
        public float headBobFrequency = 1.8f;

        // Inputs
        [NonSerialized] public Vector2 move;        // x = strafe, y = forward
        [NonSerialized] public Vector2 lookDelta;   // degrees this frame (accumulated, consumed every Update)
        [NonSerialized] public bool jump;
        [NonSerialized] public bool sprint;
        [NonSerialized] public bool interactPressed;

        public bool ControlsEnabled { get; set; } = true;
        public bool IsGrounded => controller && controller.isGrounded;
        public Vector3 Velocity => velocity;

        CharacterController controller;
        Vector3 velocity;
        float yaw, pitch, bobPhase;
        Vector3 pivotRest;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            Player = this;
            yaw = transform.eulerAngles.y;
            if (cameraPivot)
            {
                pivotRest = cameraPivot.localPosition;
                pitch = cameraPivot.localEulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
            }
        }

        void OnDestroy()
        {
            if (Player == this) Player = null;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (ControlsEnabled)
            {
                yaw += lookDelta.x;
                pitch = Mathf.Clamp(pitch - lookDelta.y, -85f, 85f);
            }
            lookDelta = Vector2.zero;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            Vector2 input = ControlsEnabled ? Vector2.ClampMagnitude(move, 1f) : Vector2.zero;
            Vector3 wish = (transform.right * input.x + transform.forward * input.y) * (sprint ? sprintSpeed : walkSpeed);
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float control = controller.isGrounded ? 1f : airControl;
            horizontal = Vector3.MoveTowards(horizontal, wish, acceleration * control * dt);

            if (controller.isGrounded && velocity.y < 0f) velocity.y = -2f;
            if (jump && controller.isGrounded && ControlsEnabled) velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jump = false;
            velocity.y += gravity * dt;
            velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
            controller.Move(velocity * dt);

            if (cameraPivot)
            {
                float speed01 = horizontal.magnitude / sprintSpeed;
                if (controller.isGrounded && speed01 > 0.05f)
                    bobPhase += dt * headBobFrequency * Mathf.Lerp(1f, 1.6f, speed01) * Mathf.PI * 2f;
                float bob = Mathf.Sin(bobPhase) * headBobAmount * Mathf.Clamp01(speed01 * 2f);
                cameraPivot.localPosition = pivotRest + new Vector3(0f, bob, 0f);
            }
        }

        /// <summary>Turns the view to face a world position (used by cutscenes and the demo autopilot).</summary>
        public void LookAt(Vector3 worldPosition, float sharpness)
        {
            Vector3 to = worldPosition - (cameraPivot ? cameraPivot.position : transform.position);
            if (to.sqrMagnitude < 0.01f) return;
            float targetYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float targetPitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
            float k = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
            lookDelta.x += Mathf.DeltaAngle(yaw, targetYaw) * k;
            lookDelta.y -= Mathf.DeltaAngle(pitch, targetPitch) * k;
        }
    }
}
