using EscapeUNIFRANZ.Player;
using NUnit.Framework;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class PlayerMovementLogicTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void NormalizeMovementInput_DiagonalInput_HasUnitMagnitude()
        {
            Vector2 result = PlayerMovement2D.NormalizeMovementInput(new Vector2(1f, 1f));

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(result.x, Is.EqualTo(Mathf.Sqrt(0.5f)).Within(Tolerance));
            Assert.That(result.y, Is.EqualTo(Mathf.Sqrt(0.5f)).Within(Tolerance));
        }

        [Test]
        public void NormalizeMovementInput_ZeroInput_RemainsZero()
        {
            Vector2 result = PlayerMovement2D.NormalizeMovementInput(Vector2.zero);

            Assert.That(result, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ResolveFacing_PositiveHorizontal_FacesRight()
        {
            PlayerVisualController.HorizontalFacing result = PlayerVisualController.ResolveFacing(
                Vector2.right,
                PlayerVisualController.HorizontalFacing.Left);

            Assert.That(result, Is.EqualTo(PlayerVisualController.HorizontalFacing.Right));
        }

        [Test]
        public void ResolveFacing_NegativeHorizontal_FacesLeft()
        {
            PlayerVisualController.HorizontalFacing result = PlayerVisualController.ResolveFacing(
                Vector2.left,
                PlayerVisualController.HorizontalFacing.Right);

            Assert.That(result, Is.EqualTo(PlayerVisualController.HorizontalFacing.Left));
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void ResolveFacing_VerticalMovement_PreservesPreviousFacing(float vertical)
        {
            PlayerVisualController.HorizontalFacing previous =
                PlayerVisualController.HorizontalFacing.Left;

            PlayerVisualController.HorizontalFacing result = PlayerVisualController.ResolveFacing(
                new Vector2(0f, vertical),
                previous);

            Assert.That(result, Is.EqualTo(previous));
        }
    }
}
