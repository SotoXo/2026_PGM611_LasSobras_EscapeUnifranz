using System;

namespace EscapeUNIFRANZ.Core
{
    /// <summary>
    /// Pure application flow. GameplayMode remains responsible for local gameplay exclusivity.
    /// </summary>
    public sealed class PresentationFlowState
    {
        private readonly int introPanelCount;
        private PresentationScreen controlsReturnScreen = PresentationScreen.MainMenu;
        private PresentationScreen loadingFailureScreen = PresentationScreen.MainMenu;

        public PresentationFlowState(int panelCount = 3)
        {
            introPanelCount = Math.Max(1, panelCount);
        }

        public PresentationScreen Current { get; private set; } = PresentationScreen.MainMenu;
        public int IntroPanelIndex { get; private set; }
        public int IntroPanelCount => introPanelCount;
        public bool ShowsGameplayHud => Current == PresentationScreen.Gameplay;
        public bool ShouldFreezeTime => Current != PresentationScreen.Gameplay;

        public event Action<PresentationScreen> Changed;

        public static bool CanPause(
            PresentationScreen current,
            bool sceneTransitioning,
            bool victoryPending)
        {
            return current == PresentationScreen.Gameplay &&
                !sceneTransitioning &&
                !victoryPending;
        }

        public static bool CanShowDefeat(
            PresentationScreen current,
            bool sceneTransitioning,
            bool victoryPending)
        {
            return current == PresentationScreen.Gameplay &&
                !sceneTransitioning &&
                !victoryPending;
        }

        public static GameplayMode ResolveResumeMode(GameplayMode currentMode)
        {
            return currentMode == GameplayMode.Encounter
                ? GameplayMode.Encounter
                : GameplayMode.Explore;
        }

        public bool StartNewGame()
        {
            IntroPanelIndex = 0;
            return SetScreen(PresentationScreen.Intro);
        }

        public bool AdvanceIntro()
        {
            if (Current != PresentationScreen.Intro)
            {
                return false;
            }

            if (IntroPanelIndex < introPanelCount - 1)
            {
                IntroPanelIndex++;
                Changed?.Invoke(Current);
                return true;
            }

            return BeginLoading(PresentationScreen.MainMenu);
        }

        public bool SkipIntro()
        {
            return Current == PresentationScreen.Intro &&
                BeginLoading(PresentationScreen.MainMenu);
        }

        public bool BeginLoading(PresentationScreen failureScreen)
        {
            if (Current != PresentationScreen.Intro &&
                Current != PresentationScreen.Pause &&
                Current != PresentationScreen.Defeat)
            {
                return false;
            }

            loadingFailureScreen = failureScreen;
            return SetScreen(PresentationScreen.Loading);
        }

        public bool CompleteLoading()
        {
            return Current == PresentationScreen.Loading &&
                SetScreen(PresentationScreen.Gameplay);
        }

        public bool FailLoading()
        {
            return Current == PresentationScreen.Loading && SetScreen(loadingFailureScreen);
        }

        public bool Pause()
        {
            return Current == PresentationScreen.Gameplay && SetScreen(PresentationScreen.Pause);
        }

        public bool Resume()
        {
            return Current == PresentationScreen.Pause && SetScreen(PresentationScreen.Gameplay);
        }

        public bool OpenControls()
        {
            if (Current != PresentationScreen.MainMenu && Current != PresentationScreen.Pause)
            {
                return false;
            }

            controlsReturnScreen = Current;
            return SetScreen(PresentationScreen.Controls);
        }

        public bool CloseControls()
        {
            return Current == PresentationScreen.Controls && SetScreen(controlsReturnScreen);
        }

        public bool ShowDefeat()
        {
            return Current == PresentationScreen.Gameplay && SetScreen(PresentationScreen.Defeat);
        }

        public bool RetryFromCheckpoint()
        {
            return Current == PresentationScreen.Defeat &&
                BeginLoading(PresentationScreen.Defeat);
        }

        public bool RestartFromPause()
        {
            return Current == PresentationScreen.Pause &&
                BeginLoading(PresentationScreen.Pause);
        }

        public bool ShowVictory()
        {
            return Current == PresentationScreen.Gameplay && SetScreen(PresentationScreen.Victory);
        }

        public bool OpenCredits()
        {
            return (Current == PresentationScreen.MainMenu ||
                Current == PresentationScreen.Victory) &&
                SetScreen(PresentationScreen.Credits);
        }

        public bool ReturnToMainMenu()
        {
            IntroPanelIndex = 0;
            return SetScreen(PresentationScreen.MainMenu);
        }

        private bool SetScreen(PresentationScreen screen)
        {
            if (Current == screen)
            {
                return false;
            }

            Current = screen;
            Changed?.Invoke(Current);
            return true;
        }
    }
}
