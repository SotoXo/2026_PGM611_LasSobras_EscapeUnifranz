using EscapeUNIFRANZ.Interaction;
using UnityEngine;

namespace EscapeUNIFRANZ.Encounters
{
    public sealed class ExolegOverloadInteractable : InteractableBehaviour, IEncounterResettable, IScanStateProvider
    {
        [SerializeField] private ExolegEncounterController controller;
        private bool activated;
        public int nodeIndex;

        public string ScanStateText => activated ? "[DESCARGADO]" : "[ENERGIZADO]";

        public override bool CanInteract =>
            base.CanInteract && !activated && controller != null && controller.CanUseNode(nodeIndex);

        protected override void PerformInteraction()
        {
            if (controller.ActivateOverloadPoint(nodeIndex))
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
