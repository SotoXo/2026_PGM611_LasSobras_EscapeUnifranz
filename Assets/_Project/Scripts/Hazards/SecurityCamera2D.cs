using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.World;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using System.Linq;
using System.Collections.Generic;
namespace EscapeUNIFRANZ.Hazards
{
    public sealed class SecurityCamera2D : MonoBehaviour, IZoneRuntimeInitializable
    {
        public DetectionSensor2D sensor;
        public Light2D fieldLight;
        public Light2D ledLight;
        public float baseAngle, sweepDegrees=24f, sweepSpeed=0.35f, phase;
        public GameFlagId disabledFlag=GameFlagId.SecurityNodeDisabled;
        public GameFlagId enabledAfterFlag;
        public UnityEvent onAlert=new UnityEvent(), onClear=new UnityEvent();
        public AudioSource alertAudio;
        private GameSessionController session;
        private GameplayModeController mode;
        private bool disabled;
        private float nextMessageAt;
        private float suspendedUntil, rotatedUntil;
        private float remoteRotation;
        public bool notifyNearestRobot;
        private IReadOnlyList<MiniAutoController> robots;
        private float nextReport;
        public static MiniAutoController NearestRobot(IEnumerable<MiniAutoController> units,Vector2 point)=>units.Where(r=>r!=null&&r.CanBeStunned).OrderBy(r=>((Vector2)r.transform.position-point).sqrMagnitude).FirstOrDefault();
        private void Report(Transform target){if(!notifyNearestRobot||target==null||robots==null)return;NearestRobot(robots,target.position)?.RespondToCamera(target.position);nextReport=Time.time+1f;}
        private Narrative.WorldHorrorDirector director;
        public string Status => disabled ? "OFF / AISLADA" : Time.time<suspendedUntil ? "SUSPENDIDA "+Mathf.CeilToInt(suspendedUntil-Time.time)+" s" : sensor.HasTarget ? "ALERT / SUBJECT_DETECTED" : "ONLINE / BARRIDO";
        public void DisableTemporarily(float seconds)
        { suspendedUntil=Time.time+Mathf.Clamp(seconds,1f,15f); sensor.enabled=false; sensor.ClearTarget(); Lost(); }
        public void RotateTemporarily(float degrees,float seconds)
        { remoteRotation=degrees; rotatedUntil=Time.time+Mathf.Clamp(seconds,1f,12f); }
        public void ResetLink() { rotatedUntil=0f; remoteRotation=0f; DisableTemporarily(3f); }
        public void Initialize(ZoneRuntimeContext context)
        {
            session=context.Session; mode=context.GameplayMode; director=context.Zone.GetComponentInChildren<Narrative.WorldHorrorDirector>(true);
            robots=context.Zone.Robots;
            sensor.TargetDetected+=Detected; sensor.TargetLost+=Lost; session.State.FlagChanged+=FlagChanged; ApplyState();
        }
        private void Update()
        {
            if (disabled || mode==null || !mode.AllowsMovement) return;
            bool suspended=Time.time<suspendedUntil;
            if (sensor.enabled==suspended) { sensor.enabled=!suspended; Lost(); }
            if (suspended) return;
            float angle=baseAngle+(Time.time<rotatedUntil ? remoteRotation : 0f)+Mathf.Sin(Time.time*sweepSpeed+phase)*sweepDegrees;
            sensor.transform.localRotation=Quaternion.Euler(0f,0f,angle);
            if (fieldLight!=null) fieldLight.transform.localRotation=sensor.transform.localRotation;
            if(notifyNearestRobot&&sensor.HasTarget&&Time.time>=nextReport)Report(sensor.CurrentTarget);
        }
        private void Detected(Transform _)
        {
            Report(_);
            Narrative.ProjectAudioHooks.EmitAt(Narrative.AudioCue.CameraDetection,transform.position);
            GetComponent<MinimapTrackable2D>()?.Discover();
            if (fieldLight!=null) { fieldLight.color=new Color(1f,0.08f,0.06f); fieldLight.intensity=0.65f; }
            if (ledLight!=null) { ledLight.color=new Color(1f,0.03f,0.02f); ledLight.intensity=0.6f; }
            if (Time.time>=nextMessageAt) { session.ShowMessage("IA: SUJETO DETECTADO."); director?.Alarm(); nextMessageAt=Time.time+18f; }
            if (alertAudio!=null && alertAudio.clip!=null) alertAudio.Play(); onAlert.Invoke();
        }
        private void Lost()
        {
            if (fieldLight!=null) { fieldLight.color=notifyNearestRobot?new Color(1f,.72f,.12f):new Color(0.7f,0.15f,0.12f); fieldLight.intensity=disabled || Time.time<suspendedUntil ? 0f : notifyNearestRobot ? .22f : .13f; }
            if (ledLight!=null){ledLight.intensity=disabled || Time.time<suspendedUntil ? 0f : 0.25f;if(notifyNearestRobot)ledLight.color=Color.green;}
            onClear.Invoke();
        }
        private void FlagChanged(GameFlagId flag) { if (flag==disabledFlag || flag==enabledAfterFlag) ApplyState(); }
        private void ApplyState()
        { disabled=session.State.HasFlag(disabledFlag) || (enabledAfterFlag!=GameFlagId.None && !session.State.HasFlag(enabledAfterFlag)); sensor.enabled=!disabled && Time.time>=suspendedUntil; Lost(); }
        private void OnDestroy()
        {
            if (sensor!=null) { sensor.TargetDetected-=Detected; sensor.TargetLost-=Lost; }
            if (session!=null) session.State.FlagChanged-=FlagChanged;
        }
    }
}
