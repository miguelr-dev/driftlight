using UnityEngine;

namespace Driftlight
{
    /// <summary>Blinks a renderer (and an optional light) on and off, like a navigation beacon.</summary>
    public class Blinker : MonoBehaviour
    {
        public float period = 1.6f;
        [Range(0f, 1f)] public float onFraction = 0.25f;
        [Range(0f, 1f)] public float phase;

        Renderer target;
        Light lamp;

        void Awake()
        {
            target = GetComponent<Renderer>();
            lamp = GetComponentInChildren<Light>();
        }

        void Update()
        {
            bool on = Mathf.Repeat(Time.time / period + phase, 1f) < onFraction;
            if (target) target.enabled = on;
            if (lamp) lamp.enabled = on;
        }
    }
}
