using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.World;
using UnityEngine;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed class ItemPickupInteractable : InteractableBehaviour, IZoneRuntimeInitializable, IScanStateProvider
    {
        public string pickupId, itemId;
        public string ScanStateText => "[RECOLECTABLE]";
        public void Initialize(ZoneRuntimeContext context)
        { if (context.Session.State.IsWorldItemDiscovered("pickup",pickupId)) gameObject.SetActive(false); }
        protected override void PerformInteraction()
        {
            if (BoundSession.State.TryCollectPickup(pickupId,itemId))
            { ProjectAudioHooks.Emit(AudioCue.Pickup); BoundSession.ShowMessage("Recogiste: "+Label(itemId)+"."); gameObject.SetActive(false); }
        }
        public static string Label(string id)
        { switch(id) { case "battery":return "batería";case "key":return "llave";case "flashlight":return "linterna";case "lab_card":return "tarjeta de laboratorio";case "maintenance_card":return "tarjeta de mantenimiento";case "network_module":return "módulo de red";case "fuse":return "fusible de respaldo";default:return id; } }
    }
}
