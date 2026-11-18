using UnityEngine;

namespace Driftlight.EditorTools
{
    /// <summary>
    /// Small, dependency-free 3D value noise used by the scene builder to generate skyboxes,
    /// planet textures, terrain and asteroid shapes. Deterministic: same seed, same result.
    /// </summary>
    public static class ProceduralNoise
    {
        static float Hash(int x, int y, int z, int seed)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u;
                h ^= (uint)x * 0x85EBCA6Bu;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 0xC2B2AE35u;
                h = (h << 13) | (h >> 19);
                h ^= (uint)z * 0x27D4EB2Fu;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                h *= 0x297A2D39u;
                h ^= h >> 15;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        static int Wrap(int v, int period) => period > 0 ? ((v % period) + period) % period : v;

        /// <summary>Smooth value noise in 0..1. A period above 0 makes it tile on x and z.</summary>
        public static float Value(float x, float y, float z, int seed, int period = 0)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y), zi = Mathf.FloorToInt(z);
            float xf = x - xi, yf = y - yi, zf = z - zi;
            float u = xf * xf * (3f - 2f * xf);
            float v = yf * yf * (3f - 2f * yf);
            float w = zf * zf * (3f - 2f * zf);

            int x0 = Wrap(xi, period), x1 = Wrap(xi + 1, period);
            int z0 = Wrap(zi, period), z1 = Wrap(zi + 1, period);
            int y0 = yi, y1 = yi + 1;

            float c000 = Hash(x0, y0, z0, seed), c100 = Hash(x1, y0, z0, seed);
            float c010 = Hash(x0, y1, z0, seed), c110 = Hash(x1, y1, z0, seed);
            float c001 = Hash(x0, y0, z1, seed), c101 = Hash(x1, y0, z1, seed);
            float c011 = Hash(x0, y1, z1, seed), c111 = Hash(x1, y1, z1, seed);

            float x00 = Mathf.Lerp(c000, c100, u), x10 = Mathf.Lerp(c010, c110, u);
            float x01 = Mathf.Lerp(c001, c101, u), x11 = Mathf.Lerp(c011, c111, u);
            return Mathf.Lerp(Mathf.Lerp(x00, x10, v), Mathf.Lerp(x01, x11, v), w);
        }

        /// <summary>Fractal (fBm) noise in roughly 0..1.</summary>
        public static float Fbm(Vector3 p, int octaves, int seed, float lacunarity = 2.03f, float gain = 0.5f)
        {
            float sum = 0f, amplitude = 0.5f, norm = 0f, frequency = 1f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * Value(p.x * frequency, p.y * frequency, p.z * frequency, seed + i * 131);
                norm += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }
            return sum / norm;
        }

        /// <summary>fBm that tiles on x/z with the given base period (for seamless ground textures).</summary>
        public static float FbmTiled(float x, float z, int period, int octaves, int seed)
        {
            float sum = 0f, amplitude = 0.5f, norm = 0f;
            int frequency = 1;
            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * Value(x * frequency, 0.5f, z * frequency, seed + i * 131, period * frequency);
                norm += amplitude;
                amplitude *= 0.5f;
                frequency *= 2;
            }
            return sum / norm;
        }

        /// <summary>Ridged noise in 0..1: sharp crests, good for mountains and glowing veins.</summary>
        public static float Ridged(Vector3 p, int octaves, int seed)
        {
            float sum = 0f, amplitude = 0.5f, norm = 0f, frequency = 1f;
            for (int i = 0; i < octaves; i++)
            {
                float n = Value(p.x * frequency, p.y * frequency, p.z * frequency, seed + i * 71);
                n = 1f - Mathf.Abs(n * 2f - 1f);
                sum += amplitude * n * n;
                norm += amplitude;
                amplitude *= 0.5f;
                frequency *= 2.1f;
            }
            return sum / norm;
        }

        public static float Smoothstep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Direction for a pixel of a latitude-longitude (equirectangular) texture, matching the
        /// mapping of Unity's Skybox/Panoramic shader. u and v are 0..1, v = 0 is the bottom row.
        /// </summary>
        public static Vector3 DirectionFromLatLong(float u, float v)
        {
            float lon = (0.5f - u) * 2f * Mathf.PI;
            float lat = (1f - v) * Mathf.PI;
            float s = Mathf.Sin(lat);
            return new Vector3(s * Mathf.Cos(lon), Mathf.Cos(lat), s * Mathf.Sin(lon));
        }
    }
}
