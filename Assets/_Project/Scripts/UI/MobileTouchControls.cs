using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    /// <summary>Safe-area-aware Android controls using the game's red pixel art.</summary>
    public sealed class MobileTouchControls : MonoBehaviour
    {
        private const string ArtPath = "MobileControls/";
        private RectTransform safeArea;
        private GameObject controls;
        private GameFlowController flow;
        private GameplayModeController mode;
        private GameSessionController session;
        private PlayerInteraction interaction;
        private MobileControlButton pickupButton, flashlightButton, mapButton;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private TMP_FontAsset font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var owner = new GameObject("MobileTouchControls");
            DontDestroyOnLoad(owner);
            owner.AddComponent<MobileTouchControls>();
#endif
        }

        private void Awake()
        {
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            var canvasObject = new GameObject("TouchCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            controls = new GameObject("SafeArea", typeof(RectTransform));
            safeArea = controls.GetComponent<RectTransform>();
            safeArea.SetParent(canvasObject.transform, false);
            ApplySafeArea();
            CreateJoystick();
            pickupButton = ArtButton("recoger", "e", new Vector2(-180, 210), 224);
            ArtButton("inventario", "i", new Vector2(-180, 425), 176);
            flashlightButton = ArtButton("linterna", "f", new Vector2(-395, 195), 176);
            ArtButton("pausa", "escape", new Vector2(-440, -95), 136, true);
            TextButton("ESCÁNER", "tab", new Vector2(-185, 180));
            mapButton = TextButton("MAPA", "m", new Vector2(0, 180));
            TextButton("DIARIO", "j", new Vector2(185, 180));
            controls.SetActive(false);
            SceneManager.sceneLoaded += OnSceneLoaded;
            ResolveControllers();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode) => ResolveControllers();

        private void ResolveControllers()
        {
            flow = FindFirstObjectByType<GameFlowController>();
            mode = FindFirstObjectByType<GameplayModeController>();
            session = FindFirstObjectByType<GameSessionController>();
            interaction = FindFirstObjectByType<PlayerInteraction>();
        }

        private void Update()
        {
            bool visible = flow != null && flow.ShowsGameplayHud &&
                mode != null && mode.AllowsGameplay;
            if (controls.activeSelf != visible) controls.SetActive(visible);
            if (visible)
            {
                pickupButton.SetInteractable(interaction != null && interaction.InteractionEnabled &&
                    interaction.CurrentTarget != null && interaction.CurrentTarget.CanInteract);
                flashlightButton.SetInteractable(session != null && session.State != null &&
                    session.State.HasItem("flashlight"));
                mapButton.SetInteractable(flow.HasMapAccess);
            }
            if (Screen.safeArea != lastSafeArea ||
                lastScreenSize != new Vector2Int(Screen.width, Screen.height)) ApplySafeArea();
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width,
                lastSafeArea.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width,
                lastSafeArea.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }

        private RectTransform ControlRect(string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(safeArea, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private void CreateJoystick()
        {
            var rect = ControlRect("Joystick", Vector2.zero, new Vector2(230, 240), new Vector2(320, 320));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>(ArtPath + "joystick_base");
            image.preserveAspect = true;
            var handle = new GameObject("Palanca", typeof(RectTransform), typeof(Image));
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.SetParent(rect, false);
            handleRect.anchorMin = handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = rect.sizeDelta;
            var handleImage = handle.GetComponent<Image>();
            handleImage.sprite = Resources.Load<Sprite>(ArtPath + "joystick_palanca");
            handleImage.preserveAspect = true;
            handleImage.raycastTarget = false;
            rect.gameObject.AddComponent<MobileJoystick>().Configure(handleRect, 70f);
        }

        private MobileControlButton ArtButton(string artName, string key, Vector2 position, float size,
            bool top = false)
        {
            var rect = ControlRect(artName, new Vector2(1, top ? 1 : 0), position, new Vector2(size, size));
            var image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            var button = rect.gameObject.AddComponent<MobileControlButton>();
            button.Configure(image, Resources.Load<Texture2D>(ArtPath + artName), key);
            return button;
        }

        private MobileControlButton TextButton(string label, string key, Vector2 position)
        {
            var rect = ControlRect(label, new Vector2(0.5f, 0f), position, new Vector2(166, 80));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.025f, 0.04f, 0.85f);
            var button = rect.gameObject.AddComponent<MobileControlButton>();
            button.Configure(image, null, key);
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var text = labelObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = label;
            text.fontSize = 27;
            text.color = new Color(1f, 0.52f, 0.51f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return button;
        }
    }
}
