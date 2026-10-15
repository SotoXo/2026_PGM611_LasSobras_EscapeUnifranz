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
        public UnityEngine.Rendering.Universal.Light2D statusLight;
        public GameFlagId energyRequirement,networkRequirement,authorizationRequirement;
        private bool previousUnlocked;
        private bool statusInitialized;
        public void LockForAlarm(float seconds) { alarmLockUntil=Mathf.Max(alarmLockUntil,Time.time+seconds); }
        private void Update()
        {
            bool open=IsUnlocked; var color=open ? new Color(.45f,1f,.7f) : new Color(1f,.25f,.18f);
            if(statusIndicator!=null) statusIndicator.color=color; if(statusLight!=null) { statusLight.color=color; statusLight.intensity=.25f; }
            if(statusInitialized && open && !previousUnlocked) EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.DoorUnlock);
            previousUnlocked=open; statusInitialized=true;
        }
        public override string Prompt => IsUnlocked ? (!string.IsNullOrEmpty(RequiredItem) ? "[E] Usar "+EscapeUNIFRANZ.Narrative.ItemData.Label(RequiredItem) : "[E] Abrir") : "[E] Revisar acceso";
        public EscapeUNIFRANZ.Map.MinimapExitState MapExitState => IsUnlocked ? EscapeUNIFRANZ.Map.MinimapExitState.Available : Time.time<alarmLockUntil || lockState==DoorLockState.Locked || lockState==DoorLockState.DisabledByAI ? EscapeUNIFRANZ.Map.MinimapExitState.Blocked : EscapeUNIFRANZ.Map.MinimapExitState.Conditional;
        public string MapStatus => IsUnlocked ? "Disponible" : Time.time<alarmLockUntil ? "Alarma temporal" : "Requiere condición";

        public GameFlagId RequiredFlag => requiredFlag;
        public string TargetZoneId => targetZoneId;
        public bool IsUnlocked => Time.time>=alarmLockUntil && lockState != DoorLockState.Locked && RequirementsMet &&
            (requiredFlag == GameFlagId.None || (session != null && session.State.HasFlag(requiredFlag)));
        private bool Meets(GameFlagId flag)=>flag==GameFlagId.None || session?.State.HasFlag(flag)==true;
        private string ReadableBlock(string fallback)
        {
            if(Time.time<alarmLockUntil) return "Alarma temporal. Espera a que el acceso responda.";
            if(!Meets(energyRequirement)) return "Sin energía. Busca la alimentación del acceso.";
            if(!Meets(networkRequirement)) return "Sin conexión con el controlador. Revisa la red del sector.";
            if(!string.IsNullOrEmpty(RequiredItem) && session?.State.HasItem(RequiredItem)!=true) return "Se requiere "+EscapeUNIFRANZ.Narrative.ItemData.Label(RequiredItem)+".";
            if(!Meets(authorizationRequirement)) return "El lector todavía no autoriza el acceso.";
            return fallback;
        }
        public string ScanStateText => Time.time<alarmLockUntil ? "[BLOQUEADA / ALARMA TEMPORAL]" :
            "ENERGÍA "+(Meets(energyRequirement) ? "✓" : "✕")+" / RED "+(Meets(networkRequirement) ? "✓" : "✕")+" / AUTORIZACIÓN "+(Meets(authorizationRequirement) && (string.IsNullOrEmpty(RequiredItem) || session?.State.HasItem(RequiredItem)==true) ? "✓" : "✕");

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
                session?.ShowMessage(ReadableBlock(lockedMessage)+"\n"+ScanStateText);
                if (optionalAudio!=null && lockedSound!=null) optionalAudio.PlayOneShot(lockedSound);
                onLocked.Invoke();
                return;
            }

            if (sceneFlow.GoToZone(targetZoneId, targetSpawnId))
            { EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.DoorUnlock); EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.DoorOpen); if (optionalAudio!=null && openSound!=null) optionalAudio.PlayOneShot(openSound); onOpened.Invoke(); }
        }
        protected override void OnRequirementsBlocked(string message)
        { session?.ShowMessage(ReadableBlock(message)+"\n"+ScanStateText); if (optionalAudio!=null && lockedSound!=null) optionalAudio.PlayOneShot(lockedSound); onLocked.Invoke(); }
    }
}
