using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Narrative;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    /// <summary>Lata ya lanzada que quedó en el piso: [E] la vuelve a meter al inventario.</summary>
    public sealed class ThrownCanPickup : InteractableBehaviour
    {
        public string itemId = "soda";

        public override string Prompt => "[E] Recoger " + ItemData.Label(itemId);

        protected override void PerformInteraction()
        {
            if (BoundSession == null || !BoundSession.State.AddItem(itemId)) return;
            var canRenderer = GetComponent<SpriteRenderer>();
            InteractionFx.Pickup(canRenderer != null ? canRenderer.sprite : null, transform.position, ItemData.Label(itemId));
            gameObject.SetActive(false);
            ProjectAudioHooks.Emit(AudioCue.Pickup);
            BoundSession.GetComponent<NarrativeController>()?.NotifyPickup(itemId);
            Destroy(gameObject);
        }
    }
}
