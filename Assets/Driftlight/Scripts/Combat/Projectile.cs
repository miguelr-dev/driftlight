using UnityEngine;

namespace Driftlight
{
    /// <summary>
    /// A fast energy bolt. It ray-casts along its path every frame instead of using physics,
    /// so it never tunnels through small or fast targets.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public float speed = 280f;
        public float damage = 7f;
        public float lifetime = 2.2f;
        public GameObject impactPrefab;

        /// <summary>Hits landed this session, per team (shown in the demo log).</summary>
        public static int PlayerHits, EnemyHits;

        Team team;
        Transform owner;
        Vector3 velocity;
        float age;

        public void Launch(Team shooterTeam, Transform shooter, Vector3 direction, Vector3 shooterVelocity)
        {
            team = shooterTeam;
            owner = shooter;
            direction.Normalize();
            float inherited = Mathf.Max(0f, Vector3.Dot(shooterVelocity, direction));
            velocity = direction * (speed + inherited);
            transform.rotation = Quaternion.LookRotation(direction);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            if (age > lifetime)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 step = velocity * dt;
            float distance = step.magnitude;
            if (distance > 0f && Physics.Raycast(transform.position, step / distance, out RaycastHit hit, distance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (owner == null || !hit.transform.IsChildOf(owner))
                {
                    OnHit(hit);
                    return;
                }
            }
            transform.position += step;
        }

        void OnHit(RaycastHit hit)
        {
            var health = hit.collider.GetComponentInParent<Health>();
            if (health && health.team != team)
            {
                if (team == Team.Player) PlayerHits++; else EnemyHits++;
                bool wasAlive = !health.IsDead;
                health.TakeDamage(damage, hit.point);
                if (team == Team.Player && wasAlive && Hud.Instance) Hud.Instance.ShowHitMarker(health.IsDead);
            }

            if (impactPrefab) Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Sfx.Play(Sfx.Impact, hit.point, 0.45f);
            Destroy(gameObject);
        }
    }
}
