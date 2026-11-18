using UnityEngine;

namespace Driftlight
{
    /// <summary>
    /// A floating piece of the signal. Walk up to it and press E to recover it: it adds to the
    /// fragment count and shows one of the vanished people's logs.
    /// </summary>
    public class SignalFragment : MonoBehaviour
    {
        public string fragmentTitle = "SIGNAL FRAGMENT 1 / 3";
        [TextArea(4, 10)]
        public string loreText = "";
        [Tooltip("Objective shown after the fragment is recovered.")]
        public string afterObjective = "";
        public float interactRange = 4.5f;

        [Header("Visuals")]
        public Transform crystal;
        public Light glowLight;
        public Renderer[] hideWhenCollected;
        public GameObject collectEffect;

        public bool Collected { get; private set; }

        float baseLightIntensity;
        Vector3 crystalRest;

        void Awake()
        {
            if (glowLight) baseLightIntensity = glowLight.intensity;
            if (crystal) crystalRest = crystal.localPosition;
        }

        void Update()
        {
            float t = Time.time;
            if (crystal)
            {
                crystal.localPosition = crystalRest + Vector3.up * Mathf.Sin(t * 1.6f) * 0.25f;
                crystal.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
            }
            if (glowLight) glowLight.intensity = baseLightIntensity * (Collected ? 0.2f : 0.8f + 0.2f * Mathf.Sin(t * 3f));

            if (Collected) return;
            var player = PlayerFootController.Player;
            if (!player) return;

            bool inRange = Vector3.Distance(player.transform.position, transform.position) < interactRange;
            if (Hud.Instance) Hud.Instance.SetPrompt(inRange ? "[E]  Recover the signal fragment" : null);
            if (inRange && player.interactPressed) Collect();
        }

        public void Collect()
        {
            if (Collected) return;
            Collected = true;
            GameState.AddFragment();

            var hud = Hud.Instance;
            if (hud)
            {
                hud.SetPrompt(null);
                hud.ShowLore(fragmentTitle, loreText, 12f);
                hud.SetObjective(afterObjective);
            }

            Vector3 at = crystal ? crystal.position : transform.position;
            if (collectEffect) Instantiate(collectEffect, at, Quaternion.identity);
            Sfx.Play2D(Sfx.Pickup, 0.8f);
            foreach (var r in hideWhenCollected)
                if (r) r.enabled = false;
            if (crystal) crystal.gameObject.SetActive(false);
        }
    }
}
