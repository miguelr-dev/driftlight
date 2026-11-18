using UnityEngine;
using UnityEngine.UI;

namespace Driftlight
{
    /// <summary>
    /// The in-game HUD. It builds its own canvas at runtime, so a scene only needs an empty
    /// GameObject with this component. Sections show up when the thing they describe exists
    /// (the flight HUD appears when there is a player ship, and so on).
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public static Hud Instance { get; private set; }

        public static readonly Color Accent = new Color(0.373f, 0.89f, 1f, 1f);       // cyan #5fe3ff (docs/mockups style guide)
        public static readonly Color AccentDim = new Color(0.373f, 0.89f, 1f, 0.45f);
        public static readonly Color Warning = new Color(1f, 0.353f, 0.431f, 1f);     // red #ff5a6e: hull, enemies
        public static readonly Color Amber = new Color(1f, 0.784f, 0.341f, 1f);      // amber #ffc857: salvage, upgrades
        public static readonly Color Violet = new Color(0.773f, 0.545f, 1f, 1f);     // violet #c58bff: the signal, story
        public static readonly Color PanelColor = new Color(0.047f, 0.078f, 0.141f, 0.72f); // panel #0c1424 at 72%

        [TextArea(3, 12)]
        public string controlsText =
            "MOUSE  steer\nW / S  thrust / brake\nA / D  strafe\nQ / E  roll\nSPACE / CTRL  up / down\nSHIFT  boost\nESC  free the mouse";
        public float controlsHintSeconds = 14f;

        Font font;
        RectTransform root;

        // Flight section
        RectTransform flightGroup;
        RectTransform aimCursor;
        Text speedText;
        Image boostFill;
        Text controlsHint;
        float hintTimer;

        ShipController ship;
        ShipPlayerInput shipInput;

        void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasObject = new GameObject("HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root = (RectTransform)canvasObject.transform;

            BuildFlightHud();
            hintTimer = controlsHintSeconds;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (!ship) ship = ShipController.Player;
            if (ship && !shipInput) shipInput = ship.GetComponent<ShipPlayerInput>();
            UpdateFlightHud();
        }

        // ------------------------------------------------------------------ flight

        void BuildFlightHud()
        {
            flightGroup = Group("Flight");

            // Crosshair: four ticks around the centre plus a dot.
            var crosshair = Rect("Crosshair", flightGroup, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
            Box("Dot", crosshair, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4f, 4f), Accent);
            Box("Top", crosshair, new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(2f, 12f), Accent);
            Box("Bottom", crosshair, new Vector2(0.5f, 0.5f), new Vector2(0f, -16f), new Vector2(2f, 12f), Accent);
            Box("Left", crosshair, new Vector2(0.5f, 0.5f), new Vector2(-16f, 0f), new Vector2(12f, 2f), Accent);
            Box("Right", crosshair, new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(12f, 2f), Accent);

            // Aim cursor: shows where the mouse is pushing the virtual stick.
            aimCursor = Box("Aim Cursor", flightGroup, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(11f, 11f), AccentDim).rectTransform;
            aimCursor.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Bottom-left: boost bar (hull and shields stack above it).
            boostFill = Bar("BOOST", flightGroup, new Vector2(60f, 60f), 300f, new Color(0.75f, 0.55f, 1f, 0.95f));

            // Bottom-right: speed readout.
            Label("Speed Label", flightGroup, new Vector2(1f, 0f), new Vector2(-60f, 100f), new Vector2(300f, 24f), 18, TextAnchor.LowerRight, AccentDim).text = "SPEED  M/S";
            speedText = Label("Speed", flightGroup, new Vector2(1f, 0f), new Vector2(-60f, 50f), new Vector2(300f, 56f), 46, TextAnchor.LowerRight, Accent);

            // Top-right: controls reminder that fades out.
            controlsHint = Label("Controls", flightGroup, new Vector2(1f, 1f), new Vector2(-60f, -60f), new Vector2(420f, 260f), 18, TextAnchor.UpperRight, new Color(1f, 1f, 1f, 0.8f));
            controlsHint.text = controlsText;
        }

        void UpdateFlightHud()
        {
            bool show = ship != null;
            if (flightGroup.gameObject.activeSelf != show) flightGroup.gameObject.SetActive(show);
            if (!show) return;

            Vector2 stick = shipInput ? shipInput.Stick : ship.steer;
            aimCursor.anchoredPosition = stick * 150f;
            SetBar(boostFill, ship.BoostEnergy);
            speedText.text = Mathf.RoundToInt(ship.Speed).ToString("000");

            hintTimer -= Time.deltaTime;
            controlsHint.color = new Color(1f, 1f, 1f, Mathf.Clamp01(hintTimer / 2f) * 0.8f);
        }

        // ------------------------------------------------------------------ building blocks

        protected RectTransform Group(string name)
        {
            var group = Rect(name, root, Vector2.zero, Vector2.zero, Vector2.zero);
            group.anchorMin = Vector2.zero;
            group.anchorMax = Vector2.one;
            group.pivot = new Vector2(0.5f, 0.5f);
            group.sizeDelta = Vector2.zero;
            return group;
        }

        /// <summary>Creates a RectTransform anchored (and pivoted) at <paramref name="anchor"/>.</summary>
        protected static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        protected static Image Box(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var rt = Rect(name, parent, anchor, position, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        protected Text Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            var rt = Rect(name, parent, anchor, position, size);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        /// <summary>A labelled bar anchored bottom-left. Returns the fill image; drive it with SetBar.</summary>
        protected Image Bar(string label, Transform parent, Vector2 position, float width, Color color)
        {
            var frame = Rect(label + " Bar", parent, Vector2.zero, position, new Vector2(width, 30f));
            Label(label + " Label", frame, new Vector2(0f, 1f), new Vector2(0f, 2f), new Vector2(width, 20f), 16, TextAnchor.LowerLeft, AccentDim).text = label;
            var background = Box("Background", frame, Vector2.zero, Vector2.zero, new Vector2(width, 8f), PanelColor);
            var fill = Box("Fill", background.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, color);
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            return fill;
        }

        protected static void SetBar(Image fill, float value01)
        {
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
        }

        /// <summary>Adds spaces between letters for a wide "sci-fi" title look.</summary>
        public static string Spaced(string text) => string.Join(" ", text.ToCharArray());
    }
}
