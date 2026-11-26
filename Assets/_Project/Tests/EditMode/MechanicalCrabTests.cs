using System.Linq;
using EscapeUNIFRANZ.EditorTools;
using EscapeUNIFRANZ.Encounters;
using NUnit.Framework;
using UnityEngine;
namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class MechanicalCrabTests
    {
        [Test] public void FloorImpactDamagesNearbyPlayerOnceAndDoesNotReachAcrossTheRoom()
        {
            var root=new GameObject("CrabImpactTest");var player=new GameObject("Player");
            try
            {
                root.transform.position=new Vector2(200,200);root.AddComponent<Rigidbody2D>();
                var session=root.AddComponent<EscapeUNIFRANZ.Core.GameSessionController>();session.StartNewGame();
                var flow=root.AddComponent<EscapeUNIFRANZ.Core.SceneFlowController>();
                var body=player.AddComponent<Rigidbody2D>();var data=new UnityEditor.SerializedObject(flow);data.FindProperty("playerBody").objectReferenceValue=body;data.ApplyModifiedPropertiesWithoutUndo();
                var zone=root.AddComponent<EscapeUNIFRANZ.World.ZoneContext>();var mode=root.AddComponent<EscapeUNIFRANZ.Core.GameplayModeController>();
                var context=new EscapeUNIFRANZ.World.ZoneRuntimeContext(session,flow,mode,zone);
                var encounter=root.AddComponent<EncounterContext>();var crab=root.AddComponent<MechanicalCrabController>();crab.encounter=encounter;
                encounter.Initialize(context);crab.Initialize(context);
                var impact=typeof(MechanicalCrabController).GetMethod("Impact",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                body.position=new Vector2(210,200);impact.Invoke(crab,null);Assert.That(session.State.Lives,Is.EqualTo(5));
                body.position=new Vector2(200,199.5f);Physics2D.SyncTransforms();impact.Invoke(crab,null);Assert.That(session.State.Lives,Is.EqualTo(4));
                impact.Invoke(crab,null);Assert.That(session.State.Lives,Is.EqualTo(4));
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(player);}
        }
        [Test] public void EmptyClassroomContainsFourCompleteAnimationDirectionsAndSolidBody()
        {
            using(var inspection=SceneInspection.Open("Assets/_Project/Scenes/Zones/Z08_Piso3Aulas.unity"))
            {
                var actors=inspection.Scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<MechanicalCrabController>()).ToArray();
                Assert.That(actors.Length,Is.EqualTo(2));
                var crab=actors.First();
                Assert.That(crab.room.Contains(crab.transform.position),Is.True);
                Assert.That(crab.transform.position.x,Is.GreaterThan(31));
                Assert.That(crab.GetComponent<Rigidbody2D>().bodyType,Is.EqualTo(RigidbodyType2D.Kinematic));
                Assert.That(crab.encounter,Is.Not.Null);Assert.That(crab.visual.sprite,Is.Not.Null);
                Assert.That(crab.directions.Length,Is.EqualTo(4));
                foreach(var direction in crab.directions){Assert.That(direction.frames.Length,Is.EqualTo(44));Assert.That(direction.frames.All(f=>f!=null),Is.True);Assert.That(direction.seconds.All(t=>t>0),Is.True);}
                Physics2D.SyncTransforms();var colliders=Physics2D.OverlapBoxAll(crab.transform.position+Vector3.up*.18f,new Vector2(.85f,.45f),0);
                Assert.That(colliders.Where(c=>!c.isTrigger&&!c.transform.IsChildOf(crab.transform)),Is.Empty);
                Assert.That(MechanicalCrabController.ImpactFrame,Is.EqualTo(6));
            }
        }
    }
}
