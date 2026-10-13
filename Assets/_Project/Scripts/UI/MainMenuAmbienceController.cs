using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    /// <summary>
    /// Gives the main menu a restrained surveillance-camera ambience without
    /// moving or dimming the interactive UI itself.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuAmbienceController : MonoBehaviour
    {
        [Header("Cursor parallax")]
        [SerializeField] private RectTransform background;
        [SerializeField] private Vector2 maxOffset = new Vector2(28f, 14f);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.24f;
        [SerializeField, Range(0f, 1f)] private float maxTiltDegrees = 0.22f;

        [Header("Surveillance layers")]
        [SerializeField] private RectTransform scanline;
        [SerializeField, Min(2f)] private float scanlineCycleSeconds = 8f;
        [SerializeField] private Graphic alertPulse;
        [SerializeField] private TMP_Text statusReadout;

        private Vector2 backgroundOrigin;
        private Quaternion backgroundRotation;
        private Vector2 parallaxVelocity;

        public Vector2 CurrentParallaxOffset => background != null
            ? background.anchoredPosition - backgroundOrigin
            : Vector2.zero;

        private void OnEnable()
        {
            if (background != null)
            {
                backgroundOrigin = background.anchoredPosition;
                backgroundRotation = background.localRotation;
            }

            parallaxVelocity = Vector2.zero;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            UpdateParallax(deltaTime);
            UpdateScanline(now);
            UpdateStatusPulse(now);
        }

        private void OnDisable()
        {
            if (background != null)
            {
                background.anchoredPosition = backgroundOrigin;
                background.localRotation = backgroundRotation;
            }

        }

        private void UpdateParallax(float deltaTime)
        {
            if (background == null)
            {
                return;
            }

            Vector2 normalizedPointer = Vector2.zero;
            if (Mouse.current != null && Screen.width > 0 && Screen.height > 0)
            {
                Vector2 pointer = Mouse.current.position.ReadValue();
                normalizedPointer = new Vector2(
                    Mathf.Clamp(pointer.x / Screen.width * 2f - 1f, -1f, 1f),
                    Mathf.Clamp(pointer.y / Screen.height * 2f - 1f, -1f, 1f));
            }

            Vector2 target = backgroundOrigin + Vector2.Scale(normalizedPointer, maxOffset);
            background.anchoredPosition = Vector2.SmoothDamp(
                background.anchoredPosition,
                target,
                ref parallaxVelocity,
                smoothTime,
                Mathf.Infinity,
                deltaTime);

            Quaternion targetRotation = backgroundRotation * Quaternion.Euler(
                0f,
                0f,
                -normalizedPointer.x * maxTiltDegrees);
            float damping = 1f - Mathf.Exp(-deltaTime / smoothTime);
            background.localRotation = Quaternion.Slerp(
                background.localRotation,
                targetRotation,
                damping);
        }

        private void UpdateScanline(float now)
        {
            if (scanline == null || !(scanline.parent is RectTransform parent))
            {
                return;
            }

            float progress = Mathf.Repeat(now / scanlineCycleSeconds, 1f);
            float margin = 60f;
            Vector2 position = scanline.anchoredPosition;
            position.y = Mathf.Lerp(parent.rect.height * 0.5f + margin,
                -parent.rect.height * 0.5f - margin, progress);
            scanline.anchoredPosition = position;
        }

        private void UpdateStatusPulse(float now)
        {
            float pulse = 0.52f + Mathf.Sin(now * 2.1f) * 0.28f;
            SetGraphicAlpha(alertPulse, pulse);
            if (statusReadout != null)
            {
                Color color = statusReadout.color;
                color.a = 0.76f + Mathf.Sin(now * 1.05f) * 0.16f;
                statusReadout.color = color;
            }
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
