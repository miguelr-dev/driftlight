using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Driftlight
{
    /// <summary>
    /// Hold the warp key to charge, then the ship lines up with its destination, punches to warp
    /// speed through a streaking tunnel, fades to white and loads the destination scene.
    /// </summary>
    [RequireComponent(typeof(ShipController))]
    public class WarpDrive : MonoBehaviour
    {
        [Header("Destination")]
        public string destinationScene = "Verdance";
        public string destinationName = "VERDANCE";
        [Tooltip("Object in the sky the ship turns toward when it jumps (the planet).")]
        public Transform destinationMarker;

        [Header("Unlocking")]
        [Tooltip("The warp drive stays locked until this encounter is cleared. Leave empty to start unlocked.")]
        public EncounterDirector unlockAfter;

        [Header("Feel")]
        public float chargeTime = 1.8f;
        public float tunnelTime = 2.4f;
        public float warpSpeed = 2500f;
        [Tooltip("Particle system parented to the camera, simulating in local space.")]
        public ParticleSystem streaks;

        [NonSerialized] public bool chargeHeld;

        public bool Unlocked { get; private set; }
        public bool Engaged { get; private set; }
        public float Charge01 { get; private set; }

        ShipController ship;
        ChaseCamera chase;
        Volume volume;
        AudioSource chargeAudio;
        float streakBudget;

        void Start()
        {
            ship = GetComponent<ShipController>();
            chase = FindAnyObjectByType<ChaseCamera>();
            CreateWarpVolume();

            chargeAudio = gameObject.AddComponent<AudioSource>();
            chargeAudio.clip = Sfx.WarpCharge;
            chargeAudio.playOnAwake = false;
            chargeAudio.spatialBlend = 0f;
            chargeAudio.volume = 0.55f;

            if (unlockAfter && !unlockAfter.IsCleared) unlockAfter.Cleared += Unlock;
            else Unlock();
        }

        void OnDestroy()
        {
            if (unlockAfter) unlockAfter.Cleared -= Unlock;
        }

        public void Unlock()
        {
            if (Unlocked) return;
            Unlocked = true;
            Hud.Instance?.SetObjective($"Hold [F] to warp to {destinationName}");
        }

        void Update()
        {
            if (Engaged) return;
            float dt = Time.deltaTime;
            bool charging = Unlocked && chargeHeld && ship.ControlsEnabled;

            if (charging && !chargeAudio.isPlaying && Charge01 <= 0.01f) chargeAudio.Play();
            if (!charging && chargeAudio.isPlaying) chargeAudio.Stop();

            Charge01 = charging ? Mathf.Min(1f, Charge01 + dt / chargeTime) : Mathf.MoveTowards(Charge01, 0f, dt * 1.5f);
            ApplyEffects(Charge01 * 0.3f);
            if (charging) ChaseCamera.Shake(dt * 0.5f * Charge01);

            if (Charge01 >= 1f) StartCoroutine(Engage());
        }

        IEnumerator Engage()
        {
            Engaged = true;
            ship.ControlsEnabled = false;
            if (ship.Body) ship.Body.detectCollisions = false;

            Vector3 direction = destinationMarker
                ? (destinationMarker.position - transform.position).normalized
                : transform.forward;
            ship.alignDirection = direction;

            Hud.Instance?.SetObjective(null);
            Hud.Instance?.ShowTitle("WARP", destinationName, tunnelTime);
            Sfx.Play2D(Sfx.WarpBoom, 0.9f);

            float t = 0f;
            bool fading = false;
            while (t < tunnelTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / tunnelTime);
                float ramp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 1f, k));
                ship.velocityOverride = direction * Mathf.Lerp(ship.cruiseSpeed, warpSpeed, ramp * ramp);
                ApplyEffects(Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(t / 0.6f)));
                ChaseCamera.Shake(Time.deltaTime * 1.5f);

                if (!fading && t > tunnelTime - 0.6f)
                {
                    fading = true;
                    ScreenFader.FadeTo(Color.white, 0.55f);
                }
                yield return null;
            }

            SceneManager.LoadScene(destinationScene);
        }

        void ApplyEffects(float intensity)
        {
            if (chase) chase.warpAmount = intensity;
            if (volume) volume.weight = intensity;
            EmitStreaks(intensity);
        }

        void EmitStreaks(float intensity)
        {
            if (!streaks || intensity <= 0.01f) return;
            streakBudget += Mathf.Lerp(20f, 700f, intensity * intensity) * Time.deltaTime;
            var emit = new ParticleSystem.EmitParams();
            while (streakBudget >= 1f)
            {
                streakBudget -= 1f;
                Vector2 ring = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(7f, 40f);
                emit.position = new Vector3(ring.x, ring.y, UnityEngine.Random.Range(120f, 220f));
                emit.velocity = new Vector3(0f, 0f, -Mathf.Lerp(250f, 900f, intensity));
                emit.startLifetime = 0.6f;
                emit.startSize = UnityEngine.Random.Range(0.15f, 0.45f);
                streaks.Emit(emit, 1);
            }
        }

        void CreateWarpVolume()
        {
            var go = new GameObject("Warp Effects");
            go.transform.SetParent(transform, false);
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 0f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.Add<LensDistortion>(true).intensity.value = -0.55f;
            profile.Add<ChromaticAberration>(true).intensity.value = 1f;
            volume.sharedProfile = profile;
        }
    }
}
