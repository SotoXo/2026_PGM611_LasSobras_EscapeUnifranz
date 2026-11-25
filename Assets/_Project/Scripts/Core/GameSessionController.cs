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
        private bool saveDirty;
        private float retrySaveAt;
        private float nextContactDamageAt;

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

        public void StartNewGame() => BindState(new GameState());

        public void RestoreState(GameState savedState)
        {
            if (savedState != null && savedState.Lives == 0) savedState.RestoreLives();
            BindState(savedState);
        }
        public void RestoreLives() { nextContactDamageAt=0;state?.RestoreLives(); }
        public bool RequestContactDamage(EncounterContext encounter, string reason)
        {
            if (state == null || Time.time < nextContactDamageAt ||
                (sceneFlow != null && !sceneFlow.ModeController.AllowsMovement) || !state.LoseLife()) return false;
            nextContactDamageAt = Time.time + 1.5f;
            EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.AttackImpact);
            if (state.Lives > 0) { ShowMessage("VIDAS: "+state.Lives+" / 5"); return false; }
            return RequestDefeat(encounter,reason);
        }

        private void BindState(GameState next)
        {
            UnsubscribeState();
            state = next;
            saveDirty = false;
            retrySaveAt = 0f;
            nextContactDamageAt = 0f;
            state.CheckpointChanged += OnCheckpointChanged;
            state.FlagChanged += OnFlagChanged;
            state.InventoryChanged += MarkSaveDirty;
            state.LivesChanged += MarkSaveDirty;
            state.MapKnowledgeChanged += MarkSaveDirty;
            state.LocationChanged += MarkSaveDirty;
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
            GetComponent<EntranceTutorialController>()?.SetZone(zone!=null ? zone.ZoneId : null);
            minimapController?.SetZone(zone);
            scannerController?.SetZone(zone);
            objectiveMarkerController?.SetZone(zone);
            zoneTitleView?.Show(zone != null ? zone.DisplayName : string.Empty);
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= retrySaveAt) FlushSave();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) FlushSave();
        }

        private void OnApplicationQuit() => FlushSave();

        public void FlushSave()
        {
            if (!saveDirty || !state.HasCheckpoint || sceneFlow == null || sceneFlow.IsTransitioning)
                return;

            if (CheckpointSave.TryWrite(state))
                saveDirty = false;
            else
                retrySaveAt = Time.unscaledTime + 10f;
        }

        private void MarkSaveDirty() => saveDirty = true;
        private void OnFlagChanged(GameFlagId _) => MarkSaveDirty();

        private void OnDestroy() => UnsubscribeState();

        private void UnsubscribeState()
        {
            if (state == null) return;
            state.CheckpointChanged -= OnCheckpointChanged;
            state.FlagChanged -= OnFlagChanged;
            state.InventoryChanged -= MarkSaveDirty;
            state.LivesChanged -= MarkSaveDirty;
            state.MapKnowledgeChanged -= MarkSaveDirty;
            state.LocationChanged -= MarkSaveDirty;
        }

        private void OnCheckpointChanged(string _, string __)
        {
            MarkSaveDirty();
            ShowMessage("CHECKPOINT ACTUALIZADO");
            EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.Checkpoint);
        }
    }
}
