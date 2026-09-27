using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TopDownShooter
{
    public sealed class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform area;
        private RectTransform knob;
        private int pointerId = int.MinValue;

        public Vector2 Value { get; private set; }
        public bool Held => pointerId != int.MinValue;

        public void Initialize(RectTransform handle)
        {
            area = (RectTransform)transform;
            knob = handle;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Held) return;
            pointerId = eventData.pointerId;
            UpdateValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId) UpdateValue(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            ResetStick();
        }

        private void OnDisable() => ResetStick();

        private void ResetStick()
        {
            pointerId = int.MinValue;
            Value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        private void UpdateValue(PointerEventData eventData)
        {
            if (area == null || knob == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    area, eventData.position, eventData.pressEventCamera, out var local)) return;
            float radius = Mathf.Min(area.rect.width, area.rect.height) * 0.35f;
            Value = Vector2.ClampMagnitude(local / radius, 1f);
            knob.anchoredPosition = Value * radius;
        }
    }

    public sealed class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        public Vector2 MoveVector => moveStick != null ? moveStick.Value : Vector2.zero;
        public Vector2 AimVector => aimStick != null ? aimStick.Value : Vector2.zero;
        public bool AimActive => aimStick != null && aimStick.Held && aimStick.Value.sqrMagnitude > 0.04f;
        public bool IsDeathVisible => deathPanel != null && deathPanel.activeSelf;
        public bool IsVictoryVisible => victoryPanel != null && victoryPanel.activeSelf;
        public float FadeAlpha
        {
            get => fade.alpha;
            set
            {
                fade.alpha = value;
                fade.blocksRaycasts = value > 0.01f;
            }
        }

        private Canvas canvas;
        private RectTransform safeRoot;
        private GameObject startPanel;
        private GameObject hud;
        private GameObject controls;
        private GameObject deathPanel;
        private GameObject victoryPanel;
        private GameObject interactButton;
        private GameObject bossBar;
        private RectTransform bossFill;
        private Text healthText;
        private Text stageText;
        private Text objectiveText;
        private VirtualStick moveStick;
        private VirtualStick aimStick;
        private CanvasGroup fade;
        private Font font;
        private Sprite circle;
        private Sprite square;
        private Rect lastSafeArea;

        private void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            circle = Resources.Load<Sprite>("Shapes/Circle");
            square = Resources.Load<Sprite>("Shapes/Square");
            BuildInterface();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea) ApplySafeArea();
            var stage = GameFlow.Instance != null ? GameFlow.Instance.Stage : null;
            if (stage == null || !hud.activeSelf) return;
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                healthText.text = "HEALTH  " + player.Health.Current + " / " + player.Health.Maximum;
            stageText.text = "STAGE  " + stage.StageNumber + " / 3";
            objectiveText.text = stage.StageNumber == 3
                ? (stage.Remaining > 0 ? "DEFEAT THE BOSS" : "PORTAL OPEN")
                : (stage.Remaining > 0 ? "ENEMIES  " + stage.Remaining + " / " + stage.Total : "PORTAL OPEN");

            bossBar.SetActive(stage.StageNumber == 3 && stage.Remaining > 0);
            if (stage.StageNumber == 3 && stage.BossHealth != null)
            {
                var ratio = (float)stage.BossHealth.Current / stage.BossHealth.Maximum;
                bossFill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            }
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("Game Canvas", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var safeObject = new GameObject("Safe Area", typeof(RectTransform));
            safeObject.transform.SetParent(canvasObject.transform, false);
            safeRoot = safeObject.GetComponent<RectTransform>();
            ApplySafeArea();

            BuildStart();
            BuildHud();
            BuildControls();
            BuildDeath();
            BuildVictory();

            var fadeRect = Stretch(canvasObject.transform, "Fade");
            var image = fadeRect.gameObject.AddComponent<Image>();
            image.color = Color.black;
            fade = fadeRect.gameObject.AddComponent<CanvasGroup>();
            FadeAlpha = 0f;

            if (EventSystem.current == null)
            {
                var events = new GameObject("Event System");
                events.transform.SetParent(transform, false);
                events.AddComponent<EventSystem>();
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }

        private void BuildStart()
        {
            var root = Stretch(safeRoot, "Start Screen");
            root.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.055f, 0.11f, 0.97f);
            startPanel = root.gameObject;
            TextAt(root, "Title", "TOP DOWN\nSHOOTER DEMO", 88, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.65f), new Vector2(920, 220), Color.white);
            TextAt(root, "Instructions", "LEFT STICK  MOVE     •     RIGHT STICK  AIM + FIRE",
                29, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.44f), new Vector2(1100, 70),
                new Color(0.72f, 0.82f, 0.94f));
            ButtonAt(root, "Play Button", "PLAY", new Vector2(0.5f, 0.25f), new Vector2(350, 112),
                new Color(0.14f, 0.78f, 0.72f), () => GameFlow.Instance.StartRun());
        }

        private void BuildHud()
        {
            var root = Stretch(safeRoot, "HUD");
            hud = root.gameObject;
            var top = NewRect(root, "Top Bar", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(0, 104));
            top.pivot = new Vector2(0.5f, 1f);
            top.anchoredPosition = Vector2.zero;
            top.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.04f, 0.08f, 0.82f);
            stageText = TextAt(top, "Stage", "STAGE  1 / 3", 35, TextAnchor.MiddleLeft,
                new Vector2(0, 0.5f), new Vector2(330, 90), Color.white, new Vector2(35, 0));
            stageText.rectTransform.pivot = new Vector2(0, 0.5f);
            objectiveText = TextAt(top, "Objective", "ENEMIES", 36, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(650, 90), new Color(0.45f, 1f, 0.86f));
            healthText = TextAt(top, "Health", "HEALTH  10 / 10", 35, TextAnchor.MiddleRight,
                new Vector2(1, 0.5f), new Vector2(420, 90), Color.white, new Vector2(-35, 0));
            healthText.rectTransform.pivot = new Vector2(1, 0.5f);

            var boss = NewRect(root, "Boss Bar", new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -150), new Vector2(620, 28));
            boss.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.16f, 0.9f);
            bossBar = boss.gameObject;
            var fill = Stretch(boss, "Boss Health Fill");
            fill.gameObject.AddComponent<Image>().color = new Color(0.91f, 0.21f, 0.49f);
            bossFill = fill;
            TextAt(boss, "Boss Label", "BOSS", 26, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(620, 32), Color.white);
        }

        private void BuildControls()
        {
            var root = Stretch(safeRoot, "Touch Controls");
            controls = root.gameObject;
            moveStick = MakeStick(root, "Movement Stick", new Vector2(0, 0),
                new Vector2(205, 165), "MOVE");
            aimStick = MakeStick(root, "Aim Stick", new Vector2(1, 0),
                new Vector2(-205, 165), "AIM / FIRE");
            interactButton = ButtonAt(root, "Interact Button", "INTERACT",
                new Vector2(1, 0), new Vector2(285, 88), new Color(0.42f, 0.88f, 0.43f),
                () => GameFlow.Instance.UsePortal(), new Vector2(-210, 360)).gameObject;
            interactButton.SetActive(false);
        }

        private VirtualStick MakeStick(Transform parent, string name, Vector2 anchor, Vector2 offset, string label)
        {
            var area = NewRect(parent, name, anchor, anchor, offset, new Vector2(260, 260));
            var background = area.gameObject.AddComponent<Image>();
            background.sprite = circle;
            background.color = new Color(0.58f, 0.69f, 0.86f, 0.27f);
            var knob = NewRect(area, "Handle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(100, 100));
            var knobImage = knob.gameObject.AddComponent<Image>();
            knobImage.sprite = circle;
            knobImage.color = new Color(0.52f, 0.97f, 0.93f, 0.85f);
            knobImage.raycastTarget = false;
            var stick = area.gameObject.AddComponent<VirtualStick>();
            stick.Initialize(knob);
            TextAt(area, "Label", label, 23, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1), new Vector2(260, 44), Color.white, new Vector2(0, 26));
            return stick;
        }

        private void BuildDeath()
        {
            var root = Stretch(safeRoot, "Death Screen");
            root.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.02f, 0.06f, 0.96f);
            deathPanel = root.gameObject;
            TextAt(root, "Death Title", "STAGE FAILED", 82, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.62f), new Vector2(900, 160), Color.white);
            TextAt(root, "Death Subtitle", "Retry this stage with full health", 32,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.47f), new Vector2(900, 80),
                new Color(0.88f, 0.75f, 0.8f));
            ButtonAt(root, "Retry Button", "RETRY", new Vector2(0.5f, 0.27f),
                new Vector2(350, 110), new Color(0.92f, 0.34f, 0.49f), () => GameFlow.Instance.RetryStage());
        }

        private void BuildVictory()
        {
            var root = Stretch(safeRoot, "Victory Screen");
            root.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.10f, 0.10f, 0.97f);
            victoryPanel = root.gameObject;
            TextAt(root, "Victory Title", "DEMO COMPLETE", 82, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.62f), new Vector2(1100, 160), Color.white);
            TextAt(root, "Victory Subtitle", "All three stages cleared", 34,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.47f), new Vector2(900, 80),
                new Color(0.57f, 0.94f, 0.80f));
            ButtonAt(root, "Replay Button", "REPLAY", new Vector2(0.5f, 0.27f),
                new Vector2(350, 110), new Color(0.14f, 0.78f, 0.72f), () => GameFlow.Instance.Replay());
        }

        public void HidePanels()
        {
            startPanel.SetActive(false);
            hud.SetActive(false);
            controls.SetActive(false);
            deathPanel.SetActive(false);
            victoryPanel.SetActive(false);
            interactButton.SetActive(false);
        }

        public void ShowStart()
        {
            HidePanels();
            startPanel.SetActive(true);
        }

        public void ShowGameplay()
        {
            HidePanels();
            hud.SetActive(true);
            controls.SetActive(true);
        }

        public void ShowDeath()
        {
            controls.SetActive(false);
            interactButton.SetActive(false);
            deathPanel.SetActive(true);
        }

        public void ShowVictory()
        {
            HidePanels();
            victoryPanel.SetActive(true);
        }

        public void SetInteractVisible(bool visible)
        {
            if (interactButton != null)
                interactButton.SetActive(visible && controls.activeSelf);
        }

        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safeRoot.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
        }

        private static RectTransform Stretch(Transform parent, string name)
        {
            var rect = NewRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform NewRect(Transform parent, string name, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private Text TextAt(Transform parent, string name, string text, int size, TextAnchor alignment,
            Vector2 anchor, Vector2 dimensions, Color color, Vector2? offset = null)
        {
            var rect = NewRect(parent, name, anchor, anchor, offset ?? Vector2.zero, dimensions);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.text = text;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private Button ButtonAt(Transform parent, string name, string title, Vector2 anchor,
            Vector2 dimensions, Color color, UnityEngine.Events.UnityAction action, Vector2? offset = null)
        {
            var rect = NewRect(parent, name, anchor, anchor, offset ?? Vector2.zero, dimensions);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = square;
            image.color = color;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            TextAt(rect, "Label", title, 42, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), dimensions, new Color(0.025f, 0.08f, 0.11f));
            return button;
        }
    }
}
