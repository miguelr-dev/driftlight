using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.PrimitiveType;

namespace Driftlight.EditorTools
{
    /// <summary>Combat prefabs: energy bolts, hit sparks, explosions and the enemy drone.</summary>
    public static partial class DriftlightBuilder
    {
        struct CombatPrefabs
        {
            public Projectile playerBolt;
            public Projectile enemyBolt;
            public GameObject impact;
            public GameObject explosion;
            public GameObject bigExplosion;
            public GameObject warpIn;
            public EnemyDrone drone;
        }

        static CombatPrefabs BuildCombatPrefabs()
        {
            var prefabs = new CombatPrefabs();
            prefabs.impact = BuildBurstPrefab("Impact Sparks", new Color(1.6f, 1.6f, 2.2f, 1f), new Color(1.5f, 1.6f, 2f, 1f),
                sparks: 14, sparkSpeed: 22f, flashSize: 2.2f, lightRange: 10f, lightIntensity: 3f, lifetime: 1.2f, embers: false);
            prefabs.explosion = BuildBurstPrefab("Explosion", new Color(3.2f, 1.4f, 0.45f, 1f), new Color(3f, 1.8f, 0.9f, 1f),
                sparks: 70, sparkSpeed: 55f, flashSize: 16f, lightRange: 70f, lightIntensity: 14f, lifetime: 3f, embers: true);
            prefabs.bigExplosion = BuildBurstPrefab("Big Explosion", new Color(3.4f, 1.5f, 0.5f, 1f), new Color(3.2f, 2f, 1f, 1f),
                sparks: 140, sparkSpeed: 80f, flashSize: 30f, lightRange: 120f, lightIntensity: 20f, lifetime: 4f, embers: true);
            prefabs.warpIn = BuildBurstPrefab("Warp In Flash", new Color(0.6f, 1.4f, 3.2f, 1f), new Color(0.8f, 1.6f, 3.5f, 1f),
                sparks: 40, sparkSpeed: 30f, flashSize: 20f, lightRange: 50f, lightIntensity: 8f, lifetime: 2.5f, embers: false);

            prefabs.playerBolt = BuildBoltPrefab("Player Bolt", new Color(0.6f, 2.2f, 4.5f), speed: 300f, damage: 7f, impact: prefabs.impact);
            prefabs.enemyBolt = BuildBoltPrefab("Enemy Bolt", new Color(4.5f, 0.9f, 0.35f), speed: 190f, damage: 4f, impact: prefabs.impact);
            prefabs.drone = BuildDronePrefab(prefabs.enemyBolt, prefabs.explosion);
            return prefabs;
        }

        static GameObject SavePrefab(GameObject root, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsDir}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static Projectile BuildBoltPrefab(string name, Color hdrColor, float speed, float damage, GameObject impact)
        {
            var root = new GameObject(name);
            var core = Part("Core", Capsule, root.transform, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.2f, 1.8f, 0.2f), Unlit(name, hdrColor));
            core.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var projectile = root.AddComponent<Projectile>();
            projectile.speed = speed;
            projectile.damage = damage;
            projectile.impactPrefab = impact;
            return SavePrefab(root, name).GetComponent<Projectile>();
        }

