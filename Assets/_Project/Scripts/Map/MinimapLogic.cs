using EscapeUNIFRANZ.Core;
using UnityEngine;

namespace EscapeUNIFRANZ.Map
{
    public enum MinimapMarkerKind
    {
        Exit = 0,
        Objective = 1,
        Checkpoint = 2,
        Threat = 3
    }

    public enum MinimapExitState
    {
        Unknown = 0,
        Discovered = 1,
        Available = 2,
        Blocked = 3
    }

    public static class MinimapLogic
    {
        public static Vector2 NormalizePosition(Vector2 worldPosition, Rect worldBounds)
        {
            if (worldBounds.width <= 0f || worldBounds.height <= 0f)
            {
                return new Vector2(0.5f, 0.5f);
            }

            return new Vector2(
                Mathf.Clamp01((worldPosition.x - worldBounds.xMin) / worldBounds.width),
                Mathf.Clamp01((worldPosition.y - worldBounds.yMin) / worldBounds.height));
        }

        public static MinimapExitState ResolveExitState(
            GameState state,
            GameFlagId requiredFlag,
            bool discovered,
            bool secret)
        {
            if (!discovered || secret)
            {
                return MinimapExitState.Unknown;
            }

            if (requiredFlag == GameFlagId.None)
            {
                return MinimapExitState.Available;
            }

            return state != null && state.HasFlag(requiredFlag)
                ? MinimapExitState.Available
                : MinimapExitState.Blocked;
        }

        public static bool IsCurrentCheckpoint(
            GameState state,
            string zoneId,
            string checkpointId)
        {
            return state != null && state.HasCheckpoint &&
                state.CheckpointZoneId == zoneId &&
                state.CheckpointSpawnId == checkpointId;
        }

        public static bool IsCurrentZone(GameState state, string zoneId)
        {
            return state != null && !string.IsNullOrWhiteSpace(zoneId) &&
                state.CurrentZoneId == zoneId;
        }
    }
}
