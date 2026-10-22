using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

namespace EscapeUNIFRANZ.UI
{
    /// <summary>One finger owns the stick while other fingers can press action buttons.</summary>
    public sealed class MobileJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const int NoPointer = int.MinValue;
        private const float DeadZone = 0.12f;
        private string inputPath = "<Gamepad>/leftStick";
        private RectTransform handle;
        private float travel = 70f;
        private int pointer = NoPointer;

        public Vector2 Value { get; private set; }
        protected override string controlPathInternal { get => inputPath; set => inputPath = value; }

        public void Configure(RectTransform thumb, float movementRange)
        {
            handle = thumb;
            travel = Mathf.Max(1f, movementRange);
            ResetInput();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null || pointer != NoPointer) return;
            pointer = eventData.pointerId;
            UpdateInput(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData != null && eventData.pointerId == pointer) UpdateInput(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData != null && eventData.pointerId == pointer) ResetInput();
        }

        private void UpdateInput(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,
                eventData.position, eventData.pressEventCamera, out Vector2 localPoint)) return;
            Vector2 offset = Vector2.ClampMagnitude(localPoint, travel);
            if (handle != null) handle.anchoredPosition = offset;
            float magnitude = offset.magnitude / travel;
            Value = magnitude <= DeadZone ? Vector2.zero :
                offset.normalized * Mathf.InverseLerp(DeadZone, 1f, magnitude);
            SendValueToControl(Value);
        }

        private void ResetInput()
        {
            pointer = NoPointer;
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            SendValueToControl(Vector2.zero);
        }

        protected override void OnDisable()
        {
            ResetInput();
            base.OnDisable();
        }

        private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
        private void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
    }
}
