using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEngine;

namespace EscapeUNIFRANZ.Hazards
{
    public enum ThreatAwarenessState
    {
        Patrol = 0,
        Suspicious = 1,
        Alert = 2,
        Chase = 3,
        Disabled = 4
    }

    public static class ThreatAlertIndicatorLogic
    {
        public static string SymbolFor(ThreatAwarenessState state)
        {
            switch (state)
            {
                case ThreatAwarenessState.Suspicious: return "?";
                case ThreatAwarenessState.Alert: return "!";
                case ThreatAwarenessState.Chase: return "!!";
                default: return string.Empty;
            }
        }
    }

    public sealed class ThreatAlertIndicator2D : MonoBehaviour, IZoneRuntimeInitializable
    {
        [SerializeField] private DetectionSensor2D sensor;
        [SerializeField, Min(0f)] private float suspiciousSeconds = 1.5f;
        [SerializeField] private Vector2 localOffset = new Vector2(0f, 1.1f);

        private TMP_Text label;
        private float suspiciousUntil;

        public ThreatAwarenessState State { get; private set; } = ThreatAwarenessState.Patrol;

        public void Initialize(ZoneRuntimeContext context)
        {
            EnsureView();
            if (sensor != null)
            {
                sensor.TargetDetected += OnDetected;
                sensor.TargetLost += OnLost;
            }

            SetState(ThreatAwarenessState.Patrol);
        }

        private void Update()
        {
            if (State == ThreatAwarenessState.Suspicious && Time.time >= suspiciousUntil)
            {
                SetState(ThreatAwarenessState.Patrol);
            }
        }

        private void OnDestroy()
        {
            if (sensor != null)
            {
                sensor.TargetDetected -= OnDetected;
                sensor.TargetLost -= OnLost;
            }
        }

        private void OnDetected(Transform _)
        {
            GetComponent<MinimapTrackable2D>()?.Discover();
            SetState(ThreatAwarenessState.Alert);
        }

        private void OnLost()
        {
            suspiciousUntil = Time.time + suspiciousSeconds;
            SetState(ThreatAwarenessState.Suspicious);
        }

        public void SetState(ThreatAwarenessState state)
        {
            State = state;
            EnsureView();
            if (label == null)
            {
                return;
            }

            string symbol = ThreatAlertIndicatorLogic.SymbolFor(state);
            label.text = symbol;
            label.gameObject.SetActive(!string.IsNullOrEmpty(symbol));
        }

        public void SetDisabled()
        {
            SetState(ThreatAwarenessState.Disabled);
        }

        private void EnsureView()
        {
            if (label != null)
            {
                return;
            }

            Transform child = transform.Find("AlertIndicator");
            GameObject view = child != null ? child.gameObject : new GameObject("AlertIndicator");
            view.transform.SetParent(transform, false);
            view.transform.localPosition = new Vector3(localOffset.x, localOffset.y, -0.25f);
            label = view.GetComponent<TextMeshPro>();
            if (label == null)
            {
                label = view.AddComponent<TextMeshPro>();
            }

            label.font = PresentationTheme.Font;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 2.6f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(1f, 0.84f, 0.15f, 1f);
            label.rectTransform.sizeDelta = new Vector2(2f, 1f);
            MeshRenderer renderer = view.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 100;
            }
        }
    }
}
