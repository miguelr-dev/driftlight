using UnityEngine;

namespace Driftlight
{
    /// <summary>
    /// Third-person chase camera. It stays locked to the ship's position (so it never falls behind,
    /// even at warp speed) but lags the ship's rotation, which gives turns a sense of weight.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ChaseCamera : MonoBehaviour
    {
        public ShipController target;
        public Vector3 offset = new Vector3(0f, 3.4f, -13f);
        [Tooltip("How far ahead of the ship the camera aims.")]
        public float lookAhead = 40f;
        public float rotationSharpness = 6f;

        [Header("Field of view")]
        public float baseFov = 62f;
        public float boostFov = 76f;
        public float warpFov = 120f;
        public float boostPullback = 3.5f;

        /// <summary>0..1, driven by the warp drive while it charges and engages.</summary>
        [Range(0f, 1f)] public float warpAmount;

        static float trauma;

        /// <summary>Adds camera shake (0..1). Shake fades out on its own.</summary>
        public static void Shake(float amount) => trauma = Mathf.Clamp01(trauma + amount);

        Camera cam;
        Quaternion smoothedRotation;
        float boostBlend;
        bool initialised;

        void Awake() => cam = GetComponent<Camera>();

        void LateUpdate()
        {
            if (!target) target = ShipController.Player;
            if (!target) return;

            float dt = Time.deltaTime;
            Transform t = target.transform;
            if (!initialised)
            {
                smoothedRotation = t.rotation;
                initialised = true;
            }

            smoothedRotation = Quaternion.Slerp(smoothedRotation, t.rotation, 1f - Mathf.Exp(-rotationSharpness * dt));
            boostBlend = Mathf.Lerp(boostBlend, target.IsBoosting ? 1f : 0f, 1f - Mathf.Exp(-3f * dt));

            Vector3 localOffset = offset + new Vector3(0f, 0f, -boostPullback * boostBlend);
            Vector3 position = t.position + smoothedRotation * localOffset;
            Vector3 lookPoint = t.position + smoothedRotation * (Vector3.forward * lookAhead + Vector3.up * offset.y * 0.6f);
            Quaternion rotation = Quaternion.LookRotation(lookPoint - position, smoothedRotation * Vector3.up);

            if (trauma > 0f)
            {
                float s = trauma * trauma;
                float time = Time.time * 22f;
                rotation *= Quaternion.Euler(
                    (Mathf.PerlinNoise(time, 0.37f) - 0.5f) * 7f * s,
                    (Mathf.PerlinNoise(0.71f, time) - 0.5f) * 7f * s,
                    (Mathf.PerlinNoise(time, time * 0.5f) - 0.5f) * 9f * s);
                trauma = Mathf.Max(0f, trauma - dt * 1.3f);
            }

            transform.SetPositionAndRotation(position, rotation);
            cam.fieldOfView = Mathf.Lerp(Mathf.Lerp(baseFov, boostFov, boostBlend), warpFov, warpAmount);
        }
    }
}
