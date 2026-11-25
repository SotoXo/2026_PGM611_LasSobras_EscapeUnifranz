using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Narrative;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    public sealed class GameOptionsView : MonoBehaviour
    {
        public Slider[] volumeSliders;
        public TMP_Text[] volumeLabels;
        public Button flashes, vsync, frameLimit;
        public TMP_Text flashLabel, vsyncLabel, frameLimitLabel;
        public Slider brightness;
        public Button mute, defaults;
        public TMP_Text brightnessLabel, muteLabel, confirmation;

        private static readonly string[] BusNames = { "General", "Música", "Ambiente", "Efectos", "Interfaz", "Voces" };
        private UnityAction<float>[] listeners;

        private void Awake()
        {
            if (GetComponent<SafeAreaPanel>() == null) gameObject.AddComponent<SafeAreaPanel>();
            PresentationTheme.StylePanel(GetComponent<Image>());
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
                PresentationTheme.StyleText(label);
        }

        private void OnEnable()
        {
            listeners ??= new UnityAction<float>[volumeSliders.Length];
            for (int i = 0; i < volumeSliders.Length; i++)
            {
                int index = i;
                listeners[i] ??= value =>
                {
                    GameOptions.SetVolume((VolumeBus)index, value);
                    SetVolumeLabel(index);
                };
                volumeSliders[i].onValueChanged.AddListener(listeners[i]);
            }

            flashes.onClick.AddListener(ToggleFlashes);
            vsync.onClick.AddListener(ToggleVSync);
            frameLimit.onClick.AddListener(CycleFrameLimit);
            vsync.gameObject.SetActive(!Application.isMobilePlatform);
            if (brightness != null) brightness.onValueChanged.AddListener(ChangeBrightness);
            if (mute != null) mute.onClick.AddListener(ToggleMute);
            if (defaults != null) defaults.onClick.AddListener(RestoreDefaults);
            if (confirmation != null) confirmation.text = string.Empty;
            Refresh();
        }

        private void OnDisable()
        {
            if (listeners == null) return;
            for (int i = 0; i < volumeSliders.Length; i++)
                volumeSliders[i].onValueChanged.RemoveListener(listeners[i]);
            flashes.onClick.RemoveListener(ToggleFlashes);
            vsync.onClick.RemoveListener(ToggleVSync);
            frameLimit.onClick.RemoveListener(CycleFrameLimit);
            if (brightness != null) brightness.onValueChanged.RemoveListener(ChangeBrightness);
            if (mute != null) mute.onClick.RemoveListener(ToggleMute);
            if (defaults != null) defaults.onClick.RemoveListener(RestoreDefaults);
            GameOptions.Save();
        }

        private void ChangeBrightness(float value)
        {
            GameOptions.SetBrightness(value);
            RefreshGraphics();
        }

        private void ToggleMute()
        {
            GameOptions.SetMuted(!GameOptions.Muted);
            RefreshGraphics();
        }

        private void RestoreDefaults()
        {
            GameOptions.RestoreDefaults();
            Refresh();
            if (confirmation != null) confirmation.text = "Valores predeterminados restaurados";
        }

        private void Refresh()
        {
            for (int i = 0; i < volumeSliders.Length; i++)
            {
                volumeSliders[i].SetValueWithoutNotify(GameOptions.GetVolume((VolumeBus)i));
                SetVolumeLabel(i);
            }
            if (brightness != null) brightness.SetValueWithoutNotify(GameOptions.Brightness);
            RefreshGraphics();
        }

        private void ToggleFlashes()
        {
            GameOptions.SetReduceFlashes(!GameOptions.ReduceFlashes);
            ProjectAudioHooks.Emit(AudioCue.MenuFocus);
            RefreshGraphics();
        }

        private void ToggleVSync()
        {
            GameOptions.ToggleVSync();
            RefreshGraphics();
        }

        private void CycleFrameLimit()
        {
            GameOptions.SelectNextFrameLimit();
            RefreshGraphics();
        }

        private void SetVolumeLabel(int index) =>
            volumeLabels[index].text = $"{BusNames[index]}   {Mathf.RoundToInt(GameOptions.GetVolume((VolumeBus)index) * 100f)} %";

        private void RefreshGraphics()
        {
            if (muteLabel != null) muteLabel.text = GameOptions.Muted ? "Sonido silenciado" : "Sonido activo";
            if (brightnessLabel != null) brightnessLabel.text = $"Brillo   {Mathf.RoundToInt(GameOptions.Brightness * 100f)} %";
            flashLabel.text = GameOptions.ReduceFlashes ? "Destellos reducidos" : "Destellos normales";
            vsyncLabel.text = GameOptions.VSync ? "VSync activado" : "VSync desactivado";
            bool mobile = Application.isMobilePlatform;
            frameLimit.interactable = mobile || !GameOptions.VSync;
            frameLimitLabel.text = mobile ? (GameOptions.FrameLimit == 30 ? "30 FPS" : "60 FPS") :
                GameOptions.VSync ? "Frecuencia de pantalla" :
                    GameOptions.FrameLimit < 0 ? "Sin límite de FPS" : GameOptions.FrameLimit + " FPS";
        }
    }
}
