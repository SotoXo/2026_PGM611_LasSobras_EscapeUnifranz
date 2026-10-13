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
        private GameplayModeController mode;
        private float transitionAt;
        private Transform visibleTarget;
        private Vector2 lastSeen;
        private bool operational=true;
        private float disabledUntil;
        private float distractionUntil;
        private SpriteRenderer[] bodyVisuals;
        private void Awake() { bodyVisuals=GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.enabled).ToArray(); }
        public MobileThreatState State { get; private set; }=MobileThreatState.Patrol;
        public string ReadableState => State==MobileThreatState.Disabled ? "DESACTIVADO / DISABLED" : State==MobileThreatState.Chase ? "PERSIGUIENDO" : State==MobileThreatState.Alert || State==MobileThreatState.Suspicious ? "TE VIO / AVISO" : State==MobileThreatState.Search ? "BUSCANDO" : State==MobileThreatState.Return ? "VOLVIENDO" : "PATRULLA";
        public void Initialize(ZoneRuntimeContext context)
        {
            mode=context.GameplayMode;
            Contact(false);
            if (sensor!=null) { sensor.TargetDetected+=Detected; sensor.TargetLost+=Lost; }
        }
        public void SetOperational(bool value,bool hide=false)
        {
            if (!hide) EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(value ? EscapeUNIFRANZ.Narrative.AudioCue.RobotRestart : EscapeUNIFRANZ.Narrative.AudioCue.RobotOff);
            operational=value; State=value ? MobileThreatState.Patrol : MobileThreatState.Disabled;
            mover.enabled=value; sensor.enabled=value;
            Contact(false);
            if (bodyVisuals!=null) foreach(var renderer in bodyVisuals) if (renderer!=null) renderer.enabled=!hide;
            if (feedback!=null) { if (feedback.marker!=null) feedback.marker.gameObject.SetActive(!hide); feedback.Show(value ? ThreatVisualState.Patrol : ThreatVisualState.Disabled); }
            GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            if (!value) { visibleTarget=null; mover.ResumePatrol(); sensor.ClearTarget(); }
        }
        private void Contact(bool value) { foreach(var contact in GetComponentsInChildren<HazardContact2D>(true)) contact.SetContactAllowed(value); }
        public void SuspendFor(float seconds) { disabledUntil=Time.time+Mathf.Max(1f,seconds); SetOperational(false); }
        public void DistractAt(Vector2 point,float seconds)
        {
            if (!operational) return;
            sensor.enabled=false; sensor.ClearTarget(); distractionUntil=Time.time+seconds; visibleTarget=null; lastSeen=point; State=MobileThreatState.Search; transitionAt=Time.time+seconds;
            Contact(false); mover.Investigate(point,1.4f); feedback?.Show(ThreatVisualState.Patrol);
        }
        private void Update()
        {
            if (mode==null || !mode.AllowsMovement) return;
            if (!operational) { if (disabledUntil>0f && Time.time>=disabledUntil) { disabledUntil=0f; SetOperational(true); } return; }
            if (distractionUntil>0f && Time.time>=distractionUntil) { distractionUntil=0f; sensor.enabled=true; }
            if (sensor.transform!=mover.transform && mover.Facing.sqrMagnitude>0f)
                sensor.transform.localRotation=Quaternion.Euler(0f,0f,Vector2.SignedAngle(Vector2.up,mover.Facing));
            if (visibleTarget!=null) lastSeen=visibleTarget.position;
            if (State==MobileThreatState.Chase && visibleTarget!=null) return;
            if (Time.time<transitionAt) return;
            if (State==MobileThreatState.Suspicious && visibleTarget!=null)
            { State=MobileThreatState.Alert; transitionAt=Time.time+0.65f; feedback?.Show(ThreatVisualState.Alert); EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.RobotAlert); if (optionalAudio!=null && alertSound!=null) optionalAudio.PlayOneShot(alertSound); }
            else if (State==MobileThreatState.Alert && visibleTarget!=null)
            { State=MobileThreatState.Chase; Contact(true); feedback?.Show(ThreatVisualState.Attack); EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.RobotChase); EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.MusicChase); mover.SetChaseTarget(visibleTarget,chaseSpeed); }
            else if (State==MobileThreatState.Search)
            { State=MobileThreatState.Return; Contact(false); feedback?.Show(ThreatVisualState.Patrol); mover.ResumePatrol(); transitionAt=Time.time+0.5f; }
            else if (State==MobileThreatState.Return) State=MobileThreatState.Patrol;
        }
        private void Detected(Transform target)
        { visibleTarget=target; lastSeen=target.position; State=MobileThreatState.Suspicious; transitionAt=Time.time+0.25f; Contact(false); feedback?.Show(ThreatVisualState.Alert); mover.Investigate(lastSeen,0f); }
        private void Lost()
        {
            visibleTarget=null; if (!operational) return;
            State=MobileThreatState.Search; Contact(false); feedback?.Show(ThreatVisualState.Patrol); transitionAt=Time.time+chaseSeconds; mover.Investigate(lastSeen,chaseSpeed*0.5f);
        }
        public void ResetEncounter()
        { visibleTarget=null; sensor?.ClearTarget(); mover?.ResetEncounter(); State=operational ? MobileThreatState.Patrol : MobileThreatState.Disabled; transitionAt=0f; Contact(false); feedback?.Show(operational ? ThreatVisualState.Patrol : ThreatVisualState.Disabled); }
        private void OnDestroy()
        { if (sensor!=null) { sensor.TargetDetected-=Detected; sensor.TargetLost-=Lost; } }
    }
}
