using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.World;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
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
        }
        private void Detected(Transform _)
        {
            GetComponent<MinimapTrackable2D>()?.Discover();
            if (fieldLight!=null) { fieldLight.color=new Color(1f,0.08f,0.06f); fieldLight.intensity=0.65f; }
            if (ledLight!=null) { ledLight.color=new Color(1f,0.03f,0.02f); ledLight.intensity=0.6f; }
            if (Time.time>=nextMessageAt) { session.ShowMessage("IA: SUJETO DETECTADO."); director?.Alarm(); nextMessageAt=Time.time+18f; }
            if (alertAudio!=null && alertAudio.clip!=null) alertAudio.Play(); onAlert.Invoke();
        }
        private void Lost()
        {
            if (fieldLight!=null) { fieldLight.color=new Color(0.7f,0.15f,0.12f); fieldLight.intensity=disabled || Time.time<suspendedUntil ? 0f : 0.13f; }
            if (ledLight!=null) ledLight.intensity=disabled || Time.time<suspendedUntil ? 0f : 0.25f;
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
