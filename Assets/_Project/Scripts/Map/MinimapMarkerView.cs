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
        private bool shapeConfigured;
        public Sprite[] pixelIcons;
        private MinimapMarkerKind displayedKind;
        private MinimapExitState displayedExitState;

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

            if (!shapeConfigured || displayedKind != kind || displayedExitState != exitState)
            {
                if (icon != null) icon.gameObject.SetActive(false);
                ConfigureShape(kind, exitState);
                displayedKind = kind;
                displayedExitState = exitState;
                shapeConfigured = true;
            }

            RectTransform rect = (RectTransform)root.transform;
            var position = new Vector2(
                (normalizedPosition.x - 0.5f) * content.rect.width,
                (normalizedPosition.y - 0.5f) * content.rect.height);
            if (rect.anchoredPosition != position) rect.anchoredPosition = position;
            if (!root.activeSelf) root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null && root.activeSelf) root.SetActive(false);
        }

        private void ConfigureShape(MinimapMarkerKind kind, MinimapExitState exitState)
        {
            int index=(int)kind;
            if(pixelIcons!=null&&index<pixelIcons.Length&&pixelIcons[index]!=null)
            {
                background.sprite=pixelIcons[index];background.color=ColorFor(kind,exitState);
                background.rectTransform.localRotation=Quaternion.identity;background.rectTransform.sizeDelta=new Vector2(16,16);
                if(accentPrimary!=null)accentPrimary.gameObject.SetActive(false);
                if(accentSecondary!=null)accentSecondary.gameObject.SetActive(false);
                return;
            }
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
                accentPrimary.color = new Color(0.09f, 0.035f, 0.05f, 1f);
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
                case MinimapMarkerKind.Item:
                    baseRect.sizeDelta = new Vector2(10f, 10f);
                    if (firstRect != null) firstRect.sizeDelta = new Vector2(6f, 6f);
                    if (secondRect != null) secondRect.sizeDelta = new Vector2(3f, 3f);
                    break;
                case MinimapMarkerKind.Camera:
                    baseRect.sizeDelta = new Vector2(13f, 8f);
                    if (firstRect != null) firstRect.sizeDelta = new Vector2(8f, 4f);
                    if (secondRect != null)
                    {
                        secondRect.anchoredPosition = new Vector2(3f, 0f);
                        secondRect.sizeDelta = new Vector2(3f, 2f);
                    }
                    break;
                case MinimapMarkerKind.Point:
                    baseRect.sizeDelta = new Vector2(6f, 6f);
                    if (accentPrimary != null) accentPrimary.gameObject.SetActive(false);
                    if (accentSecondary != null) accentSecondary.gameObject.SetActive(false);
                    break;
            }
        }

        private static Color ColorFor(MinimapMarkerKind kind, MinimapExitState exitState)
        {
            if (kind == MinimapMarkerKind.Exit)
            {
                return exitState == MinimapExitState.Available
                    ? new Color(1f, .82f, .75f)
                    : new Color(1f, .47f, .38f);
            }

            switch (kind)
            {
                case MinimapMarkerKind.Objective: return new Color(1f, .77f, .53f);
                case MinimapMarkerKind.Checkpoint: return new Color(.87f, .65f, .69f);
                case MinimapMarkerKind.Threat: return new Color(1f, .17f, .26f);
                case MinimapMarkerKind.Camera: return new Color(1f, .48f, .56f);
                case MinimapMarkerKind.Item: return new Color(1f, .91f, .72f);
                case MinimapMarkerKind.Point: return new Color(.89f, .61f, .64f);
                default: return new Color(0.15f, 0.18f, 0.24f, 0.95f);
            }
        }
    }
}
