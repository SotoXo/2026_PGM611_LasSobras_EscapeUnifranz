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
                    Assert.That(route.Count,Is.EqualTo(4));
                }
                var machine=zone.GetComponentsInChildren<VendingMachineInteractable>().Single();
                var session=sessionObject.AddComponent<GameSessionController>();session.StartNewGame();machine.BindSession(session);
                var dataMachine=new SerializedObject(machine);
                Assert.That(dataMachine.FindProperty("purchaseFrames").arraySize,Is.EqualTo(6));
                Assert.That(machine.CanInteract,Is.True);
                var dispense=(IEnumerator)typeof(VendingMachineInteractable).GetMethod("Dispense",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(machine,null);
                while(dispense.MoveNext()){}
                Assert.That(machine.Prompt,Does.Contain("Recoger"));machine.Interact();
                Assert.That(session.State.HasItem("soda"),Is.True);
                Assert.That(session.State.IsWorldItemDiscovered("vending","aula2_abril_prueba:0"),Is.True);
                Assert.That(zone.GetComponentsInChildren<CompositeCollider2D>().Length,Is.EqualTo(1));
            }
            finally{Object.DestroyImmediate(sessionObject);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
