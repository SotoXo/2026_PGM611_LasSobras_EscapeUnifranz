using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class TrashBinTests
    {
        [Test]
        public void ThreeBinsSupplySixEmptyCansEachAndRememberWhatWasTaken()
        {
            const string path="Assets/_Project/Scenes/Zones/Z07_Aula2.unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var obj=new GameObject("TrashBinSession");
            try
            {
                var session=obj.AddComponent<GameSessionController>();session.StartNewGame();
                var bins=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TrashBinInteractable>()).ToArray();
                Assert.That(bins.Length,Is.EqualTo(3));Physics2D.SyncTransforms();
                var zone=bins[0].GetComponentInParent<ZoneContext>();
                var collision=zone.GetComponentsInChildren<CompositeCollider2D>().Single(c=>!c.isTrigger);
                foreach(var bin in bins)
                {
                    bin.BindSession(session);Assert.That(bin.Remaining,Is.EqualTo(6));
                    var data=new SerializedObject(bin);Assert.That(data.FindProperty("remainingFrames").arraySize,Is.EqualTo(7));
                    Assert.That(collision.OverlapPoint((Vector2)bin.transform.position+Vector2.down*.4f),Is.False);
                    for(int i=0;i<6;i++)bin.Interact();
                    Assert.That(bin.Remaining,Is.Zero);bin.Interact();
                    var renderer=bin.GetComponent<SpriteRenderer>();
                    Assert.That(renderer.sprite,Is.EqualTo(data.FindProperty("remainingFrames").GetArrayElementAtIndex(0).objectReferenceValue));
                }
                Assert.That(session.State.CarriedItems.Count,Is.EqualTo(18));
                Assert.That(session.State.CarriedItems.All(id=>id==ThrowableCanController.EmptyCanItemId),Is.True);
                Assert.That(session.State.HasItem("soda"),Is.False,"Trash bins must never supply liquid-filled cans.");
                session.RestoreState(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(session.State)));
                foreach(var bin in bins)Assert.That(bin.Remaining,Is.Zero);
                foreach(var a in bins)foreach(var b in bins)if(a!=b)Assert.That(Vector2.Distance(a.transform.position,b.transform.position),Is.GreaterThan(1));
            }
            finally{Object.DestroyImmediate(obj);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
