using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEngine;

namespace EscapeUNIFRANZ.Interaction
{
    public sealed class Scannable2D : MonoBehaviour, IZoneRuntimeInitializable
    {
        [SerializeField] private ScanCategory category;
        [SerializeField] private string shortLabel;
        [SerializeField] private string fallbackState;
        [SerializeField] private bool secret;
        [SerializeField] private GameFlagId revealFlag;

        private GameState state;
        private GameObject feedbackRoot;
        private TMP_Text feedbackLabel;
        private float visibleUntil;

        public ScanCategory Category => category;
        public bool CanScan => ScanLogic.CanReveal(state, secret, revealFlag);

        public void Initialize(ZoneRuntimeContext context)
        {
            state = context.Session.State;
            EnsureFeedbackView();
            Hide();
        }

        private void Update()
        {
            if (feedbackRoot != null && feedbackRoot.activeSelf && Time.unscaledTime >= visibleUntil)
            {
                Hide();
            }
        }

        private void OnDisable()
        {
            Hide();
        }

        public bool RevealUntil(float until)
        {
            if (!CanScan)
            {
                return false;
            }

            EnsureFeedbackView();
            if (feedbackRoot == null || feedbackLabel == null)
            {
                return false;
            }

            visibleUntil = until;
            feedbackLabel.text = BuildLabel();
            feedbackRoot.SetActive(true);
            GetComponent<MinimapTrackable2D>()?.Discover();
            return true;
        }

        private string BuildLabel()
        {
            string title = string.IsNullOrWhiteSpace(shortLabel)
                ? ScanLogic.CategoryLabel(category)
                : shortLabel.Trim();
            string status = ResolveStateText();
            return string.IsNullOrWhiteSpace(status)
                ? $"[{title}]"
                : $"[{title}]\n{status.Trim()}";
        }

        private string ResolveStateText()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] != this && behaviours[index] is IScanStateProvider provider)
                {
                    return provider.ScanStateText;
                }
            }

            return fallbackState;
        }

        private void EnsureFeedbackView()
        {
            if (feedbackRoot != null)
            {
                return;
            }

            Transform existing = transform.Find("ScanFeedback");
            feedbackRoot = existing != null
                ? existing.gameObject
                : new GameObject("ScanFeedback");
            feedbackRoot.transform.SetParent(transform, false);

            feedbackLabel = feedbackRoot.GetComponent<TextMeshPro>();
            if (feedbackLabel == null)
            {
                feedbackLabel = feedbackRoot.AddComponent<TextMeshPro>();
            }

            feedbackRoot.transform.localPosition = new Vector3(0f, 1.05f, -0.2f);
            feedbackLabel.font = PresentationTheme.Font;
            feedbackLabel.alignment = TextAlignmentOptions.Bottom;
            feedbackLabel.fontSize = 1.45f;
            feedbackLabel.rectTransform.sizeDelta = new Vector2(4.2f, 1.6f);
            feedbackLabel.textWrappingMode = TextWrappingModes.NoWrap;
            feedbackLabel.color = new Color(0.2f, 0.95f, 1f, 1f);
            MeshRenderer textRenderer = feedbackRoot.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingOrder = 80;
            }

            SpriteRenderer source = GetComponent<SpriteRenderer>();
            if (source != null && feedbackRoot.transform.Find("Halo") == null)
            {
                GameObject haloObject = new GameObject("Halo");
                haloObject.transform.SetParent(feedbackRoot.transform, false);
                haloObject.transform.localPosition = new Vector3(0f, -1.05f, 0.1f);
                haloObject.transform.localScale = new Vector3(1.16f, 1.16f, 1f);
                SpriteRenderer halo = haloObject.AddComponent<SpriteRenderer>();
                halo.sprite = source.sprite;
                halo.drawMode = source.drawMode;
                halo.size = source.size;
                halo.color = new Color(0.15f, 0.95f, 1f, 0.28f);
                halo.sortingLayerID = source.sortingLayerID;
                halo.sortingOrder = source.sortingOrder + 1;
            }
        }

        private void Hide()
        {
            feedbackRoot?.SetActive(false);
        }
    }
}
