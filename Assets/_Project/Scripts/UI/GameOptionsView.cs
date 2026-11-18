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

        private static readonly string[] BusNames = { "General", "Música", "Ambiente", "Efectos", "Interfaz", "Voces" };
        private UnityAction<float>[] listeners;

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
                volumeSliders[i].SetValueWithoutNotify(GameOptions.GetVolume((VolumeBus)i));
                volumeSliders[i].onValueChanged.AddListener(listeners[i]);
                SetVolumeLabel(i);
            }

            flashes.onClick.AddListener(ToggleFlashes);
            vsync.onClick.AddListener(ToggleVSync);
            frameLimit.onClick.AddListener(CycleFrameLimit);
            vsync.gameObject.SetActive(!Application.isMobilePlatform);
            frameLimit.gameObject.SetActive(!Application.isMobilePlatform);
            RefreshGraphics();
        }

        private void OnDisable()
        {
            if (listeners == null) return;
            for (int i = 0; i < volumeSliders.Length; i++)
                volumeSliders[i].onValueChanged.RemoveListener(listeners[i]);
            flashes.onClick.RemoveListener(ToggleFlashes);
            vsync.onClick.RemoveListener(ToggleVSync);
            frameLimit.onClick.RemoveListener(CycleFrameLimit);
            GameOptions.Save();
        }

        private void ToggleFlashes()
        {
            GameOptions.SetReduceFlashes(!GameOptions.ReduceFlashes);
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
            if (isActiveAndEnabled) ProjectAudioHooks.Emit(AudioCue.MenuFocus);
            flashLabel.text = GameOptions.ReduceFlashes ? "Destellos reducidos" : "Destellos normales";
            vsyncLabel.text = GameOptions.VSync ? "VSync activado" : "VSync desactivado";
            frameLimit.interactable = !GameOptions.VSync;
            frameLimitLabel.text = GameOptions.VSync ? "Frecuencia de pantalla" :
                GameOptions.FrameLimit < 0 ? "Sin límite de FPS" : GameOptions.FrameLimit + " FPS";
        }
    }
}
