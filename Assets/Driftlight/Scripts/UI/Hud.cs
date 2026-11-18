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
            "MOUSE  steer\nLEFT CLICK  fire\nW / S  thrust / brake\nA / D  strafe\nQ / E  roll\nSPACE / CTRL  up / down\nSHIFT  boost\nHOLD F  warp (when unlocked)\nESC  free the mouse";
        [TextArea(3, 12)]
        public string footControlsText =
            "MOUSE  look\nW A S D  move\nSHIFT  sprint\nSPACE  jump\nE  interact\nESC  free the mouse";
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
        Health shipHealth;

        // Combat section
        Image hullFill;
        Image shieldFill;
        Text salvageText;
        RectTransform hitMarker;
        Image[] hitMarkerBars;
        float hitMarkerTimer;
        Image damageFlash;
        float damageFlashAlpha;
        Color damageFlashColor;
        readonly System.Collections.Generic.List<RectTransform> enemyMarkers = new System.Collections.Generic.List<RectTransform>();

        // Warp section
        RectTransform warpGroup;
        Text warpPrompt;
        Image warpFill;
        WarpDrive warpDrive;

        // On-foot section
        RectTransform footGroup;
        Text footControlsHint;
        Text fragmentsText;
        Text promptText;
        PlayerFootController footPlayer;

        // Lore panel
        CanvasGroup lorePanel;
        Text loreTitle;
        Text loreBody;
        float loreTimer;

        // Messages (all scenes)
        Text objectiveLabel;
        Text objectiveText;
        Text titleText;
        Text subtitleText;
        float titleTimer;
        float titleDuration;

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

            BuildMessages();
            BuildFlightHud();
            BuildCombatHud();
            BuildWarpHud();
            BuildFootHud();
            BuildLorePanel();
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
            if (ship && !shipHealth) shipHealth = ship.GetComponent<Health>();
            if (ship && !warpDrive) warpDrive = ship.GetComponent<WarpDrive>();
            if (!footPlayer) footPlayer = PlayerFootController.Player;
            UpdateMessages();
            UpdateFlightHud();
            UpdateCombatHud();
            UpdateWarpHud();
            UpdateFootHud();
            UpdateLorePanel();
        }

        /// <summary>Shows an interaction prompt under the crosshair (null hides it).</summary>
        public void SetPrompt(string text)
        {
            promptText.text = text ?? "";
        }

        /// <summary>Shows a log from the vanished people in a panel at the bottom of the screen.</summary>
        public void ShowLore(string title, string body, float seconds)
        {
            loreTitle.text = Spaced(title ?? "");
            loreBody.text = body ?? "";
            loreTimer = seconds;
        }

        // ------------------------------------------------------------------ warp

        void BuildWarpHud()
        {
            warpGroup = Rect("Warp", flightGroup, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(520f, 70f));
            warpPrompt = Label("Warp Prompt", warpGroup, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(520f, 30f), 20, TextAnchor.UpperCenter, Accent);
            var background = Box("Warp Bar", warpGroup, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(420f, 8f), PanelColor);
            warpFill = Box("Fill", background.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.75f, 0.95f, 1f, 1f));
            var fillRect = warpFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            warpGroup.gameObject.SetActive(false);
        }

        void UpdateWarpHud()
        {
            bool show = ship && warpDrive && warpDrive.Unlocked && !warpDrive.Engaged;
            if (warpGroup.gameObject.activeSelf != show) warpGroup.gameObject.SetActive(show);
            if (!show) return;
            warpPrompt.text = warpDrive.Charge01 > 0.01f
                ? Spaced("CHARGING WARP DRIVE")
                : $"HOLD  [F]  TO WARP   -   {warpDrive.destinationName}";
            SetBar(warpFill, warpDrive.Charge01);
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 4f);
            warpPrompt.color = new Color(Accent.r, Accent.g, Accent.b, warpDrive.Charge01 > 0.01f ? 1f : pulse);
        }

        // ------------------------------------------------------------------ on foot

        void BuildFootHud()
        {
            footGroup = Group("On Foot");
            Box("Dot", footGroup, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f), new Color(1f, 1f, 1f, 0.85f));
            promptText = Label("Prompt", footGroup, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(900f, 40f), 26, TextAnchor.MiddleCenter, Accent);
            Label("Fragments Label", footGroup, new Vector2(1f, 0f), new Vector2(-60f, 100f), new Vector2(400f, 24f), 18, TextAnchor.LowerRight, AccentDim).text = "SIGNAL FRAGMENTS";
            fragmentsText = Label("Fragments", footGroup, new Vector2(1f, 0f), new Vector2(-60f, 50f), new Vector2(400f, 56f), 46, TextAnchor.LowerRight, Violet);
            footControlsHint = Label("Controls", footGroup, new Vector2(1f, 1f), new Vector2(-60f, -60f), new Vector2(420f, 220f), 18, TextAnchor.UpperRight, new Color(1f, 1f, 1f, 0.8f));
            footControlsHint.text = footControlsText;
            footGroup.gameObject.SetActive(false);
        }

        void UpdateFootHud()
        {
            bool show = footPlayer != null && ship == null;
            if (footGroup.gameObject.activeSelf != show) footGroup.gameObject.SetActive(show);
            if (!show) return;
            fragmentsText.text = $"{GameState.FragmentsFound} / {GameState.TotalFragments}";
            footControlsHint.color = new Color(1f, 1f, 1f, Mathf.Clamp01(hintTimer / 2f) * 0.8f);
            hintTimer -= Time.deltaTime;
        }

        // ------------------------------------------------------------------ lore panel

        void BuildLorePanel()
        {
            var panel = Box("Lore Panel", root, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(980f, 250f), new Color(0.02f, 0.04f, 0.07f, 0.78f));
            lorePanel = panel.gameObject.AddComponent<CanvasGroup>();
            lorePanel.alpha = 0f;
            lorePanel.blocksRaycasts = false;
            Box("Accent", panel.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(980f, 3f), Violet);
            loreTitle = Label("Title", panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(920f, 34f), 22, TextAnchor.UpperCenter, Violet);
            loreBody = Label("Body", panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(900f, 170f), 23, TextAnchor.UpperCenter, Color.white);
            loreBody.fontStyle = FontStyle.Italic;
            loreBody.lineSpacing = 1.15f;
        }

        void UpdateLorePanel()
        {
            float target = loreTimer > 0f ? 1f : 0f;
            loreTimer -= Time.deltaTime;
            lorePanel.alpha = Mathf.MoveTowards(lorePanel.alpha, target, Time.deltaTime * 1.5f);
        }

        // ------------------------------------------------------------------ public API

        /// <summary>Sets the objective line in the top-left corner (null or empty hides it).</summary>
        public void SetObjective(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            objectiveLabel.enabled = show;
            objectiveText.text = show ? text : "";
        }

        /// <summary>Shows a big centred title and subtitle that fade out after <paramref name="seconds"/>.</summary>
        public void ShowTitle(string title, string subtitle, float seconds)
        {
            titleText.text = Spaced(title ?? "");
            subtitleText.text = subtitle ?? "";
            titleDuration = Mathf.Max(0.5f, seconds);
            titleTimer = titleDuration;
        }

        /// <summary>Flashes the hit marker around the crosshair (orange for a kill).</summary>
        public void ShowHitMarker(bool kill)
        {
            hitMarkerTimer = kill ? 0.3f : 0.12f;
            var colour = kill ? Warning : Color.white;
            foreach (var bar in hitMarkerBars) bar.color = colour;
            hitMarker.localScale = Vector3.one * (kill ? 1.4f : 1f);
        }

        /// <summary>Flashes the screen edges: blue when shields absorb the hit, red when the hull takes it.</summary>
        public void FlashDamage(bool shielded)
        {
            damageFlashColor = shielded ? new Color(0.3f, 0.8f, 1f) : new Color(1f, 0.15f, 0.1f);
            damageFlashAlpha = shielded ? 0.18f : 0.35f;
        }

        // ------------------------------------------------------------------ messages

        void BuildMessages()
        {
            var group = Group("Messages");
            objectiveLabel = Label("Objective Label", group, new Vector2(0f, 1f), new Vector2(60f, -56f), new Vector2(600f, 22f), 16, TextAnchor.UpperLeft, AccentDim);
            objectiveLabel.text = "OBJECTIVE";
            objectiveLabel.enabled = false;
            objectiveText = Label("Objective", group, new Vector2(0f, 1f), new Vector2(60f, -80f), new Vector2(700f, 60f), 26, TextAnchor.UpperLeft, Color.white);

            titleText = Label("Title", group, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1600f, 70f), 54, TextAnchor.MiddleCenter, Accent);
            subtitleText = Label("Subtitle", group, new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(1400f, 40f), 24, TextAnchor.MiddleCenter, Color.white);
            titleText.color = new Color(Accent.r, Accent.g, Accent.b, 0f);
            subtitleText.color = new Color(1f, 1f, 1f, 0f);
        }

        void UpdateMessages()
        {
            if (titleTimer <= 0f) return;
            titleTimer -= Time.deltaTime;
            float elapsed = titleDuration - titleTimer;
            float alpha = Mathf.Clamp01(elapsed / 0.4f) * Mathf.Clamp01(titleTimer / 0.8f);
            titleText.color = new Color(Accent.r, Accent.g, Accent.b, alpha);
            subtitleText.color = new Color(1f, 1f, 1f, alpha * 0.9f);
        }

        // ------------------------------------------------------------------ combat

        void BuildCombatHud()
        {
            // Full-screen damage tint (sits under everything else).
            damageFlash = Box("Damage Flash", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.clear);
            var flashRect = damageFlash.rectTransform;
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.sizeDelta = Vector2.zero;
            flashRect.SetAsFirstSibling();

            shieldFill = Bar("SHIELDS", flightGroup, new Vector2(60f, 108f), 300f, Accent);
            hullFill = Bar("HULL", flightGroup, new Vector2(60f, 156f), 300f, Warning);

            Label("Salvage Label", flightGroup, new Vector2(1f, 0f), new Vector2(-60f, 196f), new Vector2(300f, 24f), 18, TextAnchor.LowerRight, AccentDim).text = "SALVAGE";
            salvageText = Label("Salvage", flightGroup, new Vector2(1f, 0f), new Vector2(-60f, 150f), new Vector2(300f, 46f), 36, TextAnchor.LowerRight, Amber);

            // Hit marker: four diagonal ticks around the crosshair.
            hitMarker = Rect("Hit Marker", flightGroup, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
            hitMarkerBars = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                float angle = 45f + 90f * i;
                Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 17f;
                hitMarkerBars[i] = Box("Tick", hitMarker, new Vector2(0.5f, 0.5f), offset, new Vector2(11f, 2.5f), Color.white);
                hitMarkerBars[i].rectTransform.pivot = new Vector2(0.5f, 0.5f);
                hitMarkerBars[i].rectTransform.anchoredPosition = offset;
                hitMarkerBars[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            hitMarker.gameObject.SetActive(false);
        }

        void UpdateCombatHud()
        {
            float dt = Time.deltaTime;

            damageFlashAlpha = Mathf.MoveTowards(damageFlashAlpha, 0f, dt * 1.2f);
            damageFlash.color = new Color(damageFlashColor.r, damageFlashColor.g, damageFlashColor.b, damageFlashAlpha);

            hitMarkerTimer -= dt;
            hitMarker.gameObject.SetActive(hitMarkerTimer > 0f && ship);
            hitMarker.localScale = Vector3.Lerp(hitMarker.localScale, Vector3.one, dt * 10f);

            if (!ship) return;
            if (shipHealth)
            {
                SetBar(hullFill, shipHealth.Hull01);
                SetBar(shieldFill, shipHealth.Shield01);
            }
            salvageText.text = GameState.Salvage.ToString("0000");
            UpdateEnemyMarkers();
        }

        void UpdateEnemyMarkers()
        {
            var cam = Camera.main;
            var drones = EnemyDrone.Active;
            while (enemyMarkers.Count < drones.Count) enemyMarkers.Add(BuildEnemyMarker());

            for (int i = 0; i < enemyMarkers.Count; i++)
            {
                var marker = enemyMarkers[i];
                bool show = cam && i < drones.Count;
                if (marker.gameObject.activeSelf != show) marker.gameObject.SetActive(show);
                if (!show) continue;

                Vector3 screen = cam.WorldToScreenPoint(drones[i].transform.position);
                bool behind = screen.z < 0f;
                if (behind) screen = new Vector3(Screen.width - screen.x, Screen.height - screen.y, 0f);

                const float margin = 50f;
                bool onScreen = !behind && screen.x > margin && screen.x < Screen.width - margin && screen.y > margin && screen.y < Screen.height - margin;
                if (!onScreen)
                {
                    // Push the marker to the screen edge in the drone's direction.
                    Vector2 centre = new Vector2(Screen.width, Screen.height) * 0.5f;
                    Vector2 dir = (Vector2)screen - centre;
                    if (dir.sqrMagnitude < 1f) dir = Vector2.down;
                    float scale = Mathf.Min((centre.x - margin) / Mathf.Max(Mathf.Abs(dir.x), 1e-4f),
                                            (centre.y - margin) / Mathf.Max(Mathf.Abs(dir.y), 1e-4f));
                    screen = centre + dir * scale;
                }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
                marker.anchoredPosition = local;
                float distance = Vector3.Distance(cam.transform.position, drones[i].transform.position);
                float size = onScreen ? Mathf.Clamp(900f / Mathf.Max(distance, 1f), 0.7f, 1.6f) : 0.8f;
                marker.localScale = Vector3.one * size;
                marker.localRotation = Quaternion.identity;
            }
        }

        RectTransform BuildEnemyMarker()
        {
            // A diamond outline made of four short bars.
            var marker = Rect("Enemy Marker", flightGroup, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            marker.pivot = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < 4; i++)
            {
                float angle = 45f + 90f * i;
                Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 9f;
                var bar = Box("Edge", marker, new Vector2(0.5f, 0.5f), offset, new Vector2(15f, 2.5f), Warning);
                bar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                bar.rectTransform.anchoredPosition = offset;
                bar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            }
            return marker;
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
