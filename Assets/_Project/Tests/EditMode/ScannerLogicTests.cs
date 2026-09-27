using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Interaction;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class ScannerLogicTests
    {
        [Test]
        public void ValidElement_CanBeRevealed()
        {
            Assert.That(ScanLogic.CanReveal(
                new GameState(), false, GameFlagId.None), Is.True);
        }

        [Test]
        public void LockedSecret_IsNotRevealed()
        {
            Assert.That(ScanLogic.CanReveal(
                new GameState(), true, GameFlagId.HallVisited), Is.False);
        }

        [Test]
        public void ScanEndsAfterLogicalDuration()
        {
            var timer = new ScannerTimer(2.5f, 4f);

            Assert.That(timer.TryActivate(10f), Is.True);
            Assert.That(timer.Tick(12.49f), Is.False);
            Assert.That(timer.IsActive, Is.True);
            Assert.That(timer.Tick(12.5f), Is.True);
            Assert.That(timer.IsActive, Is.False);
        }

        [Test]
        public void ScanRespectsCooldown()
        {
            var timer = new ScannerTimer(2.5f, 4f);
            timer.TryActivate(0f);
            timer.Tick(2.5f);

            Assert.That(timer.TryActivate(3.99f), Is.False);
            Assert.That(timer.TryActivate(4f), Is.True);
        }
    }
}
