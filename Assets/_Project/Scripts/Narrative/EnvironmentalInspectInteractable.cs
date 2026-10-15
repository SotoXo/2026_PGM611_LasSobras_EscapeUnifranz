using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.World;
using UnityEngine;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed class EnvironmentalInspectInteractable : InteractableBehaviour, IZoneRuntimeInitializable
    {
        public string explanation,eventId;
        public bool corruptedMonitor;
        public bool entrancePowerInspection;
        public SpriteRenderer monitor;
        private WorldHorrorDirector horror;
        public void Initialize(ZoneRuntimeContext context) { horror=context.Zone.GetComponentInChildren<WorldHorrorDirector>(true); if(corruptedMonitor && context.Session.State.IsWorldItemDiscovered("event",eventId) && monitor!=null) monitor.color=new Color(.12f,.15f,.17f); }
        protected override void PerformInteraction()
        { GetComponent<EscapeUNIFRANZ.Map.MinimapTrackable2D>()?.Discover(); if(entrancePowerInspection) BoundSession.State.SetFlag(EscapeUNIFRANZ.Core.GameFlagId.EntrancePanelInspected); BoundSession.ShowMessage(explanation); if(corruptedMonitor) horror?.InspectCorruptedMonitor(monitor,eventId); }
    }
}
