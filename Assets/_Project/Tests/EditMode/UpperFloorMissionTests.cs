using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.EditorTools;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class UpperFloorMissionTests
    {
        [Test] public void CraftingConsumesExactlyOneBatteryAndElectronicsBox()
        {
            var state=new GameState();state.AddItem("battery");Assert.That(MineInventoryController.TryCraft(state),Is.False);Assert.That(state.HasItem("battery"),Is.True);
            state.AddItem("electronics_box");state.AddItem("electronics_box");Assert.That(MineInventoryController.TryCraft(state),Is.True);
            Assert.That(state.HasItem("battery"),Is.False);Assert.That(state.CarriedItems.Count(i=>i=="electronics_box"),Is.EqualTo(1));Assert.That(state.CarriedItems.Count(i=>i=="crafted_mine"),Is.EqualTo(1));
            Assert.That(MineInventoryController.TryCraft(state),Is.False);
        }
        [TestCase("Z09_Piso4Aulas",4)] [TestCase("Z10_Piso5Aulas",5)]
        public void UpperFloorsHaveSuppliesAndSeparatedPatrols(string sceneName,int floor)
        {
            using(var inspection=SceneInspection.Open("Assets/_Project/Scenes/Zones/"+sceneName+".unity"))
            {
                var zone=inspection.Scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();
                Assert.That(zone.GetComponentsInChildren<TrashBinInteractable>().Length,Is.EqualTo(2));Assert.That(zone.GetComponentsInChildren<VendingMachineInteractable>().Length,Is.EqualTo(1));
                var supplies=zone.GetComponentsInChildren<CraftSupplyPickup>();Assert.That(supplies.Count(s=>s.item.id=="electronics_box"),Is.EqualTo(2));Assert.That(supplies.Count(s=>s.item.id=="battery"),Is.EqualTo(2));Assert.That(supplies.Select(s=>s.pickupId),Is.Unique);
                var robots=zone.GetComponentsInChildren<MiniAutoController>();Assert.That(robots.Length,Is.EqualTo(3));
                foreach(var a in robots)foreach(var b in robots)if(a!=b)Assert.That(Vector2.Distance(a.transform.position,b.transform.position),Is.GreaterThan(3));
                Physics2D.SyncTransforms();
                foreach(var robot in robots)
                {
                    var mover=(PatrolMover2D)new SerializedObject(robot).FindProperty("mover").objectReferenceValue;var path=(PatrolPath2D)new SerializedObject(mover).FindProperty("path").objectReferenceValue;
                    for(int p=0;p<path.Count;p++)Assert.That(Physics2D.OverlapCircleAll(path.GetPoint(p),.16f).Where(c=>!c.isTrigger&&!c.transform.IsChildOf(robot.transform)),Is.Empty);
                }
                if(floor==4){Assert.That(zone.transform.Find("Decoracion_Autor"),Is.Not.Null);var cameras=zone.GetComponentsInChildren<SecurityCamera2D>();Assert.That(cameras.Length,Is.EqualTo(3));foreach(var c in cameras){Assert.That(c.notifyNearestRobot,Is.True);Assert.That(c.fieldLight.shadowsEnabled,Is.True);Assert.That(c.sensor.GetComponent<CircleCollider2D>().radius,Is.EqualTo(c.fieldLight.pointLightOuterRadius));}}
                else{var fox=zone.GetComponentsInChildren<UpperFloorActor>().Single(a=>a.role==UpperActorRole.Fox);foreach(var r in robots)Assert.That(Vector2.Distance(r.transform.position,fox.transform.position),Is.GreaterThan(fox.safeRadius));}
            }
        }
        [Test] public void RescueWaitsForBothCrabsToBeDefeated()
        {
            var root=new GameObject("RescueSession");
            try
            {
                var session=root.AddComponent<GameSessionController>();session.StartNewGame();var mode=root.AddComponent<GameplayModeController>();mode.SetMode(GameplayMode.Explore);
                using(var inspection=SceneInspection.Open("Assets/_Project/Scenes/Zones/Z08_Piso3Aulas.unity"))
                {
                    var zone=inspection.Scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();var actor=zone.GetComponentsInChildren<UpperFloorActor>().Single(a=>a.role==UpperActorRole.Rescue);
                    actor.Initialize(new ZoneRuntimeContext(session,null,mode,zone));var update=typeof(UpperFloorActor).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
                    Assert.That(zone.GetComponentsInChildren<CrabHealthBar>().Length,Is.EqualTo(2));update.Invoke(actor,null);Assert.That(actor.Released,Is.False);
                    actor.rescueTargets[0].ReceiveMineHit();update.Invoke(actor,null);Assert.That(actor.Released,Is.False);
                    actor.rescueTargets[1].ReceiveMineHit();update.Invoke(actor,null);Assert.That(actor.Released,Is.True);Assert.That(session.State.IsWorldItemDiscovered("story","piso3_rescued"),Is.True);
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void FoxAppearsOnlyOnceAndHasNoNearbyEnemies()
        {
            var root=new GameObject("FoxSession");var player=new GameObject("FoxPlayer");
            try
            {
                var session=root.AddComponent<GameSessionController>();session.StartNewGame();var mode=root.AddComponent<GameplayModeController>();mode.SetMode(GameplayMode.Explore);var flow=root.AddComponent<SceneFlowController>();var body=player.AddComponent<Rigidbody2D>();
                typeof(SceneFlowController).GetField("playerBody",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(flow,body);
                using(var inspection=SceneInspection.Open("Assets/_Project/Scenes/Zones/Z10_Piso5Aulas.unity"))
                {
                    var zone=inspection.Scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();var fox=zone.GetComponentsInChildren<UpperFloorActor>().Single(a=>a.role==UpperActorRole.Fox);var context=new ZoneRuntimeContext(session,flow,mode,zone);
                    body.position=(Vector2)fox.transform.position+Vector2.right*1.5f;fox.Initialize(context);Assert.That(fox.visual.enabled,Is.False);Assert.That(fox.HasNearbyEnemy(),Is.False);Physics2D.SyncTransforms();
                    typeof(UpperFloorActor).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fox,null);Assert.That(fox.visual.enabled,Is.False,"The fox must not spend its one appearance through a wall.");
                    body.position=fox.transform.position;Physics2D.SyncTransforms();
                    typeof(UpperFloorActor).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fox,null);Assert.That(fox.visual.enabled,Is.True);Assert.That(session.State.IsWorldItemDiscovered("story","piso5_fox_once"),Is.True);
                    fox.Initialize(context);Assert.That(fox.visual.enabled,Is.False);
                }
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(player);}
        }
        [Test] public void CameraAlertSendsTheNearestRobotAndTurnsItsLightRed()
        {
            var root=new GameObject("CameraSession");var target=new GameObject("CameraTarget");
            try
            {
                var session=root.AddComponent<GameSessionController>();session.StartNewGame();var mode=root.AddComponent<GameplayModeController>();
                using(var inspection=SceneInspection.Open("Assets/_Project/Scenes/Zones/Z09_Piso4Aulas.unity"))
                {
                    var zone=inspection.Scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();var robots=zone.GetComponentsInChildren<MiniAutoController>();var list=(List<MiniAutoController>)typeof(ZoneContext).GetField("robots",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(zone);list.AddRange(robots);
                    var camera=zone.GetComponentsInChildren<SecurityCamera2D>().First();target.transform.position=robots[1].transform.position+Vector3.up;
                    var nearest=SecurityCamera2D.NearestRobot(robots,target.transform.position);camera.Initialize(new ZoneRuntimeContext(session,null,mode,zone));
                    typeof(SecurityCamera2D).GetMethod("Detected",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(camera,new object[]{target.transform});
                    Assert.That(nearest.State,Is.EqualTo(MobileThreatState.Search));Assert.That(camera.fieldLight.color.r,Is.GreaterThan(camera.fieldLight.color.g));
                }
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(target);}
        }
        [Test] public void MineIgnoresThePlayerAndDestroysAnEnemyThatStepsOnIt()
        {
            var root=new GameObject("MineTestCrab");var player=new GameObject("MineTestPlayer");GameObject mineObject=null;
            try
            {
                root.transform.position=new Vector2(200,200);root.AddComponent<Rigidbody2D>();var crab=root.AddComponent<MechanicalCrabController>();var enemyCollider=root.AddComponent<BoxCollider2D>();enemyCollider.size=new Vector2(.3f,.2f);
                player.transform.position=new Vector2(200,199);var playerCollider=player.AddComponent<BoxCollider2D>();
                mineObject=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/ajustes_assets/Generated/CraftedMine.prefab"),new Vector3(200,199.9f),Quaternion.identity);var mine=mineObject.GetComponent<CraftedMine>();
                typeof(CraftedMine).GetField("age",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(mine,2f);Physics2D.SyncTransforms();
                Assert.That(mine.TryDetonate(playerCollider),Is.False);Assert.That(mine.TryDetonate(enemyCollider),Is.True);Assert.That(mine.Exploded,Is.True);Assert.That(crab.HitsRemaining,Is.Zero);Assert.That(root.activeSelf,Is.False);
            }
            finally{if(mineObject!=null)Object.DestroyImmediate(mineObject);Object.DestroyImmediate(root);Object.DestroyImmediate(player);}
        }
    }
}
