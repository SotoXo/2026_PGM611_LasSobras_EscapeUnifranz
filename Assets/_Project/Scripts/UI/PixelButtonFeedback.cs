using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    /// <summary>Efecto de selección (pulso) y pulsación (hundir 2 px) para botones 8 bits. Convive con MenuButtonFeedback.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class PixelButtonFeedback : MonoBehaviour, ISelectHandler, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform content;
        private Vector2 basePosition;
        private Vector3 baseScale = Vector3.one;
        private Button button;
        private Coroutine pulse;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (content == null) content = transform as RectTransform;
            basePosition = content.anchoredPosition; baseScale = content.localScale;
        }

        private void OnDisable() { if (pulse != null) StopCoroutine(pulse); pulse = null; if (content != null) { content.anchoredPosition = basePosition; content.localScale = baseScale; } }
        public void OnSelect(BaseEventData e) => Pulse();
        public void OnPointerEnter(PointerEventData e) => Pulse();
        public void OnPointerDown(PointerEventData e) { if (button.IsInteractable()) content.anchoredPosition = basePosition + Vector2.down * 2f; }
        public void OnPointerUp(PointerEventData e) { content.anchoredPosition = basePosition; }

        private void Pulse()
        {
            if (!button.IsInteractable() || !isActiveAndEnabled) return;
            if (pulse != null) StopCoroutine(pulse);
            pulse = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            for (float t = 0f; t < .16f; t += Time.unscaledDeltaTime)
            {
                content.localScale = baseScale * (1f + .05f * Mathf.Sin(t / .16f * Mathf.PI));
                yield return null;
            }
            content.localScale = baseScale; pulse = null;
        }
    }
}
