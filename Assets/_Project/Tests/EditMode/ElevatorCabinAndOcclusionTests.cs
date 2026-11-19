using System.Collections;
using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.Input;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class ElevatorCabinAndOcclusionTests
    {
        [UnityTest]
        public IEnumerator BoardingMovesDisabledPlayerInsideAndDoesNotCloseWhenBlocked()
        {
            yield return new EnterPlayMode();
            var cabin = new Vector2(-100f, -100f);
            var actor = new GameObject("BoardingPlayer"); actor.SetActive(false);
            var body = actor.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
            var feet = actor.AddComponent<CapsuleCollider2D>(); feet.size = new Vector2(.38f, .22f); feet.offset = new Vector2(0, .13f);
            var input = actor.AddComponent<PlayerInputReader>(); input.enabled = false;
            var movement = actor.AddComponent<PlayerMovement2D>(); Set(movement, "inputReader", input);
            actor.SetActive(true); movement.SetMovementEnabled(false); body.position = cabin + Vector2.up * 1.4f;
            var elevator = new GameObject("BoardingDoor"); var animation = elevator.AddComponent<ElevatorDoorAnimation>();
            var texture = new Texture2D(4, 2);
            var closed = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            var open = Sprite.Create(texture, new Rect(2, 0, 2, 2), Vector2.zero);
            animation.doorRenderer = elevator.AddComponent<SpriteRenderer>(); animation.doorRenderer.sprite = open;
            animation.frames = new[] { closed, open }; animation.frameSeconds = new[] { .01f, .01f };
            animation.cabinBounds = new Rect(cabin + new Vector2(-.4f, -.3f), new Vector2(.8f, .6f)); animation.cabinCenter = cabin;
            yield return SimulateBoarding(animation, body);
            Assert.That(animation.Boarded, Is.True, $"Scripted boarding must work with input disabled: position={body.position}, playing={Application.isPlaying}, physics={Physics2D.simulationMode}, simulated={body.simulated}.");
            Assert.That(Vector2.Distance(body.position, cabin), Is.LessThan(.08f));
            Assert.That(animation.doorRenderer.sprite, Is.SameAs(closed));
            body.position = cabin + Vector2.up * 1.4f;
            var obstruction = new GameObject("BlockedEntrance"); obstruction.transform.position = cabin + Vector2.up * .7f;
            obstruction.AddComponent<BoxCollider2D>().size = new Vector2(2f, .2f); Physics2D.SyncTransforms();
            yield return SimulateBoarding(animation, body);
            Assert.That(animation.Boarded, Is.False, "A blocked passenger must never close the door or begin travel.");
            Assert.That(animation.doorRenderer.sprite, Is.SameAs(open));
            animation.CancelTravel(); animation.SetAvailable(true);
            Assert.That(animation.doorRenderer.sprite, Is.SameAs(open), "Cancelling blocked boarding must keep the doors open.");
            Object.Destroy(actor); Object.Destroy(elevator); Object.Destroy(obstruction);
            Object.Destroy(closed); Object.Destroy(open); Object.Destroy(texture);
            yield return null;
            yield return new ExitPlayMode();
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
        private static IEnumerator SimulateBoarding(ElevatorDoorAnimation animation, Rigidbody2D body)
        {
            // EditMode's EnterPlayMode runner does not advance automatic 2D
            // physics consistently. Drive actual collision simulation explicitly.
            var previous = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
            try
            {
                var boarding = animation.PrepareTravel(body);
                while (boarding.MoveNext())
                {
                    if (boarding.Current is IEnumerator child) Run(child);
                    else
                    {
                        Assert.That(Physics2D.Simulate(.02f), Is.True);
                        yield return null;
                    }
                }
            }
            finally { Physics2D.simulationMode = previous; }
        }
    }
}
