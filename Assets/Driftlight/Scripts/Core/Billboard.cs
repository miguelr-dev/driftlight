using UnityEngine;

namespace Driftlight
{
    /// <summary>Keeps a quad facing the camera (sun corona, glow sprites).</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
        }
    }
}
