using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Driftlight
{
    /// <summary>Damage feedback for the player's ship: crash damage, camera shake, and the death and respawn sequence.</summary>
    [RequireComponent(typeof(Health), typeof(ShipController))]
    public class PlayerShipDamage : MonoBehaviour
    {
        public GameObject explosionPrefab;
        [Tooltip("Impacts slower than this (m/s) do no damage.")]
        public float safeImpactSpeed = 12f;
        public float crashDamagePerSpeed = 0.8f;
        public float respawnDelay = 3.5f;

        Health health;
        ShipController ship;

        void Awake()
        {
            health = GetComponent<Health>();
            ship = GetComponent<ShipController>();
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnCollisionEnter(Collision collision)
        {
            float impact = collision.relativeVelocity.magnitude;
            if (impact > safeImpactSpeed)
                health.TakeDamage((impact - safeImpactSpeed) * crashDamagePerSpeed, collision.GetContact(0).point);
        }

        void OnDamaged(float amount, Vector3 point)
        {
            ChaseCamera.Shake(Mathf.Clamp(amount / 40f, 0.08f, 0.5f));
            if (Hud.Instance) Hud.Instance.FlashDamage(health.Shield > 0f);
            Sfx.Play2D(health.Shield > 0f ? Sfx.ShieldHit : Sfx.HullHit, 0.5f);
        }

        void OnDied() => StartCoroutine(DeathSequence());

        IEnumerator DeathSequence()
        {
            ship.ControlsEnabled = false;
            if (explosionPrefab) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Sfx.Play2D(Sfx.Explosion, 1f);
            ChaseCamera.Shake(1f);
            if (ship.visual) ship.visual.gameObject.SetActive(false);
            Hud.Instance?.ShowTitle("SHIP DESTROYED", "Reassembling from the last signal echo...", respawnDelay);
            yield return new WaitForSeconds(respawnDelay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
