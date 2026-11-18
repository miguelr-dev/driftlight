using UnityEngine;

namespace Driftlight
{
    /// <summary>Makes the engine glow, trails and light follow the ship's throttle and boost.</summary>
    public class EngineGlow : MonoBehaviour
    {
        public ShipController ship;
        public Renderer[] glowRenderers;
        public TrailRenderer[] trails;
        public Light engineLight;
        [ColorUsage(false, true)] public Color glowColor = new Color(0.45f, 1.6f, 4f);
        [Range(0f, 1f)] public float idleLevel = 0.3f;
        public float boostMultiplier = 2.2f;
        public float lightIntensity = 4f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;
        float level;
        float[] trailWidths;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (!ship) ship = GetComponentInParent<ShipController>();
            trailWidths = new float[trails.Length];
            for (int i = 0; i < trails.Length; i++) trailWidths[i] = trails[i].widthMultiplier;
        }

        void Update()
        {
            if (!ship) return;
            float forward01 = Mathf.Clamp01(ship.ForwardSpeed / Mathf.Max(1f, ship.cruiseSpeed));
            float target = Mathf.Lerp(idleLevel, 1f, forward01) * (ship.IsBoosting ? boostMultiplier : 1f);
            level = Mathf.Lerp(level, target, 1f - Mathf.Exp(-8f * Time.deltaTime));

            block.SetColor(BaseColorId, glowColor * level);
            foreach (var r in glowRenderers)
                if (r) r.SetPropertyBlock(block);

            for (int i = 0; i < trails.Length; i++)
            {
                if (!trails[i]) continue;
                trails[i].widthMultiplier = trailWidths[i] * Mathf.Clamp(level, 0.15f, 1.6f);
                trails[i].emitting = ship.ForwardSpeed > 4f;
            }

            if (engineLight) engineLight.intensity = level * lightIntensity;
        }
    }
}
