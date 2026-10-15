using System;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.World;
using UnityEngine;
namespace EscapeUNIFRANZ.Narrative
{
    [Serializable] public class SecurityFeed
    {
        public string name;
        [TextArea] public string schematic;
        public ClueData clue;
        public SecurityCamera2D camera;
        public bool noSignal;
    }
    public sealed class SecurityTerminalInteractable : InteractableBehaviour, IZoneRuntimeInitializable
    {
        public string terminalId;
        public GameFlagId powerFlag;
        public SecurityFeed[] feeds;
        public AudioSource optionalAudio;
        public AudioClip switchSound;
        public MiniAutoController vigilanceUnit, observedUnit;
        public bool IsolateUnit()
        {
            if (vigilanceUnit==null || !State.DiscoverWorldItem("cctv_robot",terminalId)) return false;
            vigilanceUnit.SuspendFor(10f); return true;
        }
        private NarrativeController controller;
        public GameState State => BoundSession.State;
        public bool Online => powerFlag==GameFlagId.None || State.HasFlag(powerFlag);
        public override string Prompt => Online ? "[E] Usar CCTV" : "[E] Revisar terminal sin energía";
        public bool CanDisable => !State.IsWorldItemDiscovered("cctv_control",terminalId);
        public void Initialize(ZoneRuntimeContext context) { controller=context.Session.GetComponent<NarrativeController>(); }
        protected override void PerformInteraction()
        {
            if (!Online) { BoundSession.ShowMessage("OFF — ENERGÍA ERROR. Recupera primero el suministro del sector."); return; }
            if(State.SetFlag(GameFlagId.CampusPlanRecovered)) controller?.TeachOnce("map","PLANO DEL CAMPUS RECUPERADO. M — Mapa. Selecciona sectores conocidos para consultar detalles.");
            GetComponent<EscapeUNIFRANZ.Map.MinimapTrackable2D>()?.Discover();
            controller?.ShowSecurity(this);
        }
        public void Read(int index)
        {
            var feed=feeds[index];
            if (feed.noSignal)
            { ProjectAudioHooks.Emit(AudioCue.CameraSignalLost); if (State.DiscoverWorldItem("event","corrupt_feed_"+terminalId)) BoundSession.GetComponentInChildren<ScreenGlitchController>(true)?.Pulse(); return; }
            if (feed.clue!=null) { State.DiscoverWorldItem("clue",feed.clue.id); State.SetFlag(feed.clue.associatedFlag); }
            if (optionalAudio!=null && switchSound!=null) optionalAudio.PlayOneShot(switchSound);
            ProjectAudioHooks.Emit(AudioCue.CameraFeed);
            if(index==1) State.DiscoverWorldItem("sector","hall");
            if(feed.camera!=null) { var marker=feed.camera.GetComponent<EscapeUNIFRANZ.Map.MinimapTrackable2D>(); if(marker!=null) { State.RecordMapPoint(State.CurrentZoneId,marker.MarkerId,"Cámara CCTV",KnownPointKind.Camera,feed.camera.Status); marker.RevealThreatFor(4f); } }
            observedUnit?.GetComponent<EscapeUNIFRANZ.Map.MinimapTrackable2D>()?.RevealThreatFor(4f);
        }
        // World time is paused in the terminal. The 15 second budget starts on leaving it.
        public bool Disable(int index)
        {
            if (!CanDisable || feeds[index].camera==null || feeds[index].noSignal) return false;
            State.DiscoverWorldItem("cctv_control",terminalId);
            feeds[index].camera.DisableTemporarily(15f); return true;
        }
        public bool Rotate(int index)
        {
            if (feeds[index].noSignal || feeds[index].camera==null || !State.DiscoverWorldItem("cctv_rotate",terminalId)) return false;
            feeds[index].camera.RotateTemporarily(90f,12f); return true;
        }
        public bool ResetLink(int index)
        {
            if (feeds[index].noSignal || feeds[index].camera==null || !State.DiscoverWorldItem("cctv_reset",terminalId)) return false;
            feeds[index].camera.ResetLink(); return true;
        }
    }
}
