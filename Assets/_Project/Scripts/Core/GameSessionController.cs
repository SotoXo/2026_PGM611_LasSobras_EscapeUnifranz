using System;
using System.Collections;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Core
{
    /// <summary>
    /// Owns the current GameState and starts a new session from Bootstrap.
    /// </summary>
    public sealed class GameSessionController : MonoBehaviour
    {
        [SerializeField] private SceneFlowController sceneFlow;
        [SerializeField] private ObjectiveController objectiveController;
        [SerializeField] private MessageToastView messageToastView;
        [SerializeField] private NotificationController notificationController;
        [SerializeField] private MinimapController minimapController;
        [SerializeField] private ScannerController scannerController;
        [SerializeField] private ObjectiveMarkerController objectiveMarkerController;
        [SerializeField] private ZoneTitleView zoneTitleView;
        [SerializeField] private BossPhaseView bossPhaseView;
        [SerializeField] private GameFlowController gameFlow;
        [SerializeField] private string initialZoneId = "entrada";
        [SerializeField] private string initialSpawnId = "from_bootstrap";
        [SerializeField] private bool startNewGameOnStart = true;

        private GameState state;

        public GameState State => state;
        public ObjectiveController Objectives => objectiveController;
        public BossPhaseView BossPhaseView => bossPhaseView;
        public event Action<GameState> StateChanged;

        private void Awake()
        {
            StartNewGame();
        }

        private IEnumerator Start()
        {
            if (!startNewGameOnStart)
            {
                yield break;
            }

            yield return null;
            sceneFlow.GoToZone(initialZoneId, initialSpawnId);
        }

        public void StartNewGame()
        {
            if (state != null)
            {
                state.CheckpointChanged -= OnCheckpointChanged;
            }

            state = new GameState();
            state.CheckpointChanged += OnCheckpointChanged;
            objectiveController?.Bind(state);
            minimapController?.Bind(state);
            objectiveMarkerController?.Bind(state);
            notificationController?.Clear();
            bossPhaseView?.Hide();
            StateChanged?.Invoke(state);
        }

        public bool RequestDefeat(EncounterContext encounter, string reason)
        {
            if (gameFlow != null)
            {
                return gameFlow.ShowDefeat(reason, encounter);
            }

            return sceneFlow != null && sceneFlow.RequestRespawn(encounter);
        }

        public void ShowMessage(string message)
        {
            if (notificationController != null)
            {
                notificationController.Enqueue(message);
            }
            else
            {
                messageToastView?.Show(message);
            }
        }

        public void BindZonePresentation(ZoneContext zone)
        {
            minimapController?.SetZone(zone);
            scannerController?.SetZone(zone);
            objectiveMarkerController?.SetZone(zone);
            zoneTitleView?.Show(zone != null ? zone.DisplayName : string.Empty);
        }

        private void OnDestroy()
        {
            if (state != null)
            {
                state.CheckpointChanged -= OnCheckpointChanged;
            }
        }

        private void OnCheckpointChanged(string _, string __)
        {
            ShowMessage("Punto de control actualizado");
        }
    }
}
