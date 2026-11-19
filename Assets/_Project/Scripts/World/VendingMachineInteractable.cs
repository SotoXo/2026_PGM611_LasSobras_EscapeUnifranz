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
        [Tooltip("Fotograma en el que la lata toca la bandeja (suena el golpe). -1 = el penúltimo.")]
        [SerializeField] private int landingFrame = -1;

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

        private void OnDisable()
        {
            // Si la zona se descarga a mitad de la compra, la máquina no debe quedar bloqueada.
            if (phase != Phase.Dispensing) return;
            phase = Phase.Idle;
            if (body != null && idleSprite != null) body.sprite = idleSprite;
        }

        /// <summary>Punto de la bandeja donde cae la lata (≈22 % desde el borde inferior del sprite).</summary>
        private Vector2 TrayPoint()
        {
            if (body == null) return transform.position;
            Bounds bounds = body.bounds;
            return new Vector2(bounds.center.x, bounds.min.y + bounds.size.y * 0.22f);
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
            // El clic del botón ya suena con el [E]; aquí arranca el motor de la máquina.
            ProjectAudioHooks.EmitAt(AudioCue.RobotServo, transform.position);

            int frames = purchaseFrames != null ? purchaseFrames.Length : 0;
            int landing = landingFrame >= 0 ? landingFrame : Mathf.Max(1, frames - 2);
            bool landed = false;
            for (int i = 1; i < frames; i++)
            {
                if (body != null) body.sprite = purchaseFrames[i];
                if (i == landing) { landed = true; OnCanLanded(); }
                bool last = i == frames - 1;
                if (!last) yield return new WaitForSeconds(frameSeconds != null && i - 1 < frameSeconds.Length ? frameSeconds[i - 1] : 0.1f);
            }
            if (!landed) OnCanLanded();
            phase = Phase.ReadyToCollect;
        }

        // Golpe y polvo en el mismo instante en que el fotograma muestra la lata en la bandeja.
        private void OnCanLanded()
        {
            ProjectAudioHooks.EmitAt(AudioCue.CanImpact, transform.position);
            InteractionFx.Puff(TrayPoint(), new Color(0.8f, 0.8f, 0.8f, 1f));
        }

        private void Collect()
        {
            for (int i = 0; i < stock; i++)
            {
                if (!BoundSession.State.DiscoverWorldItem(StateZone, SlotKey(i))) continue;
                BoundSession.State.AddItem(product.id);
                ProjectAudioHooks.Emit(AudioCue.Pickup);
                InteractionFx.Pickup(product.icon, TrayPoint(), ProductLabel);
                BoundSession.GetComponent<NarrativeController>()?.NotifyPickup(product.id);
                break;
            }
            phase = Phase.Idle;
            if (body != null && idleSprite != null) body.sprite = idleSprite;
        }
    }
}
