using EscapeUNIFRANZ.Interaction;
using UnityEngine;

namespace EscapeUNIFRANZ.Encounters
{
    public sealed class RobotDogNetworkNodeInteractable : InteractableBehaviour, IEncounterResettable, IScanStateProvider
    {
        [SerializeField] private RobotDogEncounterController controller;
        [SerializeField, Range(0, 1)] private int nodeIndex;
        private bool used;

        public string ScanStateText => used ? "[DESCONECTADO]" : "[CONECTADO]";

        public override bool CanInteract => base.CanInteract && !used && controller != null &&
            controller.State != RobotDogEncounterState.Disabled;

        protected override void PerformInteraction()
        {
            if (controller.DisableNetworkNode(nodeIndex))
            {
                used = true;
            }
        }

        public void ResetEncounter()
        {
            if (controller == null || controller.State != RobotDogEncounterState.Disabled)
            {
                used = false;
            }
        }
    }
}
