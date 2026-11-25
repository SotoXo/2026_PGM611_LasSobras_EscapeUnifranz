using EscapeUNIFRANZ.EditorTools;
using System.Linq;
using EscapeUNIFRANZ.Hazards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class EnemySceneWiringTests
    {
        [TestCase("Z00_Entrada")]
        [TestCase("Z01_Hall")]
        [TestCase("Z02_ARCA")]
        [TestCase("Z03_Piso2")]
        [TestCase("Z04_Piso3")]
        [TestCase("Z05_LabSoftware")]
        [TestCase("Z06_NucleoIA")]
        [TestCase("Z07_Aula2")]
        public void SceneHasValidEnemyRoutesAndVisualBindings(string zone)
        {
            var path="Assets/_Project/Scenes/Zones/"+zone+".unity";
            using var inspection = SceneInspection.Open(path);
            var scene = inspection.Scene;
            Physics2D.SyncTransforms();
            foreach(var root in scene.GetRootGameObjects())
            {
                Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true).All(c=>c!=null),Is.True,"Missing script in "+zone);
                foreach(var mover in root.GetComponentsInChildren<PatrolMover2D>(true))
                {
                    Assert.That(mover.GetComponent<Rigidbody2D>(),Is.Not.Null);
                    var route=(PatrolPath2D)new SerializedObject(mover).FindProperty("path").objectReferenceValue;
                    Assert.That(route,Is.Not.Null,mover.name); Assert.That(route.Count,Is.GreaterThan(0));
                    var points=new SerializedObject(route).FindProperty("points");
                    for(int i=0;i<points.arraySize;i++)
                    {
                        Assert.That(points.GetArrayElementAtIndex(i).objectReferenceValue,Is.Not.Null);
                        var blockers=Physics2D.OverlapCircleAll(route.GetPoint(i),.18f).Where(c=>c.gameObject.scene==scene&&!c.isTrigger&&!c.transform.IsChildOf(mover.transform));
                        Assert.That(blockers.Count(),Is.Zero,zone+" "+mover.name+" waypoint "+i);
                    }
                }
                foreach(var robot in root.GetComponentsInChildren<MiniAutoController>(true))
                {
                    var so=new SerializedObject(robot);
                    foreach(var field in new[]{"mover","sensor","carVisual","movementArt","verticalArt","impactArt"})
                        Assert.That(so.FindProperty(field).objectReferenceValue,Is.Not.Null,robot.name+" "+field);
                }
                foreach(var view in root.GetComponentsInChildren<ThreatMotionView2D>(true))
                {
                    var visual=(Transform)new SerializedObject(view).FindProperty("visual").objectReferenceValue;
                    Assert.That(visual,Is.Not.Null); Assert.That(visual,Is.Not.EqualTo(view.transform));
                }
            }
        }
    }
}
