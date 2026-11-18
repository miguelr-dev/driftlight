using System;
using UnityEngine;

namespace Driftlight
{
    public enum Team
    {
        Player,
        Enemy,
    }

    /// <summary>Hull plus an optional recharging shield. Shields soak damage first.</summary>
    public class Health : MonoBehaviour
    {
        public Team team = Team.Enemy;
        public float maxHull = 40f;
        public float maxShield = 0f;
        [Tooltip("Seconds without taking damage before shields start recharging.")]
        public float shieldRegenDelay = 3f;
        public float shieldRegenRate = 14f;
        [Tooltip("Hull never drops below this. The demo autopilot uses it so recordings never end early.")]
        public float minHull = 0f;

        public float Hull { get; private set; }
        public float Shield { get; private set; }
        public bool IsDead { get; private set; }
        public float Hull01 => maxHull > 0f ? Hull / maxHull : 0f;
        public float Shield01 => maxShield > 0f ? Shield / maxShield : 0f;
        public float LastDamageTime { get; private set; } = -999f;

        /// <summary>Damage amount and world position of the hit.</summary>
        public event Action<float, Vector3> Damaged;
        public event Action Died;

        void Awake()
        {
            Hull = maxHull;
            Shield = maxShield;
        }

        void Update()
        {
            if (IsDead || maxShield <= 0f || Shield >= maxShield) return;
            if (Time.time - LastDamageTime > shieldRegenDelay)
                Shield = Mathf.Min(maxShield, Shield + shieldRegenRate * Time.deltaTime);
        }

        public void TakeDamage(float amount, Vector3 point)
        {
            if (IsDead || amount <= 0f) return;
            LastDamageTime = Time.time;

            float absorbed = Mathf.Min(Shield, amount);
            Shield -= absorbed;
            Hull = Mathf.Max(minHull, Hull - (amount - absorbed));
            Damaged?.Invoke(amount, point);

            if (Hull <= 0f)
            {
                Hull = 0f;
                IsDead = true;
                Died?.Invoke();
            }
        }

        public void Restore()
        {
            Hull = maxHull;
            Shield = maxShield;
            IsDead = false;
        }
    }
}
