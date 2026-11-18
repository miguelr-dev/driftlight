using UnityEngine;

namespace Driftlight
{
    /// <summary>
    /// Walks the explorer for autoplay: takes in the view, follows the beam to the signal fragment,
    /// recovers it, then turns toward the sunset. Hops and sidesteps if something blocks the way.
    /// </summary>
    [RequireComponent(typeof(PlayerFootController))]
    public class FootAutopilot : MonoBehaviour
    {
        public float lookAroundSeconds = 4f;

        PlayerFootController player;
        SignalFragment fragment;
        float time;
        Vector3 lastPosition;
        float stuckTimer;
        float sidestepTimer;
        float sidestepDirection = 1f;

        void Awake()
        {
            player = GetComponent<PlayerFootController>();
            fragment = FindAnyObjectByType<SignalFragment>();
            lastPosition = transform.position;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            time += dt;
            player.move = Vector2.zero;
            player.sprint = false;
            player.interactPressed = false;

            if (time < lookAroundSeconds)
            {
                player.lookDelta.x += Mathf.Sin(time * 0.8f) * 14f * dt; // slow pan while the title card shows
                return;
            }
            if (!fragment) return;

            if (fragment.Collected)
            {
                var sun = RenderSettings.sun;
                Vector3 view = sun ? -sun.transform.forward : transform.forward;
                player.LookAt(player.cameraPivot.position + view * 100f, 1.2f);
                return;
            }

            Vector3 target = fragment.transform.position;
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            float distance = flat.magnitude;

            player.LookAt(target + Vector3.up * 2f, 3f);
            float heading = Vector3.Angle(transform.forward, flat);

            if (distance > 2.8f)
            {
                player.move = heading < 35f ? new Vector2(0f, 1f) : Vector2.zero;
                player.sprint = distance > 20f;

                if (sidestepTimer > 0f)
                {
                    sidestepTimer -= dt;
                    player.move = new Vector2(sidestepDirection, 0.4f);
                }
                else
                {
                    float moved = (transform.position - lastPosition).magnitude;
                    stuckTimer = player.move.sqrMagnitude > 0f && moved < 2f * dt ? stuckTimer + dt : 0f;
                    if (stuckTimer > 0.6f)
                    {
                        player.jump = true;
                        sidestepTimer = 0.8f;
                        sidestepDirection = -sidestepDirection;
                        stuckTimer = 0f;
                    }
                }
            }
            else
            {
                player.interactPressed = true;
            }
            lastPosition = transform.position;
        }
    }
}
