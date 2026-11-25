using UnityEngine;

namespace EscapeUNIFRANZ.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform card, parent;
        private Canvas canvas;
        private Rect lastSafeArea;
        private Vector2Int screenSize;
        private Vector2 authoredPosition;

        private void Awake()
        {
            card = (RectTransform)transform;
            parent = card.parent as RectTransform;
            canvas = GetComponentInParent<Canvas>();
            authoredPosition = card.anchoredPosition;
        }

        private void OnEnable() => Fit();

        private void LateUpdate()
        {
            if (Screen.safeArea != lastSafeArea || screenSize.x != Screen.width || screenSize.y != Screen.height)
                Fit();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) Fit();
        }

        public static float FitScale(Vector2 available, Vector2 design) =>
            Mathf.Clamp(Mathf.Min(available.x / design.x, available.y / design.y), .01f, 1f);

        private void Fit()
        {
            if (card == null || parent == null || canvas == null || card.sizeDelta.x <= 0 || card.sizeDelta.y <= 0)
                return;
            lastSafeArea = Screen.safeArea;
            screenSize = new Vector2Int(Screen.width, Screen.height);
            float scale = Mathf.Max(.01f, canvas.rootCanvas.scaleFactor);
            Vector2 available = Vector2.Min(parent.rect.size, lastSafeArea.size / scale) - new Vector2(32, 32);
            card.localScale = Vector3.one * FitScale(available, card.sizeDelta);
            card.anchoredPosition = authoredPosition + (lastSafeArea.center - new Vector2(screenSize.x, screenSize.y) * .5f) / scale;
        }
    }
}
