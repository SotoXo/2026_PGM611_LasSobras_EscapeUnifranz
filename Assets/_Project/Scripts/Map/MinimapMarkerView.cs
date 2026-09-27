using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.Map
{
    public sealed class MinimapMarkerView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text icon;
        [SerializeField] private Image background;
        [SerializeField] private Image accentPrimary;
        [SerializeField] private Image accentSecondary;

        public void Render(
            MinimapMarkerKind kind,
            MinimapExitState exitState,
            Vector2 normalizedPosition,
            RectTransform content)
        {
            if (root == null || background == null || content == null)
            {
                Hide();
                return;
            }

            if (icon != null)
            {
                icon.gameObject.SetActive(false);
            }

            ConfigureShape(kind, exitState);

            RectTransform rect = (RectTransform)root.transform;
            rect.anchoredPosition = new Vector2(
                (normalizedPosition.x - 0.5f) * content.rect.width,
                (normalizedPosition.y - 0.5f) * content.rect.height);
            root.SetActive(true);
        }

        public void Hide()
        {
            root?.SetActive(false);
        }

        private void ConfigureShape(MinimapMarkerKind kind, MinimapExitState exitState)
        {
            RectTransform baseRect = background.rectTransform;
            RectTransform firstRect = accentPrimary != null ? accentPrimary.rectTransform : null;
            RectTransform secondRect = accentSecondary != null ? accentSecondary.rectTransform : null;
            Color color = ColorFor(kind, exitState);
            background.color = color;
            baseRect.localRotation = Quaternion.identity;
            baseRect.sizeDelta = new Vector2(13f, 13f);

            if (accentPrimary != null)
            {
                accentPrimary.gameObject.SetActive(true);
                accentPrimary.color = new Color(0.025f, 0.06f, 0.09f, 1f);
                firstRect.localRotation = Quaternion.identity;
                firstRect.anchoredPosition = Vector2.zero;
                firstRect.sizeDelta = new Vector2(7f, 7f);
            }

            if (accentSecondary != null)
            {
                accentSecondary.gameObject.SetActive(true);
                accentSecondary.color = color;
                secondRect.localRotation = Quaternion.identity;
                secondRect.anchoredPosition = Vector2.zero;
                secondRect.sizeDelta = new Vector2(3f, 3f);
            }

            switch (kind)
            {
                case MinimapMarkerKind.Exit:
                    baseRect.sizeDelta = new Vector2(11f, 15f);
                    if (firstRect != null)
                    {
                        firstRect.sizeDelta = new Vector2(7f, 11f);
                    }
                    if (secondRect != null)
                    {
                        secondRect.sizeDelta = new Vector2(3f, 3f);
                        secondRect.anchoredPosition = new Vector2(2f, 0f);
                    }
                    break;
                case MinimapMarkerKind.Objective:
                    baseRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    if (firstRect != null) firstRect.localRotation = baseRect.localRotation;
                    if (secondRect != null) secondRect.localRotation = baseRect.localRotation;
                    break;
                case MinimapMarkerKind.Checkpoint:
                    background.rectTransform.sizeDelta = new Vector2(13f, 4f);
                    if (firstRect != null)
                    {
                        accentPrimary.color = color;
                        firstRect.sizeDelta = new Vector2(4f, 13f);
                    }
                    if (accentSecondary != null) accentSecondary.gameObject.SetActive(false);
                    break;
                case MinimapMarkerKind.Threat:
                    baseRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    if (firstRect != null)
                    {
                        accentPrimary.color = Color.white;
                        firstRect.localRotation = Quaternion.Euler(0f, 0f, -45f);
                        firstRect.sizeDelta = new Vector2(2f, 6f);
                        firstRect.anchoredPosition = new Vector2(-1f, 1f);
                    }
                    if (secondRect != null)
                    {
                        accentSecondary.color = Color.white;
                        secondRect.localRotation = Quaternion.Euler(0f, 0f, -45f);
                        secondRect.sizeDelta = new Vector2(2f, 2f);
                        secondRect.anchoredPosition = new Vector2(2f, -2f);
                    }
                    break;
            }
        }

        private static Color ColorFor(MinimapMarkerKind kind, MinimapExitState exitState)
        {
            if (kind == MinimapMarkerKind.Exit)
            {
                return exitState == MinimapExitState.Blocked
                    ? new Color(1f, 0.32f, 0.39f, 1f)
                    : new Color(0.22f, 0.84f, 0.95f, 1f);
            }

            switch (kind)
            {
                case MinimapMarkerKind.Objective: return new Color(1f, 0.72f, 0.29f, 1f);
                case MinimapMarkerKind.Checkpoint: return new Color(0.3f, 0.89f, 0.54f, 1f);
                case MinimapMarkerKind.Threat: return new Color(1f, 0.32f, 0.39f, 1f);
                default: return new Color(0.15f, 0.18f, 0.24f, 0.95f);
            }
        }
    }
}
