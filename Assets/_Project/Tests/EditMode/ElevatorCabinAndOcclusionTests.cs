using System.Collections;
using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.EditorTools;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class ElevatorCabinAndOcclusionTests
    {
        [Test]
        public void BoardingRequiresTheFullPhysicalFootprintAndLeavesThresholdOpenWhenPartial()
        {
            var elevator = new GameObject("Elevator");
            var player = new GameObject("Player");
            var texture = new Texture2D(4, 2);
            var closed = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            var open = Sprite.Create(texture, new Rect(2, 0, 2, 2), Vector2.zero);
            try
            {
                var body = player.AddComponent<Rigidbody2D>();
                var feet = player.AddComponent<CapsuleCollider2D>();
                feet.size = new Vector2(.38f, .22f);
                feet.offset = new Vector2(0, .13f);
                var flow = player.AddComponent<SceneFlowController>();
                Set(flow, "playerBody", body);
                Set(flow, "gameplayMode", player.AddComponent<GameplayModeController>());
                var animation = elevator.AddComponent<ElevatorDoorAnimation>();
                animation.doorRenderer = elevator.AddComponent<SpriteRenderer>();
                animation.frames = new[] { closed, open };
                animation.cabinBounds = new Rect(-.4f, -.3f, .8f, .7f);
                animation.BeginArrival();
                Run(animation.Arrive());

                body.position = new Vector2(0, .9f);
                Physics2D.SyncTransforms();
                Assert.That(animation.ShouldBoard(flow), Is.False);
                body.position = new Vector2(0, .28f);
                Physics2D.SyncTransforms();
                Assert.That(animation.cabinBounds.Contains(body.position), Is.True);
                Assert.That(animation.ShouldBoard(flow), Is.False);
                Run(animation.PrepareTravel(body));
                Assert.That(animation.State, Is.EqualTo(ElevatorState.AwaitingPassenger));
                Assert.That(animation.doorRenderer.sprite, Is.SameAs(open));

                body.position = Vector2.zero;
                Physics2D.SyncTransforms();
                Assert.That(animation.ShouldBoard(flow), Is.True);
                Run(animation.PrepareTravel(body));
                Assert.That(animation.Boarded, Is.True);
                Assert.That(animation.doorRenderer.sprite, Is.SameAs(closed));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(elevator);
                Object.DestroyImmediate(closed);
                Object.DestroyImmediate(open);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ArrivalOpensDoorsAndRequiresLeavingCabinBeforeReboarding()
        {
            var elevator = new GameObject("Elevator"); var actor = new GameObject("Player");
            var texture = new Texture2D(4, 2);
            var closed = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            var open = Sprite.Create(texture, new Rect(2, 0, 2, 2), Vector2.zero);
            try
            {
                var body = actor.AddComponent<Rigidbody2D>(); body.gravityScale = 0;
                var flow = actor.AddComponent<SceneFlowController>(); var mode = actor.AddComponent<GameplayModeController>();
                Set(flow, "playerBody", body); Set(flow, "gameplayMode", mode);
                var animation = elevator.AddComponent<ElevatorDoorAnimation>();
                animation.doorRenderer = elevator.AddComponent<SpriteRenderer>();
                animation.frames = new[] { closed, open }; animation.frameSeconds = new[] { .1f, .1f };
                animation.cabinBounds = new Rect(-.5f, -.5f, 1, 1); animation.cabinCenter = Vector2.zero;
                animation.BeginArrival(); Assert.That(animation.doorRenderer.sprite, Is.SameAs(closed));
                Run(animation.Arrive()); Assert.That(animation.doorRenderer.sprite, Is.SameAs(open));
                Assert.That(animation.ShouldBoard(flow), Is.False, "Arrival must not immediately send the player back.");
                body.position = Vector2.up * 2; Assert.That(animation.ShouldBoard(flow), Is.False);
                body.position = Vector2.zero; Assert.That(animation.ShouldBoard(flow), Is.True);
                Run(animation.PrepareTravel(body));
                Assert.That(animation.Boarded, Is.True);
                Assert.That(animation.doorRenderer.sprite, Is.SameAs(closed));
            }
            finally { Object.DestroyImmediate(elevator); Object.DestroyImmediate(actor); Object.DestroyImmediate(closed); Object.DestroyImmediate(open); Object.DestroyImmediate(texture); }
        }

        [TestCase("Z00_Entrada", "from_aula2_elevator")]
        [TestCase("Z07_Aula2", "from_elevator")]
        public void AuthoredArrivalsHoldTheFullPlayerFootprint(string name, string spawnId)
        {
            Vector2 offset;
            Vector2 size;
            using (var bootstrap = SceneInspection.Open("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity"))
            {
                var footprint = bootstrap.Scene.GetRootGameObjects().SelectMany(root =>
                        root.GetComponentsInChildren<EscapeUNIFRANZ.Player.PlayerMovement2D>(true)).Single()
                    .GetComponent<CapsuleCollider2D>();
                offset = footprint.offset;
                size = footprint.size;
            }

            using var zone = SceneInspection.Open("Assets/_Project/Scenes/Zones/" + name + ".unity");
            var roots = zone.Scene.GetRootGameObjects();
            var spawn = roots.SelectMany(root => root.GetComponentsInChildren<SpawnPoint>(true))
                .Single(point => point.SpawnId == spawnId);
            var doors = roots.SelectMany(root => root.GetComponentsInChildren<ElevatorDoorAnimation>(true));
            var hull = new Bounds(spawn.Position + offset, size);
            Assert.That(doors.Any(door => ElevatorDoorAnimation.IsFullyInside(door.cabinBounds, hull)), Is.True);
        }

        [Test]
        public void FurnitureOcclusionIgnoresTransparentPixelsAndBackgroundSprites()
        {
            var furniture = new GameObject("Furniture"); var actor = new GameObject("Character");
            var texture = new Texture2D(8, 8); var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels); texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(.5f, .5f), 8, 0, SpriteMeshType.FullRect);
            try
            {
                var prop = furniture.AddComponent<SpriteRenderer>(); var player = actor.AddComponent<SpriteRenderer>();
                prop.sprite = sprite; player.sprite = sprite; prop.sortingOrder = 10;
                Assert.That(WorldObjectOcclusion2D.IsOccluding(prop, player), Is.True);
                prop.sortingOrder = -10;
                Assert.That(WorldObjectOcclusion2D.IsOccluding(prop, player), Is.False);
                prop.sortingOrder = 10; texture.SetPixels(new Color[64]); texture.Apply();
                Assert.That(WorldObjectOcclusion2D.IsOccluding(prop, player), Is.False);
            }
            finally { Object.DestroyImmediate(furniture); Object.DestroyImmediate(actor); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static void Run(IEnumerator coroutine)
        {
            while (coroutine.MoveNext()) if (coroutine.Current is IEnumerator child) Run(child);
        }
    }
}
