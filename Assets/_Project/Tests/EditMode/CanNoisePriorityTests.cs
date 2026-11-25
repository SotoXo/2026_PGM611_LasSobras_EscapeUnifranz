using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class CanNoisePriorityTests
    {
        static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
        [Test]
        public void NoiseInvestigationReachesLandingBeforeStartingItsSearchTimer()
        {
            var obj=new GameObject("NoiseRobot");obj.SetActive(false);
            obj.AddComponent<Rigidbody2D>();var mover=obj.AddComponent<PatrolMover2D>();
            var child=new GameObject("Sensor");child.transform.SetParent(obj.transform,false);
            var sensor=child.AddComponent<DetectionSensor2D>();var robot=obj.AddComponent<MiniAutoController>();
            var mode=obj.AddComponent<GameplayModeController>();
            Set(robot,"mover",mover);Set(robot,"sensor",sensor);Set(robot,"mode",mode);
            obj.SetActive(true);mode.SetMode(GameplayMode.Explore);
            try
            {
                robot.DistractAt(new Vector2(4,0),1f);
                Assert.That(sensor.enabled,Is.True,"The robot must still notice the flashlight while investigating.");
                Assert.That(sensor.InvestigatingNoise,Is.True);
                Set(robot,"transitionAt",Time.time-20);
                typeof(MiniAutoController).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(robot,null);
                Assert.That(robot.State,Is.EqualTo(MobileThreatState.Search),"A travel timeout must not cancel the investigation.");
                robot.DistractAt(new Vector2(5,0),2f);
                Assert.That((Vector2)typeof(MiniAutoController).GetField("canLanding",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(robot),Is.EqualTo(new Vector2(5,0)),"A new impact must not be ignored by the former cooldown.");
                obj.transform.position=new Vector2(5,0);
                typeof(MiniAutoController).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(robot,null);
                Assert.That(sensor.InvestigatingNoise,Is.False);
                Assert.That(robot.State,Is.EqualTo(MobileThreatState.Search));
            }
            finally{Object.DestroyImmediate(obj);}
        }
        [Test]
        public void SeeingThePlayerCancelsNoiseAndStartsAttackWarning()
        {
            var obj=new GameObject("NoiseRobot");obj.SetActive(false);obj.AddComponent<Rigidbody2D>();
            var mover=obj.AddComponent<PatrolMover2D>();
            var child=new GameObject("Sensor");child.transform.SetParent(obj.transform,false);var sensor=child.AddComponent<DetectionSensor2D>();
            var robot=obj.AddComponent<MiniAutoController>();Set(robot,"mover",mover);Set(robot,"sensor",sensor);
            var player=new GameObject("Player");obj.SetActive(true);
            try
            {
                robot.DistractAt(new Vector2(4,0),1);
                typeof(MiniAutoController).GetMethod("Detected",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(robot,new object[]{player.transform});
                Assert.That(sensor.InvestigatingNoise,Is.False);
                Assert.That(robot.State,Is.EqualTo(MobileThreatState.Suspicious));
            }
            finally{Object.DestroyImmediate(obj);Object.DestroyImmediate(player);}
        }
    }
}
