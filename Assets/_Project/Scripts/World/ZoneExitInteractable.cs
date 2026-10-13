using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace EscapeUNIFRANZ.World
{
    /// <summary>
    /// Reusable contextual exit configured with stable destination IDs.
    /// </summary>
    public sealed class ZoneExitInteractable : InteractableBehaviour, IScanStateProvider
    {
        [SerializeField] private string targetZoneId;
        [SerializeField] private string targetSpawnId;
        [SerializeField] private GameFlagId requiredFlag;
        [SerializeField] private DoorLockState lockState;
        [SerializeField, TextArea] private string lockedMessage = "Acceso restringido.";
        [SerializeField] private AudioSource optionalAudio;
        [SerializeField] private AudioClip openSound, lockedSound;
        public UnityEvent onOpened=new UnityEvent(), onLocked=new UnityEvent();

        private SceneFlowController sceneFlow;
        private GameSessionController session;
        private float alarmLockUntil;
        public SpriteRenderer statusIndicator;
        public void LockForAlarm(float seconds) { alarmLockUntil=Mathf.Max(alarmLockUntil,Time.time+seconds); }
        private void Update() { if (statusIndicator!=null) statusIndicator.color=IsUnlocked ? new Color(0.45f,1f,0.7f) : new Color(1f,0.25f,0.18f); }

        public GameFlagId RequiredFlag => requiredFlag;
        public string TargetZoneId => targetZoneId;
        public bool IsUnlocked => Time.time>=alarmLockUntil && lockState != DoorLockState.Locked && RequirementsMet &&
            (requiredFlag == GameFlagId.None || (session != null && session.State.HasFlag(requiredFlag)));
        public string ScanStateText => Time.time<alarmLockUntil ? "[LOCKED / ALARMA TEMPORAL]" : IsUnlocked ? "[ENERGÍA OK / RED OK / AUTORIZACIÓN OK]" :
            "[ENERGÍA "+(session!=null && session.State.HasFlag(GameFlagId.PowerEntranceRestored) ? "OK" : "ERROR")+" / RED "+(requiredFlag==GameFlagId.None || (session!=null && session.State.HasFlag(requiredFlag)) ? "OK" : "ERROR")+" / AUTORIZACIÓN "+(RequirementsMet ? "OK" : "ERROR")+"]";

        public void Bind(SceneFlowController controller, GameSessionController gameSession)
        {
            sceneFlow = controller;
            session = gameSession;
            BindSession(gameSession);
        }

        protected override void PerformInteraction()
        {
            if (sceneFlow == null)
            {
                Debug.LogError($"{nameof(ZoneExitInteractable)} '{name}' is not bound to SceneFlow.", this);
                return;
            }


            if (!IsUnlocked)
            {
                EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.DoorLock);
                session?.ShowMessage(ScanStateText+" "+lockedMessage);
                if (optionalAudio!=null && lockedSound!=null) optionalAudio.PlayOneShot(lockedSound);
                onLocked.Invoke();
                return;
            }

            if (sceneFlow.GoToZone(targetZoneId, targetSpawnId))
            { EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.DoorUnlock); EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.DoorOpen); if (optionalAudio!=null && openSound!=null) optionalAudio.PlayOneShot(openSound); onOpened.Invoke(); }
        }
        protected override void OnRequirementsBlocked(string message)
        { session?.ShowMessage(ScanStateText+" "+message); if (optionalAudio!=null && lockedSound!=null) optionalAudio.PlayOneShot(lockedSound); onLocked.Invoke(); }
    }
}
