using System.Linq;
using System.Reflection;
using EscapeUNIFRANZ.EditorTools;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEngine;
namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class CrackCollisionTests
    {
        [TestCase(14,0)] [TestCase(15,7)] [TestCase(29,7)] [TestCase(30,15)] [TestCase(44,15)] [TestCase(45,23)]
        public void DamageAdvancesOnlyAtFifteenHitIntervals(int hits,int frame)=>Assert.That(CrabRoomRupture.StageFrame(hits),Is.EqualTo(frame));
        [Test] public void LargeHolesBlockThePlayerOnlyWhenTheyAppear()
        {
            using(var inspection=SceneInspection.Open("Assets/_Project/Scenes/Zones/Z08_Piso3Aulas.unity"))
            {
                var effect=inspection.Scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<CrabRoomRupture>()).Single();
                Assert.That(effect.middleHoles.Length,Is.EqualTo(2));Assert.That(effect.finalHoles.Length,Is.EqualTo(4));
                foreach(int frame in new[]{7,15,23})
                {
                    typeof(CrabRoomRupture).GetField("frame",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(effect,frame);effect.RefreshObstacles();
                    Assert.That(effect.middleHoles.All(c=>c.enabled),Is.EqualTo(frame==15));Assert.That(effect.finalHoles.All(c=>c.enabled),Is.EqualTo(frame==23));
                    if(frame<15)continue;
                    var hole=(frame==15?effect.middleHoles:effect.finalHoles)[0];Physics2D.SyncTransforms();
                    var hits=Physics2D.BoxCastAll((Vector2)hole.bounds.center+Vector2.left*1.2f,new Vector2(.38f,.22f),0,Vector2.right,2.4f);
                    Assert.That(hits.Any(h=>h.collider==hole),Is.True,"A player-sized body must be stopped by the large hole.");
                }
            }
        }
    }
}
