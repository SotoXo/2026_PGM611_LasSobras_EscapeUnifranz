using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Narrative;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    [RequireComponent(typeof(DoorPanel2D))]
    public sealed class InteriorDoorInteractable : InteractableBehaviour
    {
        private DoorPanel2D panel;
        public bool IsOpen => panel != null && panel.IsOpen;
        public override string Prompt => IsOpen ? "[E] Cerrar puerta" : "[E] Abrir puerta";

        private void Awake() => panel = GetComponent<DoorPanel2D>();

        protected override void PerformInteraction()
        {
            if (IsOpen)
            {
                if (!panel.TryClose())
                    BoundSession.ShowMessage("Hay algo en el paso de la puerta.");
                return;
            }

            panel.Open();
            ProjectAudioHooks.Emit(AudioCue.DoorOpen);
        }
    }
}
