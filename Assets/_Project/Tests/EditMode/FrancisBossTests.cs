using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class FrancisBossTests
    {
        [Test]
        public void Intro_ToPhaseOne()
        {
            var machine = new FrancisBossStateMachine();

            Assert.That(machine.BeginNetworkPhase(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(FrancisBossState.PhaseNetwork));
        }

        [Test]
        public void CorrectSequences_AdvanceAllPhases()
        {
            var machine = new FrancisBossStateMachine();
            machine.BeginNetworkPhase();

            machine.SubmitNetwork(FrancisNetworkStep.Cameras);
            machine.SubmitNetwork(FrancisNetworkStep.Access);
            machine.SubmitNetwork(FrancisNetworkStep.Server);
            Assert.That(machine.Current, Is.EqualTo(FrancisBossState.PhaseHardware));

            machine.SubmitHardware(FrancisHardwareStep.Power);
            machine.SubmitHardware(FrancisHardwareStep.Actuators);
            machine.SubmitHardware(FrancisHardwareStep.Safety);
            Assert.That(machine.Current, Is.EqualTo(FrancisBossState.PhaseShutdown));

            machine.SubmitShutdown(FrancisShutdownStep.FinalConnection);
            machine.SubmitShutdown(FrancisShutdownStep.CutActuators);
            machine.SubmitShutdown(FrancisShutdownStep.Shutdown);
            Assert.That(machine.Current, Is.EqualTo(FrancisBossState.Completed));
        }

        [Test]
        public void IncorrectNetworkSequence_DoesNotCompletePhase()
        {
            var machine = new FrancisBossStateMachine();
            machine.BeginNetworkPhase();
            machine.SubmitNetwork(FrancisNetworkStep.Cameras);

            SequenceSubmissionResult result = machine.SubmitNetwork(FrancisNetworkStep.Server);

            Assert.That(result, Is.EqualTo(SequenceSubmissionResult.Incorrect));
            Assert.That(machine.Current, Is.EqualTo(FrancisBossState.PhaseNetwork));
            Assert.That(machine.CurrentProgress, Is.Zero);
        }

        [Test]
        public void CompletedPhase_DoesNotRegressOnReset()
        {
            var machine = new FrancisBossStateMachine();
            machine.BeginNetworkPhase();
            machine.SubmitNetwork(FrancisNetworkStep.Cameras);
            machine.SubmitNetwork(FrancisNetworkStep.Access);
            machine.SubmitNetwork(FrancisNetworkStep.Server);

            machine.ResetCurrentPhase();

            Assert.That(machine.Current, Is.EqualTo(FrancisBossState.PhaseHardware));
        }

        [Test]
        public void GameCompleted_IsMarkedOnlyForFinalPhase()
        {
            var state = new GameState();

            FrancisProgression.ApplyCompletedPhase(state, FrancisBossState.PhaseNetwork);
            FrancisProgression.ApplyCompletedPhase(state, FrancisBossState.PhaseHardware);
            Assert.That(state.HasFlag(GameFlagId.GameCompleted), Is.False);

            FrancisProgression.ApplyCompletedPhase(state, FrancisBossState.PhaseShutdown);
            Assert.That(state.HasFlag(GameFlagId.GameCompleted), Is.True);
        }

        [Test]
        public void PhaseReset_DoesNotErasePreviousGlobalProgress()
        {
            var state = new GameState();
            FrancisProgression.ApplyCompletedPhase(state, FrancisBossState.PhaseNetwork);
            var machine = new FrancisBossStateMachine();
            machine.Restore(networkComplete: true, hardwareComplete: false, shutdownComplete: false);

            machine.SubmitHardware(FrancisHardwareStep.Safety);

            Assert.That(state.HasFlag(GameFlagId.FrancisNetworkPhaseComplete), Is.True);
            Assert.That(machine.Current, Is.EqualTo(FrancisBossState.PhaseHardware));
        }

        [Test]
        public void FailureInHardware_KeepsNetworkPhaseCompleted()
        {
            var state = new GameState();
            FrancisProgression.ApplyCompletedPhase(state, FrancisBossState.PhaseNetwork);
            var machine = new FrancisBossStateMachine();
            machine.Restore(networkComplete: true, hardwareComplete: false, shutdownComplete: false);

            machine.SubmitHardware(FrancisHardwareStep.Power);
            machine.ResetCurrentPhase();

            Assert.That(state.HasFlag(GameFlagId.FrancisNetworkPhaseComplete), Is.True);
            Assert.That(machine.Current, Is.EqualTo(FrancisBossState.PhaseHardware));
            Assert.That(machine.CurrentProgress, Is.Zero);
        }

        [Test]
        public void FailureInShutdown_KeepsNetworkAndHardwareCompleted()
        {
            var state = new GameState();
            FrancisProgression.ApplyCompletedPhase(state, FrancisBossState.PhaseNetwork);
            FrancisProgression.ApplyCompletedPhase(state, FrancisBossState.PhaseHardware);
            var machine = new FrancisBossStateMachine();
            machine.Restore(networkComplete: true, hardwareComplete: true, shutdownComplete: false);

            machine.SubmitShutdown(FrancisShutdownStep.FinalConnection);
            machine.ResetCurrentPhase();

            Assert.That(state.HasFlag(GameFlagId.FrancisNetworkPhaseComplete), Is.True);
            Assert.That(state.HasFlag(GameFlagId.FrancisHardwarePhaseComplete), Is.True);
            Assert.That(machine.Current, Is.EqualTo(FrancisBossState.PhaseShutdown));
            Assert.That(machine.CurrentProgress, Is.Zero);
        }
    }
}
