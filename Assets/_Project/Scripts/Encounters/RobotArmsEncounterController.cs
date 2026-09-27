using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Encounters
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class RobotArmsEncounterController : MonoBehaviour,
        IZoneRuntimeInitializable,
        IEncounterResettable
    {
        [SerializeField] private EncounterContext encounter;
        [SerializeField] private TimedHazardArea2D[] armHazards;
        [SerializeField, Min(0f)] private float telegraphSeconds = 0.5f;
        [SerializeField, Min(0.1f)] private float strikeSeconds = 0.75f;
        [SerializeField, Min(0.1f)] private float pauseSeconds = 0.35f;
        [SerializeField, Min(0.5f)] private float vulnerableSeconds = 3f;

        private readonly RobotArmsEncounterStateMachine machine =
            new RobotArmsEncounterStateMachine();
        private GameSessionController session;
        private int armIndex;
        private bool strikeActive;
        private float nextChangeAt;

        public RobotArmsEncounterState State => machine.Current;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void Initialize(ZoneRuntimeContext context)
        {
            session = context.Session;
            if (session.State.HasFlag(GameFlagId.ArmsDisabled))
            {
                machine.RestoreDisabled();
                DisableAllHazards();
                gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (PlayerPhysicsContact2D.IsPlayerBody(other) && machine.BeginSequence())
            {
                encounter?.BeginEncounter();
                StartPattern();
            }
        }

        private void Update()
        {
            if (machine.Current == RobotArmsEncounterState.Sequence && Time.time >= nextChangeAt)
            {
                AdvancePattern();
            }
            else if (machine.Current == RobotArmsEncounterState.Vulnerable && Time.time >= nextChangeAt &&
                machine.BeginSequence())
            {
                StartPattern();
            }
        }

        public bool TryDisableFromPanel()
        {
            if (!machine.Disable())
            {
                session?.ShowMessage("Panel fuera de la ventana segura.");
                return false;
            }

            DisableAllHazards();
            session.State.SetFlag(GameFlagId.ArmsDisabled);
            session.State.SetFlag(GameFlagId.NucleoUnlocked);
            session.ShowMessage("Brazos robóticos desactivados. Núcleo IA habilitado.");
            encounter?.CompleteEncounter();
            gameObject.SetActive(false);
            return true;
        }

        private void StartPattern()
        {
            DisableAllHazards();
            armIndex = 0;
            strikeActive = false;
            nextChangeAt = Time.time;
        }

        private void AdvancePattern()
        {
            if (armHazards == null || armHazards.Length == 0)
            {
                machine.OpenVulnerability();
                nextChangeAt = Time.time + vulnerableSeconds;
                return;
            }

            if (strikeActive)
            {
                armHazards[armIndex]?.SetHazardActive(false);
                strikeActive = false;
                armIndex++;
                if (armIndex >= armHazards.Length)
                {
                    machine.OpenVulnerability();
                    session?.ShowMessage("Sistema vulnerable: usa el panel.");
                    nextChangeAt = Time.time + vulnerableSeconds;
                }
                else
                {
                    nextChangeAt = Time.time + pauseSeconds;
                }
            }
            else
            {
                armHazards[armIndex]?.BeginTelegraph(telegraphSeconds, strikeSeconds);
                strikeActive = true;
                nextChangeAt = Time.time + telegraphSeconds + strikeSeconds;
            }
        }

        private void DisableAllHazards()
        {
            if (armHazards == null)
            {
                return;
            }

            for (int index = 0; index < armHazards.Length; index++)
            {
                armHazards[index]?.SetHazardActive(false);
            }
        }

        public void ResetEncounter()
        {
            if (machine.Reset())
            {
                DisableAllHazards();
                armIndex = 0;
                strikeActive = false;
            }
        }
    }
}
