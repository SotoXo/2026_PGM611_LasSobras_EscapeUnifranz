using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEngine;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class PerformanceBudgetTests
    {
        [Test]
        public void RoomLightingKeepsVisibleAndAdjacentRooms()
        {
            var room = new Rect(1, 1, 6, 3);
            Assert.That(RoomLightingController.IntersectsViewport(room, Vector2.zero, 4.5f, 16f / 9f, false), Is.True);
            Assert.That(RoomLightingController.IntersectsViewport(room, new Vector2(30, 5), 4.5f, 16f / 9f, true), Is.False);
        }

        [TestCase(0f, -80f)]
        [TestCase(.5f, -6.0206f)]
        [TestCase(1f, 0f)]
        public void MixerGainUsesDecibelsAndRetainsMute(float gain, float expected)
        {
            Assert.That(GameOptions.VolumeInDecibels(gain), Is.EqualTo(expected).Within(.001f));
        }
    }
}
