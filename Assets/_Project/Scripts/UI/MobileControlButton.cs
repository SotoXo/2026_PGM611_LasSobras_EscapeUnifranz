using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    /// <summary>Animated touch button with explicit pointer ownership and input release.</summary>
    public sealed class MobileControlButton : OnScreenControl, IPointerDownHandler, IPointerUpHandler
    {
        private const int NoPointer = int.MinValue;
        private string inputPath;
        private Image image;
        private Sprite[] frames;
        private Color normalColor;
        private bool interactable = true;
        private int pointer = NoPointer;
        private float pulseStarted = float.NegativeInfinity;

        public bool Interactable => interactable;
        protected override string controlPathInternal { get => inputPath; set => inputPath = value; }

        public void Configure(Image targetImage, Texture2D atlas, string key)
        {
            image = targetImage;
            normalColor = image.color;
            controlPath = "<Keyboard>/" + key;
            if (atlas != null)
            {
                frames = new Sprite[9];
                for (int index = 0; index < frames.Length; index++)
                {
                    var cell = new Rect((index % 4) * 64, atlas.height - (index / 4 + 1) * 64, 64, 64);
                    frames[index] = Sprite.Create(atlas, cell, new Vector2(0.5f, 0.5f), 64f, 0,
                        SpriteMeshType.FullRect);
                    frames[index].name = name + "_" + index;
                }
            }
            Render();
        }

        public void SetInteractable(bool available)
        {
            if (interactable == available) return;
            interactable = available;
            if (!available) Release();
            Render();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable || eventData == null || pointer != NoPointer) return;
            pointer = eventData.pointerId;
            pulseStarted = Time.unscaledTime;
            SendValueToControl(1f);
            Render();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData != null && eventData.pointerId == pointer) Release();
        }

        private void Update() => Render();

        private void Render()
        {
            if (image == null) return;
            if (frames == null)
            {
                image.color = !interactable ? normalColor * new Color(1, 1, 1, 0.35f) :
                    pointer != NoPointer ? new Color(0.65f, 0.08f, 0.12f, 1f) : normalColor;
                return;
            }
            float elapsed = Time.unscaledTime - pulseStarted;
            int frame;
            if (!interactable) frame = 8;
            else if (elapsed < 0.07f) frame = 4;
            else if (elapsed < 0.15f) frame = 5;
            else if (elapsed < 0.24f) frame = 6;
            else if (elapsed < 0.36f) frame = 7;
            else if (pointer != NoPointer) frame = 4;
            else frame = Mathf.FloorToInt(Time.unscaledTime / 0.18f) % 4;
            image.sprite = frames[frame];
        }

        private void Release()
        {
            pointer = NoPointer;
            SendValueToControl(0f);
        }

        protected override void OnDisable()
        {
            Release();
            pulseStarted = float.NegativeInfinity;
            base.OnDisable();
        }

        private void OnApplicationFocus(bool focused) { if (!focused) Release(); }
        private void OnApplicationPause(bool paused) { if (paused) Release(); }

        private void OnDestroy()
        {
            if (frames == null) return;
            foreach (Sprite frame in frames)
            {
                if (Application.isPlaying) Destroy(frame);
                else DestroyImmediate(frame);
            }
        }
    }
}
