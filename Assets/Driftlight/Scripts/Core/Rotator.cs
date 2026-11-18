using UnityEngine;

namespace Driftlight
{
    /// <summary>Spins an object at a constant rate (asteroids, station rings, pickups).</summary>
    public class Rotator : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0f, 10f, 0f);
        public Space space = Space.Self;

        void Update() => transform.Rotate(degreesPerSecond * Time.deltaTime, space);
    }
}
