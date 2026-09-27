using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Encounters
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class ExolegEncounterController : MonoBehaviour,
        IZoneRuntimeInitializable,
        IEncounterResettable
    {
        [SerializeField] private EncounterContext encounter;
        [SerializeField] private TimedHazardArea2D[] hazardAreas;
        [SerializeField, Range(2, 3)] private int requiredOverloadPoints = 3;
        [SerializeField, Min(0f)] private float warningSeconds = 1.25f;
        [SerializeField, Min(0f)] private float telegraphSeconds = 0.5f;
        [SerializeField, Min(0.1f)] private float hazardSeconds = 1f;
        [SerializeField, Min(0.1f)] private float pauseSeconds = 0.55f;
        [SerializeField, Min(0f)] private float overloadSeconds = 0.8f;

        private ExolegEncounterStateMachine machine;
        private GameSessionController session;
        private float nextChangeAt;
        private int hazardIndex;
        private bool hazardOn;

        public ExolegEncounterState State => machine?.Current ?? ExolegEncounterState.Dormant;
        public bool AcceptsOverloadPoint => State == ExolegEncounterState.Active;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            machine = new ExolegEncounterStateMachine(requiredOverloadPoints);
        }

        public void Initialize(ZoneRuntimeContext context)
        {
            session = context.Session;
            if (session.State.HasFlag(GameFlagId.ExolegsDisabled))
            {
                machine.RestoreDisabled();
                DisableAllHazards();
                gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayerPhysicsContact2D.IsPlayerBody(other) || !machine.BeginWarning())
            {
                return;
            }

            encounter?.BeginEncounter();
            nextChangeAt = Time.time + warningSeconds;
            session?.ShowMessage("EXOLEG: sistema mecánico inestable.");
        }

        private void Update()
        {
            if (machine.Current == ExolegEncounterState.Warning && Time.time >= nextChangeAt)
            {
                machine.BeginActive();
                hazardIndex = 0;
                hazardOn = false;
                nextChangeAt = Time.time;
            }

            if (machine.Current == ExolegEncounterState.Active && Time.time >= nextChangeAt)
            {
                UpdateHazardPattern();
            }

            if (machine.Current == ExolegEncounterState.Overloaded && Time.time >= nextChangeAt &&
                machine.Disable())
            {
                CompleteEncounter();
            }
        }

        public bool ActivateOverloadPoint()
        {
            if (!machine.ActivateOverloadPoint())
            {
                return false;
            }

            session?.Objectives?.SetProgress(machine.ActivatedPoints, requiredOverloadPoints);
            session?.ShowMessage($"Punto de sobrecarga {machine.ActivatedPoints}/{requiredOverloadPoints}.");
            if (machine.Current == ExolegEncounterState.Overloaded)
            {
                DisableAllHazards();
                nextChangeAt = Time.time + overloadSeconds;
            }

            return true;
        }

        private void UpdateHazardPattern()
        {
            if (hazardAreas == null || hazardAreas.Length == 0)
            {
                nextChangeAt = Time.time + pauseSeconds;
                return;
            }

            if (hazardOn)
            {
                hazardAreas[hazardIndex]?.SetHazardActive(false);
                hazardIndex = (hazardIndex + 1) % hazardAreas.Length;
                hazardOn = false;
                nextChangeAt = Time.time + pauseSeconds;
            }
            else
            {
                hazardAreas[hazardIndex]?.BeginTelegraph(telegraphSeconds, hazardSeconds);
                hazardOn = true;
                nextChangeAt = Time.time + telegraphSeconds + hazardSeconds;
            }
        }

        private void CompleteEncounter()
        {
            DisableAllHazards();
            session.State.SetFlag(GameFlagId.ExolegsDisabled);
            session.State.SetFlag(GameFlagId.Piso3Unlocked);
            session.ShowMessage("EXOLEG desactivado. Acceso al tercer piso habilitado.");
            encounter?.CompleteEncounter();
            gameObject.SetActive(false);
        }

        private void DisableAllHazards()
        {
            if (hazardAreas == null)
            {
                return;
            }

            for (int index = 0; index < hazardAreas.Length; index++)
            {
                hazardAreas[index]?.SetHazardActive(false);
            }
        }

        public void ResetEncounter()
        {
            if (machine.Reset())
            {
                DisableAllHazards();
                hazardIndex = 0;
                hazardOn = false;
                session?.Objectives?.SetProgress(0, requiredOverloadPoints);
            }
        }
    }
}
