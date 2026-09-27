using EscapeUNIFRANZ.Interaction;
using UnityEngine;

namespace EscapeUNIFRANZ.Encounters
{
    public sealed class RobotArmsPanelInteractable : InteractableBehaviour, IScanStateProvider
    {
        [SerializeField] private RobotArmsEncounterController controller;

        public string ScanStateText => controller != null &&
            controller.State == RobotArmsEncounterState.Disabled
                ? "[SIN ENERGÍA]"
                : "[ENERGIZADO]";

        public override bool CanInteract => base.CanInteract && controller != null &&
            controller.State != RobotArmsEncounterState.Disabled;

        protected override void PerformInteraction()
        {
            controller.TryDisableFromPanel();
        }
    }
}
