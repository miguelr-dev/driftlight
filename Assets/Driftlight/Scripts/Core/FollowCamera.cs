using UnityEngine;

namespace Driftlight
{
    /// <summary>Keeps an object centred on the main camera's position (ambient particles that should always surround the player).</summary>
    public class FollowCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam) transform.position = cam.transform.position;
        }
    }
}
