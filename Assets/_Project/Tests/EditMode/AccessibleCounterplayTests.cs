using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Puzzles;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class AccessibleCounterplayTests
    {
        [Test] public void FuseReplacementIsADragConnection_AndOnlyCompletionAllowsConsumption()
        {
            var data=AssetDatabase.LoadAssetAtPath<PuzzleData>("Assets/_Project/Data/Narrative/Puzzles/fuse_simulation.asset");
            var model=new PuzzleModel(data); var state=new GameState(); state.AddItem("fuse");
            Assert.That(model.Connect("OUT_SLOT","IN_FUSE"),Is.EqualTo(PuzzleSubmission.Rejected)); Assert.That(state.HasItem("fuse"),Is.True);
            Assert.That(model.Connect("IN_FUSE","OUT_SLOT"),Is.EqualTo(PuzzleSubmission.Completed));
            Assert.That(state.HasFlag(GameFlagId.SimulationFuseReplaced),Is.False,"Scratch model must not mutate world state.");
            Assert.That(data.consumeItem,Is.True);
        }
        [Test] public void EmergencyStopCanInterruptAnActiveArmSequence()
        {
            var machine=new RobotArmsEncounterStateMachine(); Assert.That(machine.EmergencyStop(),Is.False);
            machine.BeginSequence(); Assert.That(machine.EmergencyStop(),Is.True); Assert.That(machine.Current,Is.EqualTo(RobotArmsEncounterState.Disabled));
            Assert.That(machine.BeginSequence(),Is.False);
        }
        [Test] public void SuspendingAMiniStopsMovementDetectionAndContact_WhileKeepingItsObject()
        {
            var obj=new GameObject("CounterplayTestUnit");
            try
            {
                obj.AddComponent<Rigidbody2D>(); obj.AddComponent<BoxCollider2D>(); var mover=obj.AddComponent<PatrolMover2D>();
                var field=new GameObject("Sensor"); field.transform.SetParent(obj.transform); var sensor=field.AddComponent<DetectionSensor2D>();
                var hazard=obj.AddComponent<HazardContact2D>(); var mini=obj.AddComponent<MiniAutoController>();
                var so=new SerializedObject(mini); so.FindProperty("mover").objectReferenceValue=mover; so.FindProperty("sensor").objectReferenceValue=sensor; so.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(hazard.CanDealContactDamage,Is.True); mini.SuspendFor(20f);
                Assert.That(mini.State,Is.EqualTo(MobileThreatState.Disabled)); Assert.That(mover.enabled,Is.False); Assert.That(sensor.enabled,Is.False);
                Assert.That(hazard.CanDealContactDamage,Is.False); Assert.That(obj.activeSelf,Is.True);
                mini.SetOperational(true); Assert.That(hazard.CanDealContactDamage,Is.False,"Restart requires a new telegraph before contact is armed.");
                mini.DistractAt(new Vector2(2f,1f),5f); Assert.That(mini.State,Is.EqualTo(MobileThreatState.Search));
                Assert.That(sensor.enabled,Is.False,"A distraction reserves its investigation window instead of instantly reacquiring the player.");
                Assert.That(hazard.CanDealContactDamage,Is.False);
            }
            finally { Object.DestroyImmediate(obj); }
        }
        [Test] public void SoftwareButtonsUseReadableActionsRatherThanRealCommands()
        {
            var data=AssetDatabase.LoadAssetAtPath<PuzzleData>("Assets/_Project/Data/Narrative/Puzzles/software_security.asset");
            var model=new PuzzleModel(data); Assert.That(model.Command("CORE_AI"),Is.EqualTo(PuzzleSubmission.Rejected));
            Assert.That(model.Command("SECURITY_CAM"),Is.EqualTo(PuzzleSubmission.Completed));
            Assert.That(data.ports[1].label,Is.EqualTo("AISLAR SEGURIDAD")); Assert.That(data.objective,Is.Not.Empty);
        }
    }
}
