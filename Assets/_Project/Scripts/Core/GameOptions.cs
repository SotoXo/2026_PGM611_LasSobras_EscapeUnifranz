using System;
using UnityEngine;
using UnityEngine.Audio;

namespace EscapeUNIFRANZ.Core
{
    public enum VolumeBus { Master, Music, Ambience, SFX, UI, Voices }

    public static class GameOptions
    {
        private const string Prefix = "EscapeUNIFRANZ.";
        private static readonly float[] gains = new float[6];
        private static AudioMixer mixer;
        public const float DefaultBrightness = .5f;
        public static float Brightness { get; private set; } = DefaultBrightness;
        public static bool Muted { get; private set; }
        public static event Action BrightnessChanged;
        public static bool ReduceFlashes { get; private set; }
        public static bool VSync { get; private set; }
        public static int FrameLimit { get; private set; }
        public static float MasterVolume => GetVolume(VolumeBus.Master);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            mixer = null;
            ReadPreferences();
            ApplyFrameSettings();
        }

        private static void ReadPreferences()
        {
            for (int i = 0; i < gains.Length; i++)
            {
                var bus = (VolumeBus)i;
                gains[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey(bus), DefaultVolume(bus)));
            }
            Muted = ReadToggle("Muted", false);
            Brightness = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Brightness", DefaultBrightness));
            ReduceFlashes = PlayerPrefs.GetInt(Prefix + "ReduceFlashes", 0) == 1;
            VSync = PlayerPrefs.GetInt(Prefix + "VSync", 1) == 1;
            FrameLimit = PlayerPrefs.GetInt(Prefix + "FrameLimit", 60);
            if (FrameLimit != 30 && FrameLimit != 60 && FrameLimit != 120 && FrameLimit != -1) FrameLimit = 60;
        }

        private static float DefaultVolume(VolumeBus bus) => bus == VolumeBus.Master ? .8f : 1f;
        private static bool ReadToggle(string key, bool fallback) =>
            PlayerPrefs.GetInt(Prefix + key, fallback ? 1 : 0) == 1;

        public static float GetVolume(VolumeBus bus) => gains[(int)bus];

        public static void SetVolume(VolumeBus bus, float value)
        {
            gains[(int)bus] = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey(bus), gains[(int)bus]);
            ApplyVolume(bus);
        }

        public static void BindMixer(AudioMixer target)
        {
            mixer = target;
            for (int i = 0; i < gains.Length; i++) ApplyVolume((VolumeBus)i);
        }

        private static string VolumeKey(VolumeBus bus) => Prefix + Parameter(bus);

        public static string Parameter(VolumeBus bus) => bus + "Volume";

        public static float VolumeInDecibels(float gain) =>
            gain <= .0001f ? -80f : Mathf.Clamp(20f * Mathf.Log10(Mathf.Clamp01(gain)), -80f, 0f);

        private static void ApplyVolume(VolumeBus bus)
        {
            float volume = bus == VolumeBus.Master && Muted ? 0f : GetVolume(bus);
            if (mixer != null) mixer.SetFloat(Parameter(bus), VolumeInDecibels(volume));
            if (bus == VolumeBus.Master)
                AudioListener.volume = mixer == null ? volume : Muted ? 0f : 1f;
        }

        public static void SetMuted(bool value)
        {
            Muted = value;
            PlayerPrefs.SetInt(Prefix + "Muted", value ? 1 : 0);
            ApplyVolume(VolumeBus.Master);
        }

        public static void SetBrightness(float value)
        {
            Brightness = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(Prefix + "Brightness", Brightness);
            BrightnessChanged?.Invoke();
        }

        public static float AmbientIntensity(float authoredIntensity, float brightness)
        {
            brightness = Mathf.Clamp01(brightness);
            return brightness < DefaultBrightness
                ? authoredIntensity * Mathf.Lerp(.55f, 1f, brightness * 2f)
                : authoredIntensity + Mathf.Clamp((1f - authoredIntensity) * .3f, 0f, .16f) *
                    (brightness - DefaultBrightness) * 2f;
        }

        public static void SetReduceFlashes(bool value)
        {
            ReduceFlashes = value;
            PlayerPrefs.SetInt(Prefix + "ReduceFlashes", value ? 1 : 0);
        }

        public static void ToggleVSync()
        {
            VSync = !VSync;
            PlayerPrefs.SetInt(Prefix + "VSync", VSync ? 1 : 0);
            ApplyFrameSettings();
        }

        public static void SelectNextFrameLimit()
        {
            FrameLimit = NextFrameLimit(FrameLimit, Application.isMobilePlatform);
            PlayerPrefs.SetInt(Prefix + "FrameLimit", FrameLimit);
            ApplyFrameSettings();
        }

        public static int NextFrameLimit(int current, bool mobile) => current == 30 ? 60 :
            mobile ? 30 : current == 60 ? 120 : current == 120 ? -1 : 30;

        private static void ApplyFrameSettings()
        {
            if (Application.isMobilePlatform)
            {
                Application.targetFrameRate = FrameLimit == 30 ? 30 : 60;
                return;
            }
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : FrameLimit;
        }

        public static void RestoreDefaults()
        {
            for (int i = 0; i < gains.Length; i++)
                SetVolume((VolumeBus)i, DefaultVolume((VolumeBus)i));
            SetMuted(false);
            SetBrightness(DefaultBrightness);
            SetReduceFlashes(false);
            VSync = true;
            FrameLimit = 60;
            PlayerPrefs.SetInt(Prefix + "VSync", 1);
            PlayerPrefs.SetInt(Prefix + "FrameLimit", FrameLimit);
            ApplyFrameSettings();
            Save();
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
