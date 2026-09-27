using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class GameStateTests
    {
        [Test]
        public void NewState_StartsWithoutFlags()
        {
            var state = new GameState();

            Assert.That(state.CompletedFlags, Is.Empty);
            Assert.That(state.HasFlag(GameFlagId.HallVisited), Is.False);
        }

        [Test]
        public void SetFlag_AddsFlag()
        {
            var state = new GameState();

            Assert.That(state.SetFlag(GameFlagId.HallVisited), Is.True);
            Assert.That(state.HasFlag(GameFlagId.HallVisited), Is.True);
        }

        [Test]
        public void SetFlag_IsIdempotent()
        {
            var state = new GameState();

            state.SetFlag(GameFlagId.HallVisited);

            Assert.That(state.SetFlag(GameFlagId.HallVisited), Is.False);
            Assert.That(state.CompletedFlags.Count, Is.EqualTo(1));
        }

        [Test]
        public void HasFlag_ReturnsCorrectValue()
        {
            var state = new GameState();
            state.SetFlag(GameFlagId.ArcaVisited);

            Assert.That(state.HasFlag(GameFlagId.ArcaVisited), Is.True);
            Assert.That(state.HasFlag(GameFlagId.HallVisited), Is.False);
            Assert.That(state.HasFlag(GameFlagId.None), Is.False);
        }

        [Test]
        public void ChangingZone_DoesNotRemoveFlags()
        {
            var state = new GameState();
            state.SetFlag(GameFlagId.HallVisited);

            state.SetLocation("arca", "from_hall");

            Assert.That(state.HasFlag(GameFlagId.HallVisited), Is.True);
            Assert.That(state.CurrentZoneId, Is.EqualTo("arca"));
        }

        [Test]
        public void SetCheckpoint_PreservesGlobalProgress()
        {
            var state = new GameState();
            state.SetFlag(GameFlagId.ArcaAccessRestored);

            bool changed = state.SetCheckpoint("piso2", "checkpoint_exoleg");

            Assert.That(changed, Is.True);
            Assert.That(state.HasCheckpoint, Is.True);
            Assert.That(state.CheckpointZoneId, Is.EqualTo("piso2"));
            Assert.That(state.CheckpointSpawnId, Is.EqualTo("checkpoint_exoleg"));
            Assert.That(state.HasFlag(GameFlagId.ArcaAccessRestored), Is.True);
        }

        [Test]
        public void ActivatingRespawnPoints_UsesLatestStableId()
        {
            var state = new GameState();

            Assert.That(RespawnPoint2D.Activate(state, "piso2", "piso2_entrada"), Is.True);
            Assert.That(state.CheckpointSpawnId, Is.EqualTo("piso2_entrada"));

            Assert.That(RespawnPoint2D.Activate(
                state,
                "piso2",
                "piso2_antes_exoleg"), Is.True);
            Assert.That(state.CheckpointZoneId, Is.EqualTo("piso2"));
            Assert.That(state.CheckpointSpawnId, Is.EqualTo("piso2_antes_exoleg"));
        }

        [Test]
        public void UpdatingRespawnPoint_DoesNotEraseGlobalFlags()
        {
            var state = new GameState();
            state.SetFlag(GameFlagId.Piso2Unlocked);

            RespawnPoint2D.Activate(state, "piso2", "piso2_antes_exoleg");

            Assert.That(state.HasFlag(GameFlagId.Piso2Unlocked), Is.True);
        }

        [Test]
        public void RepeatingSameCheckpoint_DoesNotRaiseAnotherChange()
        {
            var state = new GameState();
            int changes = 0;
            state.CheckpointChanged += (_, __) => changes++;

            Assert.That(state.SetCheckpoint("hall", "hall_principal"), Is.True);
            Assert.That(state.SetCheckpoint("hall", "hall_principal"), Is.False);
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void DiscoveredThreat_IsScopedByZoneAndStableId()
        {
            var state = new GameState();

            Assert.That(state.DiscoverWorldItem("piso3", "robot_dog"), Is.True);
            Assert.That(state.IsWorldItemDiscovered("piso3", "robot_dog"), Is.True);
            Assert.That(state.IsWorldItemDiscovered("piso2", "robot_dog"), Is.False);
            Assert.That(state.DiscoverWorldItem("piso3", "robot_dog"), Is.False);
        }

        [Test]
        public void GameCompleted_RaisesOneCanonicalVictorySignal()
        {
            var state = new GameState();
            int signals = 0;
            state.FlagChanged += flag =>
            {
                if (flag == GameFlagId.GameCompleted) signals++;
            };

            Assert.That(state.SetFlag(GameFlagId.GameCompleted), Is.True);
            Assert.That(state.SetFlag(GameFlagId.GameCompleted), Is.False);
            Assert.That(signals, Is.EqualTo(1));
        }
    }
}
