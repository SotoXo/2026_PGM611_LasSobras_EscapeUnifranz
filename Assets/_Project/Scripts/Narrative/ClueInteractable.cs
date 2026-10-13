using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.World;
using UnityEngine;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed class ClueInteractable : InteractableBehaviour, IZoneRuntimeInitializable, IScanStateProvider
    {
        public ClueData clue;
        private NarrativeController controller;
        public string ScanStateText => "[REGISTRO DISPONIBLE]";
        public void Initialize(ZoneRuntimeContext context) { controller=context.Session.GetComponent<NarrativeController>(); }
        protected override void PerformInteraction()
        {
            if (clue==null || controller==null) return;
            BoundSession.State.DiscoverWorldItem("clue",clue.id);
            BoundSession.State.SetFlag(clue.associatedFlag);
            GetComponent<MinimapTrackable2D>()?.Discover();
            controller.ShowClue(clue);
        }
    }
}
