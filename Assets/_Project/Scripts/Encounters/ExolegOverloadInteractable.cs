using EscapeUNIFRANZ.Interaction;
using UnityEngine;

namespace EscapeUNIFRANZ.Encounters
{
    public sealed class ExolegOverloadInteractable : InteractableBehaviour, IEncounterResettable, IScanStateProvider
    {
        [SerializeField] private ExolegEncounterController controller;
        private bool activated;

        public string ScanStateText => activated ? "[DESCARGADO]" : "[ENERGIZADO]";

        public override bool CanInteract =>
            base.CanInteract && !activated && controller != null && controller.AcceptsOverloadPoint;

        protected override void PerformInteraction()
        {
            if (controller.ActivateOverloadPoint())
            {
                activated = true;
            }
        }

        public void ResetEncounter()
        {
            if (controller == null || controller.State != ExolegEncounterState.Disabled)
            {
                activated = false;
            }
        }
    }
}
