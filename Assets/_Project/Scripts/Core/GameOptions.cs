using UnityEngine;

namespace EscapeUNIFRANZ.Core
{
    public static class GameOptions
    {
        private const string VolumeKey = "EscapeUNIFRANZ.MasterVolume";
        private const string FlashKey = "EscapeUNIFRANZ.ReduceFlashes";

        public static float MasterVolume { get; private set; }
        public static bool ReduceFlashes { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.8f));
            ReduceFlashes = PlayerPrefs.GetInt(FlashKey, 0) == 1;
            ApplyVolume();
        }

        public static void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
            ApplyVolume();
        }

        public static void SetReduceFlashes(bool value)
        {
            ReduceFlashes = value;
            PlayerPrefs.SetInt(FlashKey, value ? 1 : 0);
        }

        public static void ApplyVolume() => AudioListener.volume = MasterVolume;

        public static void Save() => PlayerPrefs.Save();
    }
}
