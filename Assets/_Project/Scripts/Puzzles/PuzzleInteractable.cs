using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.World;
using UnityEngine;
using UnityEngine.Events;
namespace EscapeUNIFRANZ.Puzzles
{
    public enum PuzzleWorldAction { None, RobotDogNetwork, RobotArmsPanel, FrancisNetwork, FrancisShutdown }
    public sealed class PuzzleInteractable : InteractableBehaviour, IZoneRuntimeInitializable, IScanStateProvider
    {
        public PuzzleData puzzle;
        public PuzzleWorldAction worldAction;
        public MonoBehaviour actionTarget;
        public UnityEvent onSolved = new UnityEvent();
        private NarrativeController controller;
        public string ScanStateText => GetComponent<TerminalStatus2D>()?.ScanStateText ?? "[PANEL DE DIAGNÓSTICO]";
        public override bool CanInteract => base.CanInteract && puzzle!=null && BoundSession!=null &&
            !BoundSession.State.IsWorldItemDiscovered("puzzle",puzzle.id) && ActionAvailable;
        private bool ActionAvailable => worldAction==PuzzleWorldAction.FrancisNetwork
            ? (actionTarget as FrancisBossController)?.State==FrancisBossState.PhaseNetwork
            : worldAction==PuzzleWorldAction.FrancisShutdown
                ? (actionTarget as FrancisBossController)?.State==FrancisBossState.PhaseShutdown
                : worldAction==PuzzleWorldAction.RobotArmsPanel
                    ? (actionTarget as RobotArmsEncounterController)?.State==RobotArmsEncounterState.Vulnerable
                    : true;
        public void Initialize(ZoneRuntimeContext context) { controller=context.Session.GetComponent<NarrativeController>(); }
        protected override void PerformInteraction()
        {
            var state=BoundSession.State;
            if (puzzle.environmentalClue!=null) state.DiscoverWorldItem("clue",puzzle.environmentalClue.id);
            if (!string.IsNullOrWhiteSpace(puzzle.requiredItem) && !state.HasItem(puzzle.requiredItem))
            { BoundSession.ShowMessage(puzzle.inspectionText); return; }
            if (!string.IsNullOrWhiteSpace(puzzle.requiredClueId) && !state.IsWorldItemDiscovered("clue",puzzle.requiredClueId))
            { BoundSession.ShowMessage(puzzle.inspectionText); return; }
            if (puzzle.prerequisiteClues!=null) foreach(string id in puzzle.prerequisiteClues)
                if (!state.IsWorldItemDiscovered("clue",id))
                { BoundSession.ShowMessage(puzzle.inspectionText); return; }
            if (!DoorAccessRules.CanOpen(state,puzzle.requiredFlags,puzzle.requiredItem))
            { BoundSession.ShowMessage(string.IsNullOrWhiteSpace(puzzle.requiredItem) ? "El sistema anterior aún no está restablecido." : "Se requiere "+ItemPickupInteractable.Label(puzzle.requiredItem)+"."); return; }
            controller?.ShowPuzzle(this);
        }
        public bool Complete()
        {
            if (!CanInteract || !RequirementsMet || !DoorAccessRules.CanOpen(BoundSession.State,puzzle.requiredFlags,puzzle.requiredItem)) return false;
            bool accepted=true;
            switch(worldAction)
            {
                case PuzzleWorldAction.RobotDogNetwork:
                    var dog=actionTarget as RobotDogEncounterController;
                    accepted=dog!=null && (BoundSession.State.HasFlag(GameFlagId.RobotDogDisabled) || dog.DisableNetworkNode(0) && dog.DisableNetworkNode(1)); break;
                case PuzzleWorldAction.RobotArmsPanel:
                    accepted=(actionTarget as RobotArmsEncounterController)?.TryDisableFromPanel()==true; break;
                case PuzzleWorldAction.FrancisNetwork:
                    var network=actionTarget as FrancisBossController;
                    accepted=network!=null && network.SubmitNetwork(FrancisNetworkStep.Cameras) &&
                        network.SubmitNetwork(FrancisNetworkStep.Access) && network.SubmitNetwork(FrancisNetworkStep.Server); break;
                case PuzzleWorldAction.FrancisShutdown:
                    var shutdown=actionTarget as FrancisBossController;
                    accepted=shutdown!=null && shutdown.SubmitShutdown(FrancisShutdownStep.FinalConnection) &&
                        shutdown.SubmitShutdown(FrancisShutdownStep.CutActuators) && shutdown.SubmitShutdown(FrancisShutdownStep.Shutdown); break;
            }
            if (!accepted) return false;
            var state=BoundSession.State;
            if (puzzle.consumeItem) state.TryUseItem(puzzle.requiredItem);
            if (puzzle.completionFlags!=null) foreach(var flag in puzzle.completionFlags) state.SetFlag(flag);
            if (puzzle.rewardItems!=null) foreach(string item in puzzle.rewardItems) state.AddItem(item);
            state.DiscoverWorldItem("puzzle",puzzle.id);
            BoundSession.ShowMessage(puzzle.successMessage);
            onSolved.Invoke(); return true;
        }
    }
}
