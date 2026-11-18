using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.PrimitiveType;

namespace Driftlight.EditorTools
{
    /// <summary>Verdance, planet 1: a glowing alien jungle at dusk, with the first signal fragment in a ring of ruins.</summary>
    public static partial class DriftlightBuilder
    {
        public const string VerdanceScenePath = ScenesDir + "/Verdance.unity";

        const float VerdanceSize = 900f;
        const float VerdanceMaxHeight = 90f;
        const int VerdanceHeightResolution = 513;
        const int VerdanceAlphaResolution = 512;

        static readonly Vector3 VerdanceSunDirection = new Vector3(0.35f, 0.2f, 0.9f).normalized;
        static readonly Vector3 GasGiantDirection = new Vector3(0.9f, 0.4f, 0.26f).normalized;
        static readonly Vector2 FragmentSite = new Vector2(120f, 140f);

        const string VerdanceLore =
            "\"The gardens sang again tonight. Every flower on Verdance turned to the sky at the same moment, " +
            "as if something up there had called our names. The elders say it is only the season. " +
            "But the signal is in the soil now, in the roots, in us. Tomorrow, all of us will answer it.\"";

        [MenuItem("DRIFTLIGHT/Rebuild Verdance Scene (overwrites it)", priority = 11)]
        static void RebuildVerdanceMenu()
        {
            if (Confirm("Verdance.unity")) BuildVerdanceScene();
        }

