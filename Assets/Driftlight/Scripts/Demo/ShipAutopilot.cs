using UnityEngine;

namespace Driftlight
{
    /// <summary>
    /// Flies the ship for autoplay: heads for the relay, hunts the nearest drone with lead aiming,
    /// and charges the warp drive once it unlocks. Uses the same inputs a player would.
    /// </summary>
    [RequireComponent(typeof(ShipController))]
    public class ShipAutopilot : MonoBehaviour
    {
        ShipController ship;
        Blaster[] blasters;
        WarpDrive warp;
        Transform relay;
        float logTimer;

        void Awake()
        {
            ship = GetComponent<ShipController>();
            blasters = GetComponentsInChildren<Blaster>();
            warp = GetComponent<WarpDrive>();
            var relayObject = GameObject.Find("Signal Relay");
            if (relayObject) relay = relayObject.transform;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var target = EnemyDrone.Nearest(transform.position);
            Vector3 aim;
            float thrust = 1f;
            bool boost = false, fire = false, chargeWarp = false;

            if (target)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                float boltSpeed = blasters.Length > 0 && blasters[0].projectilePrefab ? blasters[0].projectilePrefab.speed + Mathf.Max(0f, ship.ForwardSpeed) : 300f;
                aim = target.transform.position + (target.Velocity - ship.Velocity) * (distance / boltSpeed);
                thrust = distance > 110f ? 1f : 0.3f;
                boost = distance > 260f;
                fire = distance < 240f && Vector3.Angle(transform.forward, aim - transform.position) < 4f;
            }
            else if (warp && warp.Unlocked && warp.destinationMarker)
            {
                aim = warp.destinationMarker.position;
                thrust = 0.5f;
                chargeWarp = Vector3.Angle(transform.forward, aim - transform.position) < 12f;
            }
            else if (relay)
            {
                aim = relay.position + Vector3.up * 45f;
                thrust = Vector3.Distance(transform.position, relay.position) > 160f ? 1f : 0.4f;
            }
            else
            {
                aim = transform.position + transform.forward * 100f;
            }

            Vector3 local = transform.InverseTransformDirection((aim - transform.position).normalized);
            var steer = new Vector2(Mathf.Clamp(local.x * 2.5f, -1f, 1f), Mathf.Clamp(local.y * 2.5f, -1f, 1f));
            if (local.z < 0f) steer.x = local.x >= 0f ? 1f : -1f;

            ship.steer = Vector2.Lerp(ship.steer, steer, 1f - Mathf.Exp(-6f * dt));
            ship.roll = Mathf.Clamp(transform.right.y * 2f, -1f, 1f) * 0.6f; // keep the wings roughly level
            ship.thrust = thrust;
            ship.boost = boost;
            ship.strafe = 0f;
            ship.lift = 0f;
            foreach (var blaster in blasters) blaster.triggerHeld = fire;
            if (warp) warp.chargeHeld = chargeWarp;

            logTimer -= dt;
            if (logTimer <= 0f)
            {
                logTimer = 5f;
                var health = GetComponent<Health>();
                Debug.Log($"[DEMO] t={Time.timeSinceLevelLoad:0}s drones={EnemyDrone.Active.Count} salvage={GameState.Salvage} hull={(health ? health.Hull : 0):0} shield={(health ? health.Shield : 0):0} warpUnlocked={(warp && warp.Unlocked)} charge={(warp ? warp.Charge01 : 0):0.00}");
            }
        }
    }
}
