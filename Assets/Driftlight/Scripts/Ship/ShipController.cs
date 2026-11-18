using System;
using UnityEngine;

namespace Driftlight
{
    /// <summary>
    /// Arcade flight model for a ship. It doesn't read input itself: ShipPlayerInput (or an AI/autopilot)
    /// writes the steer/thrust/strafe/roll/boost fields every frame and this turns them into motion.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ShipController : MonoBehaviour
    {
        /// <summary>The ship the player is flying in the current scene.</summary>
        public static ShipController Player { get; private set; }

        public bool isPlayer = true;

        [Header("Speed (m/s)")]
        public float cruiseSpeed = 55f;
        public float boostSpeed = 125f;
        public float reverseSpeed = 18f;
        public float strafeSpeed = 24f;
        [Tooltip("How quickly the ship reaches the speed you ask for. Higher = snappier.")]
        public float acceleration = 2.2f;

        [Header("Turning (degrees per second)")]
        public float pitchRate = 75f;
        public float yawRate = 62f;
        public float rollRate = 150f;
        [Tooltip("How quickly turning follows the stick. Higher = snappier.")]
        public float turnResponse = 6f;

        [Header("Boost")]
        [Tooltip("Boost energy used per second (full tank = 1).")]
        public float boostDrain = 0.32f;
        public float boostRecharge = 0.18f;

        [Header("Visuals")]
        [Tooltip("Child holding the model; it banks into turns without affecting physics.")]
        public Transform visual;
        public float bankAngle = 28f;

        // Inputs, written every frame by whoever is flying the ship. Each is -1..1.
        [NonSerialized] public Vector2 steer;   // x = yaw, y = pitch (positive = nose up)
        [NonSerialized] public float thrust;
        [NonSerialized] public float strafe;
        [NonSerialized] public float lift;
        [NonSerialized] public float roll;      // positive = roll right
        [NonSerialized] public bool boost;

        /// <summary>When false the ship ignores its inputs (cutscenes, warp, death).</summary>
        public bool ControlsEnabled { get; set; } = true;
        public float BoostEnergy { get; private set; } = 1f;
        public bool IsBoosting { get; private set; }
        public Vector3 Velocity => body ? body.linearVelocity : Vector3.zero;
        public float Speed => Velocity.magnitude;
        public float ForwardSpeed => Vector3.Dot(Velocity, transform.forward);
        public Rigidbody Body => body;

        /// <summary>Set by scripted moments (warp) to fly the ship along a fixed velocity.</summary>
        [NonSerialized] public Vector3? velocityOverride;
        /// <summary>Set by scripted moments to turn the ship toward a direction.</summary>
        [NonSerialized] public Vector3? alignDirection;

        Rigidbody body;
        Vector3 angularRate;
        float visualBank;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.linearDamping = 0f;
            body.angularDamping = 3f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (isPlayer) Player = this;
        }

        void OnDestroy()
        {
            if (Player == this) Player = null;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            bool controls = ControlsEnabled;

            // Rotation: smoothed turn rates, so the ship feels weighty but responsive.
            Vector2 s = controls ? Vector2.ClampMagnitude(steer, 1f) : Vector2.zero;
            float r = controls ? Mathf.Clamp(roll, -1f, 1f) : 0f;
            Vector3 targetRate = new Vector3(-s.y * pitchRate, s.x * yawRate, -r * rollRate);
            angularRate = Vector3.Lerp(angularRate, targetRate, 1f - Mathf.Exp(-turnResponse * dt));
            Quaternion rotation = body.rotation * Quaternion.Euler(angularRate * dt);
            if (alignDirection.HasValue && alignDirection.Value.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(alignDirection.Value, rotation * Vector3.up);
                rotation = Quaternion.Slerp(rotation, target, 1f - Mathf.Exp(-3f * dt));
            }
            body.MoveRotation(rotation);

            // Boost: drains while held, recharges otherwise.
            IsBoosting = controls && boost && thrust > 0.1f && BoostEnergy > 0.02f;
            BoostEnergy = Mathf.Clamp01(BoostEnergy + (IsBoosting ? -boostDrain : boostRecharge) * dt);

            // Translation: ease the velocity toward what the inputs ask for.
            Vector3 targetVelocity;
            float accel = acceleration;
            if (velocityOverride.HasValue)
            {
                targetVelocity = velocityOverride.Value;
                accel = 4f;
            }
            else
            {
                float fwd = controls ? Mathf.Clamp(thrust, -1f, 1f) : 0f;
                float forwardSpeed = fwd >= 0f ? fwd * (IsBoosting ? boostSpeed : cruiseSpeed) : fwd * reverseSpeed;
                targetVelocity = transform.forward * forwardSpeed;
                if (controls)
                {
                    targetVelocity += transform.right * Mathf.Clamp(strafe, -1f, 1f) * strafeSpeed;
                    targetVelocity += transform.up * Mathf.Clamp(lift, -1f, 1f) * strafeSpeed;
                }
            }
            body.linearVelocity = Vector3.Lerp(body.linearVelocity, targetVelocity, 1f - Mathf.Exp(-accel * dt));
        }

        void LateUpdate()
        {
            if (!visual) return;
            // Bank into turns and strafes (visual only).
            float targetBank = -(angularRate.y / Mathf.Max(1f, yawRate)) * bankAngle;
            if (ControlsEnabled) targetBank -= Mathf.Clamp(strafe, -1f, 1f) * bankAngle * 0.5f;
            visualBank = Mathf.Lerp(visualBank, targetBank, 1f - Mathf.Exp(-5f * Time.deltaTime));
            visual.localRotation = Quaternion.Euler(0f, 0f, visualBank);
        }

        void OnCollisionEnter(Collision collision)
        {
            float impact = collision.relativeVelocity.magnitude;
            if (isPlayer && impact > 6f) ChaseCamera.Shake(Mathf.Clamp01(impact / 60f));
        }
    }
}
