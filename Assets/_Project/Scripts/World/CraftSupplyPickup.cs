using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Narrative;
using UnityEngine;
namespace EscapeUNIFRANZ.World
{
    public sealed class CraftSupplyPickup : InteractableBehaviour,IZoneRuntimeInitializable
    {
        public ItemData item;public string pickupId;
        public override string Prompt=>"[E] Recoger "+item.displayName;
        public void Initialize(ZoneRuntimeContext context){ItemData.RegisterItem(item);if(context.Session.State.IsWorldItemDiscovered("craft_supply",pickupId))gameObject.SetActive(false);}
        protected override void PerformInteraction()
        {
            if(!BoundSession.State.DiscoverWorldItem("craft_supply",pickupId))return;
            BoundSession.State.AddItem(item.id);BoundSession.GetComponent<NarrativeController>()?.NotifyPickup(item.id);ProjectAudioHooks.Emit(AudioCue.Pickup);gameObject.SetActive(false);
        }
    }
}
