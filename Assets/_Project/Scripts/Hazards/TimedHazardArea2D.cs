using System;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.UI;
using TMPro;
using UnityEngine;

namespace EscapeUNIFRANZ.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class TimedHazardArea2D : MonoBehaviour, IEncounterResettable, IScanStateProvider
    {
        [SerializeField] private SpriteRenderer indicator;
        [SerializeField] private TMP_Text stateLabel;

        private Collider2D trigger;
        private float activeStartsAt;
        private float activeEndsAt;

        public HazardWindowState State { get; private set; } = HazardWindowState.Safe;
        public bool IsActive { get; private set; }
        public string ScanStateText => State == HazardWindowState.Active
            ? "[ACTIVO]"
            : State == HazardWindowState.Telegraph
                ? "[PREPARANDO]"
                : "[EN ESPERA]";
        public event Action<HazardWindowState> StateChanged;

        private void Awake()
        {
            trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            EnsureStateLabel();
            SetHazardActive(false);
        }

        private void Update()
        {
            if (State == HazardWindowState.Telegraph && Time.time >= activeStartsAt)
            {
                SetState(HazardWindowState.Active);
            }

            if (State == HazardWindowState.Active && activeEndsAt > 0f && Time.time >= activeEndsAt)
            {
                SetState(HazardWindowState.Safe);
            }

            if (State == HazardWindowState.Telegraph && indicator != null)
            {
                Color color = indicator.color;
                color.a = Mathf.Lerp(0.28f, 0.72f, Mathf.PingPong(Time.time * 4f, 1f));
                indicator.color = color;
            }
        }

        public void BeginTelegraph(float warningSeconds, float dangerSeconds)
        {
            float warning = Mathf.Max(0f, warningSeconds);
            float danger = Mathf.Max(0f, dangerSeconds);
            activeStartsAt = Time.time + warning;
            activeEndsAt = activeStartsAt + danger;
            SetState(warning > 0f ? HazardWindowState.Telegraph : HazardWindowState.Active);
        }

        public void SetHazardActive(bool active)
        {
            activeStartsAt = 0f;
            activeEndsAt = 0f;
            SetState(active ? HazardWindowState.Active : HazardWindowState.Safe);
        }

        private void SetState(HazardWindowState state)
        {
            State = state;
            IsActive = state == HazardWindowState.Active;
            if (trigger != null)
            {
                trigger.enabled = state != HazardWindowState.Safe;
            }

            EnsureStateLabel();
            if (stateLabel != null)
            {
                switch (state)
                {
                    case HazardWindowState.Telegraph:
                        stateLabel.text = "! AVISO";
                        stateLabel.gameObject.SetActive(true);
                        break;
                    case HazardWindowState.Active:
                        stateLabel.text = "× ACTIVO";
                        stateLabel.gameObject.SetActive(true);
                        break;
                    default:
                        stateLabel.text = string.Empty;
                        stateLabel.gameObject.SetActive(false);
                        break;
                }
            }

            if (indicator != null)
            {
                switch (state)
                {
                    case HazardWindowState.Telegraph:
                        indicator.color = new Color(1f, 0.75f, 0.05f, 0.5f);
                        break;
                    case HazardWindowState.Active:
                        indicator.color = new Color(1f, 0.05f, 0.05f, 0.8f);
                        break;
                    default:
                        indicator.color = new Color(1f, 0.65f, 0.1f, 0.12f);
                        break;
                }
            }

            StateChanged?.Invoke(state);
        }

        private void EnsureStateLabel()
        {
            if (stateLabel != null)
            {
                return;
            }

            Transform child = transform.Find("HazardStateLabel");
            GameObject labelObject = child != null
                ? child.gameObject
                : new GameObject("HazardStateLabel");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            stateLabel = labelObject.GetComponent<TextMeshPro>();
            if (stateLabel == null)
            {
                stateLabel = labelObject.AddComponent<TextMeshPro>();
            }

            stateLabel.font = PresentationTheme.Font;
            stateLabel.alignment = TextAlignmentOptions.Center;
            stateLabel.fontSize = 1.55f;
            stateLabel.fontStyle = FontStyles.Bold;
            stateLabel.rectTransform.sizeDelta = new Vector2(3.8f, 1.1f);
            stateLabel.color = Color.white;
            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 70;
            }
        }

        public void ResetEncounter()
        {
            SetHazardActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            Collider2D area = trigger != null ? trigger : GetComponent<Collider2D>();
            Gizmos.color = State == HazardWindowState.Active
                ? new Color(1f, 0.05f, 0.05f, 0.9f)
                : new Color(1f, 0.75f, 0.05f, 0.9f);
            Gizmos.DrawWireCube(area.bounds.center, area.bounds.size);
        }
    }

    public enum HazardWindowState
    {
        Safe = 0,
        Telegraph = 1,
        Active = 2
    }
}
