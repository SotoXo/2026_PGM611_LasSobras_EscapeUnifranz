using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Map;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class MinimapLogicTests
    {
        [Test]
        public void CurrentZone_UsesStableZoneId()
        {
            var state = new GameState();
            state.SetLocation("hall", "from_entrada");

            Assert.That(MinimapLogic.IsCurrentZone(state, "hall"), Is.True);
            Assert.That(MinimapLogic.IsCurrentZone(state, "entrada"), Is.False);
        }

        [Test]
        public void CurrentCheckpoint_RequiresMatchingZoneAndSpawn()
        {
            var state = new GameState();
            state.SetCheckpoint("piso2", "piso2_antes_exoleg");

            Assert.That(MinimapLogic.IsCurrentCheckpoint(
                state, "piso2", "piso2_antes_exoleg"), Is.True);
            Assert.That(MinimapLogic.IsCurrentCheckpoint(
                state, "piso3", "piso2_antes_exoleg"), Is.False);
        }

        [Test]
        public void LockedExit_ChangesToAvailableAfterFlag()
        {
            var state = new GameState();

            Assert.That(MinimapLogic.ResolveExitState(
                state, GameFlagId.Piso2Unlocked, true, false),
                Is.EqualTo(MinimapExitState.Blocked));

            state.SetFlag(GameFlagId.Piso2Unlocked);

            Assert.That(MinimapLogic.ResolveExitState(
                state, GameFlagId.Piso2Unlocked, true, false),
                Is.EqualTo(MinimapExitState.Available));
        }

        [Test]
        public void UnknownOrSecretExit_IsNotShown()
        {
            var state = new GameState();

            Assert.That(MinimapLogic.ResolveExitState(
                state, GameFlagId.None, false, false),
                Is.EqualTo(MinimapExitState.Unknown));
            Assert.That(MinimapLogic.ResolveExitState(
                state, GameFlagId.None, true, true),
                Is.EqualTo(MinimapExitState.Unknown));
        }
    }
}
