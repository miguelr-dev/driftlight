using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.PrimitiveType;

namespace Driftlight.EditorTools
{
    /// <summary>The Space scene: the derelict signal relay, its asteroid field, and Verdance on the horizon.</summary>
    public static partial class DriftlightBuilder
    {
        public const string SpaceScenePath = ScenesDir + "/Space.unity";

        static readonly Vector3 SpaceSunDirection = new Vector3(-0.8f, 0.25f, 0.55f).normalized;
        static readonly Vector3 GalaxyBandNormal = new Vector3(0.35f, 0.9f, -0.2f).normalized;
        static readonly Vector3 RelayPosition = new Vector3(0f, 12f, 360f);
        static readonly Vector3 VerdanceOrbitPosition = new Vector3(1400f, 600f, 3200f);

        [MenuItem("DRIFTLIGHT/Rebuild Space Scene (overwrites it)", priority = 10)]
        static void RebuildSpaceMenu()
        {
            if (Confirm("Space.unity")) BuildSpaceScene();
        }

        public static void BuildSpaceScene()
        {
            EnsureFolders();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = PanoramicSkybox("Space Skybox", SpaceSkyTexture(), 1f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.17f, 0.16f, 0.28f);
            RenderSettings.ambientEquatorColor = new Color(0.11f, 0.08f, 0.16f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.035f, 0.06f);
            RenderSettings.fog = false;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.6f;
            Sun(SpaceSunDirection, new Color(1f, 0.87f, 0.74f), 2.1f);

            var world = new GameObject("World").transform;
            BuildDeadStar(world);
            var verdance = BuildVerdanceFromOrbit(world);
            BuildRelayStation(world);
            BuildAsteroidField(world);

            var combat = BuildCombatPrefabs();
            var shipPrefab = BuildPlayerShipPrefab(combat);
            var ship = (GameObject)PrefabUtility.InstantiatePrefab(shipPrefab);
            ship.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var cam = MainCamera(new Vector3(0f, 3.4f, -13f), Quaternion.identity, 12000f);
            cam.gameObject.AddComponent<ChaseCamera>().target = ship.GetComponent<ShipController>();
            BuildSpaceDust(cam.transform);
            var streaks = BuildWarpStreaks(cam.transform);
            BuildStarfield(GalaxyBandNormal);

            GlobalVolume(SpaceProfile());

            var systems = new GameObject("Game Systems");
            systems.AddComponent<CursorLock>();
            var encounter = systems.AddComponent<EncounterDirector>();
            encounter.dronePrefab = combat.drone;
            encounter.warpInEffect = combat.warpIn;
            new GameObject("HUD").AddComponent<Hud>();

            // The warp drive lives on the ship prefab; scene objects are wired up on this instance.
            var warp = ship.GetComponent<WarpDrive>();
            warp.destinationMarker = verdance;
            warp.streaks = streaks;
            warp.unlockAfter = encounter;
            PrefabUtility.RecordPrefabInstancePropertyModifications(warp);

            SaveScene(scene, SpaceScenePath, 0);
        }

        // ------------------------------------------------------------------ player ship

        static GameObject BuildPlayerShipPrefab(CombatPrefabs combat)
        {
            var root = new GameObject("Player Ship");
            root.tag = "Player";
            var body = root.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.useGravity = false;
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0f, -0.6f);
            box.size = new Vector3(7.4f, 1.3f, 7f);

            var parts = BuildShipModel(root.transform, withTrails: true);
            var controller = root.AddComponent<ShipController>();
            controller.visual = parts.model;
            root.AddComponent<ShipPlayerInput>();
            var glow = root.AddComponent<EngineGlow>();
            glow.ship = controller;
            glow.glowRenderers = parts.engineGlows;
            glow.trails = parts.trails;
            glow.engineLight = parts.engineLight;

            var health = root.AddComponent<Health>();
            health.team = Team.Player;
            health.maxHull = 100f;
            health.maxShield = 60f;
            health.shieldRegenDelay = 3f;
            health.shieldRegenRate = 14f;

            var blaster = root.AddComponent<Blaster>();
            blaster.projectilePrefab = combat.playerBolt;
            blaster.muzzles = parts.muzzles;
            blaster.team = Team.Player;
            blaster.fireRate = 9f;
            blaster.spread = 0.25f;
            blaster.aimAtCrosshair = true;
            blaster.volume = 0.3f;

            root.AddComponent<PlayerShipDamage>().explosionPrefab = combat.bigExplosion;

            var warp = root.AddComponent<WarpDrive>();
            warp.destinationScene = "Verdance";
            warp.destinationName = "VERDANCE";

