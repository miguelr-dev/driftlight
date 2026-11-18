using UnityEngine;

namespace Driftlight
{
    /// <summary>Destroys the GameObject after a delay (one-shot effects).</summary>
    public class AutoDestroy : MonoBehaviour
    {
        public float lifetime = 3f;

        void Start() => Destroy(gameObject, lifetime);
    }
}