        public static void BuildVerdanceScene()
        {
            EnsureFolders();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var heights = VerdanceHeights();
            float H(float x, float z) => SampleHeight(heights, x, z);

            RenderSettings.skybox = PanoramicSkybox("Verdance Skybox", VerdanceSkyTexture(), 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0082f;
            RenderSettings.fogColor = ToGamma(AverageHorizon());
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.32f, 0.38f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.3f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.11f, 0.1f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.5f;
            Sun(VerdanceSunDirection, new Color(1f, 0.64f, 0.48f), 1.3f);

            var world = new GameObject("World").transform;
            BuildVerdanceTerrain(world, heights);
            var random = new System.Random(2024);
            BuildFlora(world, random, H);
            var fragmentGround = new Vector3(FragmentSite.x, H(FragmentSite.x, FragmentSite.y), FragmentSite.y);
            BuildRuins(world, fragmentGround, H, random);
            BuildFragment(world, fragmentGround);
            BuildLandedShip(world, H);
            BuildFootPlayer(H, fragmentGround);

            GlobalVolume(VerdanceProfile());

            var systems = new GameObject("Game Systems");
            systems.AddComponent<CursorLock>();
            systems.AddComponent<PlanetArrival>();
            new GameObject("HUD").AddComponent<Hud>();

            SaveScene(scene, VerdanceScenePath, 1);
        }

        // ------------------------------------------------------------------ terrain

        static float VerdanceRawHeight01(float x, float z)
        {
            float hills = ProceduralNoise.Fbm(new Vector3(x / 230f, 0.37f, z / 230f), 5, 501);
            float ridges = ProceduralNoise.Ridged(new Vector3(x / 150f + 3.1f, 1.7f, z / 150f - 2.3f), 4, 502);
            float h = 0.1f + hills * 0.22f + ridges * 0.12f;
            float r = Mathf.Sqrt(x * x + z * z);
            h += ProceduralNoise.Smoothstep(300f, 440f, r) * 0.55f * (0.6f + 0.4f * hills); // mountains around the edge
            return h;
        }

        static float VerdanceHeight01(float x, float z)
        {
            float h = VerdanceRawHeight01(x, z);

            // A low hill for the ruins, with a flat top.
            float toSite = Vector2.Distance(new Vector2(x, z), FragmentSite);
            h += Mathf.Exp(-toSite * toSite / (2f * 28f * 28f)) * 0.08f;
            h = Mathf.Lerp(h, VerdanceRawHeight01(FragmentSite.x, FragmentSite.y) + 0.08f, ProceduralNoise.Smoothstep(16f, 8f, toSite));

            // A flat clearing where the ship landed.
            float r = Mathf.Sqrt(x * x + z * z);
            h = Mathf.Lerp(h, VerdanceRawHeight01(0f, 0f), ProceduralNoise.Smoothstep(45f, 14f, r));
            return Mathf.Clamp01(h);
        }

        static float[,] VerdanceHeights()
        {
            int n = VerdanceHeightResolution;
            var heights = new float[n, n];
            for (int iz = 0; iz < n; iz++)
            for (int ix = 0; ix < n; ix++)
            {
                float x = -VerdanceSize * 0.5f + ix / (float)(n - 1) * VerdanceSize;
                float z = -VerdanceSize * 0.5f + iz / (float)(n - 1) * VerdanceSize;
                heights[iz, ix] = VerdanceHeight01(x, z);
            }
            return heights;
        }

        /// <summary>World height (metres) at x/z, bilinearly sampled from the heightmap.</summary>
        static float SampleHeight(float[,] heights, float x, float z)
        {
            int n = heights.GetLength(0);
            float fx = Mathf.Clamp((x / VerdanceSize + 0.5f) * (n - 1), 0f, n - 1.001f);
            float fz = Mathf.Clamp((z / VerdanceSize + 0.5f) * (n - 1), 0f, n - 1.001f);
            int ix = (int)fx, iz = (int)fz;
            float tx = fx - ix, tz = fz - iz;
            float a = Mathf.Lerp(heights[iz, ix], heights[iz, ix + 1], tx);
            float b = Mathf.Lerp(heights[iz + 1, ix], heights[iz + 1, ix + 1], tx);
            return Mathf.Lerp(a, b, tz) * VerdanceMaxHeight;
        }

        static void BuildVerdanceTerrain(Transform world, float[,] heights)
        {
            var moss = TerrainLayerAsset("Verdance Moss", GroundTexture("Verdance Moss", new Color(0.06f, 0.24f, 0.18f), new Color(0.13f, 0.38f, 0.26f), 601), 14f, 0.1f);
            var grass = TerrainLayerAsset("Verdance Grass", GroundTexture("Verdance Grass", new Color(0.04f, 0.14f, 0.07f), new Color(0.1f, 0.26f, 0.11f), 602), 11f, 0.08f);
            var rock = TerrainLayerAsset("Verdance Rock", GroundTexture("Verdance Rock", new Color(0.2f, 0.17f, 0.22f), new Color(0.36f, 0.32f, 0.38f), 603), 18f, 0.12f);

            string path = $"{SettingsDir}/Verdance Terrain.asset";
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, path);
            }
            data.heightmapResolution = VerdanceHeightResolution;
            data.size = new Vector3(VerdanceSize, VerdanceMaxHeight, VerdanceSize);
            data.SetHeights(0, 0, heights);
            data.alphamapResolution = VerdanceAlphaResolution;
            data.terrainLayers = new[] { moss, grass, rock };
            data.SetAlphamaps(0, 0, VerdanceAlphamaps(heights));
            EditorUtility.SetDirty(data);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.SetParent(world, false);
            go.transform.position = new Vector3(-VerdanceSize * 0.5f, 0f, -VerdanceSize * 0.5f);
            var terrain = go.GetComponent<Terrain>();
            terrain.materialTemplate = MaterialAsset("Verdance Terrain", "Universal Render Pipeline/Terrain/Lit");
            terrain.heightmapPixelError = 3f;
            terrain.basemapDistance = 400f;
            terrain.drawInstanced = true;
        }

        static float[,,] VerdanceAlphamaps(float[,] heights)
        {
            int n = VerdanceAlphaResolution;
            int hn = heights.GetLength(0);
            float cell = VerdanceSize / (hn - 1);
            var alphas = new float[n, n, 3];
            for (int iz = 0; iz < n; iz++)
            for (int ix = 0; ix < n; ix++)
            {
                int hx = Mathf.Clamp(Mathf.RoundToInt(ix / (float)(n - 1) * (hn - 1)), 1, hn - 2);
                int hz = Mathf.Clamp(Mathf.RoundToInt(iz / (float)(n - 1) * (hn - 1)), 1, hn - 2);
                float dx = (heights[hz, hx + 1] - heights[hz, hx - 1]) * VerdanceMaxHeight / (2f * cell);
                float dz = (heights[hz + 1, hx] - heights[hz - 1, hx]) * VerdanceMaxHeight / (2f * cell);
                float slope = Mathf.Sqrt(dx * dx + dz * dz);

                float x = -VerdanceSize * 0.5f + ix / (float)(n - 1) * VerdanceSize;
                float z = -VerdanceSize * 0.5f + iz / (float)(n - 1) * VerdanceSize;
                float patches = ProceduralNoise.Fbm(new Vector3(x / 45f, 4.2f, z / 45f), 4, 611);

                float rockWeight = ProceduralNoise.Smoothstep(0.45f, 0.85f, slope);
                float mossWeight = ProceduralNoise.Smoothstep(0.38f, 0.62f, patches) * (1f - rockWeight);
                float grassWeight = Mathf.Max(0f, 1f - rockWeight - mossWeight);
                float total = rockWeight + mossWeight + grassWeight;
                alphas[iz, ix, 0] = mossWeight / total;
                alphas[iz, ix, 1] = grassWeight / total;
                alphas[iz, ix, 2] = rockWeight / total;
            }
            return alphas;
        }

