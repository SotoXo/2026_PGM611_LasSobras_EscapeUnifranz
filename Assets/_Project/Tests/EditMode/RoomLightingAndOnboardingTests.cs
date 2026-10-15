using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEngine;
namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class RoomLightingAndOnboardingTests
    {
        [Test] public void ReadingSequenceHoldsFullLightBeforeFailureAndSettles()
        {
            var timeline=new RoomLightingTimeline(RoomLightingLogic.Reading(.7f,1.5f,RoomFailurePattern.Strong,1f));
            Assert.That(timeline.Current.state,Is.EqualTo(RoomLightingState.Dim)); timeline.Tick(.71f); Assert.That(timeline.Current.state,Is.EqualTo(RoomLightingState.Normal)); timeline.Tick(1.4f); Assert.That(timeline.Current.state,Is.EqualTo(RoomLightingState.Normal)); timeline.Tick(.1f); Assert.That(timeline.Current.state,Is.EqualTo(RoomLightingState.Blackout)); timeline.Tick(4f); Assert.That(timeline.Running,Is.False);
        }
        [Test] public void FrozenTimeDoesNotAdvanceRoomSequence()
        { var timeline=new RoomLightingTimeline(RoomLightingLogic.Reading(.7f,2f,RoomFailurePattern.Double,.6f)); for(int i=0;i<100;i++) timeline.Tick(0); Assert.That(timeline.Index,Is.Zero); Assert.That(timeline.Running,Is.True); }
        [TestCase(RoomFailurePattern.Mild)][TestCase(RoomFailurePattern.Double)][TestCase(RoomFailurePattern.Strong)]
        public void FailureSequencesHaveModerateIntervalsAndLimitedDuration(RoomFailurePattern pattern)
        { var steps=RoomLightingLogic.Reading(.7f,1.5f,pattern,.8f); float sum=0; foreach(var step in steps) { Assert.That(step.seconds,Is.GreaterThanOrEqualTo(.05f)); sum+=step.seconds; } Assert.That(sum,Is.LessThan(5f)); }
        [Test] public void BlackoutExtinguishesPrimaryLightsAndRestorationIsPartial()
        { Assert.That(RoomLightingLogic.Level(RoomLightingState.Blackout,.36f,.075f),Is.Zero); Assert.That(RoomLightingLogic.Level(RoomLightingState.Restored,.36f,.075f),Is.InRange(.075f,.36f)); Assert.That(RoomLightingLogic.Level(RoomLightingState.Emergency,.36f,.075f),Is.GreaterThan(0)); }
        [Test] public void RoomEventsAreIndependentAndRecordedAcrossRespawn()
        { var state=new GameState(); Assert.That(RoomLightingLogic.ClaimEntry(state,"entrada/recepcion",true),Is.True); state.SetCheckpoint("entrada","inicio"); state.SetLocation("entrada","inicio"); Assert.That(RoomLightingLogic.ClaimEntry(state,"entrada/recepcion",true),Is.False); Assert.That(RoomLightingLogic.ClaimEntry(state,"entrada/dental",true),Is.True); Assert.That(RoomLightingLogic.ClaimEntry(state,"entrada/recepcion",false),Is.True); }
        [Test] public void IlluminatedRoomExtendsVisibilityOnlyInsideItsBounds()
        { var bounds=new Rect(1,1,10,6); Assert.That(RoomLightingLogic.AmbientVisibility(bounds,new Vector2(2,2),new Vector2(9,5),.2f),Is.True); Assert.That(RoomLightingLogic.AmbientVisibility(bounds,new Vector2(2,2),new Vector2(9,5),.04f),Is.False); Assert.That(RoomLightingLogic.AmbientVisibility(bounds,new Vector2(2,2),new Vector2(12,5),.2f),Is.False); }
        [Test] public void InteractionTutorialWaitsForRealMovement()
        { var progress=new EntranceTutorialProgress(new GameState()); progress.SetZone("entrada"); progress.Offer(TutorialAction.Interaction); Assert.That(progress.TakeNext(),Is.EqualTo(TutorialAction.None)); progress.Offer(TutorialAction.Movement); Assert.That(progress.TakeNext(),Is.EqualTo(TutorialAction.Movement)); progress.Complete(TutorialAction.Movement); Assert.That(progress.TakeNext(),Is.EqualTo(TutorialAction.Interaction)); }
        [TestCase("hall")][TestCase("arca")][TestCase("piso2")][TestCase("piso3")][TestCase("lab_software")][TestCase("nucleo_ia")]
        public void BasicTutorialsCannotStartOutsideEntrance(string zone)
        { var progress=new EntranceTutorialProgress(new GameState()); progress.SetZone(zone); Assert.That(progress.Offer(TutorialAction.Movement),Is.False); Assert.That(progress.Offer(TutorialAction.Interaction),Is.False); Assert.That(progress.TakeNext(),Is.EqualTo(TutorialAction.None)); }
        [Test] public void InventoryFlashlightAndMapLessonsRequireProgression()
        {
            var state=new GameState(); var progress=new EntranceTutorialProgress(state); progress.SetZone("entrada"); Assert.That(progress.Offer(TutorialAction.Inventory),Is.False); Assert.That(progress.Offer(TutorialAction.Flashlight),Is.False); Assert.That(progress.Offer(TutorialAction.Map),Is.False);
            state.TryCollectPickup("battery","battery"); Assert.That(progress.Offer(TutorialAction.Inventory),Is.True); state.TryCollectPickup("torch","flashlight"); Assert.That(progress.Offer(TutorialAction.Flashlight),Is.True); state.SetFlag(GameFlagId.CampusPlanRecovered); Assert.That(progress.Offer(TutorialAction.Map),Is.True);
        }
        [Test] public void CompletedHintsDoNotRepeatOnReentryOrRespawn()
        { var state=new GameState(); var progress=new EntranceTutorialProgress(state); progress.SetZone("entrada"); progress.Complete(TutorialAction.Movement); progress.SetZone("hall"); progress.SetZone("entrada"); Assert.That(progress.Offer(TutorialAction.Movement),Is.False); var restored=new EntranceTutorialProgress(state); restored.SetZone("entrada"); Assert.That(restored.Offer(TutorialAction.Movement),Is.False); }
        [Test] public void ShownHintDoesNotRepeatEvenWhenItsActionIsSkipped()
        { var state=new GameState(); state.AddItem("flashlight"); var progress=new EntranceTutorialProgress(state); progress.SetZone("entrada"); progress.Offer(TutorialAction.Flashlight); Assert.That(progress.TakeNext(),Is.EqualTo(TutorialAction.Flashlight)); progress.SetZone("entrada"); Assert.That(progress.Offer(TutorialAction.Flashlight),Is.False); }
        [Test] public void ThreatWarningTakesPrecedenceOverOptionalScannerLesson()
        { var state=new GameState(); state.SetFlag(GameFlagId.FirstThreatActivated); var progress=new EntranceTutorialProgress(state); progress.SetZone("entrada"); progress.Offer(TutorialAction.Scanner); progress.Offer(TutorialAction.AvoidThreat); Assert.That(progress.TakeNext(),Is.EqualTo(TutorialAction.AvoidThreat)); }
    }
}
