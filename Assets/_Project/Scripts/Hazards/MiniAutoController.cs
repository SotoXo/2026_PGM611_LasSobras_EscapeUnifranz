using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.World;
using UnityEngine;
using System.Linq;
namespace EscapeUNIFRANZ.Hazards
{
    public enum MobileThreatState { Idle, Patrol, Suspicious, Alert, Chase, Search, Return, Disabled }
    public sealed class MiniAutoController : MonoBehaviour, IZoneRuntimeInitializable, IEncounterResettable
    {
        [SerializeField] private PatrolMover2D mover;
        [SerializeField] private DetectionSensor2D sensor;
        [SerializeField, Min(0f)] private float chaseSpeed=3.5f;
        [SerializeField, Min(0f)] private float chaseSeconds=1.5f;
        public AudioSource optionalAudio;
        public AudioClip alertSound;
        public ThreatFeedback2D feedback;
        [Header("Mini-auto art")]
        [SerializeField] private SpriteRenderer carVisual;
        [SerializeField] private Texture2D movementArt;
        [SerializeField] private Texture2D verticalArt;
        [SerializeField] private Texture2D impactArt;
        [SerializeField] private MiniAutoWear wear;
        private GameplayModeController mode;
        private float transitionAt;
        private Transform visibleTarget;
        private Vector2 lastSeen;
        private bool operational=true;
        private float disabledUntil;
        private float distractionUntil;
        private float startupUntil;
        private EscapeUNIFRANZ.UI.EntranceTutorialController tutorial;
        private SpriteRenderer[] bodyVisuals;
        private MiniAutoSpriteSet spriteSet;
        private float movementTime;
        private float impactTime = -1f;
        private float nextServoAt;
        [SerializeField, Min(0.1f)] private float reactionSeconds = 0.65f;
        [SerializeField, Min(1f)] private float maxChaseSeconds = 9f;
        private float chaseUntil, nextDistractionAt;
        private bool operationalBeforeSuspend;

        private void Awake()
        {
            if (carVisual != null)
                spriteSet = MiniAutoSpriteSet.Create(movementArt, verticalArt, impactArt);

            bodyVisuals = GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer.enabled || (spriteSet != null && renderer == carVisual))
                .ToArray();
            RenderCar(false);
        }
        public MobileThreatState State { get; private set; }=MobileThreatState.Patrol;
        public string ReadableState => State==MobileThreatState.Disabled ? "DESACTIVADO / DISABLED" : State==MobileThreatState.Chase ? "PERSIGUIENDO" : State==MobileThreatState.Alert || State==MobileThreatState.Suspicious ? "TE VIO / AVISO" : State==MobileThreatState.Search ? "BUSCANDO" : State==MobileThreatState.Return ? "VOLVIENDO" : "PATRULLA";
        public void Initialize(ZoneRuntimeContext context)
        {
            mode=context.GameplayMode;
            tutorial=context.Session.GetComponent<EscapeUNIFRANZ.UI.EntranceTutorialController>();
            Contact(false);
            if (sensor!=null) { sensor.TargetDetected+=Detected; sensor.TargetLost+=Lost; }
        }
        public void SetOperational(bool value,bool hide=false,float startupSeconds=0f)
        {
            impactTime = -1f;
            if (!hide) EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(value ? EscapeUNIFRANZ.Narrative.AudioCue.RobotRestart : EscapeUNIFRANZ.Narrative.AudioCue.RobotOff);
            operational=value; State=value ? MobileThreatState.Patrol : MobileThreatState.Disabled;
            startupUntil=value && startupSeconds>0 ? Time.time+startupSeconds : 0;
            mover.enabled=value; sensor.enabled=value;
            Contact(false);
            if (bodyVisuals!=null) foreach(var renderer in bodyVisuals) if (renderer!=null) renderer.enabled=!hide;
            if (feedback!=null) { if (feedback.marker!=null) feedback.marker.gameObject.SetActive(!hide); feedback.Show(value ? ThreatVisualState.Patrol : ThreatVisualState.Disabled); }
            GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            if (!value) { visibleTarget=null; mover.ResumePatrol(); sensor.ClearTarget(); }
            if(value && startupSeconds>0) { State=MobileThreatState.Idle; mover.enabled=false; sensor.enabled=false; feedback?.Show(ThreatVisualState.Alert); }
        }
        private void Contact(bool value) { foreach(var contact in GetComponentsInChildren<HazardContact2D>(true)) contact.SetContactAllowed(value); }
        public void PlayImpact() { impactTime=0f; RenderCar(false); }
        public void SuspendFor(float seconds)
        {
            bool wasOperational = operational;
            if (disabledUntil<=0f) operationalBeforeSuspend=operational;
            disabledUntil = Time.time + Mathf.Max(1f, seconds);
            SetOperational(false);
            if (wasOperational && spriteSet != null)
                impactTime = 0f;
        }

        private void LateUpdate()
        {
            if (spriteSet == null || mode == null || !mode.AllowsMovement)
                return;

            bool moving = mover != null && mover.isActiveAndEnabled && mover.IsMoving;
            if (!moving)
                movementTime = 0f;
            RenderCar(moving);
            if (moving)
                movementTime = Mathf.Repeat(movementTime + Time.deltaTime, MiniAutoSpriteSet.CycleSeconds);

            if (impactTime >= 0f)
            {
                impactTime += Time.deltaTime;
                if (impactTime >= MiniAutoSpriteSet.CycleSeconds)
                    impactTime = -1f;
            }
        }

