using System;

namespace EscapeUNIFRANZ.Core
{
    /// <summary>
    /// Pure mode state used by the runtime controller and EditMode tests.
    /// </summary>
    public sealed class GameplayModeState
    {
        public GameplayMode Current { get; private set; } = GameplayMode.Explore;
        public bool AllowsMovement =>
            Current == GameplayMode.Explore || Current == GameplayMode.Encounter;
        public bool AllowsInteraction =>
            Current == GameplayMode.Explore || Current == GameplayMode.Encounter;
        public bool AllowsMapToggle =>
            Current == GameplayMode.Explore || Current == GameplayMode.Map;
        public bool AllowsGameplay => AllowsMovement && AllowsInteraction;

        public event Action<GameplayMode> Changed;

        public bool SetMode(GameplayMode mode)
        {
            if (Current == mode)
            {
                return false;
            }

            Current = mode;
            Changed?.Invoke(Current);
            return true;
        }
    }
}
