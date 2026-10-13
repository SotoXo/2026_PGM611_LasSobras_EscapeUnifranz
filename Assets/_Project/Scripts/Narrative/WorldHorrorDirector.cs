using System.Collections;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed class WorldHorrorDirector : MonoBehaviour, IZoneRuntimeInitializable
    {
        public MiniAutoController firstRobot;
        public RobotDogEncounterController alarmDog;
        public Light2D[] powerLights, alarmLights;
        public ZoneExitInteractable[] alarmDoors;
        public EncounterContext firstEncounter;
        public GameFlagId localPowerFlag;
        public AudioSource optionalAudio;
        public AudioClip powerSound, alarmSound, robotSound;
        public AudioSource ambientAudio;
        public AudioClip electricalBuzz;
        private GameSessionController session;
        private ScreenGlitchController glitch;
        private float alarmUntil;
        private Color[] originalColors;
        private float[] originalIntensity;
        private bool sequenceRunning;
        private GameplayModeController mode;
        public void Initialize(ZoneRuntimeContext context)
        {
            session=context.Session; mode=context.GameplayMode; glitch=session.GetComponentInChildren<ScreenGlitchController>(true);
            ProjectAudioHooks.Emit(AudioCue.ElectricalHum); ProjectAudioHooks.Emit(context.Zone.ZoneId=="nucleo_ia" ? AudioCue.MusicCore : AudioCue.MusicExplore);
            if (ambientAudio!=null && electricalBuzz!=null) { ambientAudio.clip=electricalBuzz; ambientAudio.loop=true; ambientAudio.Play(); }
            if ((context.Zone.ZoneId=="piso3" || context.Zone.ZoneId=="nucleo_ia") && session.State.DiscoverWorldItem("event","corruption_enter_"+context.Zone.ZoneId)) StartCoroutine(ArrivalPulse());
            originalColors=new Color[alarmLights.Length]; originalIntensity=new float[alarmLights.Length];
            for(int i=0;i<alarmLights.Length;i++) if (alarmLights[i]!=null) { originalColors[i]=alarmLights[i].color; originalIntensity[i]=alarmLights[i].intensity; }
            session.State.FlagChanged+=Changed;
            if (localPowerFlag!=GameFlagId.None) foreach(var light in powerLights) if (light!=null)
                light.intensity=session.State.HasFlag(localPowerFlag) ? 0.42f : 0f;
            if (firstRobot!=null)
            {
                bool active=session.State.HasFlag(GameFlagId.FirstThreatActivated);
                firstRobot.SetOperational(active,!active);
                if (!session.State.HasFlag(GameFlagId.PowerEntranceRestored)) foreach(var lamp in powerLights) if (lamp!=null) { var f=lamp.GetComponent<GroupLampFlicker2D>(); if (f!=null) f.enabled=false; lamp.intensity=0f; }
                else if (!active) StartCoroutine(PowerSequence());
            }
        }
        private void Changed(GameFlagId flag)
        {
            if (flag==GameFlagId.PowerEntranceRestored && firstRobot!=null && !sequenceRunning) StartCoroutine(PowerSequence());
            else if (flag==localPowerFlag && localPowerFlag!=GameFlagId.None) StartCoroutine(RestoreSector());
            else if (flag==GameFlagId.FrancisEncounterStarted || flag==GameFlagId.FrancisNetworkPhaseComplete || flag==GameFlagId.FrancisHardwarePhaseComplete || flag==GameFlagId.TechnologyNetworkRestored) glitch?.Pulse();
        }
        private IEnumerator ArrivalPulse()
        { while(mode!=null && !mode.AllowsMovement) yield return null; yield return new WaitForSeconds(0.8f); glitch?.Pulse(); }
        private IEnumerator RestoreSector()
        {
            Play(powerSound);
            foreach(var light in powerLights) if (light!=null) { light.intensity=0.42f; yield return new WaitForSeconds(0.25f); }
            glitch?.Pulse(); session.ShowMessage("ENERGÍA OK / RED OK. Los actuadores necesitan validación física.");
        }
        private IEnumerator PowerSequence()
        {
            sequenceRunning=true; Play(powerSound);
            ProjectAudioHooks.Emit(AudioCue.Electricity);
            foreach(var light in powerLights) if (light!=null) { light.intensity=0.42f; yield return new WaitForSeconds(0.25f); }
            session.ShowMessage("ENERGÍA RESTAURADA. El edificio responde parcialmente.");
            yield return new WaitForSeconds(1f);
            glitch?.Pulse(false,true); session.ShowMessage("IA: SUJETO DETECTADO.\nUNIDAD DE INSPECCIÓN — ALIMENTACIÓN EXTERNA. Evita su ruta o corta energía en el panel.");
            session.State.SetFlag(GameFlagId.FirstThreatActivated); firstRobot.SetOperational(true);
            firstEncounter?.BeginEncounter(); Play(robotSound); Alarm(false); sequenceRunning=false;
        }
        public void Alarm(bool activateDog=true)
        {
            if (session==null) return;
            alarmUntil=Time.time+5f; glitch?.Pulse(true); Play(alarmSound);
            ProjectAudioHooks.Emit(AudioCue.RedAlarm); ProjectAudioHooks.Emit(AudioCue.MusicTension);
            foreach(var door in alarmDoors) if (door!=null) door.LockForAlarm(5f);
            if (activateDog && alarmDog!=null) alarmDog.ActivateFromAlarm();
            for(int i=0;i<alarmLights.Length;i++) if (alarmLights[i]!=null)
            { alarmLights[i].GetComponent<GroupLampFlicker2D>()?.SetOverride(true,0.55f); alarmLights[i].color=new Color(1f,0.08f,0.04f); alarmLights[i].intensity=0.55f; }
        }
        private void Update()
        {
            if (alarmUntil<=0f || Time.time<alarmUntil) return;
            alarmUntil=0f;
            for(int i=0;i<alarmLights.Length;i++) if (alarmLights[i]!=null)
            { alarmLights[i].GetComponent<GroupLampFlicker2D>()?.SetOverride(false); alarmLights[i].color=originalColors[i]; alarmLights[i].intensity=originalIntensity[i]; }
        }
        private void OnDestroy() { if (session!=null) session.State.FlagChanged-=Changed; }
        private void Play(AudioClip sound) { if (optionalAudio!=null && sound!=null) optionalAudio.PlayOneShot(sound); }
    }
}