            return SavePrefab(root, "Player Ship");
        }

        // ------------------------------------------------------------------ sky objects

        static void BuildDeadStar(Transform world)
        {
            var star = Empty("Dead Star", world, SpaceSunDirection * 5200f).transform;
            var surface = MeshPart("Surface", HiResSphere(), star, Vector3.zero, Vector3.zero, Vector3.one * 260f, Unlit("Star Surface", new Color(14f, 10f, 6.5f)));
            surface.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var corona = Part("Corona", Quad, star, Vector3.zero, Vector3.zero, Vector3.one * 2400f,
                Additive("Star Corona", new Color(2.6f, 1.5f, 0.75f, 1f), GlowTexture()));
            corona.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            corona.AddComponent<Billboard>();
        }

        static Transform BuildVerdanceFromOrbit(Transform world)
        {
            var (albedo, glow) = VerdanceOrbitTextures();
            var surface = Lit("Verdance Surface", Color.white, 0.25f, 0f, new Color(1.7f, 1.7f, 1.7f), albedo, glow);

            var planet = Empty("Verdance", world, VerdanceOrbitPosition).transform;
            planet.localRotation = Quaternion.Euler(12f, 0f, 18f);
            var body = MeshPart("Surface", HiResSphere(), planet, Vector3.zero, Vector3.zero, Vector3.one * 780f, surface);
            body.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            body.AddComponent<Rotator>().degreesPerSecond = new Vector3(0f, 0.35f, 0f);
            var atmosphere = MeshPart("Atmosphere", HiResSphere(), planet, Vector3.zero, Vector3.zero, Vector3.one * 780f * 1.1f,
                Glow("Verdance Atmosphere", new Color(0.3f, 1.1f, 1.5f), atmosphere: true, power: 3f, inner: 2f));
            atmosphere.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            var moon = MeshPart("Moon", HiResSphere(), world, VerdanceOrbitPosition + new Vector3(-780f, 260f, -420f), Vector3.zero,
                Vector3.one * 110f, Lit("Moon Rock", new Color(0.42f, 0.4f, 0.43f), 0.1f));
            moon.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return planet;
        }

