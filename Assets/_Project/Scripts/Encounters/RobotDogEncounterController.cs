using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Encounters
{
    public sealed class RobotDogEncounterController : MonoBehaviour,
        IZoneRuntimeInitializable,
        IEncounterResettable
    {
        [SerializeField] private EncounterContext encounter;
        [SerializeField] private PatrolMover2D mover;
        [SerializeField] private DetectionSensor2D sensor;
        [SerializeField, Min(0f)] private float alertSpeed = 4f;
        [SerializeField, Min(0f)] private float searchSeconds = 2f;

        private readonly RobotDogEncounterStateMachine machine = new RobotDogEncounterStateMachine();
        private GameSessionController session;
        private float searchUntil;

        public RobotDogEncounterState State => machine.Current;

        public void Initialize(ZoneRuntimeContext context)
        {
            session = context.Session;
            if (session.State.HasFlag(GameFlagId.RobotDogDisabled))
            {
                machine.RestoreDisabled();
                gameObject.SetActive(false);
                return;
            }

            if (sensor != null)
            {
                sensor.TargetDetected += OnDetected;
                sensor.TargetLost += OnLost;
            }
        }

        private void OnDestroy()
        {
            if (sensor != null)
            {
                sensor.TargetDetected -= OnDetected;
                sensor.TargetLost -= OnLost;
            }
        }

        private void Update()
        {
            if (machine.Current == RobotDogEncounterState.Search && Time.time >= searchUntil)
            {
                machine.FinishSearch();
                mover?.ResumePatrol();
            }
        }

        private void OnDetected(Transform target)
        {
            if (machine.Detect())
            {
                encounter?.BeginEncounter();
                mover?.SetChaseTarget(target, alertSpeed);
            }
        }

        private void OnLost()
        {
            if (machine.LoseTarget())
            {
                searchUntil = Time.time + searchSeconds;
                mover?.ResumePatrol();
            }
        }

        public bool DisableNetworkNode(int index)
        {
            if (!machine.DisableNode(index))
            {
                return false;
            }

            session.Objectives?.SetProgress(machine.DisabledNodeCount, 2);
            session.ShowMessage($"Nodo de red desconectado {machine.DisabledNodeCount}/2.");
            if (machine.Current == RobotDogEncounterState.Disabled)
            {
                session.State.SetFlag(GameFlagId.RobotDogDisabled);
                session.State.SetFlag(GameFlagId.LabSoftwareUnlocked);
                session.ShowMessage("Perro robot desconectado. Laboratorio de Software habilitado.");
                mover?.ResumePatrol();
                sensor?.ClearTarget();
                encounter?.CompleteEncounter();
                gameObject.SetActive(false);
            }

            return true;
        }

        public void ResetEncounter()
        {
            if (machine.Reset())
            {
                searchUntil = 0f;
                sensor?.ClearTarget();
                mover?.ResetEncounter();
            }
        }
    }
}