        static Texture2D GroundTexture(string name, Color dark, Color light, int seed)
        {
            const int size = 512;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var dither = new System.Random(seed);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float broad = ProceduralNoise.FbmTiled(u * 8f, v * 8f, 8, 5, seed);
                float fine = ProceduralNoise.FbmTiled(u * 32f, v * 32f, 32, 3, seed + 1);
                Color c = Color.Lerp(dark, light, ProceduralNoise.Smoothstep(0.3f, 0.7f, broad)) * (0.8f + 0.4f * fine);
                var pixel = ToSrgb32(c, dither);
                pixel.a = (byte)(20 + fine * 30f); // URP terrain reads smoothness from alpha: keep the ground matte
                pixels[y * size + x] = pixel;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return SaveTexture(texture, name, alpha: true, maxSize: 512);
        }

        static TerrainLayer TerrainLayerAsset(string name, Texture2D texture, float tileSize, float smoothness)
        {
            string path = $"{SettingsDir}/{name}.terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture = texture;
            layer.tileSize = new Vector2(tileSize, tileSize);
            layer.smoothness = smoothness;
            layer.metallic = 0f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        // ------------------------------------------------------------------ sky

        static Texture2D VerdanceSkyTexture()
        {
            var texture = LatLongTexture(2048, 1024, VerdanceSky, 5);
            return SaveTexture(texture, "Verdance Sky", srgb: true, mipmaps: false,
                wrapU: TextureWrapMode.Repeat, wrapV: TextureWrapMode.Clamp, compress: false, maxSize: 2048);
        }

        static Color VerdanceSky(Vector3 d)
        {
            Color zenith = new Color(0.025f, 0.06f, 0.15f);
            Color middle = new Color(0.12f, 0.12f, 0.28f);
            Color horizon = new Color(0.62f, 0.3f, 0.36f);
            Color below = new Color(0.08f, 0.07f, 0.1f);

            Color c;
            if (d.y >= 0f)
            {
                float t = Mathf.Pow(Mathf.Clamp01(d.y), 0.45f);
                c = t < 0.5f ? Color.Lerp(horizon, middle, t / 0.5f) : Color.Lerp(middle, zenith, (t - 0.5f) / 0.5f);
            }
            else
            {
                c = Color.Lerp(horizon, below, Mathf.Clamp01(-d.y * 6f));
            }

            // Aurora-like wisps high in the sky.
            float wisp = ProceduralNoise.Fbm(new Vector3(d.x * 2.2f, d.y * 6f, d.z * 2.2f) + new Vector3(4f, 1f, 7f), 4, 701);
            c += new Color(0.03f, 0.17f, 0.13f) * ProceduralNoise.Smoothstep(0.55f, 0.8f, wisp) * ProceduralNoise.Smoothstep(0.2f, 0.65f, d.y);

            // Faint stars near the top.
            float star = ProceduralNoise.Value(d.x * 300f, d.y * 300f, d.z * 300f, 77);
            if (star > 0.985f) c += Color.white * ((star - 0.985f) / 0.015f) * 0.5f * ProceduralNoise.Smoothstep(0.25f, 0.7f, d.y);

            // The setting sun.
            float s = Mathf.Max(0f, Vector3.Dot(d, VerdanceSunDirection));
            c += new Color(1f, 0.5f, 0.25f) * (Mathf.Pow(s, 8f) * 0.35f + Mathf.Pow(s, 120f) * 0.8f);
            c = Color.Lerp(c, new Color(1f, 0.86f, 0.72f), ProceduralNoise.Smoothstep(0.99955f, 0.9998f, s));

            return GasGiant(d, c);
        }

        static Color GasGiant(Vector3 d, Color sky)
        {
            Vector3 p = GasGiantDirection;
            float radius = 11f * Mathf.Deg2Rad;
            if (Vector3.Dot(d, p) < Mathf.Cos(radius * 1.25f)) return sky;

            Vector3 right = Vector3.Cross(Vector3.up, p).normalized;
            Vector3 up = Vector3.Cross(p, right);
            float sinR = Mathf.Sin(radius);
            float x = Vector3.Dot(d, right) / sinR, y = Vector3.Dot(d, up) / sinR;
            float r = Mathf.Sqrt(x * x + y * y);

            if (r >= 1f)
            {
                float halo = Mathf.Exp(-(r - 1f) * 16f) * 0.22f;
                return sky + new Color(0.35f, 0.55f, 0.6f) * halo;
            }

            float z = Mathf.Sqrt(1f - r * r);
            Vector3 normal = (right * x + up * y - p * z).normalized;
            Vector3 axis = (Quaternion.AngleAxis(20f, p) * up).normalized;
            float latitude = Vector3.Dot(normal, axis);
            float bands = Mathf.Sin(latitude * 18f + ProceduralNoise.Fbm(normal * 4f, 4, 88) * 3f);
            Color surface = Color.Lerp(new Color(0.55f, 0.62f, 0.6f), new Color(0.3f, 0.36f, 0.42f), bands * 0.5f + 0.5f);
            surface = Color.Lerp(surface, new Color(0.65f, 0.45f, 0.4f), ProceduralNoise.Smoothstep(0.6f, 0.9f, ProceduralNoise.Fbm(normal * 9f, 3, 89)) * 0.4f);

            float light = Mathf.Clamp01(Vector3.Dot(normal, VerdanceSunDirection) * 0.7f + 0.45f); // fantasy sky: keep the bands readable on the night side
            float rim = Mathf.Pow(1f - z, 3f);
            Color planet = surface * light + new Color(0.35f, 0.5f, 0.55f) * rim * 0.4f * Mathf.Clamp01(light + 0.3f);
            float edge = ProceduralNoise.Smoothstep(1f, 0.985f, r);
            return Color.Lerp(sky, planet * 0.85f + sky * 0.15f, edge);
        }

        static Color AverageHorizon()
        {
            Color sum = Color.black;
            const int samples = 24;
            for (int i = 0; i < samples; i++)
            {
                float a = i * Mathf.PI * 2f / samples;
                sum += VerdanceSky(new Vector3(Mathf.Cos(a), 0.03f, Mathf.Sin(a)).normalized);
            }
            return sum / samples;
        }

        static Color ToGamma(Color linear) => new Color(Mathf.LinearToGammaSpace(linear.r), Mathf.LinearToGammaSpace(linear.g), Mathf.LinearToGammaSpace(linear.b), 1f);

        // ------------------------------------------------------------------ flora

        static void BuildFlora(Transform world, System.Random random, Func<float, float, float> H)
        {
            float R() => (float)random.NextDouble();
            var flora = Empty("Flora", world, Vector3.zero).transform;

            var stem = Lit("Mushroom Stem", new Color(0.62f, 0.6f, 0.55f), 0.3f, 0f, new Color(0.04f, 0.06f, 0.05f));
            var capColours = new[] { new Color(0.25f, 1.7f, 1.45f), new Color(1.7f, 0.3f, 1.4f), new Color(1.8f, 0.85f, 0.2f) };
            var caps = new[]
            {
                Lit("Cap Teal", new Color(0.05f, 0.25f, 0.22f), 0.55f, 0f, capColours[0]),
                Lit("Cap Magenta", new Color(0.25f, 0.05f, 0.2f), 0.55f, 0f, capColours[1]),
                Lit("Cap Amber", new Color(0.3f, 0.15f, 0.04f), 0.55f, 0f, capColours[2]),
            };
            var trunk = Lit("Tree Trunk", new Color(0.12f, 0.09f, 0.11f), 0.2f);
            var canopy = Lit("Tree Canopy", new Color(0.03f, 0.13f, 0.1f), 0.35f, 0f, new Color(0f, 0.06f, 0.05f));
            var pod = Unlit("Glow Pod", new Color(0.4f, 2.2f, 1.8f));
            var crystal = Lit("Crystal", new Color(0.12f, 0.05f, 0.2f), 0.9f, 0f, new Color(1.1f, 0.35f, 2.6f));
            var rock = Lit("Mossy Rock", new Color(0.17f, 0.21f, 0.19f), 0.15f);
            var rockMeshes = new Mesh[4];
            for (int i = 0; i < rockMeshes.Length; i++) rockMeshes[i] = SaveMesh(AsteroidMesh(i + 1), $"Asteroid {i + 1}");

            // Keeps the landing site, the ruins and a trail between them clear.
            bool Open(Vector2 p, float spawnRadius) =>
                p.magnitude > spawnRadius && Vector2.Distance(p, FragmentSite) > 19f && p.magnitude < 425f
                && DistanceToSegment(new Vector3(p.x, 0f, p.y), Vector3.zero, new Vector3(FragmentSite.x, 0f, FragmentSite.y)) > 6f;
            Vector2 RandomPoint(float maxRadius)
            {
                float a = R() * Mathf.PI * 2f, r = Mathf.Sqrt(R()) * maxRadius;
                return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            float Density(Vector2 p, int seed) => ProceduralNoise.Fbm(new Vector3(p.x / 70f, seed * 0.37f, p.y / 70f), 3, seed);

            int placed = 0, tries = 0;
            while (placed < 360 && tries++ < 9000)
            {
                var p = RandomPoint(415f);
                if (!Open(p, 12f) || R() > Density(p, 11) * 1.6f) continue;
                int kind = random.Next(caps.Length);
                Mushroom(flora, p, H, Mathf.Lerp(0.5f, 2.6f, Mathf.Pow(R(), 2f)), stem, caps[kind], capColours[kind], random, giant: false);
                placed++;
            }

            // Small glowing mushrooms around the landing site, so the first view isn't empty.
            placed = 0;
            tries = 0;
            while (placed < 34 && tries++ < 3000)
            {
                var p = RandomPoint(18f);
                if (!Open(p, 3.5f) || Vector2.Distance(p, new Vector2(-11f, 9f)) < 6.5f) continue;
                int kind = random.Next(caps.Length);
                Mushroom(flora, p, H, Mathf.Lerp(0.35f, 1.2f, R()), stem, caps[kind], capColours[kind], random, giant: false);
                placed++;
            }

            placed = 0;
            tries = 0;
            while (placed < 26 && tries++ < 2000)
            {
                var p = RandomPoint(400f);
                if (!Open(p, 30f)) continue;
                int kind = random.Next(caps.Length);
                Mushroom(flora, p, H, Mathf.Lerp(5f, 10f, R()), stem, caps[kind], capColours[kind], random, giant: true);
                placed++;
            }

            placed = 0;
            tries = 0;
            while (placed < 150 && tries++ < 9000)
            {
                var p = RandomPoint(420f);
                if (!Open(p, 18f) || R() > Density(p, 23) * 1.5f) continue;
                Tree(flora, p, H, trunk, canopy, pod, random);
                placed++;
            }

            placed = 0;
            tries = 0;
            while (placed < 70 && tries++ < 4000)
            {
                var p = RandomPoint(410f);
                if (!Open(p, 6f)) continue;
                CrystalCluster(flora, p, H, crystal, random, withLight: placed % 4 == 0);
                placed++;
            }

            placed = 0;
            tries = 0;
            while (placed < 90 && tries++ < 4000)
            {
                var p = RandomPoint(420f);
                if (!Open(p, 12f)) continue;
                float size = Mathf.Lerp(0.8f, 6f, Mathf.Pow(R(), 1.8f));
                var go = MeshPart("Rock", rockMeshes[random.Next(rockMeshes.Length)], flora,
                    new Vector3(p.x, H(p.x, p.y) - size * 0.3f, p.y), new Vector3(R() * 360f, R() * 360f, R() * 360f),
                    new Vector3(size * Mathf.Lerp(0.8f, 1.3f, R()), size * Mathf.Lerp(0.5f, 0.9f, R()), size * Mathf.Lerp(0.8f, 1.3f, R())), rock);
                var collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                collider.convex = true;
                placed++;
            }

            foreach (var t in flora.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }

        static void Mushroom(Transform parent, Vector2 p, Func<float, float, float> H, float size, Material stem, Material cap,
            Color glow, System.Random random, bool giant)
        {
            float R() => (float)random.NextDouble();
            var root = Empty(giant ? "Giant Mushroom" : "Mushroom", parent, new Vector3(p.x, H(p.x, p.y) - 0.05f, p.y)).transform;
            root.localRotation = Quaternion.Euler((R() - 0.5f) * 14f, R() * 360f, (R() - 0.5f) * 14f);

            float height = size * Mathf.Lerp(0.8f, 1.4f, R());
            float stemRadius = size * 0.12f;
            Part("Stem", Cylinder, root, new Vector3(0f, height * 0.5f, 0f), Vector3.zero, new Vector3(stemRadius * 2f, height * 0.5f, stemRadius * 2f), stem, keepCollider: giant);
            float capWidth = size * Mathf.Lerp(0.9f, 1.4f, R());
            Part("Cap", Sphere, root, new Vector3(0f, height, 0f), Vector3.zero, new Vector3(capWidth, capWidth * 0.38f, capWidth), cap, keepCollider: giant);

            if (giant)
            {
                float peak = Mathf.Max(glow.r, Mathf.Max(glow.g, glow.b));
                PointLight("Glow", root, new Vector3(0f, height - capWidth * 0.3f, 0f), new Color(glow.r / peak, glow.g / peak, glow.b / peak), 2.5f, capWidth * 2.2f);
            }
        }

        static void Tree(Transform parent, Vector2 p, Func<float, float, float> H, Material trunk, Material canopy, Material pod, System.Random random)
        {
            float R() => (float)random.NextDouble();
            float height = Mathf.Lerp(9f, 22f, R());
            float radius = Mathf.Lerp(0.45f, 1f, R());
            var root = Empty("Tree", parent, new Vector3(p.x, H(p.x, p.y) - 0.3f, p.y)).transform;
            root.localRotation = Quaternion.Euler((R() - 0.5f) * 10f, R() * 360f, (R() - 0.5f) * 10f);

            Part("Trunk", Cylinder, root, new Vector3(0f, height * 0.5f, 0f), Vector3.zero, new Vector3(radius * 2f, height * 0.5f, radius * 2f), trunk, keepCollider: true);
            int blobs = 2 + random.Next(3);
            for (int i = 0; i < blobs; i++)
            {
                float width = Mathf.Lerp(5f, 10f, R()) * (radius + 0.5f);
                var offset = new Vector3((R() - 0.5f) * width * 0.5f, height - R() * 2.5f + i * 0.8f, (R() - 0.5f) * width * 0.5f);
                Part("Canopy", Sphere, root, offset, Vector3.zero, new Vector3(width, width * 0.38f, width), canopy);
            }
            int pods = 2 + random.Next(5);
            for (int i = 0; i < pods; i++)
            {
                float a = R() * Mathf.PI * 2f, d = Mathf.Lerp(1.5f, 4f, R());
                Part("Pod", Sphere, root, new Vector3(Mathf.Cos(a) * d, height - 1.6f - R() * 2f, Mathf.Sin(a) * d), Vector3.zero, Vector3.one * Mathf.Lerp(0.25f, 0.5f, R()), pod);
            }
        }

        static void CrystalCluster(Transform parent, Vector2 p, Func<float, float, float> H, Material crystal, System.Random random, bool withLight)
        {
            float R() => (float)random.NextDouble();
            var root = Empty("Crystals", parent, new Vector3(p.x, H(p.x, p.y) - 0.2f, p.y)).transform;
            float scale = Mathf.Lerp(0.6f, 1.8f, R());
            int count = 3 + random.Next(4);
            for (int i = 0; i < count; i++)
            {
                float h = Mathf.Lerp(0.8f, 3.4f, R()) * scale;
                float w = Mathf.Lerp(0.25f, 0.5f, R()) * scale;
                Part("Crystal", Cube, root, new Vector3((R() - 0.5f) * 1.2f * scale, h * 0.35f, (R() - 0.5f) * 1.2f * scale),
                    new Vector3((R() - 0.5f) * 50f, R() * 360f, (R() - 0.5f) * 50f), new Vector3(w, h, w), crystal);
            }
            if (withLight) PointLight("Crystal Light", root, new Vector3(0f, 1.5f * scale, 0f), new Color(0.6f, 0.3f, 1f), 2f, 9f * scale);
        }

        // ------------------------------------------------------------------ the ruins and the fragment

        static void BuildRuins(Transform world, Vector3 centre, Func<float, float, float> H, System.Random random)
        {
            float R() => (float)random.NextDouble();
            var stone = Lit("Ruin Stone", new Color(0.4f, 0.39f, 0.43f), 0.25f);
            var glyph = Unlit("Ruin Glyphs", new Color(0.6f, 1.8f, 2.4f));
            var ruins = Empty("Ruins", world, centre).transform;

            Part("Dais", Cylinder, ruins, new Vector3(0f, 0.15f, 0f), Vector3.zero, new Vector3(9f, 0.35f, 9f), stone, keepCollider: true);
            Part("Dais Ring", Cylinder, ruins, new Vector3(0f, 0.33f, 0f), Vector3.zero, new Vector3(9.4f, 0.03f, 9.4f), glyph);

            const int pillars = 9;
            for (int i = 0; i < pillars; i++)
            {
                float a = i * Mathf.PI * 2f / pillars;
                var radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 position = radial * 10.5f;
                float groundY = H(centre.x + position.x, centre.z + position.z) - centre.y;

                if (i == 4)
                {
                    var tangent = new Vector3(-radial.z, 0f, radial.x);
                    Part("Fallen Pillar", Cylinder, ruins, position + new Vector3(0f, groundY + 0.7f, 0f),
                        Quaternion.FromToRotation(Vector3.up, tangent).eulerAngles, new Vector3(1.4f, 3.2f, 1.4f), stone, keepCollider: true);
                    continue;
                }

                bool broken = i % 3 == 1;
                float height = broken ? Mathf.Lerp(1.5f, 3f, R()) : Mathf.Lerp(5.5f, 7.5f, R());
                Part("Pillar", Cylinder, ruins, position + new Vector3(0f, groundY + height * 0.5f - 0.3f, 0f),
                    new Vector3((R() - 0.5f) * 6f, 0f, (R() - 0.5f) * 6f), new Vector3(1.4f, height * 0.5f, 1.4f), stone, keepCollider: true);
                Part("Glyph", Cube, ruins, radial * 9.78f + new Vector3(0f, groundY + Mathf.Min(height - 0.8f, 3f), 0f),
                    Quaternion.LookRotation(-radial).eulerAngles, new Vector3(0.5f, 1.8f, 0.05f), glyph);
            }

            // An archway facing the landing site.
            var toSpawn = new Vector3(-centre.x, 0f, -centre.z).normalized;
            var side = Vector3.Cross(Vector3.up, toSpawn);
            Vector3 archCentre = toSpawn * 17f;
            float archGround = H(centre.x + archCentre.x, centre.z + archCentre.z) - centre.y;
            foreach (float s in new[] { -1f, 1f })
                Part("Arch Pillar", Cube, ruins, archCentre + side * 3.6f * s + new Vector3(0f, archGround + 3.7f, 0f),
                    Quaternion.LookRotation(toSpawn).eulerAngles, new Vector3(1.3f, 8f, 1.3f), stone, keepCollider: true);
            Part("Arch Lintel", Cube, ruins, archCentre + new Vector3(0f, archGround + 8.1f, 0f),
                Quaternion.LookRotation(toSpawn).eulerAngles, new Vector3(9f, 1.1f, 1.5f), stone, keepCollider: true);
            Part("Arch Glyph", Cube, ruins, archCentre + toSpawn * 0.78f + new Vector3(0f, archGround + 8.1f, 0f),
                Quaternion.LookRotation(toSpawn).eulerAngles, new Vector3(6f, 0.25f, 0.05f), glyph);
        }

        static void BuildFragment(Transform world, Vector3 ground)
        {
            var root = Empty("Signal Fragment", world, ground + new Vector3(0f, 0.35f, 0f));
            var pivot = Empty("Crystal", root.transform, new Vector3(0f, 2.2f, 0f)).transform;
            var core = Unlit("Fragment Core", new Color(4f, 1.4f, 4.6f));
            Part("Shard", Cube, pivot, Vector3.zero, new Vector3(45f, 0f, 45f), Vector3.one * 0.9f, core);
            Part("Shard Inner", Cube, pivot, Vector3.zero, new Vector3(0f, 45f, 35f), Vector3.one * 0.6f, core);
            var halo = Part("Halo", Sphere, pivot, Vector3.zero, Vector3.zero, Vector3.one * 4f,
                Glow("Fragment Halo", new Color(1.4f, 0.45f, 1.6f), atmosphere: false, power: 2.5f));
            var light = PointLight("Fragment Light", root.transform, new Vector3(0f, 2.2f, 0f), new Color(1f, 0.45f, 1f), 7f, 28f);
            var beam = Part("Beam", Cylinder, root.transform, new Vector3(0f, 160f, 0f), Vector3.zero, new Vector3(1.6f, 160f, 1.6f),
                Glow("Signal Beam", new Color(0.9f, 0.35f, 1.3f), atmosphere: false, power: 1.6f));
            beam.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            var fragment = root.AddComponent<SignalFragment>();
            fragment.crystal = pivot;
            fragment.glowLight = light;
            fragment.hideWhenCollected = new[] { beam.GetComponent<Renderer>(), halo.GetComponent<Renderer>() };
            fragment.collectEffect = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Warp In Flash.prefab");
            fragment.fragmentTitle = "SIGNAL FRAGMENT 1 / 3";
            fragment.loreText = VerdanceLore;
            fragment.afterObjective = "Fragment recovered. The signal goes deeper into the system (next planet coming soon)";
        }

        static void BuildLandedShip(Transform world, Func<float, float, float> H)
        {
            var spot = new Vector2(-11f, 9f);
            var ship = Empty("Landed Ship", world, new Vector3(spot.x, H(spot.x, spot.y) + 1.55f, spot.y)).transform;
            ship.localRotation = Quaternion.Euler(-4f, 150f, 3f);
            BuildShipModel(ship, withTrails: false);

            var dark = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/Ship Dark.mat");
            foreach (var leg in new[] { new Vector3(0f, -0.95f, 1.7f), new Vector3(-1.7f, -0.95f, -1.5f), new Vector3(1.7f, -0.95f, -1.5f) })
            {
                Part("Landing Leg", Cylinder, ship, leg, Vector3.zero, new Vector3(0.18f, 0.9f, 0.18f), dark);
                Part("Landing Foot", Cylinder, ship, leg + new Vector3(0f, -0.9f, 0f), Vector3.zero, new Vector3(0.7f, 0.06f, 0.7f), dark);
            }
            var box = ship.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, -0.3f, -0.6f);
            box.size = new Vector3(7.4f, 2.2f, 7f);
        }

        static void BuildFootPlayer(Func<float, float, float> H, Vector3 lookTarget)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, H(0f, 0f) + 0.05f, 0f);
            Vector3 toTarget = lookTarget - player.transform.position;
            player.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg, 0f);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.45f;
            controller.slopeLimit = 50f;

