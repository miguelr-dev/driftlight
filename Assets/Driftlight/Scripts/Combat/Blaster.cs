using System;
using UnityEngine;

namespace Driftlight
{
    /// <summary>
    /// Fires projectiles from one or more muzzles, alternating between them. Whoever owns the weapon
    /// (player input or AI) sets <see cref="triggerHeld"/>.
    /// </summary>
    public class Blaster : MonoBehaviour
    {
        public Projectile projectilePrefab;
        public Transform[] muzzles;
        [Tooltip("Shots per second.")]
        public float fireRate = 9f;
        [Tooltip("Random spread in degrees.")]
        public float spread = 0.25f;
        public Team team = Team.Player;
        [Tooltip("Aim at whatever is under the screen's crosshair instead of straight ahead (player ships).")]
        public bool aimAtCrosshair;
        public float convergenceDistance = 260f;
        public float volume = 0.35f;

        [NonSerialized] public bool triggerHeld;
        /// <summary>Velocity of a shooter that has no Rigidbody (AI drones set this every frame).</summary>
        [NonSerialized] public Vector3 externalVelocity;

        public event Action Fired;

        float cooldown;
        int nextMuzzle;
        Rigidbody body;

        void Awake()
        {
            body = GetComponentInParent<Rigidbody>();
        }

        void Update()
        {
            cooldown -= Time.deltaTime;
            if (!triggerHeld || cooldown > 0f || !projectilePrefab || muzzles == null || muzzles.Length == 0) return;
            cooldown = 1f / Mathf.Max(0.01f, fireRate);

            Transform muzzle = muzzles[nextMuzzle++ % muzzles.Length];
            Vector3 direction = muzzle.forward;
            if (aimAtCrosshair)
            {
                var cam = Camera.main;
                if (cam) direction = (CrosshairPoint(cam) - muzzle.position).normalized;
            }
            direction = Quaternion.Euler(UnityEngine.Random.insideUnitCircle * spread) * direction;

            var projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(direction));
            Vector3 shooterVelocity = body ? body.linearVelocity : externalVelocity;
            projectile.Launch(team, transform.root, direction, shooterVelocity);

            Sfx.Play(team == Team.Player ? Sfx.PlayerShot : Sfx.EnemyShot, muzzle.position, volume);
            Fired?.Invoke();
        }

        Vector3 CrosshairPoint(Camera cam)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 start = ray.origin + ray.direction * 18f; // start past our own ship
            if (Physics.Raycast(start, ray.direction, out RaycastHit hit, convergenceDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(transform.root))
                return hit.point;
            return ray.origin + ray.direction * convergenceDistance;
        }
    }
}
