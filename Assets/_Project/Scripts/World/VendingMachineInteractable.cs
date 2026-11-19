using System.Collections;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Narrative;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    /// <summary>
    /// Expendedora de bebidas. [E] compra: reproduce la animación (la lata cae a la bandeja) y
    /// se queda en "lista para recoger". Otro [E] saca la lata y la mete al inventario.
    /// El stock se descuenta al recoger y queda guardado en GameState.
    /// </summary>
    public sealed class VendingMachineInteractable : InteractableBehaviour, IScanStateProvider
    {
        private const string StateZone = "vending";
        private enum Phase { Idle, Dispensing, ReadyToCollect }

        [SerializeField] private string machineId;
        [SerializeField] private ItemData product;
        [SerializeField, Min(1)] private int stock = 3;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Sprite idleSprite;
        [Tooltip("Fotogramas 00..05. La compra reproduce del 01 al 05 y mantiene el último.")]
        [SerializeField] private Sprite[] purchaseFrames;
        [SerializeField] private float[] frameSeconds = { 0.18f, 0.10f, 0.10f, 0.12f };

        private Phase phase = Phase.Idle;

        private string ProductLabel => product != null ? product.displayName : "objeto";
        private string SlotKey(int i) => machineId + ":" + i;

        private int Remaining()
        {
            if (BoundSession == null) return stock;
            int count = 0;
            for (int i = 0; i < stock; i++) if (!BoundSession.State.IsWorldItemDiscovered(StateZone, SlotKey(i))) count++;
            return count;
        }

        public string ScanStateText => Remaining() > 0 ? "[EXPENDEDORA]" : "[VACÍA]";

        public override bool CanInteract => base.CanInteract && BoundSession != null &&
            product != null && !string.IsNullOrWhiteSpace(product.id) &&
            !string.IsNullOrWhiteSpace(machineId) && phase != Phase.Dispensing;

        public override string Prompt =>
            phase == Phase.ReadyToCollect ? "[E] Recoger " + ProductLabel :
            phase == Phase.Dispensing ? "..." :
            Remaining() > 0 ? "[E] Comprar " + ProductLabel : "Expendedora vacía";

        private void Awake()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (body != null && idleSprite != null) body.sprite = idleSprite;
        }

        protected override void PerformInteraction()
        {
            if (phase == Phase.ReadyToCollect) { Collect(); return; }
            if (Remaining() <= 0)
            {
                BoundSession.ShowMessage("La expendedora está vacía.");
                ProjectAudioHooks.Emit(AudioCue.ObjectError);
                return;
            }
            StartCoroutine(Dispense());
        }

        private IEnumerator Dispense()
        {
            phase = Phase.Dispensing;
            ProjectAudioHooks.EmitAt(AudioCue.ElectronicDoor, transform.position);
            for (int i = 1; purchaseFrames != null && i < purchaseFrames.Length; i++)
            {
                if (body != null) body.sprite = purchaseFrames[i];
                bool last = i == purchaseFrames.Length - 1;
                if (!last) yield return new WaitForSeconds(frameSeconds != null && i - 1 < frameSeconds.Length ? frameSeconds[i - 1] : 0.1f);
            }
            phase = Phase.ReadyToCollect;
            ProjectAudioHooks.EmitAt(AudioCue.CanImpact, transform.position);
        }

        private void Collect()
        {
            for (int i = 0; i < stock; i++)
            {
                if (!BoundSession.State.DiscoverWorldItem(StateZone, SlotKey(i))) continue;
                BoundSession.State.AddItem(product.id);
                ProjectAudioHooks.Emit(AudioCue.Pickup);
                BoundSession.GetComponent<NarrativeController>()?.NotifyPickup(product.id);
                break;
            }
            phase = Phase.Idle;
            if (body != null && idleSprite != null) body.sprite = idleSprite;
        }
    }
}
