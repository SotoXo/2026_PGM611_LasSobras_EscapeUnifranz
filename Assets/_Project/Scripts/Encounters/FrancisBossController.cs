using System;
using EscapeUNIFRANZ.Core;
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
            session = context.Session;
            if (phaseView == null)
            {
                phaseView = session.BossPhaseView;
            }
            RestoreFromState();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (started || State == FrancisBossState.Completed ||
                !PlayerPhysicsContact2D.IsPlayerBody(other))
            {
                return;
            }

            started = true;
            session.State.SetFlag(GameFlagId.FrancisEncounterStarted);
            encounter?.BeginEncounter();
            phaseView?.Show(FrancisBossState.Intro);
            session.ShowMessage("Francis intenta destruir el núcleo.");
            introEndsAt = Time.time + introSeconds;
            nextInterferenceAt = Time.time + interferenceInterval;
            Debug.Log("Boss phase changed: Intro", this);
        }

        private void Update()
        {
            if (!started || State == FrancisBossState.Completed)
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
                    interferenceHazard.BeginTelegraph(
                        interferenceTelegraphSeconds,
                        interferenceSeconds);
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
            FrancisBossState before = State;
            SequenceSubmissionResult result = machine.SubmitShutdown(step);
            if (result == SequenceSubmissionResult.Incorrect)
            {
                phaseProgress = 0;
                session.Objectives?.SetProgress(0, 3);
                session.ShowMessage("Sistema aún no seguro.");
                return false;
            }

            phaseProgress = result == SequenceSubmissionResult.Completed
                ? 3
                : Mathf.Min(3, phaseProgress + 1);
            session.Objectives?.SetProgress(phaseProgress, 3);

            if (result == SequenceSubmissionResult.Completed)
            {
                FrancisProgression.ApplyCompletedPhase(session.State, FrancisBossState.PhaseShutdown);
                interferenceHazard?.SetHazardActive(false);
                phaseView?.Show(FrancisBossState.Completed);
                session.ShowMessage(
                    "Ninguna rama es superior por sí sola. La integración forma Ingeniería de Sistemas.");
                encounter?.CompleteEncounter();
                PhaseChanged?.Invoke(State);
                Debug.Log("Game completed", this);
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
                phaseView?.Show(State);
                nextInterferenceAt = Time.time + interferenceInterval;
            }
            else if (State == FrancisBossState.Completed)
            {
                phaseView?.Show(State);
            }
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