        static ParticleSystem BuildWarpStreaks(Transform camera)
        {
            var go = Empty("Warp Streaks", camera, Vector3.zero);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 0.6f;
            main.startSpeed = 0f;
            main.startSize = 0.3f;
            main.maxParticles = 1500;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            colour.color = FadeInOut();

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Additive("Warp Streaks", new Color(1.2f, 1.9f, 3.2f, 1f), GlowTexture());
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 2f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        static Texture2D SpaceSkyTexture()
        {
            const int seed = 9;
            var texture = LatLongTexture(2048, 1024, d =>
            {
                float band = Mathf.Exp(-Sq(Vector3.Dot(d, GalaxyBandNormal) / 0.36f));
                float w1 = ProceduralNoise.Fbm(d * 2.1f + new Vector3(1.3f, 7.1f, 4.2f), 3, seed);
                float w2 = ProceduralNoise.Fbm(d * 2.1f + new Vector3(8.4f, 2.2f, 6.6f), 3, seed + 1);
                Vector3 q = d * 2.6f + new Vector3(w1 - 0.5f, w2 - 0.5f, w1 - w2) * 2.4f;
                float n = ProceduralNoise.Fbm(q, 5, seed + 2);
                float fine = ProceduralNoise.Fbm(d * 8f + q, 3, seed + 3);
                float dust = ProceduralNoise.Smoothstep(0.52f, 0.68f, ProceduralNoise.Fbm(q * 1.8f + Vector3.one * 3.3f, 4, seed + 4)) * band;

                float cloud = ProceduralNoise.Smoothstep(0.4f, 0.8f, n) * (0.2f + 0.8f * band);
                float bright = ProceduralNoise.Smoothstep(0.58f, 0.85f, n * 0.75f + fine * 0.25f) * band;

                Color c = new Color(0.0018f, 0.0022f, 0.0065f);
                c += new Color(0.1f, 0.03f, 0.19f) * cloud;
                c += new Color(0.02f, 0.11f, 0.16f) * bright;
                c += new Color(0.22f, 0.05f, 0.13f) * ProceduralNoise.Smoothstep(0.62f, 0.9f, fine) * band * cloud;
                c *= 1f - 0.8f * dust;
                c += new Color(0.015f, 0.012f, 0.02f) * band;

                float s = Mathf.Max(0f, Vector3.Dot(d, SpaceSunDirection));
                c += new Color(1f, 0.55f, 0.25f) * (Mathf.Pow(s, 60f) * 0.35f + Mathf.Pow(s, 6f) * 0.04f);
                return c;
            });
            return SaveTexture(texture, "Space Sky", srgb: true, mipmaps: false,
                wrapU: TextureWrapMode.Repeat, wrapV: TextureWrapMode.Clamp, compress: false, maxSize: 2048);
        }

        static (Texture2D albedo, Texture2D glow) VerdanceOrbitTextures()
        {
            const int width = 1024, height = 512, seed = 42;
            var albedoTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var glowTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var albedo = new Color32[width * height];
            var glow = new Color32[width * height];
            var dither = new System.Random(3);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Vector3 d = ProceduralNoise.DirectionFromLatLong((x + 0.5f) / width, (y + 0.5f) / height);
                float continents = ProceduralNoise.Fbm(d * 1.7f + new Vector3(3.1f, 1.7f, 9.2f), 6, seed);
                float land = ProceduralNoise.Smoothstep(0.5f, 0.535f, continents);
                float detail = ProceduralNoise.Fbm(d * 7f, 4, seed + 3);

                Color ocean = Color.Lerp(new Color(0.008f, 0.04f, 0.07f), new Color(0.02f, 0.14f, 0.16f), ProceduralNoise.Smoothstep(0.38f, 0.5f, continents));
                Color jungle = Color.Lerp(new Color(0.02f, 0.09f, 0.045f), new Color(0.07f, 0.19f, 0.06f), detail);
                Color ground = Color.Lerp(jungle, new Color(0.16f, 0.18f, 0.12f), ProceduralNoise.Smoothstep(0.6f, 0.7f, continents));
                Color surface = Color.Lerp(ocean, ground, land);

                float ice = ProceduralNoise.Smoothstep(0.82f, 0.9f, Mathf.Abs(d.y) + (detail - 0.5f) * 0.1f);
                surface = Color.Lerp(surface, new Color(0.75f, 0.85f, 0.9f), ice);

                Vector3 warp = new Vector3(ProceduralNoise.Fbm(d * 2.5f, 3, seed + 11), ProceduralNoise.Fbm(d * 2.5f + Vector3.one * 5f, 3, seed + 12), 0f) * 1.2f;
                float clouds = ProceduralNoise.Smoothstep(0.52f, 0.72f, ProceduralNoise.Fbm(d * 3.2f + warp, 5, seed + 21));
                surface = Color.Lerp(surface, new Color(0.8f, 0.86f, 0.9f), clouds * 0.85f);

                float veins = ProceduralNoise.Ridged(d * 4.5f + Vector3.one * 2f, 4, seed + 31);
                float g = land * (1f - ice) * ProceduralNoise.Smoothstep(0.72f, 0.92f, veins) * (1f - clouds * 0.9f);

                albedo[y * width + x] = ToSrgb32(surface, dither);
                glow[y * width + x] = ToSrgb32(new Color(0.05f, 0.75f, 0.6f) * g, dither);
            }

            albedoTex.SetPixels32(albedo);
            albedoTex.Apply();
            glowTex.SetPixels32(glow);
            glowTex.Apply();
            return (SaveTexture(albedoTex, "Verdance Orbit Albedo", wrapV: TextureWrapMode.Clamp),
                    SaveTexture(glowTex, "Verdance Orbit Glow", wrapV: TextureWrapMode.Clamp));
        }

        // ------------------------------------------------------------------ the signal relay

