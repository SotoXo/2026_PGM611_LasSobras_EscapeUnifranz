using EscapeUNIFRANZ.Core;

namespace EscapeUNIFRANZ.Encounters
{
    public enum FrancisBossState
    {
        Intro = 0,
        PhaseNetwork = 1,
        PhaseHardware = 2,
        PhaseShutdown = 3,
        Completed = 4
    }

    public enum FrancisNetworkStep
    {
        Cameras = 0,
        Access = 1,
        Server = 2
    }

    public enum FrancisHardwareStep
    {
        Power = 0,
        Actuators = 1,
        Safety = 2
    }

    public enum FrancisShutdownStep
    {
        FinalConnection = 0,
        CutActuators = 1,
        Shutdown = 2
    }

    public sealed class FrancisBossStateMachine
    {
        private readonly OrderedStepSequence<FrancisNetworkStep> network =
            new OrderedStepSequence<FrancisNetworkStep>(new[]
            {
                FrancisNetworkStep.Cameras,
                FrancisNetworkStep.Access,
                FrancisNetworkStep.Server
            });

        private readonly OrderedStepSequence<FrancisHardwareStep> hardware =
            new OrderedStepSequence<FrancisHardwareStep>(new[]
            {
                FrancisHardwareStep.Power,
                FrancisHardwareStep.Actuators,
                FrancisHardwareStep.Safety
            });

        private readonly OrderedStepSequence<FrancisShutdownStep> shutdown =
            new OrderedStepSequence<FrancisShutdownStep>(new[]
            {
                FrancisShutdownStep.FinalConnection,
                FrancisShutdownStep.CutActuators,
                FrancisShutdownStep.Shutdown
            });

        public FrancisBossState Current { get; private set; } = FrancisBossState.Intro;
        public int CurrentProgress
        {
            get
            {
                switch (Current)
                {
                    case FrancisBossState.PhaseNetwork: return network.Progress;
                    case FrancisBossState.PhaseHardware: return hardware.Progress;
                    case FrancisBossState.PhaseShutdown: return shutdown.Progress;
                    default: return 0;
                }
            }
        }

        public bool BeginNetworkPhase()
        {
            if (Current != FrancisBossState.Intro)
            {
                return false;
            }

            Current = FrancisBossState.PhaseNetwork;
            return true;
        }

        public SequenceSubmissionResult SubmitNetwork(FrancisNetworkStep step)
        {
            if (Current != FrancisBossState.PhaseNetwork)
            {
                return SequenceSubmissionResult.Incorrect;
            }

            SequenceSubmissionResult result = network.Submit(step, true);
            if (result == SequenceSubmissionResult.Completed)
            {
                Current = FrancisBossState.PhaseHardware;
            }

            return result;
        }

        public SequenceSubmissionResult SubmitHardware(FrancisHardwareStep step)
        {
            if (Current != FrancisBossState.PhaseHardware)
            {
                return SequenceSubmissionResult.Incorrect;
            }

            SequenceSubmissionResult result = hardware.Submit(step, true);
            if (result == SequenceSubmissionResult.Completed)
            {
                Current = FrancisBossState.PhaseShutdown;
            }

            return result;
        }

        public SequenceSubmissionResult SubmitShutdown(FrancisShutdownStep step)
        {
            if (Current != FrancisBossState.PhaseShutdown)
            {
                return SequenceSubmissionResult.Incorrect;
            }

            SequenceSubmissionResult result = shutdown.Submit(step, false);
            if (result == SequenceSubmissionResult.Completed)
            {
                Current = FrancisBossState.Completed;
            }

            return result;
        }

        public void ResetCurrentPhase()
        {
            switch (Current)
            {
                case FrancisBossState.PhaseNetwork:
                    network.Reset();
                    break;
                case FrancisBossState.PhaseHardware:
                    hardware.Reset();
                    break;
                case FrancisBossState.PhaseShutdown:
                    shutdown.Reset();
                    break;
            }
        }

        public void Restore(bool networkComplete, bool hardwareComplete, bool shutdownComplete)
        {
            if (shutdownComplete)
            {
                network.Complete();
                hardware.Complete();
                shutdown.Complete();
                Current = FrancisBossState.Completed;
            }
            else if (hardwareComplete)
            {
                network.Complete();
                hardware.Complete();
                Current = FrancisBossState.PhaseShutdown;
            }
            else if (networkComplete)
            {
                network.Complete();
                Current = FrancisBossState.PhaseHardware;
            }
        }
    }

    public static class FrancisProgression
    {
        public static bool ApplyCompletedPhase(GameState state, FrancisBossState completedPhase)
        {
            if (state == null)
            {
                return false;
            }

            switch (completedPhase)
            {
                case FrancisBossState.PhaseNetwork:
                    return state.SetFlag(GameFlagId.FrancisNetworkPhaseComplete);
                case FrancisBossState.PhaseHardware:
                    return state.SetFlag(GameFlagId.FrancisHardwarePhaseComplete);
                case FrancisBossState.PhaseShutdown:
                    bool changed = state.SetFlag(GameFlagId.FrancisShutdownPhaseComplete);
                    changed |= state.SetFlag(GameFlagId.GameCompleted);
                    return changed;
                default:
                    return false;
            }
        }
    }
}
