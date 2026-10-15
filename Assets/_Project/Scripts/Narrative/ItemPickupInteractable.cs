using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.World;
using UnityEngine;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed class ItemPickupInteractable : InteractableBehaviour, IZoneRuntimeInitializable, IScanStateProvider
    {
        public string pickupId, itemId;
        public ItemData item;
        public string ItemId => item!=null ? item.id : itemId;
        public override string Prompt => "[E] Recoger "+Label(ItemId);
        public string ScanStateText => "[RECOLECTABLE]";
        public void Initialize(ZoneRuntimeContext context)
        { if (context.Session.State.IsWorldItemDiscovered("pickup",pickupId) || context.Session.State.HasItem(ItemId) || context.Session.State.IsWorldItemDiscovered("acquired",ItemId)) gameObject.SetActive(false); }
        protected override void PerformInteraction()
        {
            if (BoundSession.State.TryCollectPickup(pickupId,ItemId))
            { ProjectAudioHooks.Emit(AudioCue.Pickup); BoundSession.GetComponent<NarrativeController>()?.NotifyPickup(ItemId); GetComponent<EscapeUNIFRANZ.Map.MinimapTrackable2D>()?.Record("Recogido"); gameObject.SetActive(false); }
        }
        public static string Label(string id)
        { return ItemData.Label(id); }
    }
}