        static void BuildRelayStation(Transform world)
        {
            var hull = Lit("Station Hull", new Color(0.5f, 0.52f, 0.56f), 0.5f, 0.35f);
            var dark = Lit("Station Dark", new Color(0.13f, 0.14f, 0.16f), 0.45f, 0.4f);
            var panel = Lit("Solar Panel", new Color(0.04f, 0.07f, 0.18f), 0.85f, 0.5f, new Color(0.01f, 0.03f, 0.08f));
            var windows = Unlit("Station Windows", new Color(0.5f, 1.7f, 2.6f));
            var core = Unlit("Signal Core", new Color(4.5f, 0.6f, 3.2f));
            var beacon = Unlit("Beacon Red", new Color(5f, 0.3f, 0.2f));

            var relay = Empty("Signal Relay", world, RelayPosition).transform;
            relay.localRotation = Quaternion.Euler(8f, -20f, 4f);

            Part("Spine", Cylinder, relay, Vector3.zero, Vector3.zero, new Vector3(5f, 22f, 5f), dark, keepCollider: true);
            Part("Signal Core", Sphere, relay, Vector3.zero, Vector3.zero, Vector3.one * 9f, core, keepCollider: true);
            Part("Core Glow", Sphere, relay, Vector3.zero, Vector3.zero, Vector3.one * 26f, Glow("Signal Core Glow", new Color(1.6f, 0.25f, 1.2f), atmosphere: false, power: 2.2f));
            PointLight("Core Light", relay, Vector3.zero, new Color(1f, 0.3f, 0.8f), 8f, 140f);

            var ring = Empty("Ring", relay, Vector3.zero).transform;
            ring.gameObject.AddComponent<Rotator>().degreesPerSecond = new Vector3(0f, 4f, 0f);
            const int segments = 32;
            const float ringRadius = 30f;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var euler = Quaternion.LookRotation(radial).eulerAngles;
                Part($"Ring Segment {i}", Cube, ring, radial * ringRadius, euler, new Vector3(6.2f, 2.4f, 3.2f), hull, keepCollider: true);
                if (i % 2 == 0)
                    Part($"Ring Windows {i}", Cube, ring, radial * (ringRadius + 1.65f), euler, new Vector3(4.6f, 0.4f, 0.1f), windows);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Part($"Spoke {i}", Cube, ring, radial * 16f, Quaternion.LookRotation(radial).eulerAngles, new Vector3(1.3f, 1.3f, 26f), dark, keepCollider: true);
            }

            Part("Dish", Sphere, relay, new Vector3(0f, 25f, 0f), new Vector3(18f, 0f, 0f), new Vector3(15f, 2.4f, 15f), hull, keepCollider: true);
            Part("Mast", Cylinder, relay, new Vector3(0f, 31f, 0f), Vector3.zero, new Vector3(0.45f, 6f, 0.45f), dark);
            var mastBeacon = Part("Mast Beacon", Sphere, relay, new Vector3(0f, 37.3f, 0f), Vector3.zero, Vector3.one * 1.3f, beacon);
            mastBeacon.AddComponent<Blinker>();

