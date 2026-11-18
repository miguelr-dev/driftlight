using UnityEngine;

namespace Driftlight
{
    /// <summary>
    /// Fills a huge sphere around the camera with star particles. The sphere follows the camera's
    /// position (not its rotation), so the stars look infinitely far away.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class Starfield : MonoBehaviour
    {
        public int starCount = 2600;
        public float radius = 6000f;
        public Vector2 sizeRange = new Vector2(5f, 26f);
        [Tooltip("Share of stars packed into the galaxy band (matches the nebula in the skybox).")]
        [Range(0f, 1f)] public float bandShare = 0.45f;
        public Vector3 bandNormal = new Vector3(0.35f, 0.9f, -0.2f);
        public int seed = 1234;

        static readonly Color[] Palette =
        {
            new Color(1f, 1f, 1f),
            new Color(0.72f, 0.84f, 1f),
            new Color(0.62f, 0.75f, 1f),
            new Color(1f, 0.88f, 0.72f),
            new Color(1f, 0.74f, 0.6f),
        };

        void Start()
        {
            var ps = GetComponent<ParticleSystem>();
            var main = ps.main;
            main.maxParticles = starCount;

            var random = new System.Random(seed);
            float Rand() => (float)random.NextDouble();
            Vector3 normal = bandNormal.normalized;

            var stars = new ParticleSystem.Particle[starCount];
            for (int i = 0; i < starCount; i++)
            {
                Vector3 dir = RandomOnSphere(Rand);
                if (Rand() < bandShare)
                {
                    // Squash toward the galactic plane.
                    float h = Vector3.Dot(dir, normal);
                    dir = (dir - normal * h * 0.85f).normalized;
                }

                float sizeT = Mathf.Pow(Rand(), 3.2f);
                float brightness = Mathf.Lerp(0.35f, 1f, Mathf.Pow(Rand(), 0.6f));
                Color c = Palette[random.Next(Palette.Length)] * brightness;
                c.a = 1f;

                stars[i].position = dir * radius;
                stars[i].startSize = Mathf.Lerp(sizeRange.x, sizeRange.y, sizeT);
                stars[i].startColor = c;
                stars[i].startLifetime = 1e6f;
                stars[i].remainingLifetime = 1e6f;
            }

            ps.SetParticles(stars, starCount);
            ps.Play();
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam) transform.position = cam.transform.position;
        }

        static Vector3 RandomOnSphere(System.Func<float> rand)
        {
            float z = rand() * 2f - 1f;
            float a = rand() * Mathf.PI * 2f;
            float r = Mathf.Sqrt(1f - z * z);
            return new Vector3(r * Mathf.Cos(a), z, r * Mathf.Sin(a));
        }
    }
}