            var pivot = Empty("Camera Pivot", player.transform, new Vector3(0f, 1.65f, 0f)).transform;
            var cam = MainCamera(Vector3.zero, Quaternion.identity, 2500f);
            cam.transform.SetParent(pivot, false);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            cam.fieldOfView = 70f;

            player.AddComponent<PlayerFootController>().cameraPivot = pivot;
            player.AddComponent<PlayerFootInput>();
            BuildSpores(player.transform);
        }

        static void BuildSpores(Transform parent)
        {
            var go = Empty("Spores", parent, new Vector3(0f, 2f, 0f));
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 11f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 1f, 0.9f), new Color(1f, 0.5f, 0.95f));
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 45f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(60f, 14f, 60f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.2f;
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            colour.color = FadeInOut();

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Additive("Spores", new Color(1.6f, 1.8f, 1.8f, 1f), GlowTexture());
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static VolumeProfile VerdanceProfile() => CreateProfile("Verdance Post Processing", profile =>
        {
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.85f;
            bloom.intensity.value = 1.6f;
            bloom.scatter.value = 0.75f;
            bloom.highQualityFiltering.value = true;
            profile.Add<Tonemapping>(true).mode.value = TonemappingMode.ACES;
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.34f;
            vignette.smoothness.value = 0.45f;
            var colour = profile.Add<ColorAdjustments>(true);
            colour.postExposure.value = 0.35f;
            colour.contrast.value = 12f;
            colour.saturation.value = 15f;
            profile.Add<ChromaticAberration>(true).intensity.value = 0.05f;
            var grain = profile.Add<FilmGrain>(true);
            grain.intensity.value = 0.18f;
            grain.response.value = 0.8f;
        });
    }
}
