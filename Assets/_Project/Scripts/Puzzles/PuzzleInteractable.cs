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
        private string ContextItem => puzzle.installBeforePuzzle && BoundSession.State.HasFlag(puzzle.installedFlag) ? null : puzzle.requiredItem;
        public override string Prompt => puzzle!=null && BoundSession!=null && !string.IsNullOrEmpty(ContextItem)
            ? BoundSession.State.HasItem(ContextItem) ? "[E] "+(puzzle.installBeforePuzzle ? "Insertar " : "Usar ")+ItemData.Label(ContextItem) : "[E] Inspeccionar "+puzzle.title : base.Prompt;
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
            if (!string.IsNullOrEmpty(ContextItem) && state.CheckContextItem(ContextItem)==ContextItemResult.WrongSelection)
            { BoundSession.ShowMessage("Eso no sirve aquí. Necesitas "+ItemData.Label(ContextItem)+"."); ProjectAudioHooks.Emit(AudioCue.ObjectError); return; }
            if (puzzle.installBeforePuzzle && !state.HasFlag(puzzle.installedFlag))
            {
                state.SetFlag(GameFlagId.EntrancePanelInspected);
                if (!state.ApplyContextItem(puzzle.requiredItem,true)) { BoundSession.ShowMessage("Sin alimentación. Necesitas "+ItemData.Label(puzzle.requiredItem)+"."); return; }
                state.SetFlag(puzzle.installedFlag); ProjectAudioHooks.Emit(AudioCue.ObjectUsed); ProjectAudioHooks.Emit(AudioCue.TerminalBoot);
                BoundSession.ShowMessage("BATERÍA INSTALADA. El panel tiene energía. Examina la nota y repara las conexiones."); return;
            }
            if (puzzle.environmentalClue!=null) state.DiscoverWorldItem("clue",puzzle.environmentalClue.id);
            if (!string.IsNullOrWhiteSpace(ContextItem) && !state.HasItem(ContextItem))
            { BoundSession.ShowMessage(puzzle.inspectionText); return; }
            if (!string.IsNullOrWhiteSpace(puzzle.requiredClueId) && !state.IsWorldItemDiscovered("clue",puzzle.requiredClueId))
            { BoundSession.ShowMessage(puzzle.inspectionText); return; }
            if (puzzle.prerequisiteClues!=null) foreach(string id in puzzle.prerequisiteClues)
                if (!state.IsWorldItemDiscovered("clue",id))
                { BoundSession.ShowMessage(puzzle.inspectionText); return; }
            if (!DoorAccessRules.CanOpen(state,puzzle.requiredFlags,ContextItem))
            { BoundSession.ShowMessage(string.IsNullOrWhiteSpace(puzzle.requiredItem) ? "El sistema anterior aún no está restablecido." : "Se requiere "+ItemPickupInteractable.Label(puzzle.requiredItem)+"."); return; }
            controller?.ShowPuzzle(this);
        }
        public bool Complete()
        {
            if (!CanInteract || !RequirementsMet || !DoorAccessRules.CanOpen(BoundSession.State,puzzle.requiredFlags,ContextItem) ||
                puzzle.installBeforePuzzle && !BoundSession.State.HasFlag(puzzle.installedFlag) || !string.IsNullOrEmpty(ContextItem) && BoundSession.State.CheckContextItem(ContextItem)!=ContextItemResult.Ready) return false;
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
            if (puzzle.consumeItem && !state.ApplyContextItem(puzzle.requiredItem,true)) return false;
            if (!string.IsNullOrEmpty(puzzle.requiredItem)) ProjectAudioHooks.Emit(AudioCue.ObjectUsed);
            if (puzzle.completionFlags!=null) foreach(var flag in puzzle.completionFlags) state.SetFlag(flag);
            if (puzzle.rewardItems!=null) foreach(string item in puzzle.rewardItems) if (!state.HasItem(item)) { state.AddItem(item); BoundSession.GetComponent<NarrativeController>()?.NotifyPickup(item); }
            state.DiscoverWorldItem("puzzle",puzzle.id);
            BoundSession.ShowMessage(puzzle.successMessage);
            onSolved.Invoke(); return true;
        }
    }
}