            foreach (float side in new[] { -1f, 1f })
            {
                Part("Panel Strut", Cube, relay, new Vector3(9f * side, -16f, 0f), Vector3.zero, new Vector3(14f, 0.8f, 0.8f), dark);
                Part("Solar Panel", Cube, relay, new Vector3(25f * side, -16f, 0f), Vector3.zero, new Vector3(20f, 0.3f, 9f), panel, keepCollider: true);
                var tip = Part("Panel Beacon", Sphere, relay, new Vector3(35.5f * side, -16f, 0f), Vector3.zero, Vector3.one * 0.9f, beacon);
                tip.AddComponent<Blinker>().phase = side < 0 ? 0f : 0.5f;
            }
        }

        // ------------------------------------------------------------------ asteroids

        static void BuildAsteroidField(Transform world)
        {
            var field = Empty("Asteroid Field", world, Vector3.zero).transform;
            var meshes = new Mesh[4];
            for (int i = 0; i < meshes.Length; i++) meshes[i] = SaveMesh(AsteroidMesh(i + 1), $"Asteroid {i + 1}");
            var materials = new[]
            {
                Lit("Asteroid Grey", new Color(0.33f, 0.31f, 0.3f), 0.18f),
                Lit("Asteroid Brown", new Color(0.36f, 0.27f, 0.2f), 0.14f),
                Lit("Asteroid Dark", new Color(0.17f, 0.17f, 0.19f), 0.22f),
            };

            var random = new System.Random(77);
            float R() => (float)random.NextDouble();
            Vector3 pathEnd = RelayPosition - RelayPosition.normalized * 60f;

            int placed = 0, attempts = 0;
            while (placed < 170 && attempts++ < 6000)
            {
                float angle = R() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(70f, 430f, Mathf.Sqrt(R()));
                float height = (R() + R() + R() - 1.5f) * 60f;
                var position = RelayPosition + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                if (DistanceToSegment(position, Vector3.zero, pathEnd) < 40f) continue; // keep the opening flight path clear
                float size = Mathf.Lerp(2f, 24f, Mathf.Pow(R(), 2.6f));
                AddAsteroid(field, meshes, materials, random, position, size, placed++);
            }

            for (int i = 0; i < 10; i++)
            {
                float angle = R() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(480f, 760f, R());
                var position = RelayPosition + new Vector3(Mathf.Cos(angle) * radius, (R() - 0.5f) * 140f, Mathf.Sin(angle) * radius);
                AddAsteroid(field, meshes, materials, random, position, Mathf.Lerp(38f, 75f, R()), placed++);
            }
        }

        static void AddAsteroid(Transform parent, Mesh[] meshes, Material[] materials, System.Random random, Vector3 position, float size, int index)
        {
            float R() => (float)random.NextDouble();
            var mesh = meshes[random.Next(meshes.Length)];
            var go = new GameObject($"Asteroid {index}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(R() * 360f, R() * 360f, R() * 360f);
            go.transform.localScale = new Vector3(size * Mathf.Lerp(0.75f, 1.25f, R()), size * Mathf.Lerp(0.7f, 1.1f, R()), size * Mathf.Lerp(0.8f, 1.3f, R()));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = materials[random.Next(materials.Length)];
            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;
            if (R() < 0.35f)
                go.AddComponent<Rotator>().degreesPerSecond = new Vector3(R() - 0.5f, R() - 0.5f, R() - 0.5f) * 14f;
        }

        // ------------------------------------------------------------------ particles

        static void BuildStarfield(Vector3 bandNormal)
        {
            var go = new GameObject("Starfield");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 1e6f;
            main.startSpeed = 0f;
            main.startSize = 10f;
            main.maxParticles = 3000;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Additive("Stars", new Color(2.4f, 2.4f, 2.4f, 1f), GlowTexture());
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.minParticleSize = 0.0009f;
            renderer.maxParticleSize = 0.05f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            go.AddComponent<Starfield>().bandNormal = bandNormal;
        }

        static void BuildSpaceDust(Transform camera)
        {
            var go = Empty("Space Dust", camera, Vector3.zero);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
            main.startColor = new Color(0.75f, 0.88f, 1f, 0.7f);
            main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 3f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(70f, 45f, 80f);
            shape.position = new Vector3(0f, 0f, 45f);
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            colour.color = FadeInOut();

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Additive("Space Dust", new Color(0.8f, 0.9f, 1.2f, 0.6f), GlowTexture());
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.cameraVelocityScale = 0.035f;
            renderer.velocityScale = 0f;
            renderer.lengthScale = 1f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static ParticleSystem.MinMaxGradient FadeInOut()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(gradient);
        }

        static VolumeProfile SpaceProfile() => CreateProfile("Space Post Processing", profile =>
        {
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.9f;
            bloom.intensity.value = 1.3f;
            bloom.scatter.value = 0.72f;
            bloom.highQualityFiltering.value = true;
            profile.Add<Tonemapping>(true).mode.value = TonemappingMode.ACES;
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.24f;
            vignette.smoothness.value = 0.45f;
            var colour = profile.Add<ColorAdjustments>(true);
            colour.postExposure.value = 0.6f;
            colour.contrast.value = 10f;
            colour.saturation.value = 8f;
            profile.Add<ChromaticAberration>(true).intensity.value = 0.06f;
        });

        // ------------------------------------------------------------------ small helpers

        static float Sq(float x) => x * x;

        static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-5f));
            return Vector3.Distance(p, a + ab * t);
        }

        static Mesh hiResSphere;

        /// <summary>A smooth 128x64 UV sphere (radius 0.5, like Unity's) whose UVs match the lat-long textures.</summary>
        static Mesh HiResSphere()
        {
            if (hiResSphere) return hiResSphere;
            const int lon = 128, lat = 64;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            for (int iy = 0; iy <= lat; iy++)
            for (int ix = 0; ix <= lon; ix++)
            {
                float u = (float)ix / lon, v = (float)iy / lat;
                Vector3 d = ProceduralNoise.DirectionFromLatLong(u, v);
                vertices.Add(d * 0.5f);
                normals.Add(d);
                uvs.Add(new Vector2(u, v));
            }

            var triangles = new List<int>();
            void AddTriangle(int a, int b, int c)
            {
                Vector3 pa = vertices[a], pb = vertices[b], pc = vertices[c];
                Vector3 cross = Vector3.Cross(pb - pa, pc - pa);
                if (cross.sqrMagnitude < 1e-12f) return; // degenerate triangle at a pole
                if (Vector3.Dot(cross, pa + pb + pc) < 0f) (b, c) = (c, b);
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }

            for (int iy = 0; iy < lat; iy++)
            for (int ix = 0; ix < lon; ix++)
            {
                int a = iy * (lon + 1) + ix, b = a + 1, c = a + lon + 1, d = c + 1;
                AddTriangle(a, c, b);
                AddTriangle(b, c, d);
            }

            var mesh = new Mesh { name = "Sphere Hi-Res" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            hiResSphere = SaveMesh(mesh, "Sphere Hi-Res");
            return hiResSphere;
        }

        static GameObject MeshPart(string name, Mesh mesh, Transform parent, Vector3 position, Vector3 euler, Vector3 scale, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
    }
}
