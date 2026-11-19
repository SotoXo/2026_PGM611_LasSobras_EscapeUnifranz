using System;
using System.IO;
using UnityEngine;

namespace EscapeUNIFRANZ.Core
{
    public static class CheckpointSave
    {
        private const int CurrentVersion = 1;
        private static string PlayerPath => Path.Combine(Application.persistentDataPath, "campaign.json");

        [Serializable]
        private sealed class SaveData
        {
            public int version;
            public GameState state;
        }

        public static bool TryLoad(out GameState state) => TryLoad(PlayerPath, out state);

        public static bool TryLoad(string path, out GameState state)
        {
            return TryRead(path, out state) || TryRead(path + ".bak", out state);
        }

        public static bool TryWrite(GameState state) => TryWrite(PlayerPath, state);

        public static bool TryWrite(string path, GameState state)
        {
            if (state == null || !state.HasCheckpoint) return false;

            string pending = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                File.WriteAllText(pending, JsonUtility.ToJson(new SaveData
                {
                    version = CurrentVersion,
                    state = state
                }));
                if (File.Exists(path))
                    ReplaceCheckpoint(pending, path);
                else
                    File.Move(pending, path);
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogWarning("No se pudo guardar la partida: " + error.Message);
                return false;
            }
        }

        private static void ReplaceCheckpoint(string pending, string path)
        {
            string backup = path + ".bak";
            try
            {
                File.Replace(pending, path, backup);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(path, backup, true);
                File.Delete(path);
                File.Move(pending, path);
            }
        }

        private static bool TryRead(string path, out GameState state)
        {
            state = null;
            try
            {
                if (!File.Exists(path)) return false;
                var file = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (file == null || file.version != CurrentVersion ||
                    file.state == null || !file.state.HasValidSaveData)
                    return false;
                state = file.state;
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                                          error is ArgumentException)
            {
                return false;
            }
        }
    }
}
