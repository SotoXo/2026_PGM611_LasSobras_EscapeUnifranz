using System.Linq;
using EscapeUNIFRANZ.EditorTools;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class FiveFloorTests
    {
        [TestCase(3,"Z08_Piso3Aulas")]
        [TestCase(4,"Z09_Piso4Aulas")]
        [TestCase(5,"Z10_Piso5Aulas")]
        public void FloorsHaveIndependentSuppliesAndOnlyThirdHasAuthoredDecoration(int floor,string name)
        {
            using(var inspection=SceneInspection.Open("Assets/_Project/Scenes/Zones/"+name+".unity"))
            {
                var zone=inspection.Scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();
                Assert.That(zone.ZoneId,Is.EqualTo("piso"+floor+"_aulas"));
                Assert.That(zone.transform.Find("Decoracion_Autor")!=null,Is.EqualTo(floor<=4));
                var effect=zone.GetComponentInChildren<CrabRoomRupture>();
                if(floor==3){Assert.That(effect,Is.Not.Null);Assert.That(effect.frames.Length,Is.EqualTo(24));Assert.That(effect.frames.All(f=>f!=null),Is.True);Assert.That(effect.crab,Is.Not.Null);Assert.That(effect.transform.position.x,Is.EqualTo(968f/32));Assert.That(effect.transform.position.y,Is.EqualTo(228f/32));}
                else Assert.That(effect,Is.Null);
                var bins=zone.GetComponentsInChildren<TrashBinInteractable>();Assert.That(bins.Length,Is.EqualTo(floor==3?3:2));
                Physics2D.SyncTransforms();
                foreach(var bin in bins)
                {
                    Assert.That(new SerializedObject(bin).FindProperty("binId").stringValue,Does.StartWith("piso"+floor+"_"));
                    var colliders=Physics2D.OverlapBoxAll(bin.transform.position+Vector3.down*.27f,new Vector2(.38f,.22f),0);
                    Assert.That(colliders.Where(c=>!c.isTrigger),Is.Empty,"The front of the bin must be accessible.");
                }
                var machine=zone.GetComponentsInChildren<VendingMachineInteractable>().Single();
                Assert.That(new SerializedObject(machine).FindProperty("machineId").stringValue,Is.EqualTo("piso"+floor+"_bebidas"));
                var catalog=AssetDatabase.LoadAssetAtPath<ZoneCatalog>("Assets/_Project/Data/Zones/ZoneCatalog.asset");Assert.That(catalog.TryResolveScene(zone.ZoneId,out var path,out _),Is.True);
                Assert.That(EditorBuildSettings.scenes.Any(s=>s.path==path&&s.enabled),Is.True);
            }
        }
    }
}
