using EscapeUNIFRANZ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    /// <summary>Touch controls reuse the existing keyboard bindings on Android.</summary>
    public sealed class MobileTouchControls : MonoBehaviour
    {
        private RectTransform safeArea;
        private GameObject controls;
        private GameFlowController flow;
        private GameplayModeController mode;
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

            Key("ARRIBA", "w", new Vector2(195, 310), new Vector2(140, 110), false);
            Key("IZQ", "a", new Vector2(45, 190), new Vector2(140, 110), false);
            Key("ABAJO", "s", new Vector2(195, 190), new Vector2(140, 110), false);
            Key("DER", "d", new Vector2(345, 190), new Vector2(140, 110), false);
            Key("USAR", "e", new Vector2(-210, 190), new Vector2(170, 120), true);
            Key("ESCANEAR", "tab", new Vector2(-400, 190), new Vector2(170, 120), true);
            Key("LUZ", "f", new Vector2(-590, 190), new Vector2(150, 120), true);
            Key("MAPA", "m", new Vector2(-210, 330), new Vector2(170, 90), true);
            Key("OBJETOS", "i", new Vector2(-400, 330), new Vector2(170, 90), true);
            Key("DIARIO", "j", new Vector2(-590, 330), new Vector2(150, 90), true);
            Key("PAUSA", "escape", new Vector2(-210, 45), new Vector2(170, 100), true);
            controls.SetActive(false);
        }

        private void Update()
        {
            if (flow == null) flow = FindFirstObjectByType<GameFlowController>();
            if (mode == null) mode = FindFirstObjectByType<GameplayModeController>();
            bool visible = flow != null && flow.ShowsGameplayHud &&
                mode != null && mode.AllowsGameplay;
            if (controls.activeSelf != visible) controls.SetActive(visible);
            if (Screen.safeArea != lastSafeArea ||
                lastScreenSize != new Vector2Int(Screen.width, Screen.height)) ApplySafeArea();
        }

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

        private void Key(string label, string key, Vector2 position, Vector2 size, bool right)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image),
                typeof(Button), typeof(OnScreenButton));
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(safeArea, false);
            rect.anchorMin = rect.anchorMax = new Vector2(right ? 1f : 0f, 0f);
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.04f, 0.08f, 0.11f, 0.72f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.pressedColor = new Color(0.2f, 0.8f, 0.9f, 1f);
            button.colors = colors;
            buttonObject.GetComponent<OnScreenButton>().controlPath = "<Keyboard>/" + key;

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
            text.color = new Color(0.85f, 0.95f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }
    }
}
