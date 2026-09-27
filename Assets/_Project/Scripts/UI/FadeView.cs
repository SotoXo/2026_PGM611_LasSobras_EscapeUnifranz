using System.Collections;
using UnityEngine;

namespace EscapeUNIFRANZ.UI
{
    /// <summary>
    /// Minimal unscaled fullscreen fade used by scene transitions.
    /// </summary>
    public sealed class FadeView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float duration = 0.4f;

        private void Awake()
        {
            SetImmediate(0f, false);
        }

        public IEnumerator FadeOut()
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            canvasGroup.blocksRaycasts = true;
            yield return FadeTo(1f);
        }

        public IEnumerator FadeIn()
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            yield return FadeTo(0f);
            canvasGroup.blocksRaycasts = false;
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = canvasGroup.alpha;
            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private void SetImmediate(float alpha, bool blocksRaycasts)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = blocksRaycasts;
            canvasGroup.interactable = false;
        }
    }
}
