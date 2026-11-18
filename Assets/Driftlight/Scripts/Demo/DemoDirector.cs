using UnityEngine;
using UnityEngine.SceneManagement;

namespace Driftlight
{
    /// <summary>
    /// Autoplay: in every scene it swaps the player's input for an autopilot, keeps the ship alive,
    /// and quits a few seconds after the planet's signal fragment is recovered.
    /// </summary>
    public class DemoDirector : MonoBehaviour
    {
        public float maxSeconds = 300f;
        public float secondsAfterFragment = 10f;

        float elapsed;
        float fragmentTime = -1f;
        SignalFragment fragment;
        bool jumped;

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[DEMO] Scene loaded: {scene.name}");
            if (!string.IsNullOrEmpty(DemoMode.StartScene) && scene.name != DemoMode.StartScene && !jumped)
            {
                jumped = true;
                SceneManager.LoadScene(DemoMode.StartScene);
                return;
            }
            fragment = FindAnyObjectByType<SignalFragment>();

            var cursor = FindAnyObjectByType<CursorLock>();
            if (cursor) cursor.enabled = false;

            var ship = FindAnyObjectByType<ShipController>();
            if (ship)
            {
                var input = ship.GetComponent<ShipPlayerInput>();
                if (input) input.enabled = false;
                if (!ship.GetComponent<ShipAutopilot>()) ship.gameObject.AddComponent<ShipAutopilot>();
                var health = ship.GetComponent<Health>();
                if (health) health.minHull = 30f;
            }

            var walker = FindAnyObjectByType<PlayerFootController>();
            if (walker)
            {
                var input = walker.GetComponent<PlayerFootInput>();
                if (input) input.enabled = false;
                if (!walker.GetComponent<FootAutopilot>()) walker.gameObject.AddComponent<FootAutopilot>();
            }
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            if (fragment && fragment.Collected && fragmentTime < 0f)
            {
                fragmentTime = elapsed;
                Debug.Log($"[DEMO] Fragment recovered at {elapsed:0.0}s");
            }

            bool done = fragmentTime >= 0f && elapsed - fragmentTime > secondsAfterFragment;
            if (done || elapsed > maxSeconds)
            {
                Debug.Log($"[DEMO] Finished after {elapsed:0.0}s (fragment {(fragmentTime >= 0f ? "recovered" : "NOT recovered")})");
                Application.Quit();
                enabled = false;
            }
        }
    }
}
