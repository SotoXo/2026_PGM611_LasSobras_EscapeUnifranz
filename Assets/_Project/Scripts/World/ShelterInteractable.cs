using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.Narrative;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    public sealed class ShelterInteractable : InteractableBehaviour
    {
        private PlayerMovement2D player;

        public override bool CanInteract => base.CanInteract && BoundSession != null;
        public override string Prompt => player != null && player.IsUsingCover(transform)
            ? "[E] Dejar cobertura" : "[E] Agacharse tras el mostrador";

        protected override void PerformInteraction()
        {
            if (player == null) player = BoundSession.GetComponentInChildren<PlayerMovement2D>(true);
            if (player == null) return;
            bool wasHidden = player.IsUsingCover(transform);
            player.SetCover(wasHidden ? null : transform);
            ProjectAudioHooks.Emit(AudioCue.Breathing);
            if (!wasHidden)
                BoundSession.ShowMessage("CUBIERTO. Quédate quieto. Si una unidad se acerca demasiado, puede encontrarte.");
        }
    }
}
