using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Puzzles;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEngine;
namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class NarrativePuzzleLogicTests
    {
        private PuzzleData definition;
        [SetUp] public void Setup()
        {
            definition=ScriptableObject.CreateInstance<PuzzleData>();
            definition.ports=new[]{new CablePort{id="ENERGIA"},new CablePort{id="PANEL"},new CablePort{id="PUERTA"},new CablePort{id="OTRO"}};
            definition.connections=new[]{new CableConnection("ENERGIA","PANEL"),new CableConnection("PANEL","PUERTA")};
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(definition); }
        [Test] public void CableChain_RejectsReverseAndPrematureEdges_AndCompletesOnlyAllRequiredConnections()
        {
            var model=new PuzzleModel(definition);
            Assert.That(model.Connect("PANEL","ENERGIA"),Is.EqualTo(PuzzleSubmission.Rejected));
            Assert.That(model.Connect("PANEL","PUERTA"),Is.EqualTo(PuzzleSubmission.Rejected));
            Assert.That(model.Connect("ENERGIA","PANEL"),Is.EqualTo(PuzzleSubmission.Accepted));
            Assert.That(model.Connect("ENERGIA","PANEL"),Is.EqualTo(PuzzleSubmission.Rejected));
            Assert.That(model.Connect("ENERGIA","OTRO"),Is.EqualTo(PuzzleSubmission.Rejected));
            Assert.That(model.IsComplete,Is.False);
            Assert.That(model.Connect("PANEL","PUERTA"),Is.EqualTo(PuzzleSubmission.Completed));
        }
        [Test] public void NonOrderedTopology_AcceptsDeclaredEdgesInEitherOrder()
        {
            definition.connectInOrder=false; var model=new PuzzleModel(definition);
            Assert.That(model.Connect("PANEL","PUERTA"),Is.EqualTo(PuzzleSubmission.Accepted));
            Assert.That(model.Connect("ENERGIA","PANEL"),Is.EqualTo(PuzzleSubmission.Completed));
        }
        [Test] public void IncompleteOrDanglingDefinitions_CannotAutomaticallySolve()
        {
            definition.connections=new[]{new CableConnection("ENERGIA","MISSING")};
            Assert.That(PuzzleModel.Validate(definition,out _),Is.False);
            definition.connections=new CableConnection[0]; Assert.That(PuzzleModel.Validate(definition,out _),Is.False);
        }
        [Test] public void CommandError_ResetsScratchProgress_AndDoesNotChangeWorldState()
        {
            definition.kind=PuzzleKind.CommandSequence; definition.commandOrder=new[]{"ENERGIA","PANEL","PUERTA"};
            var model=new PuzzleModel(definition); var state=new GameState();
            model.Command("ENERGIA"); model.Command("OTRO"); Assert.That(model.Progress,Is.Zero);
            Assert.That(state.HasFlag(GameFlagId.PowerEntranceRestored),Is.False);
            model.Command("ENERGIA"); model.Command("PANEL"); Assert.That(model.Command("PUERTA"),Is.EqualTo(PuzzleSubmission.Completed));
        }
        [Test] public void SamePickupAcrossSharedScenes_IsCollectedOnce_AndBatteryCanBeConsumedOnce()
        {
            var state=new GameState(); Assert.That(state.TryCollectPickup("planta:battery:1","battery"),Is.True);
            Assert.That(state.TryCollectPickup("planta:battery:1","battery"),Is.False);
            Assert.That(state.TryUseItem("battery"),Is.True); Assert.That(state.TryUseItem("battery"),Is.False);
            Assert.That(state.TryCollectPickup("planta:battery:1","battery"),Is.False);
        }
        [Test] public void DoorNeedsBothCredentialAndCompletedSystem()
        {
            var state=new GameState(); var flags=new[]{GameFlagId.TechnologyNetworkRestored};
            Assert.That(DoorAccessRules.CanOpen(state,flags,"lab_card"),Is.False);
            state.AddItem("lab_card"); Assert.That(DoorAccessRules.CanOpen(state,flags,"lab_card"),Is.False);
            state.SetFlag(flags[0]); Assert.That(DoorAccessRules.CanOpen(state,flags,"lab_card"),Is.True);
        }
        [Test] public void CameraCone_DoesNotDetectBehindOrOutsideRange()
        {
            Assert.That(DetectionSensor2D.IsInField(Vector2.zero,new Vector2(0f,4f),Vector2.up,5f,75f),Is.True);
            Assert.That(DetectionSensor2D.IsInField(Vector2.zero,new Vector2(0f,-4f),Vector2.up,5f,75f),Is.False);
            Assert.That(DetectionSensor2D.IsInField(Vector2.zero,new Vector2(0f,6f),Vector2.up,5f,75f),Is.False);
        }
        [Test] public void CancellingOrResettingPuzzle_DoesNotResetPreviouslyCompletedBossPhases()
        {
            var state=new GameState(); state.SetFlag(GameFlagId.FrancisNetworkPhaseComplete);
            var model=new PuzzleModel(definition); model.Connect("ENERGIA","PANEL"); model.Reset();
            Assert.That(state.HasFlag(GameFlagId.FrancisNetworkPhaseComplete),Is.True); Assert.That(model.Progress,Is.Zero);
        }
    }
}
