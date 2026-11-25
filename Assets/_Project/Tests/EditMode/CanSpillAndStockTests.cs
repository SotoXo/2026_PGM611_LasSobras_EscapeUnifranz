using System.Collections;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class CanSpillAndStockTests
    {
        private MiniAutoController MakeRobot(Vector2 point)
        {
            var obj=new GameObject("SpillRobot");obj.SetActive(false);obj.transform.position=point;
            obj.AddComponent<Rigidbody2D>();var mover=obj.AddComponent<PatrolMover2D>();
            var child=new GameObject("Sensor");child.transform.SetParent(obj.transform,false);
            var sensor=child.AddComponent<DetectionSensor2D>();var robot=obj.AddComponent<MiniAutoController>();
            sensor.GetComponent<CircleCollider2D>().isTrigger=true;
            obj.AddComponent<BoxCollider2D>().size=new Vector2(.25f,.2f);
            var data=new SerializedObject(robot);data.FindProperty("mover").objectReferenceValue=mover;
            data.FindProperty("sensor").objectReferenceValue=sensor;data.FindProperty("maxHits").intValue=3;data.ApplyModifiedPropertiesWithoutUndo();
            obj.SetActive(true);robot.ResetEncounter();return robot;
        }
        [Test]
        public void SpillStopsDifferentRobotsOnceEachAndLeavesThemUndamaged()
        {
            var a=MakeRobot(new Vector2(200-.35f,200));var b=MakeRobot(new Vector2(200+.35f,200));
            var spill=CanSpill2D.Create(new Vector2(200,200),null,null);
            try
            {
                Physics2D.SyncTransforms();
                Assert.That(spill.TryAffect(a),Is.True);Assert.That(a.State,Is.EqualTo(MobileThreatState.Disabled));
                Assert.That(a.HitsRemaining,Is.EqualTo(3));
                a.SetOperational(true);
                Assert.That(spill.TryAffect(a),Is.False,"Standing in the spill must not perpetually renew a stun.");
                Assert.That(spill.TryAffect(b),Is.True);
            }
            finally{Object.DestroyImmediate(spill.gameObject);Object.DestroyImmediate(a.gameObject);Object.DestroyImmediate(b.gameObject);}
        }
        [Test]
        public void EmptyCanIsReusableAndCanBeSelectedInsteadOfSpillingAnotherFullCan()
        {
            var state=new GameState();state.AddItem("soda");state.AddItem(ThrowableCanController.EmptyCanItemId);
            Assert.That(ThrowableCanController.ChooseCan(state),Is.EqualTo("empty_can"));
            state.SelectItem("soda");Assert.That(ThrowableCanController.ChooseCan(state),Is.EqualTo("soda"));
            state.SelectItem("empty_can");Assert.That(state.TryUseItem("empty_can"),Is.True);
            state.AddItem("empty_can");Assert.That(state.HasItem("empty_can"),Is.True);
        }
        [Test]
        public void OversizedDispenserStockStillStopsAtThreeAndRemembersItsSales()
        {
            var obj=new GameObject("StockLimit");var sessionObj=new GameObject("StockSession");
            var item=ScriptableObject.CreateInstance<ItemData>();item.id="soda";
            try
            {
                var session=sessionObj.AddComponent<GameSessionController>();session.StartNewGame();
                var machine=obj.AddComponent<VendingMachineInteractable>();var data=new SerializedObject(machine);
                data.FindProperty("machineId").stringValue="limited_test";data.FindProperty("stock").intValue=99;
                data.FindProperty("product").objectReferenceValue=item;data.ApplyModifiedPropertiesWithoutUndo();machine.BindSession(session);
                for(int i=0;i<3;i++)
                {
                    var dispense=(IEnumerator)typeof(VendingMachineInteractable).GetMethod("Dispense",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(machine,null);
                    while(dispense.MoveNext()){}
                    machine.Interact();
                }
                Assert.That(session.State.CarriedItems.Count,Is.EqualTo(3));
                Assert.That(machine.Prompt,Does.Contain("vacía"));
                machine.Interact();Assert.That(session.State.CarriedItems.Count,Is.EqualTo(3));
                var saved=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(session.State));session.RestoreState(saved);
                Assert.That(machine.Prompt,Does.Contain("vacía"));
            }
            finally{Object.DestroyImmediate(obj);Object.DestroyImmediate(sessionObj);Object.DestroyImmediate(item);}
        }
    }
}
