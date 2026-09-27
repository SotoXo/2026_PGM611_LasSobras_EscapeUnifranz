using EscapeUNIFRANZ.Interaction;
using UnityEngine;

namespace EscapeUNIFRANZ.Encounters
{
    public enum FrancisSystemPhase
    {
        Network = 0,
        Hardware = 1,
        Shutdown = 2
    }

    public sealed class FrancisSystemNodeInteractable : InteractableBehaviour, IEncounterResettable, IScanStateProvider
    {
        [SerializeField] private FrancisBossController controller;
        [SerializeField] private FrancisSystemPhase phase;
        [SerializeField, Range(0, 2)] private int stepIndex;
        private bool used;

        public string ScanStateText => used ? "[AISLADO]" : "[CONECTADO]";

        public override bool CanInteract => base.CanInteract && !used && controller != null && IsCurrentPhase();

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.PhaseProgressReset += ResetProgress;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.PhaseProgressReset -= ResetProgress;
            }
        }

        protected override void PerformInteraction()
        {
            bool accepted;
            switch (phase)
            {
                case FrancisSystemPhase.Network:
                    accepted = controller.SubmitNetwork((FrancisNetworkStep)stepIndex);
                    break;
                case FrancisSystemPhase.Hardware:
                    accepted = controller.SubmitHardware((FrancisHardwareStep)stepIndex);
                    break;
                default:
                    accepted = controller.SubmitShutdown((FrancisShutdownStep)stepIndex);
                    break;
            }

            used = accepted;
        }

        private bool IsCurrentPhase()
        {
            return phase == FrancisSystemPhase.Network && controller.State == FrancisBossState.PhaseNetwork ||
                phase == FrancisSystemPhase.Hardware && controller.State == FrancisBossState.PhaseHardware ||
                phase == FrancisSystemPhase.Shutdown && controller.State == FrancisBossState.PhaseShutdown;
        }

        private void ResetProgress()
        {
            used = false;
        }

        public void ResetEncounter()
        {
            ResetProgress();
        }
    }
}
