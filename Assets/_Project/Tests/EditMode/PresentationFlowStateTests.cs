using EscapeUNIFRANZ.Core;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class PresentationFlowStateTests
    {
        [Test]
        public void Launch_StartsAtMainMenuWithFrozenGameplayAndHiddenHud()
        {
            var flow = new PresentationFlowState();

            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.MainMenu));
            Assert.That(flow.ShouldFreezeTime, Is.True);
            Assert.That(flow.ShowsGameplayHud, Is.False);
        }

        [Test]
        public void NewGame_TraversesThreeIntroPanelsBeforeLoadingAndGameplay()
        {
            var flow = new PresentationFlowState(3);

            Assert.That(flow.StartNewGame(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Intro));
            Assert.That(flow.IntroPanelIndex, Is.Zero);

            flow.AdvanceIntro();
            Assert.That(flow.IntroPanelIndex, Is.EqualTo(1));
            flow.AdvanceIntro();
            Assert.That(flow.IntroPanelIndex, Is.EqualTo(2));
            flow.AdvanceIntro();

            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Loading));
            Assert.That(flow.ShouldFreezeTime, Is.True);
            Assert.That(flow.CompleteLoading(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Gameplay));
            Assert.That(flow.ShowsGameplayHud, Is.True);
        }

        [Test]
        public void Intro_CanBeSkippedDirectlyToLoading()
        {
            var flow = new PresentationFlowState();
            flow.StartNewGame();

            Assert.That(flow.SkipIntro(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Loading));
        }

        [Test]
        public void Pause_FreezesAndHidesHudUntilResume()
        {
            var flow = ReachGameplay();

            Assert.That(flow.Pause(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Pause));
            Assert.That(flow.ShouldFreezeTime, Is.True);
            Assert.That(flow.ShowsGameplayHud, Is.False);

            Assert.That(flow.Resume(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Gameplay));
            Assert.That(flow.ShouldFreezeTime, Is.False);
            Assert.That(flow.ShowsGameplayHud, Is.True);
        }

        [Test]
        public void PendingVictory_BlocksPauseAndDefeatInterruptions()
        {
            Assert.That(PresentationFlowState.CanPause(
                PresentationScreen.Gameplay,
                sceneTransitioning: false,
                victoryPending: true), Is.False);
            Assert.That(PresentationFlowState.CanShowDefeat(
                PresentationScreen.Gameplay,
                sceneTransitioning: false,
                victoryPending: true), Is.False);
        }

        [Test]
        public void GameplayWithoutTransition_AllowsPauseAndDefeat()
        {
            Assert.That(PresentationFlowState.CanPause(
                PresentationScreen.Gameplay,
                sceneTransitioning: false,
                victoryPending: false), Is.True);
            Assert.That(PresentationFlowState.CanShowDefeat(
                PresentationScreen.Gameplay,
                sceneTransitioning: false,
                victoryPending: false), Is.True);
        }

        [Test]
        public void EncounterMode_IsRestoredAfterPresentationOverlay()
        {
            Assert.That(
                PresentationFlowState.ResolveResumeMode(GameplayMode.Encounter),
                Is.EqualTo(GameplayMode.Encounter));
            Assert.That(
                PresentationFlowState.ResolveResumeMode(GameplayMode.Explore),
                Is.EqualTo(GameplayMode.Explore));
            Assert.That(
                PresentationFlowState.ResolveResumeMode(GameplayMode.Pause),
                Is.EqualTo(GameplayMode.Explore));
        }

        [Test]
        public void Controls_ReturnsToItsMainMenuOrPauseOrigin()
        {
            var menuFlow = new PresentationFlowState();
            menuFlow.OpenControls();
            Assert.That(menuFlow.CloseControls(), Is.True);
            Assert.That(menuFlow.Current, Is.EqualTo(PresentationScreen.MainMenu));

            var pauseFlow = ReachGameplay();
            pauseFlow.Pause();
            pauseFlow.OpenControls();
            Assert.That(pauseFlow.CloseControls(), Is.True);
            Assert.That(pauseFlow.Current, Is.EqualTo(PresentationScreen.Pause));
        }

        [Test]
        public void Defeat_DoesNotRespawnUntilRetryCompletesLoading()
        {
            var flow = ReachGameplay();

            Assert.That(flow.ShowDefeat(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Defeat));
            Assert.That(flow.ShouldFreezeTime, Is.True);

            Assert.That(flow.RetryFromCheckpoint(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Loading));
            Assert.That(flow.CompleteLoading(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Gameplay));
        }

        [Test]
        public void FailedRetry_ReturnsToDefeatPanel()
        {
            var flow = ReachGameplay();
            flow.ShowDefeat();
            flow.RetryFromCheckpoint();

            Assert.That(flow.FailLoading(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Defeat));
        }

        [Test]
        public void PauseCheckpointRestart_CanFailSafelyBackToPause()
        {
            var flow = ReachGameplay();
            flow.Pause();

            Assert.That(flow.RestartFromPause(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Loading));
            flow.FailLoading();
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Pause));
        }

        [Test]
        public void Victory_LeadsToCreditsAndThenMainMenu()
        {
            var flow = ReachGameplay();

            Assert.That(flow.ShowVictory(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Victory));
            Assert.That(flow.ShouldFreezeTime, Is.True);
            Assert.That(flow.OpenCredits(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.Credits));
            Assert.That(flow.ReturnToMainMenu(), Is.True);
            Assert.That(flow.Current, Is.EqualTo(PresentationScreen.MainMenu));
        }

        [Test]
        public void InvalidTransitions_AreRejected()
        {
            var flow = new PresentationFlowState();

            Assert.That(flow.Pause(), Is.False);
            Assert.That(flow.Resume(), Is.False);
            Assert.That(flow.ShowDefeat(), Is.False);
            Assert.That(flow.ShowVictory(), Is.False);
            Assert.That(flow.CompleteLoading(), Is.False);
        }

        private static PresentationFlowState ReachGameplay()
        {
            var flow = new PresentationFlowState(1);
            flow.StartNewGame();
            flow.AdvanceIntro();
            flow.CompleteLoading();
            return flow;
        }
    }
}
