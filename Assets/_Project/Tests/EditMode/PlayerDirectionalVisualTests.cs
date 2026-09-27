using EscapeUNIFRANZ.Player;
using NUnit.Framework;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class PlayerDirectionalVisualTests
    {
        [TestCase(0f, -1f, PlayerVisualController.FacingDirection.Front)]
        [TestCase(0f, 1f, PlayerVisualController.FacingDirection.Back)]
        [TestCase(-1f, 0f, PlayerVisualController.FacingDirection.Left)]
        [TestCase(1f, 0f, PlayerVisualController.FacingDirection.Right)]
        public void ResolveDirection_CardinalInput_UsesMatchingDirection(
            float x,
            float y,
            PlayerVisualController.FacingDirection expected)
        {
            PlayerVisualController.FacingDirection result =
                PlayerVisualController.ResolveDirection(
                    new Vector2(x, y),
                    PlayerVisualController.FacingDirection.Front);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ResolveDirection_NoInput_PreservesLastDirection()
        {
            PlayerVisualController.FacingDirection result =
                PlayerVisualController.ResolveDirection(
                    Vector2.zero,
                    PlayerVisualController.FacingDirection.Left);

            Assert.That(result, Is.EqualTo(PlayerVisualController.FacingDirection.Left));
        }

        [Test]
        public void ResolveDirection_DiagonalInput_UsesDominantAxis()
        {
            PlayerVisualController.FacingDirection horizontal =
                PlayerVisualController.ResolveDirection(
                    new Vector2(-1f, 0.25f),
                    PlayerVisualController.FacingDirection.Front);
            PlayerVisualController.FacingDirection vertical =
                PlayerVisualController.ResolveDirection(
                    new Vector2(0.25f, 1f),
                    PlayerVisualController.FacingDirection.Front);

            Assert.That(horizontal, Is.EqualTo(PlayerVisualController.FacingDirection.Left));
            Assert.That(vertical, Is.EqualTo(PlayerVisualController.FacingDirection.Back));
        }
    }
}