        static GameObject BuildBurstPrefab(string name, Color sparkColor, Color flashColor, int sparks, float sparkSpeed,
            float flashSize, float lightRange, float lightIntensity, float lifetime, bool embers)
        {
            var root = new GameObject(name);
            root.AddComponent<AutoDestroy>().lifetime = lifetime;

            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            float peak = Mathf.Max(flashColor.r, Mathf.Max(flashColor.g, flashColor.b));
            light.color = new Color(flashColor.r / peak, flashColor.g / peak, flashColor.b / peak);
            light.range = lightRange;
            light.intensity = lightIntensity;
            light.shadows = LightShadows.None;
            root.AddComponent<LightFlash>().duration = 0.45f;

            var glow = GlowTexture();
            AddBurst(root.transform, "Sparks", Additive(name + " Sparks", sparkColor, glow), sparks,
                new Vector2(sparkSpeed * 0.3f, sparkSpeed), new Vector2(0.45f, 1.2f), new Vector2(0.25f, 0.8f), stretch: true);
            if (embers)
                AddBurst(root.transform, "Embers", Additive(name + " Embers", sparkColor * 0.7f, glow), sparks / 2,
                    new Vector2(sparkSpeed * 0.08f, sparkSpeed * 0.3f), new Vector2(1.2f, 2.4f), new Vector2(0.5f, 1.5f), stretch: false);
            AddBurst(root.transform, "Flash", Additive(name + " Flash", flashColor, glow), 1,
                Vector2.zero, new Vector2(0.22f, 0.22f), new Vector2(flashSize, flashSize), stretch: false, grow: true);

            return SavePrefab(root, name);
        }

        static void AddBurst(Transform parent, string name, Material material, int count, Vector2 speed, Vector2 life, Vector2 size,
            bool stretch, bool grow = false)
        {
            var go = Empty(name, parent, Vector3.zero);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = Color.white;
            main.maxParticles = Mathf.Max(1, count);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.enabled = count > 1;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.55f, 0.35f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            colour.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, grow ? AnimationCurve.Linear(0f, 0.5f, 1f, 1.4f) : AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (stretch)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.05f;
                renderer.lengthScale = 1.5f;
            }
            else
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
        }

        static EnemyDrone BuildDronePrefab(Projectile enemyBolt, GameObject explosion)
        {
            var shell = Lit("Drone Shell", new Color(0.1f, 0.1f, 0.12f), 0.75f, 0.5f);
            var band = Lit("Drone Band", new Color(0.35f, 0.36f, 0.4f), 0.6f, 0.4f);
            var accent = Lit("Drone Accent", new Color(0.4f, 0.05f, 0.04f), 0.5f, 0f, new Color(1.2f, 0.08f, 0.04f));
            var eye = Unlit("Drone Eye", new Color(6f, 0.7f, 0.3f));

            var root = new GameObject("Enemy Drone");
            root.AddComponent<SphereCollider>().radius = 1.7f;
            var health = root.AddComponent<Health>();
            health.team = Team.Enemy;
            health.maxHull = 30f;

            var visual = Empty("Visual", root.transform, Vector3.zero).transform;
            Part("Shell", Sphere, visual, Vector3.zero, Vector3.zero, Vector3.one * 2.4f, shell);
            Part("Band", Cylinder, visual, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(2.7f, 0.16f, 2.7f), band);
            Part("Eye", Sphere, visual, new Vector3(0f, 0f, 1.02f), Vector3.zero, new Vector3(0.95f, 0.95f, 0.5f), eye);
            PointLight("Eye Light", visual, new Vector3(0f, 0f, 1.8f), new Color(1f, 0.25f, 0.1f), 5f, 16f);

            var spinner = Empty("Spinner", visual, Vector3.zero).transform;
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f;
                Vector3 dir = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
                Part($"Fin {i}", Cube, spinner, dir * 1.55f + new Vector3(0f, 0f, -0.3f), new Vector3(0f, 0f, angle), new Vector3(0.28f, 1.3f, 1.3f), accent);
            }

            var muzzle = Empty("Muzzle", visual, new Vector3(0f, 0f, 1.7f)).transform;
            var blaster = root.AddComponent<Blaster>();
            blaster.projectilePrefab = enemyBolt;
            blaster.muzzles = new[] { muzzle };
            blaster.team = Team.Enemy;
            blaster.fireRate = 1.6f;
            blaster.spread = 1.2f;
            blaster.volume = 0.22f;

            var drone = root.AddComponent<EnemyDrone>();
            drone.blaster = blaster;
            drone.visual = visual;
            drone.spinner = spinner;
            drone.explosionPrefab = explosion;

            return SavePrefab(root, "Enemy Drone").GetComponent<EnemyDrone>();
        }
    }
}
