using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Map;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class CampusMapProgressionTests
    {
        [Test]
        public void HiddenZone_IsUnknown()
        {
            var state = new GameState();
            var node = new CampusMapNodeDefinition(
                "nucleo_ia", "NÚCLEO IA", GameFlagId.NucleoVisited,
                GameFlagId.NucleoUnlocked, GameFlagId.LabSoftwareVisited);

            Assert.That(CampusMapProgression.Resolve(state, node),
                Is.EqualTo(CampusMapNodeState.Unknown));
        }

        [Test]
        public void RevealedButLockedZone_IsLocked()
        {
            var state = new GameState();
            state.SetFlag(GameFlagId.HallVisited);
            var node = new CampusMapNodeDefinition(
                "piso2", "PISO 2", GameFlagId.Piso2Visited,
                GameFlagId.Piso2Unlocked, GameFlagId.HallVisited);

            Assert.That(CampusMapProgression.Resolve(state, node),
                Is.EqualTo(CampusMapNodeState.Locked));
        }

        [Test]
        public void UnlockedZone_IsDiscovered()
        {
            var state = new GameState();
            state.SetFlag(GameFlagId.HallVisited);
            state.SetFlag(GameFlagId.Piso2Unlocked);
            var node = new CampusMapNodeDefinition(
                "piso2", "PISO 2", GameFlagId.Piso2Visited,
                GameFlagId.Piso2Unlocked, GameFlagId.HallVisited);

            Assert.That(CampusMapProgression.Resolve(state, node),
                Is.EqualTo(CampusMapNodeState.Discovered));
        }

        [Test]
        public void VisitedZone_IsCompleted()
        {
            var state = new GameState();
            state.SetFlag(GameFlagId.Piso2Unlocked);
            state.SetFlag(GameFlagId.Piso2Visited);
            var node = new CampusMapNodeDefinition(
                "piso2", "PISO 2", GameFlagId.Piso2Visited, GameFlagId.Piso2Unlocked);

            Assert.That(CampusMapProgression.Resolve(state, node),
                Is.EqualTo(CampusMapNodeState.Completed));
        }

        [Test]
        public void CurrentZone_OverridesOtherVisualStates()
        {
            var state = new GameState();
            state.SetLocation("piso2", "from_hall");
            var node = new CampusMapNodeDefinition(
                "piso2", "PISO 2", GameFlagId.Piso2Visited, GameFlagId.Piso2Unlocked);

            Assert.That(CampusMapProgression.Resolve(state, node),
                Is.EqualTo(CampusMapNodeState.Current));
        }

        [Test]
        public void ResolvingMap_DoesNotMutateGameState()
        {
            var state = new GameState();
            var node = new CampusMapNodeDefinition(
                "arca", "ARCA", GameFlagId.ArcaVisited);
            int flagCount = state.CompletedFlags.Count;

            CampusMapProgression.Resolve(state, node);

            Assert.That(state.CompletedFlags.Count, Is.EqualTo(flagCount));
            Assert.That(state.CurrentZoneId, Is.Empty);
        }
    }
}
