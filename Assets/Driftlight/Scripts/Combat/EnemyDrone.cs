using System.Collections.Generic;
using UnityEngine;

namespace Driftlight
{
    /// <summary>
    /// A hostile drone that circles the player at a set distance, leads its shots and keeps apart
    /// from other drones. Gives salvage when destroyed.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyDrone : MonoBehaviour
    {
        /// <summary>Every live drone in the scene.</summary>
        public static readonly List<EnemyDrone> Active = new List<EnemyDrone>();

        [Header("Movement")]
        public float speed = 42f;
        public float acceleration = 36f;
        public float preferredDistance = 75f;
        [Tooltip("Sideways push that makes drones circle instead of hovering in place.")]
        public float orbitStrength = 34f;
        public float turnSpeed = 3.5f;

        [Header("Weapons")]
        public Blaster blaster;
        public float fireRange = 190f;
        public float fireAngle = 9f;
        public Vector2 firstShotDelay = new Vector2(1.2f, 2.8f);

        [Header("Reward and effects")]
        public int salvageReward = 15;
        public GameObject explosionPrefab;
        public Transform visual;
        public Transform spinner;

        public Health Health { get; private set; }
        public Vector3 Velocity { get; private set; }

        Vector3 orbitAxis;
        float wobble;
        float fireReadyTime;
        float punch;
        Health playerHealth;

        void Awake()
        {
            Health = GetComponent<Health>();
            Health.Died += OnDied;
            Health.Damaged += (amount, point) => punch = 1f;
            if (!blaster) blaster = GetComponentInChildren<Blaster>();
            orbitAxis = Random.onUnitSphere;
            wobble = Random.value * 10f;
            fireReadyTime = Time.time + Random.Range(firstShotDelay.x, firstShotDelay.y);
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 desiredVelocity = Vector3.zero;
            var player = ShipController.Player;

            if (player)
            {
                if (!playerHealth) playerHealth = player.GetComponent<Health>();
                Vector3 toPlayer = player.transform.position - transform.position;
                float distance = toPlayer.magnitude;
                Vector3 away = -toPlayer / Mathf.Max(distance, 0.01f);

                Vector3 tangent = Vector3.Cross(away, orbitAxis);
                if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(away, Vector3.up);
                tangent.Normalize();

                wobble += dt;
                Vector3 desired = player.transform.position + player.Velocity * 0.5f
                                  + away * preferredDistance
                                  + tangent * orbitStrength
                                  + Vector3.up * Mathf.Sin(wobble * 1.3f) * 8f;
                desiredVelocity = Vector3.ClampMagnitude((desired - transform.position) * 1.1f, speed);

                // Keep some space from other drones.
                foreach (var other in Active)
                {
                    if (other == this) continue;
                    Vector3 apart = transform.position - other.transform.position;
                    float d = apart.magnitude;
                    if (d < 18f && d > 0.01f) desiredVelocity += apart / d * (18f - d) * 1.5f;
                }

                // Aim where the player is going to be.
                float boltSpeed = blaster && blaster.projectilePrefab ? blaster.projectilePrefab.speed : 200f;
                Vector3 lead = player.transform.position + player.Velocity * (distance / boltSpeed);
                Vector3 aim = lead - transform.position;
                if (aim.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aim), 1f - Mathf.Exp(-turnSpeed * dt));

                if (blaster)
                {
                    bool playerAlive = !(playerHealth && playerHealth.IsDead) && player.ControlsEnabled;
                    blaster.triggerHeld = playerAlive && Time.time > fireReadyTime && distance < fireRange
                                          && Vector3.Angle(transform.forward, aim) < fireAngle;
                    blaster.externalVelocity = Velocity;
                }
            }
            else if (blaster)
            {
                blaster.triggerHeld = false;
            }

            Velocity = Vector3.MoveTowards(Velocity, desiredVelocity, acceleration * dt);
            transform.position += Velocity * dt;

            if (spinner) spinner.Rotate(0f, 0f, 120f * dt, Space.Self);
            if (visual)
            {
                punch = Mathf.MoveTowards(punch, 0f, dt * 5f);
                visual.localScale = Vector3.one * (1f + punch * 0.3f);
            }
        }

        void OnDied()
        {
            if (explosionPrefab) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            GameState.AddSalvage(salvageReward);
            Sfx.Play(Sfx.Explosion, transform.position, 1f);
            var player = ShipController.Player;
            if (player && Vector3.Distance(player.transform.position, transform.position) < 160f) ChaseCamera.Shake(0.35f);
            Destroy(gameObject);
        }

        public static EnemyDrone Nearest(Vector3 position)
        {
            EnemyDrone best = null;
            float bestDistance = float.MaxValue;
            foreach (var drone in Active)
            {
                float d = (drone.transform.position - position).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = drone;
                }
            }
            return best;
        }
    }
}
