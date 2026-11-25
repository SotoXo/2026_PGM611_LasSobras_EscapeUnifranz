using System.Linq;
using EscapeUNIFRANZ.EditorTools;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class ThreeFloorGameplayTests
    {
        [TestCase("Z00_Entrada")]
        [TestCase("Z01_Hall")]
        [TestCase("Z02_ARCA")]
        [TestCase("Z07_Aula2")]
        [TestCase("Z08_Piso3Aulas")]
        public void FloorsShareOneColdDimAmbientLight(string name)
        {
            using(var inspection=SceneInspection.Open("Assets/_Project/Scenes/Zones/"+name+".unity"))
            {
                var lights=inspection.Scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Light2D>()).Where(l=>l.enabled&&l.lightType==Light2D.LightType.Global).ToArray();
                Assert.That(lights.Length,Is.EqualTo(1));
                Assert.That(lights[0].intensity,Is.EqualTo(.095f).Within(.001f));
                Assert.That(lights[0].color.b,Is.GreaterThan(lights[0].color.r));
            }
        }
        [TestCase("Z07_Aula2","piso3_aulas","from_piso3")]
        [TestCase("Z08_Piso3Aulas","aula2","from_elevator")]
        public void BothFloorsHaveReturnAccessAndUnobstructedArrival(string name,string destination,string arrivalId)
        {
            using(var inspection=SceneInspection.Open("Assets/_Project/Scenes/Zones/"+name+".unity"))
            {
                var zone=inspection.Scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();
                Assert.That(zone.GetComponentsInChildren<ZoneExitInteractable>().Any(e=>e.TargetZoneId==destination),Is.True);
                var spawn=zone.GetComponentsInChildren<SpawnPoint>().Single(s=>s.SpawnId==arrivalId);
                Physics2D.SyncTransforms();
                var hits=new System.Collections.Generic.List<Collider2D>();var filter=ContactFilter2D.noFilter;filter.useTriggers=false;
                Physics2D.OverlapBox(spawn.Position+Vector2.up*.13f,new Vector2(.38f,.22f),0,filter,hits);
                Assert.That(hits,Is.Empty,"The player must arrive clear of furniture, walls and robot bodies.");
            }
        }
        [Test]
        public void ThirdFloorContainsSeparatePatrolsAndItsOwnVendingStock()
        {
            const string path="Assets/_Project/Scenes/Zones/Z08_Piso3Aulas.unity";
            using(var inspection=SceneInspection.Open(path))
            {
                var zone=inspection.Scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();
                var robots=zone.GetComponentsInChildren<MiniAutoController>();Assert.That(robots.Length,Is.EqualTo(3));
                foreach(var a in robots)foreach(var b in robots)if(a!=b)Assert.That(Vector2.Distance(a.transform.position,b.transform.position),Is.GreaterThan(3));
                var machine=zone.GetComponentsInChildren<VendingMachineInteractable>().Single();
                Assert.That(new SerializedObject(machine).FindProperty("machineId").stringValue,Is.EqualTo("piso3_bebidas"));
                Assert.That(EditorBuildSettings.scenes.Any(s=>s.path==path&&s.enabled),Is.True);
                var catalog=AssetDatabase.LoadAssetAtPath<ZoneCatalog>("Assets/_Project/Data/Zones/ZoneCatalog.asset");
                Assert.That(catalog.TryResolveScene("piso3_aulas",out var resolved,out _),Is.True);Assert.That(resolved,Is.EqualTo(path));
            }
        }
    }
}