        private void RenderCar(bool moving)
        {
            if (spriteSet == null)
                return;

            Vector2 facing = mover != null ? mover.Facing : Vector2.down;
            carVisual.sprite = spriteSet.Frame(facing, wear, moving, movementTime, impactTime, out bool flipX);
            carVisual.flipX = flipX;
        }
        public void DistractAt(Vector2 point,float seconds)
        {
            if (!operational || startupUntil > 0f || Time.time < nextDistractionAt || (sensor != null && sensor.HasTarget)) return;
            nextDistractionAt=Time.time+Mathf.Max(1f,seconds)+2f;
            sensor.enabled=false; sensor.ClearTarget(); distractionUntil=Time.time+seconds; visibleTarget=null; lastSeen=point; State=MobileThreatState.Search; transitionAt=Time.time+seconds;
            Contact(false); mover.Investigate(point,1.4f); feedback?.Show(ThreatVisualState.Alert);
        }
        private void Update()
        {
            if (mode==null || !mode.AllowsMovement) return;
            if(startupUntil>0) { if(Time.time<startupUntil) return; startupUntil=0; SetOperational(true); }
            if (!operational) { if (disabledUntil>0f && Time.time>=disabledUntil) { disabledUntil=0f; SetOperational(true); } return; }
            if (distractionUntil>0f && Time.time>=distractionUntil) { distractionUntil=0f; sensor.enabled=true; }
            if (mover.IsMoving && Time.time >= nextServoAt)
            {
                EscapeUNIFRANZ.Narrative.ProjectAudioHooks.EmitAt(EscapeUNIFRANZ.Narrative.AudioCue.RobotWheels, transform.position);
                nextServoAt = Time.time + 1.55f;
            }
            if (sensor.transform!=mover.transform && mover.Facing.sqrMagnitude>0f)
                sensor.transform.localRotation=Quaternion.Euler(0f,0f,Vector2.SignedAngle(Vector2.up,mover.Facing));
            if (visibleTarget!=null) lastSeen=visibleTarget.position;
            if (State==MobileThreatState.Chase && visibleTarget!=null) { if (Time.time < chaseUntil && !mover.RouteBlocked) return; sensor.ClearTarget(); sensor.enabled=false; distractionUntil=Time.time+chaseSeconds; }
            if (Time.time<transitionAt) return;
            if (State==MobileThreatState.Suspicious && visibleTarget!=null)
            { State=MobileThreatState.Alert; transitionAt=Time.time+reactionSeconds; feedback?.Show(ThreatVisualState.Alert); EscapeUNIFRANZ.Narrative.ProjectAudioHooks.EmitAt(EscapeUNIFRANZ.Narrative.AudioCue.RobotAlert, transform.position); if (optionalAudio!=null && alertSound!=null) optionalAudio.PlayOneShot(alertSound); }
            else if (State==MobileThreatState.Alert && visibleTarget!=null)
            { State=MobileThreatState.Chase; chaseUntil=Time.time+maxChaseSeconds; Contact(true); feedback?.Show(ThreatVisualState.Attack); EscapeUNIFRANZ.Narrative.ProjectAudioHooks.EmitAt(EscapeUNIFRANZ.Narrative.AudioCue.RobotChase, transform.position); EscapeUNIFRANZ.Narrative.ProjectAudioHooks.AlertMusic(EscapeUNIFRANZ.Narrative.AudioCue.MusicChase, 6f); mover.SetChaseTarget(visibleTarget,chaseSpeed); }
            else if (State==MobileThreatState.Search)
            { State=MobileThreatState.Return; Contact(false); feedback?.Show(ThreatVisualState.Patrol); mover.ResumePatrol(); transitionAt=Time.time+0.5f; }
            else if (State==MobileThreatState.Return) State=MobileThreatState.Patrol;
        }
        private void Detected(Transform target)
        { tutorial?.Offer(EscapeUNIFRANZ.UI.TutorialAction.AvoidThreat); visibleTarget=target; lastSeen=target.position; State=MobileThreatState.Suspicious; transitionAt=Time.time+0.25f; Contact(false); feedback?.Show(ThreatVisualState.Alert); mover.Investigate(lastSeen,0f); }
        private void Lost()
        {
            visibleTarget=null; if (!operational) return;
            tutorial?.Complete(EscapeUNIFRANZ.UI.TutorialAction.AvoidThreat);
            State=MobileThreatState.Search; Contact(false); feedback?.Show(ThreatVisualState.Patrol); transitionAt=Time.time+chaseSeconds; mover.Investigate(lastSeen,chaseSpeed*0.5f);
        }
        public void ResetEncounter()
        {
            impactTime = -1f;
            movementTime = 0f;
            visibleTarget = null;
            if (disabledUntil > 0f) operational = operationalBeforeSuspend;
            distractionUntil = disabledUntil = startupUntil = nextDistractionAt = chaseUntil = 0f;
            if (mover != null) mover.enabled = operational;
            sensor?.ClearTarget();
            if (sensor != null) sensor.enabled = operational && startupUntil <= 0f;
            mover?.ResetEncounter();
            State = operational ? MobileThreatState.Patrol : MobileThreatState.Disabled;
            transitionAt = 0f;
            Contact(false);
            feedback?.Show(operational ? ThreatVisualState.Patrol : ThreatVisualState.Disabled);
        }
        private void OnDestroy()
        {
            if (sensor!=null) { sensor.TargetDetected-=Detected; sensor.TargetLost-=Lost; }
            spriteSet?.Dispose();
        }
    }
}
