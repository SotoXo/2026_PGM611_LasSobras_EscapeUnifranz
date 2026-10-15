using System;
using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Narrative;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace EscapeUNIFRANZ.World
{
    public enum RoomLightingState { Normal, Dim, Flicker, Blackout, Emergency, Restored }
    public enum RoomFailurePattern { Mild, Double, Strong }
    [Serializable] public struct RoomLightingStep
    {
        public RoomLightingState state;
        [Min(.05f)] public float seconds;
        public int failedFixtures;
        public RoomLightingStep(RoomLightingState state,float seconds,int failed=0) { this.state=state; this.seconds=Mathf.Max(.05f,seconds); failedFixtures=failed; }
    }
    public static class RoomLightingLogic
    {
        public static bool ClaimEntry(GameState state,string id,bool oneShot)=>state!=null && (!oneShot || state.DiscoverWorldItem("room_event",id));
        public static float Level(RoomLightingState state,float normal,float dim)
        {
            switch(state)
            {
                case RoomLightingState.Blackout:return 0;
                case RoomLightingState.Dim:return dim;
                case RoomLightingState.Emergency:return normal*.48f;
                case RoomLightingState.Flicker:return normal*.65f;
                case RoomLightingState.Restored:return normal*.78f;
                default:return normal;
            }
        }
        public static RoomLightingStep[] Reading(float delay,float hold,RoomFailurePattern pattern,float blackout)
        {
            var steps=new List<RoomLightingStep>{new RoomLightingStep(RoomLightingState.Dim,delay),new RoomLightingStep(RoomLightingState.Normal,hold)};
            if(pattern==RoomFailurePattern.Strong)
            { steps.Add(new RoomLightingStep(RoomLightingState.Blackout,.4f)); steps.Add(new RoomLightingStep(RoomLightingState.Flicker,.3f)); }
            else
            { steps.Add(new RoomLightingStep(RoomLightingState.Flicker,.22f,1)); if(pattern==RoomFailurePattern.Double) { steps.Add(new RoomLightingStep(RoomLightingState.Normal,.24f)); steps.Add(new RoomLightingStep(RoomLightingState.Flicker,.26f,2)); } }
            if(blackout>0) steps.Add(new RoomLightingStep(RoomLightingState.Blackout,blackout));
            steps.Add(new RoomLightingStep(RoomLightingState.Dim,.35f)); return steps.ToArray();
        }
        public static bool AmbientVisibility(Rect room,Vector2 observer,Vector2 target,float fillLevel)
            =>fillLevel>=.16f && room.Contains(observer) && room.Contains(target);
    }
    public sealed class RoomLightingTimeline
    {
        private readonly RoomLightingStep[] steps;
        private float elapsed;
        public int Index { get; private set; }
        public bool Running=>Index<steps.Length;
        public RoomLightingStep Current=>steps[Mathf.Min(Index,steps.Length-1)];
        public RoomLightingTimeline(RoomLightingStep[] source) { steps=source ?? Array.Empty<RoomLightingStep>(); }
        public bool Tick(float seconds)
        {
            int old=Index; elapsed+=Mathf.Max(0,seconds);
            while(Running && elapsed>=Mathf.Max(.05f,steps[Index].seconds)) { elapsed-=Mathf.Max(.05f,steps[Index].seconds); Index++; }
            return old!=Index;
        }
    }
    [DisallowMultipleComponent]
    public sealed class RoomLightingController : MonoBehaviour, IZoneRuntimeInitializable
    {
        public string roomId,displayName;
        public Rect roomBounds;
        public Light2D[] mainLights,fillLights,auxiliaryLights;
        public GroupLampFlicker2D[] fixtures;
        public RoomLightingState initialState=RoomLightingState.Dim,afterEntry=RoomLightingState.Dim;
        public GameFlagId restoreFlag;
        public bool restoredByDirector,entryEvent=true,oneShot=true;
        public Vector2 revealSeconds=new Vector2(1.0f,2.0f);
        public float enterDelay=.7f,blackoutSeconds;
        public RoomFailurePattern failurePattern;
        public float normalIntensity=.36f,dimIntensity=.07f;
        public Color normalColor=new Color(.8f,.9f,1),emergencyColor=new Color(1,.08f,.04f);
        public int defectiveFixture=-1;
        private static readonly List<RoomLightingController> active=new List<RoomLightingController>();
        private GameState state;
        private GameplayModeController mode;
        private Transform player;
        private string zoneId;
        private RoomLightingTimeline timeline;
        private RoomLightingState finalState;
        private bool entered,wasInside;
        private float emergencyRemaining;
        private RoomLightingState preEmergency;
        private int eventCount;
        public RoomLightingState CurrentState { get; private set; }
        public float FillLevel { get; private set; }
        public bool SequenceRunning=>timeline!=null && timeline.Running;
        public bool Initialized=>state!=null;
        private void OnEnable() { if(!active.Contains(this)) active.Add(this); }
        private void OnDisable() { active.Remove(this); }
        public void Initialize(ZoneRuntimeContext context)
        {
            state=context.Session.State; mode=context.GameplayMode; zoneId=context.Zone.ZoneId;
            player=context.Session.GetComponentInChildren<EscapeUNIFRANZ.Player.PlayerMovement2D>(true)?.transform;
            state.FlagChanged+=Changed; entered=oneShot && state.IsWorldItemDiscovered("room_event",EventId);
            ApplyState(state.HasFlag(restoreFlag) ? RoomLightingState.Restored : entered ? afterEntry : initialState);
        }
        private string EventId=>zoneId+"/"+roomId;
        public bool TryBeginEntry()
        {
            if(!entryEvent || entered && oneShot || SequenceRunning || emergencyRemaining>0 || state==null || state.HasFlag(restoreFlag)) return false;
            if(!RoomLightingLogic.ClaimEntry(state,EventId,oneShot)) return false;
            entered=true; eventCount++;
            ProjectAudioHooks.Emit(AudioCue.ElectricBuzz);
            // Instance-local deterministic variation; never changes a global random stream.
            int seed=17; foreach(char c in EventId) seed=unchecked(seed*31+c); var random=new System.Random(unchecked(seed+eventCount*397));
            float hold=Mathf.Lerp(Mathf.Max(.5f,revealSeconds.x),Mathf.Max(.5f,revealSeconds.y),(float)random.NextDouble());
            Begin(RoomLightingLogic.Reading(enterDelay,hold,failurePattern,blackoutSeconds),afterEntry); return true;
        }
        public void BeginRestorationReading()
        {
            Begin(new[]{new RoomLightingStep(RoomLightingState.Normal,1.8f),new RoomLightingStep(RoomLightingState.Flicker,.28f,1),new RoomLightingStep(RoomLightingState.Flicker,.3f,2),new RoomLightingStep(RoomLightingState.Blackout,.35f),new RoomLightingStep(RoomLightingState.Dim,.4f)},RoomLightingState.Restored);
            ProjectAudioHooks.Emit(AudioCue.LightRestore);
        }
        public void BlackoutThenRecover(float seconds)
        { Begin(new[]{new RoomLightingStep(RoomLightingState.Blackout,Mathf.Max(.4f,seconds)),new RoomLightingStep(RoomLightingState.Flicker,.3f)},state!=null && state.HasFlag(restoreFlag) ? RoomLightingState.Restored : afterEntry); }
        public void EmergencyFor(float seconds)
        { if(emergencyRemaining<=0) preEmergency=CurrentState; emergencyRemaining=Mathf.Max(emergencyRemaining,seconds); ApplyState(RoomLightingState.Emergency); }
        private void Begin(RoomLightingStep[] steps,RoomLightingState settled)
        { timeline=new RoomLightingTimeline(steps); finalState=settled; ApplyStep(timeline.Current); }
        private void Update()
        {
            if(state==null || mode==null || !mode.AllowsMovement) return;
            bool inside=player!=null && roomBounds.Contains(player.position);
            if(inside && !wasInside) TryBeginEntry(); wasInside=inside;
            if(emergencyRemaining>0)
            { emergencyRemaining-=Time.deltaTime; if(emergencyRemaining<=0) { if(SequenceRunning) ApplyStep(timeline.Current); else ApplyState(preEmergency); } return; }
            if(SequenceRunning && timeline.Tick(Time.deltaTime))
            { if(timeline.Running) ApplyStep(timeline.Current); else { timeline=null; ApplyState(finalState); } }
        }
        private void ApplyStep(RoomLightingStep step)
        {
            ApplyState(step.state,step.failedFixtures,true);
            if(step.state==RoomLightingState.Blackout) ProjectAudioHooks.Emit(AudioCue.Blackout);
            else if(step.state==RoomLightingState.Flicker) ProjectAudioHooks.Emit(AudioCue.Flicker);
            else if(step.state==RoomLightingState.Normal) ProjectAudioHooks.Emit(AudioCue.Electricity);
        }
        private void ApplyState(RoomLightingState value,int failed=0,bool sequenced=false)
        {
            CurrentState=value; float level=RoomLightingLogic.Level(value,normalIntensity,dimIntensity);
            Color color=value==RoomLightingState.Emergency ? emergencyColor : normalColor;
            for(int i=0;i<mainLights.Length;i++) if(mainLights[i]!=null)
            {
                float power=i<failed ? 0 : level; mainLights[i].color=color;
                var fixture=i<fixtures.Length ? fixtures[i] : null;
                if(fixture!=null) { if(!sequenced && i==defectiveFixture && value!=RoomLightingState.Blackout && value!=RoomLightingState.Emergency) fixture.SetAutomaticPower(power); else fixture.SetExternalPower(power,value==RoomLightingState.Flicker); }
                mainLights[i].intensity=power;
            }
            float fraction=mainLights.Length==0 ? 1f : Mathf.Clamp01((float)(mainLights.Length-failed)/mainLights.Length);
            FillLevel=level*.6f*fraction;
            foreach(var light in fillLights) if(light!=null) { light.color=color; light.intensity=FillLevel; }
            foreach(var light in auxiliaryLights) if(light!=null) { light.color=value==RoomLightingState.Emergency ? emergencyColor : normalColor; light.intensity=value==RoomLightingState.Blackout ? .015f : .06f; }
        }
        private void Changed(GameFlagId flag) { if(flag==restoreFlag && !restoredByDirector) { BeginRestorationReading(); } }
        public static bool IsAmbientlyVisible(Vector2 observer,Vector2 target)
        { foreach(var room in active) if(room!=null && room.isActiveAndEnabled && RoomLightingLogic.AmbientVisibility(room.roomBounds,observer,target,room.FillLevel)) return true; return false; }
        private void OnDestroy() { active.Remove(this); if(state!=null) state.FlagChanged-=Changed; }
    }
}
