using System.Collections;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeUNIFRANZ.Core
{
    /// <summary>
    /// Coordinates the persistent presentation flow without owning encounter mechanics.
    /// </summary>
    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private GameSessionController session;
        [SerializeField] private SceneFlowController sceneFlow;
        [SerializeField] private GameplayModeController gameplayMode;
        [SerializeField] private CampusMapController campusMap;
        [SerializeField] private GameFlowView view;
        [SerializeField] private FadeView fadeView;
        [SerializeField] private string initialZoneId = "entrada";
        [SerializeField] private string initialSpawnId = "entrada_inicio";

        private readonly PresentationFlowState flow = new PresentationFlowState(3);
        private GameState boundState;
        private EncounterContext pendingEncounter;
        private GameplayMode resumeMode = GameplayMode.Explore;
        private Coroutine outcomeTransition;

        public PresentationScreen CurrentScreen => flow.Current;
        public bool ShowsGameplayHud => flow.ShowsGameplayHud;
        public bool IsTimeFrozen => flow.ShouldFreezeTime;
        public string DefeatReason { get; private set; } = string.Empty;

        private void Awake()
        {
            flow.Changed += OnScreenChanged;
            view?.Bind(this);
        }

        private void OnEnable()
        {
            if (session != null)
            {
                session.StateChanged += BindState;
            }

            if (sceneFlow != null)
            {
                sceneFlow.ZoneTransitionFinished += OnZoneTransitionFinished;
                sceneFlow.RespawnFinished += OnRespawnFinished;
            }
        }

        private void Start()
        {
            BindState(session != null ? session.State : null);
            ApplyScreen();

            if (MainMenuController.ConsumeNewGameRequest())
            {
                NewGame();
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (flow.Current == PresentationScreen.Intro)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    SkipIntro();
                }
                return;
            }

            if (!keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            switch (flow.Current)
            {
                case PresentationScreen.Gameplay:
                    PauseGame();
                    break;
                case PresentationScreen.Pause:
                    ResumeGame();
                    break;
                case PresentationScreen.Controls:
                    CloseControls();
                    break;
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.StateChanged -= BindState;
            }

            if (sceneFlow != null)
            {
                sceneFlow.ZoneTransitionFinished -= OnZoneTransitionFinished;
                sceneFlow.RespawnFinished -= OnRespawnFinished;
            }

            UnbindState();
        }

        private void OnDestroy()
        {
            flow.Changed -= OnScreenChanged;
            view?.Unbind(this);
            Time.timeScale = 1f;
        }

        public void NewGame()
        {
            if (sceneFlow != null && sceneFlow.IsTransitioning)
            {
                return;
            }

            pendingEncounter = null;
            DefeatReason = string.Empty;
            resumeMode = GameplayMode.Explore;
            session?.StartNewGame();
            flow.StartNewGame();
        }

        public void ContinueIntro()
        {
            if (flow.AdvanceIntro() && flow.Current == PresentationScreen.Loading)
            {
                LoadInitialZone();
            }
        }

        public void SkipIntro()
        {
            if (flow.SkipIntro())
            {
                LoadInitialZone();
            }
        }

        public void PauseGame()
        {
            if (!PresentationFlowState.CanPause(
                flow.Current,
                sceneFlow != null && sceneFlow.IsTransitioning,
                outcomeTransition != null))
            {
                return;
            }

            resumeMode = PresentationFlowState.ResolveResumeMode(
                gameplayMode != null ? gameplayMode.CurrentMode : GameplayMode.Explore);
            campusMap?.CloseIfOpen();
            flow.Pause();
        }

        public void ResumeGame()
        {
            flow.Resume();
        }

        public void RestartCheckpointFromPause()
        {
            if (session?.State == null || !session.State.HasCheckpoint ||
                !flow.RestartFromPause())
            {
                return;
            }

            pendingEncounter = null;
            if (sceneFlow == null || !sceneFlow.RequestRespawn())
            {
                flow.FailLoading();
            }
        }

        public bool ShowDefeat(string reason, EncounterContext encounter)
        {
            if (!PresentationFlowState.CanShowDefeat(
                flow.Current,
                sceneFlow != null && sceneFlow.IsTransitioning,
                outcomeTransition != null))
            {
                return false;
            }

            string acceptedReason = string.IsNullOrWhiteSpace(reason)
                ? "El sistema de seguridad neutralizó al jugador."
                : reason.Trim();

            bool changed = flow.ShowDefeat();
            if (changed)
            {
                pendingEncounter = encounter;
                resumeMode = PresentationFlowState.ResolveResumeMode(
                    gameplayMode != null ? gameplayMode.CurrentMode : GameplayMode.Explore);
                DefeatReason = acceptedReason;
                ApplyScreen();
                StartOutcomeTransition();
            }
            return changed;
        }

        public void RetryCheckpoint()
        {
            if (session?.State == null || !session.State.HasCheckpoint ||
                !flow.RetryFromCheckpoint())
            {
                return;
            }

            if (sceneFlow == null || !sceneFlow.RequestRespawn(pendingEncounter))
            {
                flow.FailLoading();
            }
        }

        public void OpenControls()
        {
            flow.OpenControls();
        }

        public void CloseControls()
        {
            flow.CloseControls();
        }

        public void OpenCredits()
        {
            flow.OpenCredits();
        }

        public void ReturnToMainMenu()
        {
            if (sceneFlow != null && sceneFlow.IsTransitioning)
            {
                return;
            }

            campusMap?.CloseIfOpen();
            pendingEncounter = null;
            DefeatReason = string.Empty;
            flow.ReturnToMainMenu();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("Salir solicitado. Application.Quit solo cierra una build.", this);
#else
            Application.Quit();
#endif
        }

        private void LoadInitialZone()
        {
            resumeMode = GameplayMode.Explore;
            if (sceneFlow == null || !sceneFlow.GoToZone(initialZoneId, initialSpawnId))
            {
                flow.FailLoading();
            }
        }

        private void OnZoneTransitionFinished(bool succeeded)
        {
            if (flow.Current != PresentationScreen.Loading)
            {
                return;
            }

            if (succeeded)
            {
                resumeMode = GameplayMode.Explore;
                flow.CompleteLoading();
            }
            else
            {
                flow.FailLoading();
            }
        }

        private void OnRespawnFinished(bool succeeded)
        {
            if (flow.Current != PresentationScreen.Loading)
            {
                return;
            }

            if (succeeded)
            {
                pendingEncounter = null;
                flow.CompleteLoading();
            }
            else
            {
                flow.FailLoading();
            }
        }

        private void BindState(GameState gameState)
        {
            if (ReferenceEquals(boundState, gameState))
            {
                return;
            }

            UnbindState();
            boundState = gameState;
            if (boundState != null)
            {
                boundState.FlagChanged += OnFlagChanged;
                if (boundState.HasFlag(GameFlagId.GameCompleted))
                {
                    QueueVictory();
                }
            }
        }

        private void UnbindState()
        {
            if (boundState != null)
            {
                boundState.FlagChanged -= OnFlagChanged;
                boundState = null;
            }
        }

        private void OnFlagChanged(GameFlagId flag)
        {
            if (flag == GameFlagId.GameCompleted)
            {
                QueueVictory();
            }
        }

        private void QueueVictory()
        {
            if (outcomeTransition == null && isActiveAndEnabled)
            {
                outcomeTransition = StartCoroutine(ShowVictoryAfterEncounter());
            }
        }

        private IEnumerator ShowVictoryAfterEncounter()
        {
            yield return null;
            outcomeTransition = null;
            if (boundState != null && boundState.HasFlag(GameFlagId.GameCompleted) &&
                flow.ShowVictory())
            {
                StartOutcomeTransition();
            }
        }

        private void StartOutcomeTransition()
        {
            if (fadeView != null)
            {
                StartCoroutine(OutcomeTransitionRoutine());
            }
        }

        private IEnumerator OutcomeTransitionRoutine()
        {
            yield return fadeView.FadeOut();
            yield return null;
            yield return fadeView.FadeIn();
        }

        private void OnScreenChanged(PresentationScreen _)
        {
            ApplyScreen();
        }

        private void ApplyScreen()
        {
            Time.timeScale = flow.ShouldFreezeTime ? 0f : 1f;

            if (gameplayMode != null)
            {
                switch (flow.Current)
                {
                    case PresentationScreen.Gameplay:
                        gameplayMode.SetMode(resumeMode);
                        break;
                    case PresentationScreen.Loading:
                        gameplayMode.SetMode(GameplayMode.Transition);
                        break;
                    case PresentationScreen.Pause:
                        gameplayMode.SetMode(GameplayMode.Pause);
                        break;
                    case PresentationScreen.Defeat:
                        gameplayMode.SetMode(GameplayMode.Defeat);
                        break;
                    case PresentationScreen.Victory:
                        gameplayMode.SetMode(GameplayMode.Ending);
                        break;
                    default:
                        gameplayMode.SetMode(GameplayMode.Presentation);
                        break;
                }
            }

            view?.Render(
                flow.Current,
                flow.IntroPanelIndex,
                flow.IntroPanelCount,
                DefeatReason);
        }
    }
}
