using EscapeUNIFRANZ.Player;
using NUnit.Framework;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class PlayerVisionDirectionTests
    {
        [Test]
        public void FlashlightRevealsForwardTargetButNeverThroughWallsOrWhenOff()
        {
            var player=new GameObject("VisionAwarenessTest");
            var wall=new GameObject("OpaqueWall");
            try
            {
                player.transform.position=new Vector2(200,200);
                player.AddComponent<PlayerMovement2D>();
                player.AddComponent<PlayerVisualController>();
                var vision=player.AddComponent<PlayerVisionController>();
                vision.ApplyLightConfiguration();
                Vector2 ahead=new Vector2(200,197);
                vision.SetFlashlightState(true,false);
                Assert.That(vision.IlluminatesPoint(ahead),Is.False);
                vision.SetFlashlightState(true,true);
                Assert.That(vision.IlluminatesPoint(ahead),Is.True);
                Assert.That(vision.IlluminatesPoint(new Vector2(200,203)),Is.False);
                wall.transform.position=new Vector2(200,198.5f);
                wall.AddComponent<BoxCollider2D>().size=new Vector2(2,.2f);
                Physics2D.SyncTransforms();
                Assert.That(vision.IlluminatesPoint(ahead),Is.False);
                Assert.That(vision.IsLookingAt(new Vector2(200,198)),Is.True);
                Assert.That(vision.IsLookingAt(new Vector2(200,202)),Is.False);
            }
            finally
            {
                Object.DestroyImmediate(wall);Object.DestroyImmediate(player);
                Physics2D.SyncTransforms();
            }
        }
        [TestCase(PlayerVisualController.FacingDirection.Front, 180f)]
        [TestCase(PlayerVisualController.FacingDirection.Back, 0f)]
        [TestCase(PlayerVisualController.FacingDirection.Left, 90f)]
        [TestCase(PlayerVisualController.FacingDirection.Right, -90f)]
        public void VisionRotation_UsesPlayerVisualDirection(
            PlayerVisualController.FacingDirection direction,
            float expectedDegrees)
        {
            Assert.That(
                PlayerVisionController.ResolveRotationDegrees(direction),
                Is.EqualTo(expectedDegrees));
        }

        [Test]
        public void ZeroMovement_KeepsLastDirectionAndVisionRotation()
        {
            PlayerVisualController.FacingDirection previous =
                PlayerVisualController.FacingDirection.Left;
            PlayerVisualController.FacingDirection resolved =
                PlayerVisualController.ResolveDirection(Vector2.zero, previous);

            Assert.That(resolved, Is.EqualTo(previous));
            Assert.That(
                PlayerVisionController.ResolveRotationDegrees(resolved),
                Is.EqualTo(90f));
        }

        [TestCase(PlayerVisualController.FacingDirection.Front, 0f, -1f)]
        [TestCase(PlayerVisualController.FacingDirection.Back, 0f, 1f)]
        [TestCase(PlayerVisualController.FacingDirection.Left, -1f, 0f)]
        [TestCase(PlayerVisualController.FacingDirection.Right, 1f, 0f)]
        public void ConePositiveYAxis_AlignsWithSpriteFacing(
            PlayerVisualController.FacingDirection facing, float x, float y)
        {
            Vector3 emitted = Quaternion.Euler(0f, 0f,
                PlayerVisionController.ResolveRotationDegrees(facing)) * Vector3.up;
            Assert.That(Vector2.Distance(emitted, new Vector2(x, y)), Is.LessThan(0.0001f));
        }
    }
}
