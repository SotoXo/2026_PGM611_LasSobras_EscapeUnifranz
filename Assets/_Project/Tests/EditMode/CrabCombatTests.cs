using System;
using System.Linq;
using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.EditorTools;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class CrabCombatTests
    {
        [Test] public void EmptyCansDamageAndFullCansStunWithoutDamage()
        {
            var obj=new GameObject("CrabCanTest");var crab=obj.AddComponent<MechanicalCrabController>();
            try
            {
                Assert.That(ThrowableCanController.ApplyCrabImpact(crab,true),Is.True);Assert.That(crab.HitsRemaining,Is.EqualTo(3));Assert.That(crab.IsStunned,Is.True);
                float until=(float)typeof(MechanicalCrabController).GetField("stunnedUntil",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(crab);Assert.That(until-Time.time,Is.GreaterThan(3.9f));
                for(int i=0;i<3;i++)
                {
                    typeof(MechanicalCrabController).GetField("nextCanHit",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(crab,0f);
                    Assert.That(ThrowableCanController.ApplyCrabImpact(crab,false),Is.True);
                    Assert.That(crab.HitsRemaining,Is.EqualTo(2-i));
                }
                Assert.That(obj.activeSelf,Is.False);crab.ResetEncounter();Assert.That(obj.activeSelf,Is.True);Assert.That(crab.HitsRemaining,Is.EqualTo(3));
            }
            finally{Object.DestroyImmediate(obj);}
        }
        [Test] public void PuddleStunsEachCrabOnlyOnce()
        {
            var obj=new GameObject("CrabSpillTest");obj.transform.position=new Vector2(200,200);var crab=obj.AddComponent<MechanicalCrabController>();
            var spill=CanSpill2D.Create(obj.transform.position,null,null);
            try{Assert.That(spill.TryAffectCrab(crab),Is.True);Assert.That(crab.IsStunned,Is.True);Assert.That(spill.TryAffectCrab(crab),Is.False);Assert.That(crab.HitsRemaining,Is.EqualTo(3));}
            finally{Object.DestroyImmediate(spill.gameObject);Object.DestroyImmediate(obj);}
        }
        [Test] public void BothCrabsAdvanceSharedDamageEveryFifteenHits()
        {
            var sessionObject=new GameObject("RuptureSession");
            try
            {
                var session=sessionObject.AddComponent<GameSessionController>();session.StartNewGame();var mode=sessionObject.AddComponent<GameplayModeController>();mode.SetMode(GameplayMode.Explore);
                using(var inspection=SceneInspection.Open("Assets/_Project/Scenes/Zones/Z08_Piso3Aulas.unity"))
                {
                    var zone=inspection.Scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();var effect=zone.GetComponentInChildren<CrabRoomRupture>();
                    effect.Initialize(new ZoneRuntimeContext(session,null,mode,zone));Assert.That(effect.crabs.Length,Is.EqualTo(2));
                    for(int hit=1;hit<=48;hit++)
                    {
                        var actor=effect.crabs[(hit-1)%2];((Action)typeof(MechanicalCrabController).GetField("GroundSlam",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(actor)).Invoke();
                        typeof(CrabRoomRupture).GetField("elapsed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(effect,100f);
                        typeof(CrabRoomRupture).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(effect,null);
                        Assert.That(effect.CurrentFrame,Is.EqualTo(CrabRoomRupture.StageFrame(hit)));Assert.That(effect.GroundHits,Is.EqualTo(Math.Min(45,hit)));
                    }
                    Assert.That(effect.CurrentFrame,Is.EqualTo(23));
                }
            }
            finally{Object.DestroyImmediate(sessionObject);}
        }
    }
}
