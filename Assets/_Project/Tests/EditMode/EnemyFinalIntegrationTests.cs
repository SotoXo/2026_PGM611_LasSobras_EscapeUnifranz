using System;
using System.Collections.Generic;
using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class EnemyFinalIntegrationTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private GameObject Make(string name, Vector2 position)
        { var o=new GameObject(name); o.transform.position=position; objects.Add(o); return o; }
        private static void Set(object o,string name,object value) => o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
        private static object Get(object o,string name) => o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        [TearDown] public void Cleanup() { foreach(var o in objects) Object.DestroyImmediate(o); objects.Clear(); }
        [Test] public void WallOccludesVisionAndTriggerDoesNot()
        {
            var wall=Make("wall",new Vector2(102,100)).AddComponent<BoxCollider2D>();
            wall.size=new Vector2(.2f,3f); Physics2D.SyncTransforms();
            Assert.That(DetectionSensor2D.HasLineOfSight(new Vector2(100,100),new Vector2(104,100),~0),Is.False);
            wall.isTrigger=true; Physics2D.SyncTransforms();
            Assert.That(DetectionSensor2D.HasLineOfSight(new Vector2(100,100),new Vector2(104,100),~0),Is.True);
        }
        private object Navigator() => Activator.CreateInstance(typeof(PatrolMover2D).Assembly.GetType("EscapeUNIFRANZ.Hazards.ObstacleRoute2D"),true);
        private Vector2 Next(object nav,Vector2 start,Vector2 goal,Transform owner,float time) =>
            (Vector2)nav.GetType().GetMethod("Next").Invoke(nav,new object[]{start,goal,new Vector2(.3f,.3f),owner,null,.4f,time});
        [Test] public void NavigationGoesAroundSolidWallWithoutTeleporting()
        {
            var start=new Vector2(100,100); var goal=start+Vector2.right*4;
            var owner=Make("unit",start);
            var wall=Make("wall",start+Vector2.right*2).AddComponent<BoxCollider2D>(); wall.size=new Vector2(.4f,2f);
            Physics2D.SyncTransforms(); var nav=Navigator(); var point=start;
            for(int i=0;i<160 && Vector2.Distance(point,goal)>.1f;i++) {
                var next=Next(nav,point,goal,owner.transform,i*.1f);
                var moved=Vector2.MoveTowards(point,next,.16f);
                Assert.That(wall.OverlapPoint(moved),Is.False); point=moved;
            }
            Assert.That(Vector2.Distance(point,goal),Is.LessThan(.2f));
        }
        [Test] public void NavigationStopsForUnreachableDestination()
        {
            var start=new Vector2(100,100); var owner=Make("unit",start);
            var wall=Make("sealed wall",start+Vector2.right*2).AddComponent<BoxCollider2D>(); wall.size=new Vector2(.4f,60f);
            Physics2D.SyncTransforms();
            Assert.That(Next(Navigator(),start,start+Vector2.right*4,owner.transform,1f),Is.EqualTo(start));
        }
        private MiniAutoController Robot(out DetectionSensor2D sensor)
        {
            var o=Make("robot",new Vector2(100,100)); o.AddComponent<Rigidbody2D>();
            var mover=o.AddComponent<PatrolMover2D>();
            var child=Make("sensor",o.transform.position); child.transform.SetParent(o.transform);
            sensor=child.AddComponent<DetectionSensor2D>();
            var robot=o.AddComponent<MiniAutoController>(); Set(robot,"mover",mover); Set(robot,"sensor",sensor); return robot;
        }
        [Test] public void CanNoiseDoesNotCancelVisiblePursuit()
        {
            var robot=Robot(out var sensor); var target=Make("visible player",new Vector2(101,100));
            typeof(DetectionSensor2D).GetProperty("CurrentTarget").SetValue(sensor,target.transform);
            robot.DistractAt(new Vector2(98,100),5f);
            Assert.That(robot.State,Is.EqualTo(MobileThreatState.Patrol)); Assert.That(sensor.HasTarget,Is.True);
        }
        [Test] public void CanInvestigatesOriginAndRejectsImmediateSpam()
        {
            var robot=Robot(out var sensor); var noise=new Vector2(98,100);
            robot.DistractAt(noise,5f); robot.DistractAt(new Vector2(106,100),5f);
            Assert.That(robot.State,Is.EqualTo(MobileThreatState.Search)); Assert.That(Get(robot,"lastSeen"),Is.EqualTo(noise));
            robot.ResetEncounter(); Assert.That(sensor.enabled,Is.True); Assert.That(robot.State,Is.EqualTo(MobileThreatState.Patrol));
            robot.DistractAt(new Vector2(106,100),5f); Assert.That(Get(robot,"lastSeen"),Is.EqualTo(new Vector2(106,100)));
        }
        [Test] public void DisabledRobotIgnoresNoise()
        {
            var robot=Robot(out var sensor); Set(robot,"operational",false);
            robot.DistractAt(new Vector2(104,100),5f); Assert.That(robot.State,Is.EqualTo(MobileThreatState.Patrol)); Assert.That(sensor.enabled,Is.True);
        }
        [Test] public void ResetRestoresTemporarilyStunnedRobot()
        {
            var robot=Robot(out var sensor); Set(robot,"operational",false); Set(robot,"operationalBeforeSuspend",true); Set(robot,"disabledUntil",Time.time+5f);
            robot.ResetEncounter(); Assert.That(sensor.enabled,Is.True); Assert.That(Get(robot,"disabledUntil"),Is.EqualTo(0f));
        }
        [Test] public void RespawnGraceSuppressesOtherwiseActiveContact()
        {
            var o=Make("contact",new Vector2(100,100)); o.AddComponent<BoxCollider2D>(); var contact=o.AddComponent<HazardContact2D>();
            contact.ResetEncounter(); Assert.That(contact.CanDealContactDamage,Is.False);
            Set(contact,"graceUntil",Time.time-1f); Assert.That(contact.CanDealContactDamage,Is.True);
            contact.SetContactAllowed(false); Assert.That(contact.CanDealContactDamage,Is.False);
        }
        [Test] public void AlarmResetPreservesPermanentPermission()
        {
            var session=Make("session",Vector2.zero).AddComponent<GameSessionController>(); session.StartNewGame();
            var exit=Make("exit",Vector2.zero).AddComponent<ZoneExitInteractable>(); exit.Bind(null,session);
            Set(exit,"requiredFlag",GameFlagId.LabSoftwareUnlocked); session.State.SetFlag(GameFlagId.LabSoftwareUnlocked);
            exit.LockForAlarm(5f); Assert.That(exit.IsUnlocked,Is.False);
            exit.ResetEncounter(); Assert.That(exit.IsUnlocked,Is.True); Assert.That(session.State.HasFlag(GameFlagId.LabSoftwareUnlocked),Is.True);
        }
        [Test] public void IncorrectShutdownPreservesAcceptedStepAndDisplayedProgress()
        {
            var session=Make("session",Vector2.zero).AddComponent<GameSessionController>(); session.StartNewGame();
            var o=Make("boss",Vector2.zero); o.AddComponent<BoxCollider2D>(); var boss=o.AddComponent<FrancisBossController>();
            var machine=new FrancisBossStateMachine(); machine.Restore(true,true,false);
            Set(boss,"machine",machine);Set(boss,"session",session);
            Assert.That(boss.SubmitShutdown(FrancisShutdownStep.FinalConnection),Is.True);
            Assert.That(boss.SubmitShutdown(FrancisShutdownStep.Shutdown),Is.False);
            Assert.That(machine.CurrentProgress,Is.EqualTo(1));Assert.That(Get(boss,"phaseProgress"),Is.EqualTo(1));
        }
        [Test] public void FinalExitRequiresAllThreePhases()
        {
            var session=Make("session",Vector2.zero).AddComponent<GameSessionController>(); session.StartNewGame();
            var exit=Make("exit",Vector2.zero).AddComponent<ZoneExitInteractable>(); exit.Bind(null,session); Set(exit,"escapesAfterShutdown",true);
            var canEscape=typeof(ZoneExitInteractable).GetProperty("CanEscape",BindingFlags.NonPublic|BindingFlags.Instance);
            session.State.SetFlag(GameFlagId.FrancisShutdownPhaseComplete); Assert.That(canEscape.GetValue(exit),Is.False);
            session.State.SetFlag(GameFlagId.FrancisHardwarePhaseComplete); Assert.That(canEscape.GetValue(exit),Is.False);
            session.State.SetFlag(GameFlagId.FrancisNetworkPhaseComplete); Assert.That(canEscape.GetValue(exit),Is.True);
        }
    }
}
