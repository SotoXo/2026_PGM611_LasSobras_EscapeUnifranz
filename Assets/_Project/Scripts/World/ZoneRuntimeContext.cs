using EscapeUNIFRANZ.Core;

namespace EscapeUNIFRANZ.World
{
    /// <summary>
    /// Explicit dependencies delivered once to objects owned by a loaded zone.
    /// </summary>
    public readonly struct ZoneRuntimeContext
    {
        public ZoneRuntimeContext(
            GameSessionController session,
            SceneFlowController sceneFlow,
            GameplayModeController gameplayMode,
            ZoneContext zone)
        {
            Session = session;
            SceneFlow = sceneFlow;
            GameplayMode = gameplayMode;
            Zone = zone;
        }

        public GameSessionController Session { get; }
        public SceneFlowController SceneFlow { get; }
        public GameplayModeController GameplayMode { get; }
        public ZoneContext Zone { get; }
    }

    public interface IZoneRuntimeInitializable
    {
        void Initialize(ZoneRuntimeContext context);
    }
}
