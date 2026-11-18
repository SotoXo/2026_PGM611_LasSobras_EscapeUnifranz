using EscapeUNIFRANZ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    public sealed class GameOptionsView : MonoBehaviour
    {
        public Slider volume;
        public Button flashes;
        public TMP_Text flashLabel;
        public TMP_Text volumeLabel;

        private void OnEnable()
        {
            volume.SetValueWithoutNotify(GameOptions.MasterVolume);
            volume.onValueChanged.AddListener(SetVolume);
            flashes.onClick.AddListener(ToggleFlashes);
            Refresh();
        }

        private void OnDisable()
        {
            volume.onValueChanged.RemoveListener(SetVolume);
            flashes.onClick.RemoveListener(ToggleFlashes);
            GameOptions.Save();
        }

        private void SetVolume(float value)
        {
            GameOptions.SetMasterVolume(value);
            ProjectPlayFeedback();
            Refresh();
        }

        private void ToggleFlashes()
        {
            GameOptions.SetReduceFlashes(!GameOptions.ReduceFlashes);
            ProjectPlayFeedback();
            Refresh();
        }

        private static void ProjectPlayFeedback() =>
            Narrative.ProjectAudioHooks.Emit(Narrative.AudioCue.Button);

        private void Refresh()
        {
            volumeLabel.text = $"VOLUMEN GENERAL  {GameOptions.MasterVolume:P0}";
            flashLabel.text = GameOptions.ReduceFlashes ? "REDUCIR DESTELLOS   SÍ" : "REDUCIR DESTELLOS   NO";
        }
    }
}
