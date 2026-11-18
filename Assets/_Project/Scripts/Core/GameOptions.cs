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
        public static bool ReduceFlashes { get; private set; }
        public static bool VSync { get; private set; }
        public static int FrameLimit { get; private set; }
        public static float MasterVolume => GetVolume(VolumeBus.Master);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            mixer = null;
            for (int i = 0; i < gains.Length; i++)
            {
                var bus = (VolumeBus)i;
                gains[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey(bus), bus == VolumeBus.Master ? .8f : 1f));
            }
            ReduceFlashes = PlayerPrefs.GetInt(Prefix + "ReduceFlashes", 0) == 1;
            VSync = PlayerPrefs.GetInt(Prefix + "VSync", 1) == 1;
            FrameLimit = PlayerPrefs.GetInt(Prefix + "FrameLimit", 60);
            if (FrameLimit != 60 && FrameLimit != 120 && FrameLimit != -1) FrameLimit = 60;
            ApplyFrameSettings();
        }

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
            AudioListener.volume = target != null ? 1f : MasterVolume;
            for (int i = 0; i < gains.Length; i++) ApplyVolume((VolumeBus)i);
        }

        private static string VolumeKey(VolumeBus bus) => Prefix + Parameter(bus);

        public static string Parameter(VolumeBus bus) => bus + "Volume";

        public static float VolumeInDecibels(float gain) =>
            gain <= .0001f ? -80f : Mathf.Clamp(20f * Mathf.Log10(Mathf.Clamp01(gain)), -80f, 0f);

        private static void ApplyVolume(VolumeBus bus)
        {
            if (mixer != null) mixer.SetFloat(Parameter(bus), VolumeInDecibels(GetVolume(bus)));
            else if (bus == VolumeBus.Master) AudioListener.volume = MasterVolume;
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
            FrameLimit = FrameLimit == 60 ? 120 : FrameLimit == 120 ? -1 : 60;
            PlayerPrefs.SetInt(Prefix + "FrameLimit", FrameLimit);
            ApplyFrameSettings();
        }

        private static void ApplyFrameSettings()
        {
            if (Application.isMobilePlatform) return;
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : FrameLimit;
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
