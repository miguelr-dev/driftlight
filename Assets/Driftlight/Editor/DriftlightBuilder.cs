using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Driftlight.EditorTools
{
    /// <summary>
    /// Builds DRIFTLIGHT's generated content from code: textures, materials, meshes, prefabs and
    /// scenes. Everything it writes lives in Assets/Driftlight/Generated and Assets/Scenes.
    ///
    /// Menu: DRIFTLIGHT > Rebuild ...
    /// Batch: Unity -batchmode -quit -projectPath . -executeMethod Driftlight.EditorTools.DriftlightBuilder.BuildAll
    ///
    /// Rebuilding a scene overwrites it, so once a scene is being hand-edited in Unity, stop rebuilding it.
    /// </summary>
    public static partial class DriftlightBuilder
    {
        public const string GeneratedRoot = "Assets/Driftlight/Generated";
        public const string TexturesDir = GeneratedRoot + "/Textures";
        public const string MaterialsDir = GeneratedRoot + "/Materials";
        public const string MeshesDir = GeneratedRoot + "/Meshes";
        public const string PrefabsDir = GeneratedRoot + "/Prefabs";
        public const string SettingsDir = GeneratedRoot + "/Settings";
        public const string ScenesDir = "Assets/Scenes";

        [MenuItem("DRIFTLIGHT/Rebuild Everything (overwrites generated scenes)", priority = 0)]
        static void RebuildEverythingMenu()
        {
            if (Confirm("every generated scene")) BuildAll();
        }

        public static void BuildAll()
        {
            BuildSpaceScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[DRIFTLIGHT] Build finished.");
        }

        static bool Confirm(string what)
        {
            if (Application.isBatchMode) return true;
            return EditorUtility.DisplayDialog("DRIFTLIGHT builder",
                $"This regenerates {what} and overwrites any manual changes in it. Continue?", "Rebuild", "Cancel");
        }

        // ------------------------------------------------------------------ folders & assets

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void EnsureFolders()
        {
            foreach (var dir in new[] { TexturesDir, MaterialsDir, MeshesDir, PrefabsDir, SettingsDir, ScenesDir })
                EnsureFolder(dir);
        }

        static Texture2D SaveTexture(Texture2D texture, string name, bool srgb = true, bool alpha = false,
            bool mipmaps = true, TextureWrapMode wrapU = TextureWrapMode.Repeat, TextureWrapMode wrapV = TextureWrapMode.Repeat,
            bool compress = true, int maxSize = 2048)
        {
            string path = $"{TexturesDir}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = alpha;
            importer.mipmapEnabled = mipmaps;
            importer.wrapModeU = wrapU;
            importer.wrapModeV = wrapV;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = compress ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material MaterialAsset(string name, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new System.Exception($"Shader not found: {shaderName}");
            string path = $"{MaterialsDir}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }
            return material;
        }

        static Material Lit(string name, Color color, float smoothness = 0.4f, float metallic = 0f,
            Color? emission = null, Texture2D baseMap = null, Texture2D emissionMap = null)
        {
            var m = MaterialAsset(name, "Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", color);
            m.SetTexture("_BaseMap", baseMap);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (emission.HasValue || emissionMap != null)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission ?? Color.white);
                m.SetTexture("_EmissionMap", emissionMap);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Unlit(string name, Color hdrColor)
        {
            var m = MaterialAsset(name, "Universal Render Pipeline/Unlit");
            m.SetColor("_BaseColor", hdrColor);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Additive(string name, Color hdrColor, Texture2D texture)
        {
            var m = MaterialAsset(name, "Driftlight/Additive");
            m.SetColor("_Color", hdrColor);
            m.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Glow(string name, Color hdrColor, bool atmosphere, float power, float inner = 2f)
        {
            var m = MaterialAsset(name, "Driftlight/Glow");
            m.SetColor("_Color", hdrColor);
            m.SetFloat("_Mode", atmosphere ? 0f : 1f);
            m.SetFloat("_Cull", atmosphere ? (float)CullMode.Front : (float)CullMode.Back);
            m.SetFloat("_Power", power);
            m.SetFloat("_Inner", inner);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Saves a mesh asset. If it already exists it is updated in place, so references to it survive.</summary>
        static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = $"{MeshesDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            EditorUtility.CopySerialized(mesh, existing);
            existing.name = name;
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>Creates (or refills) a post-processing profile asset, keeping its GUID stable.</summary>
        static VolumeProfile CreateProfile(string name, System.Action<VolumeProfile> configure)
        {
            string path = $"{SettingsDir}/{name}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            else
            {
                foreach (var old in profile.components)
                {
                    AssetDatabase.RemoveObjectFromAsset(old);
                    Object.DestroyImmediate(old, true);
                }
                profile.components.Clear();
            }
            configure(profile);
            foreach (var component in profile.components)
            {
                component.name = component.GetType().Name;
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        // ------------------------------------------------------------------ scene helpers

        static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 euler,
            Vector3 scale, Material material, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Empty(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go;
        }

        static Light PointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var go = Empty(name, parent, position);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        static Light Sun(Vector3 towardSun, Color color, float intensity)
        {
            var go = new GameObject("Sun Light");
            go.transform.rotation = Quaternion.LookRotation(-towardSun.normalized);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
            RenderSettings.sun = light;
            return light;
        }

        static Camera MainCamera(Vector3 position, Quaternion rotation, float far)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.SetPositionAndRotation(position, rotation);
            var cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = far;
            cam.fieldOfView = 62f;
            cam.clearFlags = CameraClearFlags.Skybox;
            go.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            return cam;
        }

        static Volume GlobalVolume(VolumeProfile profile)
        {
            var go = new GameObject("Post Processing");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            return volume;
        }

        static void SaveScene(UnityEngine.SceneManagement.Scene scene, string path, int buildIndex)
        {
            EditorSceneManager.SaveScene(scene, path);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(Mathf.Clamp(buildIndex, 0, scenes.Count), new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ shared textures

        static Texture2D glowTexture;

        /// <summary>Soft round dot used by every glowing particle and sprite.</summary>
        static Texture2D GlowTexture()
        {
            if (glowTexture) return glowTexture;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float falloff = Mathf.Clamp01(1f - r);
                float a = Mathf.Clamp01(falloff * falloff * 0.65f + Mathf.Exp(-r * r * 16f) * 0.35f) * (r < 1f ? 1f : 0f);
                byte b = (byte)Mathf.RoundToInt(a * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, b);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            glowTexture = SaveTexture(tex, "Glow", srgb: false, alpha: true, mipmaps: true,
                wrapU: TextureWrapMode.Clamp, wrapV: TextureWrapMode.Clamp, compress: false, maxSize: 128);
            return glowTexture;
        }

        static Color32 ToSrgb32(Color linear, System.Random dither)
        {
            float d = ((float)dither.NextDouble() - 0.5f) / 255f;
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt((Mathf.LinearToGammaSpace(Mathf.Clamp01(linear.r)) + d) * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt((Mathf.LinearToGammaSpace(Mathf.Clamp01(linear.g)) + d) * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt((Mathf.LinearToGammaSpace(Mathf.Clamp01(linear.b)) + d) * 255f), 0, 255),
                255);
        }

        /// <summary>Fills an equirectangular texture by asking <paramref name="shade"/> for each direction (linear colour).</summary>
        static Texture2D LatLongTexture(int width, int height, System.Func<Vector3, Color> shade, int ditherSeed = 1)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            var dither = new System.Random(ditherSeed);
            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    pixels[y * width + x] = ToSrgb32(shade(ProceduralNoise.DirectionFromLatLong(u, v)), dither);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        static Material PanoramicSkybox(string name, Texture2D texture, float exposure)
        {
            var m = MaterialAsset(name, "Skybox/Panoramic");
            m.SetTexture("_MainTex", texture);
            m.SetFloat("_Mapping", 1f);   // latitude-longitude layout
            m.SetFloat("_ImageType", 0f); // 360 degrees
            m.SetFloat("_Exposure", exposure);
            m.SetFloat("_Rotation", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ meshes

        /// <summary>A noisy, lumpy sphere for asteroids and rocks.</summary>
        static Mesh AsteroidMesh(int seed, int subdivisions = 3)
        {
            var mesh = Icosphere(subdivisions);
            var vertices = mesh.vertices;
            var offset = new Vector3(seed * 17.13f, seed * 5.71f, seed * 11.3f);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i];
                float big = ProceduralNoise.Fbm(p * 1.4f + offset, 3, seed) - 0.5f;
                float small = ProceduralNoise.Fbm(p * 4.5f + offset, 3, seed + 7) - 0.5f;
                float crater = ProceduralNoise.Smoothstep(0.62f, 0.8f, ProceduralNoise.Value(p.x * 3f + offset.x, p.y * 3f, p.z * 3f, seed + 3));
                vertices[i] = p * (1f + big * 0.75f + small * 0.22f - crater * 0.12f);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = "Asteroid_" + seed;
            return mesh;
        }

        static Mesh Icosphere(int subdivisions)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var verts = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;

            var tris = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };

            // Make every base triangle face outward for Unity's winding (normal = cross(b - a, c - a)).
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 a = verts[tris[i]], b = verts[tris[i + 1]], c = verts[tris[i + 2]];
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f)
                    (tris[i + 1], tris[i + 2]) = (tris[i + 2], tris[i + 1]);
            }

            var cache = new Dictionary<long, int>();
            int Midpoint(int a, int b)
            {
                long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
                if (cache.TryGetValue(key, out int index)) return index;
                verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
                cache[key] = verts.Count - 1;
                return verts.Count - 1;
            }

            for (int s = 0; s < subdivisions; s++)
            {
                var next = new List<int>(tris.Count * 4);
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Midpoint(a, b), bc = Midpoint(b, c), ca = Midpoint(c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = next;
            }

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------------ the player's ship

        struct ShipParts
        {
            public Transform model;
            public Renderer[] engineGlows;
            public TrailRenderer[] trails;
            public Light engineLight;
            public Transform[] muzzles;
        }

        /// <summary>Builds the ship's model under <paramref name="parent"/> (forward is +Z, about 7 m long).</summary>
        static ShipParts BuildShipModel(Transform parent, bool withTrails)
        {
            var hull = Lit("Ship Hull", new Color(0.63f, 0.67f, 0.73f), 0.55f, 0.25f);
            var dark = Lit("Ship Dark", new Color(0.09f, 0.1f, 0.12f), 0.6f, 0.3f);
            var accent = Lit("Ship Accent", new Color(0.95f, 0.45f, 0.12f), 0.5f, 0f, new Color(0.6f, 0.18f, 0.03f));
            var glass = Lit("Ship Glass", new Color(0.04f, 0.12f, 0.16f), 0.95f, 0f, new Color(0.05f, 0.35f, 0.45f));
            var engine = Unlit("Ship Engine Glow", new Color(0.45f, 1.6f, 4f));
            var red = Unlit("Nav Light Red", new Color(4f, 0.25f, 0.15f));
            var green = Unlit("Nav Light Green", new Color(0.25f, 4f, 0.6f));

            var model = Empty("Model", parent, Vector3.zero).transform;

            Part("Fuselage", PrimitiveType.Capsule, model, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(1.3f, 3.2f, 0.9f), hull);
            Part("Canopy", PrimitiveType.Sphere, model, new Vector3(0f, 0.42f, 1.2f), Vector3.zero, new Vector3(0.75f, 0.5f, 1.6f), glass);
            Part("Dorsal Fin", PrimitiveType.Cube, model, new Vector3(0f, 0.62f, -1.9f), new Vector3(-12f, 0f, 0f), new Vector3(0.12f, 0.8f, 1.9f), dark);

            foreach (float side in new[] { -1f, 1f })
            {
                string s = side < 0 ? "Left" : "Right";
                Part($"{s} Wing", PrimitiveType.Cube, model, new Vector3(2.1f * side, -0.12f, -0.9f), new Vector3(0f, 14f * side, -5f * side), new Vector3(3f, 0.14f, 1.9f), hull);
                Part($"{s} Wing Stripe", PrimitiveType.Cube, model, new Vector3(2.3f * side, -0.03f, -0.75f), new Vector3(0f, 14f * side, -5f * side), new Vector3(1.8f, 0.03f, 0.28f), accent);
                Part($"{s} Engine", PrimitiveType.Cylinder, model, new Vector3(0.95f * side, -0.05f, -2.55f), new Vector3(90f, 0f, 0f), new Vector3(0.62f, 1f, 0.62f), dark);
                Part($"{s} Gun", PrimitiveType.Cylinder, model, new Vector3(1.35f * side, -0.15f, 0.45f), new Vector3(90f, 0f, 0f), new Vector3(0.16f, 0.7f, 0.16f), dark);
                Part($"{s} Nav Light", PrimitiveType.Sphere, model, new Vector3(3.55f * side, -0.25f, -1.27f), Vector3.zero, Vector3.one * 0.2f, side < 0 ? red : green);
            }

            var glows = new List<Renderer>();
            var trails = new List<TrailRenderer>();
            var muzzles = new List<Transform>();
            var trailMaterial = withTrails ? Additive("Engine Trail", new Color(0.35f, 1f, 2.4f, 0.9f), GlowTexture()) : null;

            foreach (float side in new[] { -1f, 1f })
            {
                string s = side < 0 ? "Left" : "Right";
                glows.Add(Part($"{s} Engine Glow", PrimitiveType.Sphere, model, new Vector3(0.95f * side, -0.05f, -3.58f), Vector3.zero, new Vector3(0.5f, 0.5f, 0.22f), engine).GetComponent<Renderer>());
                muzzles.Add(Empty($"{s} Muzzle", model, new Vector3(1.35f * side, -0.15f, 1.2f)).transform);

                if (withTrails)
                {
                    var trailObject = Empty($"{s} Trail", model, new Vector3(0.95f * side, -0.05f, -3.7f));
                    var trail = trailObject.AddComponent<TrailRenderer>();
                    trail.sharedMaterial = trailMaterial;
                    trail.time = 0.35f;
                    trail.minVertexDistance = 0.25f;
                    trail.widthMultiplier = 0.55f;
                    trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                    var gradient = new Gradient();
                    gradient.SetKeys(
                        new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                        new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                    trail.colorGradient = gradient;
                    trail.shadowCastingMode = ShadowCastingMode.Off;
                    trail.receiveShadows = false;
                    trails.Add(trail);
                }
            }

            var engineLight = PointLight("Engine Light", model, new Vector3(0f, 0f, -4.4f), new Color(0.4f, 0.8f, 1f), 3f, 12f);

            return new ShipParts
            {
                model = model,
                engineGlows = glows.ToArray(),
                trails = trails.ToArray(),
                engineLight = engineLight,
                muzzles = muzzles.ToArray(),
            };
        }
    }
}
