using System.Linq;
using EscapeUNIFRANZ.EditorTools;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class ElevatorFloorSelectionTests
    {
        [Test]
        public void BoardingRequiresAnExtraStepBeyondTheThreshold()
        {
            var cabin=new Rect(0,0,1,.8f);
            var nearDoor=new Bounds(new Vector3(.5f,.66f),new Vector3(.38f,.22f));
            Assert.That(ElevatorDoorAnimation.IsFullyInside(cabin,nearDoor),Is.True);
            Assert.That(ElevatorDoorAnimation.IsDeepInside(cabin,nearDoor,.12f),Is.False);
            nearDoor.center=new Vector3(.5f,.52f);
            Assert.That(ElevatorDoorAnimation.IsDeepInside(cabin,nearDoor,.12f),Is.True);
            nearDoor.center=new Vector3(.5f,.14f);
            Assert.That(ElevatorDoorAnimation.IsFullyInside(cabin,nearDoor),Is.True);
            Assert.That(ElevatorDoorAnimation.IsDeepInside(cabin,nearDoor,.12f,false),Is.False);
            nearDoor.center=new Vector3(.5f,.28f);
            Assert.That(ElevatorDoorAnimation.IsDeepInside(cabin,nearDoor,.12f,false),Is.True);
        }
        [Test]
        public void AllDestinationsContainAnAnimatedCabinAndTheArrivalSpawn()
        {
            string[] scenes={"Z00_Entrada","Z07_Aula2","Z08_Piso3Aulas"};
            for(int i=0;i<scenes.Length;i++)
            using(var inspection=SceneInspection.Open("Assets/_Project/Scenes/Zones/"+scenes[i]+".unity"))
            {
                var zone=inspection.Scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();
                Assert.That(zone.ZoneId,Is.EqualTo(ZoneExitInteractable.ElevatorZones[i]));
                var lift=zone.GetComponentsInChildren<ZoneExitInteractable>().Single(e=>e.elevatorAnimation!=null);
                var animation=lift.elevatorAnimation;
                Assert.That(animation.entranceAtTop,Is.EqualTo(i!=2));
                var spawn=zone.GetComponentsInChildren<SpawnPoint>().Single(s=>s.SpawnId==ZoneExitInteractable.ElevatorSpawn(i));
                Assert.That(animation.cabinBounds.Contains(spawn.Position),Is.True);
                Assert.That(animation.frames.Length,Is.EqualTo(10));
                Physics2D.SyncTransforms();
                var hits=new System.Collections.Generic.List<Collider2D>();var filter=ContactFilter2D.noFilter;filter.useTriggers=false;
                Physics2D.OverlapBox(spawn.Position+Vector2.up*.13f,new Vector2(.38f,.22f),0,filter,hits);
                Assert.That(hits,Is.Empty,scenes[i]+" must leave room to stand inside the lift.");
            }
        }
    }
}
