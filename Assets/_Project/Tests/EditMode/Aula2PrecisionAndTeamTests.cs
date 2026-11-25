using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class Aula2PrecisionAndTeamTests
    {
        private static Type Builder=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="EscapeUNIFRANZ.Editor").GetType("EscapeUNIFRANZ.EditorTools.Aula2MapBuilder");
        [Test]
        public void EighteenPixelDoorwayFitsPlayerButTenPixelDoorwayDoesNot()
        {
            foreach(int gap in new[]{18,16,10})
            {
                var blocked=new bool[64,64];int left=32-gap/2,right=left+gap;
                for(int y=30;y<37;y++)for(int x=0;x<64;x++)blocked[x,y]=x<left||x>=right;
                var free=(bool[,])Builder.GetMethod("WalkableMask",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{blocked});
                Assert.That(free[32,32],Is.EqualTo(gap>=16));
            }
        }

        [Test]
        public void FurnitureBlocksItsBaseAndLeavesSpaceBehindItsUpperArtwork()
        {
            var floor=new Texture2D(64,64);var walls=new Texture2D(64,64);var objects=new Texture2D(64,64);
            try
            {
                floor.SetPixels(Enumerable.Repeat(Color.gray,4096).ToArray());floor.Apply();
                walls.SetPixels(new Color[4096]);walls.Apply();var pixels=new Color[4096];
                for(int y=24;y<40;y++)for(int x=24;x<32;x++)pixels[y*64+x]=Color.white;
                objects.SetPixels(pixels);objects.Apply();
                var mask=(bool[,])Builder.GetMethod("CollisionMask",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{floor,walls,objects});
                Assert.That(mask[27,25],Is.True);Assert.That(mask[27,37],Is.False);
            }
            finally{Object.DestroyImmediate(floor);Object.DestroyImmediate(walls);Object.DestroyImmediate(objects);}
        }
        [Test]
        public void HallBenchRearSupportLeavesTheWalkingLaneAndSidePaddingClear()
        {
            const int width=960,height=576;
            var floor=new Texture2D(width,height);var walls=new Texture2D(width,height);var objects=new Texture2D(width,height);
            try
            {
                floor.SetPixels(Enumerable.Repeat(Color.gray,width*height).ToArray());floor.Apply();
                walls.SetPixels(new Color[width*height]);walls.Apply();var pixels=new Color[width*height];
                for(int y=306;y<319;y++)for(int x=300;x<328;x++)pixels[y*width+x]=Color.white;
                objects.SetPixels(pixels);objects.Apply();
                var mask=(bool[,])Builder.GetMethod("CollisionMask",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{floor,walls,objects});
                Assert.That(mask[310,317],Is.True);Assert.That(mask[310,318],Is.True);
                Assert.That(mask[310,306],Is.False);Assert.That(mask[310,307],Is.False);
                Assert.That(mask[310,310],Is.False,"The seat must not create a deep solid rectangle.");
                Assert.That(mask[300,306],Is.False,"Visual side padding must not block the player.");
            }
            finally{Object.DestroyImmediate(floor);Object.DestroyImmediate(walls);Object.DestroyImmediate(objects);}
        }

        [TestCase(295f,335f)]
        [TestCase(600f,648f)]
        public void PlayerBodyCanCrossTheHallUnderEitherBench(float startPixel,float endPixel)
        {
            const string path="Assets/_Project/Scenes/Zones/Z07_Aula2.unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var geometry=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CompositeCollider2D>()).Single(c=>!c.isTrigger);
                Physics2D.SyncTransforms();
                Vector2 from=new Vector2(startPixel/32f,(576-275)/32f)+Vector2.up*.13f;
                var hits=Physics2D.BoxCastAll(from,new Vector2(.38f,.22f),0,Vector2.right,(endPixel-startPixel)/32f);
                Assert.That(hits.Any(h=>h.collider!=null&&h.collider.transform.IsChildOf(geometry.transform)),Is.False,
                    "The exact player feet box must fit through the corridor below the bench.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }

        [Test]
        public void NewFloorContainsLimberRobotsAndAprilsWorkingDispenser()
        {
            const string path="Assets/_Project/Scenes/Zones/Z07_Aula2.unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var sessionObject=new GameObject("DispenserTestSession");
            try
            {
                var zone=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ZoneContext>()).Single();
                var robots=zone.GetComponentsInChildren<MiniAutoController>();Assert.That(robots.Length,Is.EqualTo(3));
                foreach(var robot in robots)
                {
                    var data=new SerializedObject(robot);
                    var texture=(Texture2D)data.FindProperty("movementArt").objectReferenceValue;
                    Assert.That(texture,Is.Not.Null);Assert.That(texture.width,Is.EqualTo(192));Assert.That(texture.height,Is.EqualTo(640));
                    Assert.That(data.FindProperty("verticalArt").objectReferenceValue,Is.Not.Null);
                    Assert.That(data.FindProperty("impactArt").objectReferenceValue,Is.Not.Null);
                    var mover=(PatrolMover2D)data.FindProperty("mover").objectReferenceValue;
                    var route=(PatrolPath2D)new SerializedObject(mover).FindProperty("path").objectReferenceValue;
                    Assert.That(route.Count,Is.EqualTo(2));
                    Assert.That(Vector2.Distance(route.GetPoint(0),route.GetPoint(1)),Is.GreaterThan(.5f));
                    Assert.That(data.FindProperty("maxHits").intValue,Is.EqualTo(3));
                    var sensor=(DetectionSensor2D)data.FindProperty("sensor").objectReferenceValue;
                    Assert.That(sensor.enabled,Is.True);
                    Assert.That(sensor.GetComponent<CircleCollider2D>().radius,Is.EqualTo(8f));
                    Assert.That(robot.GetComponentInChildren<HazardContact2D>(),Is.Not.Null);
                }
                var machine=zone.GetComponentsInChildren<VendingMachineInteractable>().Single();
                Physics2D.SyncTransforms();
                var mapCollision=zone.GetComponentsInChildren<CompositeCollider2D>().Single(c=>!c.isTrigger);
                Vector2 machineFeet=machine.transform.position;
                Assert.That(Vector2.Distance(machineFeet,new Vector2(21.26f,4.84f)),Is.LessThan(.01f),"Preserve the manually designated dispenser location.");
                Assert.That(mapCollision.OverlapPoint(machineFeet+Vector2.down*.4f),Is.False,"Its front approach must remain free.");
                var session=sessionObject.AddComponent<GameSessionController>();session.StartNewGame();machine.BindSession(session);
                var dataMachine=new SerializedObject(machine);
                Assert.That(dataMachine.FindProperty("purchaseFrames").arraySize,Is.EqualTo(6));
                Assert.That(machine.CanInteract,Is.True);
                var dispense=(IEnumerator)typeof(VendingMachineInteractable).GetMethod("Dispense",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(machine,null);
                while(dispense.MoveNext()){}
                Assert.That(machine.Prompt,Does.Contain("Recoger"));machine.Interact();
                Assert.That(session.State.HasItem("soda"),Is.True);
                Assert.That(session.State.IsWorldItemDiscovered("vending","aula2_abril_prueba:0"),Is.True);
                Assert.That(zone.GetComponentsInChildren<CompositeCollider2D>().Count(c=>!c.isTrigger),Is.EqualTo(1));
                Assert.That(zone.GetComponentsInChildren<CompositeCollider2D>().Count(c=>c.isTrigger),Is.EqualTo(1));
                var ambient=zone.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>().Single(l=>l.lightType==UnityEngine.Rendering.Universal.Light2D.LightType.Global);
                Assert.That(ambient.intensity,Is.EqualTo(.06f).Within(.001f));
            }
            finally{Object.DestroyImmediate(sessionObject);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }

        [Test]
        public void CanHitStunsOnceAndCheckpointRestoresRobotHealth()
        {
            var obj=new GameObject("DamageTest");obj.SetActive(false);
            try
            {
                obj.AddComponent<Rigidbody2D>();
                var mover=obj.AddComponent<PatrolMover2D>();
                var child=new GameObject("Sensor");child.transform.SetParent(obj.transform);
                var sensor=child.AddComponent<DetectionSensor2D>();
                var robot=obj.AddComponent<MiniAutoController>();
                var data=new SerializedObject(robot);data.FindProperty("mover").objectReferenceValue=mover;
                data.FindProperty("sensor").objectReferenceValue=sensor;data.FindProperty("maxHits").intValue=3;
                data.ApplyModifiedPropertiesWithoutUndo();obj.SetActive(true);robot.ResetEncounter();
                Assert.That(robot.HitsRemaining,Is.EqualTo(3));
                Assert.That(robot.ReceiveCanHit(),Is.True);
                Assert.That(robot.HitsRemaining,Is.EqualTo(2));
                Assert.That(robot.State,Is.EqualTo(MobileThreatState.Disabled));
                Assert.That(robot.ReceiveCanHit(),Is.False,"A stunned robot cannot take repeated damage from one impact.");
                for(int hit=0;hit<2;hit++)
                {
                    robot.SetOperational(true);
                    typeof(MiniAutoController).GetField("nextHitAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(robot,0f);
                    Assert.That(robot.ReceiveCanHit(),Is.True);
                }
                Assert.That(robot.HitsRemaining,Is.Zero);
                Assert.That(robot.State,Is.EqualTo(MobileThreatState.Disabled));
                Assert.That(obj.activeSelf,Is.False,"A defeated robot and all of its hitboxes must disappear.");
                robot.ResetEncounter();
                Assert.That(obj.activeSelf,Is.True);
                Assert.That(robot.HitsRemaining,Is.EqualTo(3));
                Assert.That(robot.State,Is.EqualTo(MobileThreatState.Patrol));
                Assert.That(sensor.enabled,Is.True);
            }
            finally {Object.DestroyImmediate(obj);}
        }

        [Test]
        public void FullCanImmobilizesWithoutDamageAndEmptyCanCanDamageTheImmobilizedRobot()
        {
            var obj=new GameObject("CanEffectsRobot");obj.SetActive(false);
            try
            {
                obj.AddComponent<Rigidbody2D>();var mover=obj.AddComponent<PatrolMover2D>();
                var child=new GameObject("Sensor");child.transform.SetParent(obj.transform,false);
                var sensor=child.AddComponent<DetectionSensor2D>();var robot=obj.AddComponent<MiniAutoController>();
                var data=new SerializedObject(robot);data.FindProperty("mover").objectReferenceValue=mover;
                data.FindProperty("sensor").objectReferenceValue=sensor;data.FindProperty("maxHits").intValue=3;data.ApplyModifiedPropertiesWithoutUndo();
                obj.SetActive(true);robot.ResetEncounter();
                Assert.That(EscapeUNIFRANZ.Player.ThrowableCanController.ApplyRobotImpact(robot,true),Is.True);
                Assert.That(robot.HitsRemaining,Is.EqualTo(3));Assert.That(robot.State,Is.EqualTo(MobileThreatState.Disabled));
                Assert.That(EscapeUNIFRANZ.Player.ThrowableCanController.ApplyRobotImpact(robot,false),Is.True);
                Assert.That(robot.HitsRemaining,Is.EqualTo(2));
            }
            finally{Object.DestroyImmediate(obj);}
        }

        [Test]
        public void SideRobotsHavePhysicalBodiesAndVerticalPatrolsInTheirDesignatedCorridors()
        {
            const string path="Assets/_Project/Scenes/Zones/Z07_Aula2.unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var robots=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MiniAutoController>()).ToArray();
                foreach(var robot in robots)
                {
                    var data=new SerializedObject(robot);var visual=(SpriteRenderer)data.FindProperty("carVisual").objectReferenceValue;
                    Assert.That(Vector2.Distance(robot.transform.position,visual.transform.position),Is.LessThan(.001f));
                    var mover=(PatrolMover2D)data.FindProperty("mover").objectReferenceValue;
                    var route=(PatrolPath2D)new SerializedObject(mover).FindProperty("path").objectReferenceValue;
                    Vector2 delta=route.GetPoint(1)-route.GetPoint(0);
                    if(robot.name.EndsWith("rampa")){Assert.That(Mathf.Abs(delta.x),Is.GreaterThan(.5f));continue;}
                    Assert.That(Mathf.Abs(delta.x),Is.LessThan(.01f));Assert.That(Mathf.Abs(delta.y),Is.GreaterThan(.5f));
                    if(robot.name.EndsWith("garra"))Assert.That(robot.transform.position.x,Is.LessThan(10));
                    else Assert.That(robot.transform.position.x,Is.GreaterThan(23));
                }
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }

        [TestCase(true,true,true)]
        [TestCase(true,false,true)]
        [TestCase(false,true,true)]
        [TestCase(false,false,false)]
        public void PursuitEndsOnlyOutsideReachAndWithoutSight(bool near,bool visible,bool retain)
            => Assert.That(DetectionSensor2D.RetainPursuit(near,visible),Is.EqualTo(retain));
    }
}
