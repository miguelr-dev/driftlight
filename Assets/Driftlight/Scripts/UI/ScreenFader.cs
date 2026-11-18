using UnityEngine;
using UnityEngine.UI;

namespace Driftlight
{
    /// <summary>Full-screen colour fades that survive scene loads (warp flash, arrivals).</summary>
    public class ScreenFader : MonoBehaviour
    {
        static ScreenFader instance;

        Image image;
        Color from, to;
        float duration, elapsed;
        bool running;

        static ScreenFader Instance
        {
            get
            {
                if (instance) return instance;
                var go = new GameObject("Screen Fader");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<ScreenFader>();
                instance.Build();
                return instance;
            }
        }

        /// <summary>Fades from the current state to a solid colour.</summary>
        public static void FadeTo(Color color, float seconds)
        {
            var fader = Instance;
            var current = fader.image.color;
            fader.Begin(new Color(color.r, color.g, color.b, current.a), new Color(color.r, color.g, color.b, 1f), seconds);
        }

        /// <summary>Starts at a solid colour and fades to clear.</summary>
        public static void FadeFrom(Color color, float seconds)
        {
            Instance.Begin(new Color(color.r, color.g, color.b, 1f), new Color(color.r, color.g, color.b, 0f), seconds);
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var fill = new GameObject("Fade", typeof(RectTransform));
            fill.transform.SetParent(transform, false);
            var rect = (RectTransform)fill.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            image = fill.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
        }

        void Begin(Color start, Color end, float seconds)
        {
            from = start;
            to = end;
            duration = Mathf.Max(0.01f, seconds);
            elapsed = 0f;
            running = true;
            image.color = start;
        }

        void Update()
        {
            if (!running) return;
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            image.color = Color.Lerp(from, to, k * k * (3f - 2f * k));
            if (k >= 1f) running = false;
        }
    }
}
