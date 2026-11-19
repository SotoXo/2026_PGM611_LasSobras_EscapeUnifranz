using System;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Encounters
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FrancisBossController : MonoBehaviour,
        IZoneRuntimeInitializable,
        IEncounterResettable
    {
        [SerializeField] private EncounterContext encounter;
        [SerializeField] private BossPhaseView phaseView;
        [SerializeField] private TimedHazardArea2D interferenceHazard;
        [SerializeField] private string phaseTwoRespawnId = "francis_fase2";
        [SerializeField] private string phaseThreeRespawnId = "francis_fase3";
        [SerializeField, Min(0.5f)] private float introSeconds = 1.5f;
        [SerializeField, Min(1f)] private float interferenceInterval = 4f;
        [SerializeField, Min(0f)] private float interferenceTelegraphSeconds = 0.5f;
        [SerializeField, Min(0.1f)] private float interferenceSeconds = 0.8f;

        private FrancisBossStateMachine machine = new FrancisBossStateMachine();
        private GameSessionController session;
        private GameplayModeController gameplayMode;
        private float introEndsAt;
        private float nextInterferenceAt;
        private bool started;
        private int phaseProgress;

        public FrancisBossState State => machine.Current;
        public event Action PhaseProgressReset;
        public event Action<FrancisBossState> PhaseChanged;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void Initialize(ZoneRuntimeContext context)
        {
            session = context.Session; gameplayMode=context.GameplayMode;
            if (phaseView == null)
            {
                phaseView = session.BossPhaseView;
            }
            RestoreFromState();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (session == null || started || State == FrancisBossState.Completed ||
                !PlayerPhysicsContact2D.IsPlayerBody(other))
            {
                return;
            }

            started = true;
            session.State.SetFlag(GameFlagId.FrancisEncounterStarted);
            encounter?.BeginEncounter();
            phaseView?.Show(FrancisBossState.Intro);
            session.ShowMessage("FRANCIS: He usado tu voz para pedir ayuda. Nadie ha venido.");
            ProjectAudioHooks.Emit(AudioCue.FrancisBroadcast);
            introEndsAt = Time.time + introSeconds;
            nextInterferenceAt = Time.time + interferenceInterval;
            Debug.Log("Boss phase changed: Intro", this);
        }

        private void Update()
        {
            if (session == null || gameplayMode == null || !gameplayMode.AllowsMovement || !started || State == FrancisBossState.Completed)
            {
                return;
            }

            if (State == FrancisBossState.Intro && Time.time >= introEndsAt && machine.BeginNetworkPhase())
            {
                ChangePhase("Aísla la IA de la red.");
            }

            if (interferenceHazard != null)
            {
                if (interferenceHazard.State == HazardWindowState.Safe &&
                    Time.time >= nextInterferenceAt)
                {
                    float lead = State == FrancisBossState.PhaseNetwork ? 1.45f :
                        State == FrancisBossState.PhaseHardware ? 1.1f : .95f;
                    float exposure = State == FrancisBossState.PhaseShutdown ? .72f : interferenceSeconds;
                    interferenceHazard.BeginTelegraph(Mathf.Max(lead, interferenceTelegraphSeconds), exposure);
                    nextInterferenceAt = Time.time + interferenceInterval;
                }
            }
        }

        public bool SubmitNetwork(FrancisNetworkStep step)
        {
            FrancisBossState before = State;
            SequenceSubmissionResult result = machine.SubmitNetwork(step);
            return HandleSubmission(result, before,
                "Desenergiza los actuadores.");
        }

        public bool SubmitHardware(FrancisHardwareStep step)
        {
            FrancisBossState before = State;
            SequenceSubmissionResult result = machine.SubmitHardware(step);
            return HandleSubmission(result, before,
                "Ejecuta el apagado seguro.");
        }

        public bool SubmitShutdown(FrancisShutdownStep step)
        {
            SequenceSubmissionResult result = machine.SubmitShutdown(step);
            if (result == SequenceSubmissionResult.Incorrect)
            {
                phaseProgress = machine.CurrentProgress;
                session.Objectives?.SetProgress(phaseProgress, 3);
                session.ShowMessage("Sistema aún no seguro. Sigue: CONEXIÓN FINAL → CORTAR ACTUADORES → SHUTDOWN.");
                return false;
            }

            phaseProgress = result == SequenceSubmissionResult.Completed
                ? 3
                : Mathf.Min(3, phaseProgress + 1);
            session.Objectives?.SetProgress(phaseProgress, 3);

            if (result == SequenceSubmissionResult.Completed)
            {
                FrancisProgression.ApplyCompletedPhase(session.State, FrancisBossState.PhaseShutdown);
                StopThreats();
                phaseView?.Show(FrancisBossState.Completed);
                session.ShowMessage("FRANCIS: Si abandono mi función, ¿quién los protegerá?");
                ProjectAudioHooks.Emit(AudioCue.FrancisFarewell);
                session.Objectives?.SetCurrent("core_escape", "Vuelve por el corredor y sal por la puerta del laboratorio.");
                session.ShowMessage("Los seguros se abren. Regresa al acceso del laboratorio.");
                encounter?.CompleteEncounter();
                PhaseChanged?.Invoke(State);
                Debug.Log("Francis apagada. Extracción disponible.", this);
            }

            return result == SequenceSubmissionResult.Correct ||
                result == SequenceSubmissionResult.Completed;
        }

        private bool HandleSubmission(
            SequenceSubmissionResult result,
            FrancisBossState previousPhase,
            string nextObjective)
        {
            if (result == SequenceSubmissionResult.Incorrect)
            {
                phaseProgress = 0;
                session.Objectives?.SetProgress(0, 3);
                session.ShowMessage("Secuencia incorrecta. La fase se reinició.");
                PhaseProgressReset?.Invoke();
                return false;
            }

            phaseProgress = result == SequenceSubmissionResult.Completed
                ? 3
                : Mathf.Min(3, phaseProgress + 1);
            session.Objectives?.SetProgress(phaseProgress, 3);

            if (result == SequenceSubmissionResult.Completed)
            {
                FrancisProgression.ApplyCompletedPhase(session.State, previousPhase);
                string respawnId = previousPhase == FrancisBossState.PhaseNetwork
                    ? phaseTwoRespawnId
                    : phaseThreeRespawnId;
                session.State.SetCheckpoint(session.State.CurrentZoneId, respawnId);
                ChangePhase(nextObjective);
            }

            return result == SequenceSubmissionResult.Correct ||
                result == SequenceSubmissionResult.Completed;
        }

        private void ChangePhase(string objective)
        {
            phaseProgress = 0;
            phaseView?.Show(State);
            session.Objectives?.SetCurrent($"francis_{State}", objective, 0, 3);
            string message = State == FrancisBossState.PhaseNetwork
                ? "FRANCIS: Retire las manos del distribuidor. Permanezca en su sector."
                : State == FrancisBossState.PhaseHardware
                    ? "FRANCIS: Perdí contacto con las cámaras. Los actuadores siguen en línea."
                    : "FRANCIS: No puedo ver los pasillos. ¿Sigues ahí?";
            session.ShowMessage(message);
            ProjectAudioHooks.Emit(State == FrancisBossState.PhaseNetwork ? AudioCue.TerminalBoot :
                State == FrancisBossState.PhaseHardware ? AudioCue.RobotAlert : AudioCue.Blackout);
            nextInterferenceAt = Time.time + interferenceInterval;
            PhaseProgressReset?.Invoke();
            PhaseChanged?.Invoke(State);
            Debug.Log($"Boss phase changed: {State}", this);
        }

        private void RestoreFromState()
        {
            machine = new FrancisBossStateMachine();
            machine.Restore(
                session.State.HasFlag(GameFlagId.FrancisNetworkPhaseComplete),
                session.State.HasFlag(GameFlagId.FrancisHardwarePhaseComplete),
                session.State.HasFlag(GameFlagId.FrancisShutdownPhaseComplete));

            started = session.State.HasFlag(GameFlagId.FrancisEncounterStarted);
            if (started && State != FrancisBossState.Completed)
            {
                encounter?.BeginEncounter();
                session.State.SetCheckpoint(session.State.CurrentZoneId, State==FrancisBossState.PhaseShutdown ? phaseThreeRespawnId : State==FrancisBossState.PhaseHardware ? phaseTwoRespawnId : encounter.CheckpointSpawnId);
                phaseView?.Show(State);
                nextInterferenceAt = Time.time + interferenceInterval;
            }
            else if (State == FrancisBossState.Completed)
            {
                StopThreats();
                phaseView?.Show(State);
            }
        }

        private void StopThreats()
        {
            interferenceHazard?.SetHazardActive(false);
            if (encounter == null) return;
            foreach(var h in encounter.GetComponentsInChildren<TimedHazardArea2D>(true)) h.SetHazardActive(false);
            foreach(var h in encounter.GetComponentsInChildren<HazardContact2D>(true)) h.SetContactAllowed(false);
            foreach(var m in encounter.GetComponentsInChildren<PatrolMover2D>(true)) { m.ResumePatrol(); m.enabled=false; }
            foreach(var sensor in encounter.GetComponentsInChildren<DetectionSensor2D>(true)) sensor.enabled=false;
        }

        public void ResetEncounter()
        {
            interferenceHazard?.SetHazardActive(false);
            machine.ResetCurrentPhase();
            phaseProgress = 0;
            session?.Objectives?.SetProgress(0, 3);
            PhaseProgressReset?.Invoke();
            nextInterferenceAt = Time.time + interferenceInterval;
        }
    }
}
