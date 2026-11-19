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
        private GameplayModeController gameplayMode;
        private float searchUntil;
        public bool requireCameraAlarm;
        private bool alarmActivated;
        private Vector2 lastSeen;
        private float disabledUntil, attackAt;
        private float distractionUntil, nextDistractionAt, chaseUntil;
        public ThreatFeedback2D feedback;
        public Vector2 UnitPosition => mover!=null ? (Vector2)mover.transform.position : (Vector2)transform.position;
        public Transform UnitTransform => mover!=null ? mover.transform : transform;
        public bool TemporarilyDisabled => disabledUntil>Time.time;
        private void Contact(bool value) { foreach(var c in mover.GetComponentsInChildren<HazardContact2D>(true)) c.SetContactAllowed(value); }
        public void SuspendFor(float seconds)
        {
            if (session.State.HasFlag(GameFlagId.RobotDogDisabled)) return;
            disabledUntil=Time.time+seconds; sensor.enabled=false; mover.enabled=false; Contact(false);
            mover.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero; feedback?.Show(ThreatVisualState.Disabled);
        }
        public void DistractAt(Vector2 point,float seconds)
        { if (session == null || TemporarilyDisabled || machine.Current==RobotDogEncounterState.Disabled || sensor.HasTarget || Time.time < nextDistractionAt) return; nextDistractionAt=Time.time+Mathf.Max(1f,seconds)+2f; ActivateFromAlarm(); sensor.enabled=false; sensor.ClearTarget(); lastSeen=point; machine.Detect(); machine.LoseTarget(); distractionUntil=Time.time+seconds; searchUntil=Time.time+seconds; Contact(false); mover.Investigate(point,2f); }
        public bool CaptureAtStation() { return DisableNetworkNode(0) && DisableNetworkNode(1); }
        public void ActivateFromAlarm()
        { if (session==null || session.State.HasFlag(GameFlagId.RobotDogDisabled)) return; alarmActivated=true; if (TemporarilyDisabled) return; mover.enabled=true; sensor.enabled=Time.time>=distractionUntil; foreach(var contact in GetComponentsInChildren<HazardContact2D>(true)) contact.enabled=true; }

        public RobotDogEncounterState State => TemporarilyDisabled ? RobotDogEncounterState.Disabled : machine.Current;

        public void Initialize(ZoneRuntimeContext context)
        {
            session = context.Session; gameplayMode=context.GameplayMode;
            Contact(false);
            alarmActivated=!requireCameraAlarm;
            if (requireCameraAlarm) { mover.enabled=false; sensor.enabled=false; foreach(var contact in GetComponentsInChildren<HazardContact2D>(true)) contact.enabled=false; }
            if (session.State.HasFlag(GameFlagId.RobotDogDisabled))
            {
                machine.RestoreDisabled();
                mover.enabled=false; sensor.enabled=false; mover.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero; Contact(false); feedback?.Show(ThreatVisualState.Disabled);
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
            if (session == null || gameplayMode == null || !gameplayMode.AllowsMovement) return;
            if (disabledUntil>0f)
            {
                if (Time.time<disabledUntil) return;
                disabledUntil=0f; mover.enabled=alarmActivated; sensor.enabled=alarmActivated; mover.ResumePatrol(); feedback?.Show(ThreatVisualState.Patrol); attackAt=Time.time+0.65f;
            }
            if (sensor!=null && sensor.CurrentTarget!=null) { lastSeen=sensor.CurrentTarget.position; if(Time.time>=chaseUntil || mover.RouteBlocked) { sensor.ClearTarget(); sensor.enabled=false; distractionUntil=Time.time+searchSeconds; } }
            if (distractionUntil>0f && Time.time>=distractionUntil) { distractionUntil=0f; sensor.enabled=alarmActivated && machine.Current!=RobotDogEncounterState.Disabled; }
            if (machine.Current==RobotDogEncounterState.Alert && sensor.HasTarget && Time.time>=attackAt)
            { Contact(true); feedback?.Show(ThreatVisualState.Attack); mover.SetChaseTarget(sensor.CurrentTarget,alertSpeed); }
            if (machine.Current == RobotDogEncounterState.Search && Time.time >= searchUntil)
            {
                machine.FinishSearch();
                Contact(false); feedback?.Show(ThreatVisualState.Patrol);
                mover?.ResumePatrol();
            }
        }

        private void OnDetected(Transform target)
        {
            if (!alarmActivated) return;
            lastSeen=target.position;
            chaseUntil=Time.time+9f;
            if (machine.Detect())
            {
                encounter?.BeginEncounter();
                Contact(false); attackAt=Time.time+0.65f; feedback?.Show(ThreatVisualState.Alert); mover?.Investigate(target.position,0f);
            }
        }

        private void OnLost()
        {
            if (machine.LoseTarget())
            {
                searchUntil = Time.time + searchSeconds;
                Contact(false); feedback?.Show(ThreatVisualState.Patrol);
                mover?.Investigate(lastSeen,alertSpeed*0.45f);
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
                mover.enabled=false; sensor.enabled=false; mover.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero; Contact(false); feedback?.Show(ThreatVisualState.Disabled);
            }

            return true;
        }

        public void ResetEncounter()
        {
            if (machine.Reset())
            {
                searchUntil = disabledUntil = distractionUntil = nextDistractionAt = 0f;
                attackAt=Time.time+0.65f;
                Contact(false);
                mover.enabled=alarmActivated; sensor.enabled=alarmActivated;
                feedback?.Show(ThreatVisualState.Patrol);
                sensor?.ClearTarget();
                mover?.ResetEncounter();
            }
        }
    }
}
