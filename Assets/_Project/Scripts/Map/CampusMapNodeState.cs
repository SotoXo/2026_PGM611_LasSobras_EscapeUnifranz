using EscapeUNIFRANZ.Core;

namespace EscapeUNIFRANZ.Map
{
    public enum CampusMapNodeState
    {
        Unknown = 0,
        Discovered = 1,
        Current = 2,
        Locked = 3,
        Completed = 4
    }

    /// <summary>
    /// Pure projection from GameState to a map node. It never mutates the session.
    /// </summary>
    public static class CampusMapProgression
    {
        public static CampusMapNodeState Resolve(
            GameState state,
            CampusMapNodeDefinition node)
        {
            if (state == null || node == null)
            {
                return CampusMapNodeState.Unknown;
            }

            if (state.CurrentZoneId == node.ZoneId)
            {
                return CampusMapNodeState.Current;
            }

            if (node.RevealFlag != GameFlagId.None && !state.HasFlag(node.RevealFlag))
            {
                return CampusMapNodeState.Unknown;
            }

            if (node.UnlockFlag != GameFlagId.None && !state.HasFlag(node.UnlockFlag))
            {
                return CampusMapNodeState.Locked;
            }

            if (node.VisitedFlag != GameFlagId.None && state.HasFlag(node.VisitedFlag))
            {
                return CampusMapNodeState.Completed;
            }

            return CampusMapNodeState.Discovered;
        }
    }
}
