using UnityEngine;

namespace Driftlight
{
    /// <summary>Fades a light out quickly after it spawns (explosion and muzzle flashes).</summary>
    [RequireComponent(typeof(Light))]
    public class LightFlash : MonoBehaviour
    {
        public float duration = 0.4f;

        Light lamp;
        float startIntensity;
        float age;

        void Awake()
        {
            lamp = GetComponent<Light>();
            startIntensity = lamp.intensity;
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01(1f - age / duration);
            lamp.intensity = startIntensity * k * k;
            if (k <= 0f) lamp.enabled = false;
        }
    }
}
