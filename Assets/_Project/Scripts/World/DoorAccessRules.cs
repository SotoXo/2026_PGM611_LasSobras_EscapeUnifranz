using EscapeUNIFRANZ.Core;

namespace EscapeUNIFRANZ.World
{
    public enum DoorLockState { Unlocked, Locked, RequiresKey, RequiresCard, RequiresPuzzle, DisabledByAI }
    public static class DoorAccessRules
    {
        public static bool CanOpen(GameState state, GameFlagId[] flags, string item)
        {
            if (!string.IsNullOrWhiteSpace(item) && (state == null || !state.HasItem(item))) return false;
            if (flags != null)
                foreach (var flag in flags)
                    if (flag != GameFlagId.None && (state == null || !state.HasFlag(flag))) return false;
            return true;
        }
    }
}
