using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.Core;
using NUnit.Framework;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class PlayerVisionDirectionTests
    {
        [Test]
        public void FlashlightCannotTurnOnBeforePickup()
        {
            var state = new GameState();
            Assert.That(state.HasActiveFlashlight, Is.False);
            Assert.That(state.ToggleFlashlight(), Is.False);
            Assert.That(state.TryCollectPickup("test-flashlight", "flashlight"), Is.True);
            Assert.That(state.HasActiveFlashlight, Is.True);
            Assert.That(state.ToggleFlashlight(), Is.True);
            Assert.That(state.HasActiveFlashlight, Is.False);
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
