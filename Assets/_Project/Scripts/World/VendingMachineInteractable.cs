
using System.Collections;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Narrative;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    /// <summary>
    /// Expendedora de bebidas. [E] compra: reproduce la animación y deja la lata lista
    /// para recoger. Un segundo [E] guarda la lata en el inventario.
    /// El stock se conserva en GameState.
    /// </summary>
    public sealed class VendingMachineInteractable : InteractableBehaviour, IScanStateProvider
    {
        private const string StateZone = "vending";
        public const int MaximumStock = 3;
        private int Capacity => Mathf.Clamp(stock,1,MaximumStock);
        private enum Phase { Idle, Dispensing, ReadyToCollect }

        [SerializeField] private string machineId;
        [SerializeField] private ItemData product;
        [SerializeField, Min(1)] private int stock = 3;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] purchaseFrames;
        [SerializeField] private float[] frameSeconds = { 0.18f, 0.10f, 0.10f, 0.12f };
        [SerializeField] private int landingFrame = -1;

        private Phase phase = Phase.Idle;
        private Coroutine dispensingRoutine;

        private string ProductLabel => product != null ? product.displayName : "objeto";
        private string SlotKey(int index) => machineId + ":" + index;

        private int Remaining()
        {
            if (BoundSession == null) return Capacity;

            int count = 0;
            for (int i = 0; i < Capacity; i++)
                if (!BoundSession.State.IsWorldItemDiscovered(StateZone, SlotKey(i)))
                    count++;

            return count;
        }

        public string ScanStateText => Remaining() > 0 ? "[EXPENDEDORA]" : "[VACÍA]";

        public override bool CanInteract => base.CanInteract &&
            BoundSession != null &&
            product != null && !string.IsNullOrWhiteSpace(product.id) &&
            !string.IsNullOrWhiteSpace(machineId) &&
            phase != Phase.Dispensing;

        public override string Prompt =>
            phase == Phase.ReadyToCollect ? "[E] Recoger " + ProductLabel :
            phase == Phase.Dispensing ? "..." :
            Remaining() > 0 ? "[E] Comprar " + ProductLabel : "Expendedora vacía";

        private void Awake()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (body != null && idleSprite != null) body.sprite = idleSprite;
            stock = Capacity;
        }

        private Vector2 TrayPoint()
        {
            if (body == null) return transform.position;

            Bounds bounds = body.bounds;
            return new Vector2(bounds.center.x, bounds.min.y + bounds.size.y * 0.22f);
        }

        protected override void PerformInteraction()
        {
            if (BoundSession == null) return;

            if (phase == Phase.ReadyToCollect)
            {
                Collect();
                return;
            }

            if (Remaining() <= 0)
            {
                BoundSession.ShowMessage("La expendedora está vacía.");
                ProjectAudioHooks.Emit(AudioCue.ObjectError);
                return;
            }

            if (dispensingRoutine == null)
                dispensingRoutine = StartCoroutine(Dispense());
        }

        private IEnumerator Dispense()
        {
            phase = Phase.Dispensing;

            ProjectAudioHooks.EmitAt(AudioCue.ElectronicDoor, transform.position);

            int frames = purchaseFrames != null ? purchaseFrames.Length : 0;
            int landing = landingFrame >= 0
                ? landingFrame
                : Mathf.Max(1, frames - 2);

            bool landed = false;

            for (int i = 1; i < frames; i++)
            {
                if (body != null && purchaseFrames[i] != null)
                    body.sprite = purchaseFrames[i];

                if (i == landing)
                {
                    landed = true;
                    OnCanLanded();
                }

                bool last = i == frames - 1;

                if (!last)
                {
                    float delay = frameSeconds != null && i - 1 < frameSeconds.Length
                        ? Mathf.Max(0.01f, frameSeconds[i - 1])
                        : 0.10f;

                    yield return new WaitForSeconds(delay);
                }
            }

            if (!landed)
                OnCanLanded();

            phase = Phase.ReadyToCollect;
            dispensingRoutine = null;
        }

        private void OnCanLanded()
        {
            ProjectAudioHooks.EmitAt(AudioCue.CanImpact, transform.position);
            InteractionFx.Puff(
                TrayPoint(),
                new Color(0.8f, 0.8f, 0.8f, 1f)
            );
        }

        private void Collect()
        {
            if (BoundSession == null || product == null) return;

            bool collected = false;

            for (int i = 0; i < Capacity; i++)
            {
                if (!BoundSession.State.DiscoverWorldItem(StateZone, SlotKey(i)))
                    continue;

                BoundSession.State.AddItem(product.id);

                InteractionFx.Pickup(
                    product.icon,
                    TrayPoint(),
                    ProductLabel
                );

                BoundSession.GetComponent<NarrativeController>()
                    ?.NotifyPickup(product.id);

                collected = true;
                break;
            }

            if (collected)
                ProjectAudioHooks.Emit(AudioCue.Pickup);
            else
                BoundSession.ShowMessage("No queda ninguna lata disponible.");

            phase = Phase.Idle;

            if (body != null && idleSprite != null)
                body.sprite = idleSprite;
        }

        private void OnDisable()
        {
            if (dispensingRoutine != null)
            {
                StopCoroutine(dispensingRoutine);
                dispensingRoutine = null;
            }

            if (phase == Phase.Dispensing)
                phase = Phase.Idle;

            if (body != null && idleSprite != null)
                body.sprite = idleSprite;
        }
    }
}
