using EscapeUNIFRANZ.Player;
using NUnit.Framework;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class PlayerVisionDirectionTests
    {
        [TestCase(PlayerVisualController.FacingDirection.Front, 0f)]
        [TestCase(PlayerVisualController.FacingDirection.Back, 180f)]
        [TestCase(PlayerVisualController.FacingDirection.Left, -90f)]
        [TestCase(PlayerVisualController.FacingDirection.Right, 90f)]
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
                Is.EqualTo(-90f));
        }
    }
}
