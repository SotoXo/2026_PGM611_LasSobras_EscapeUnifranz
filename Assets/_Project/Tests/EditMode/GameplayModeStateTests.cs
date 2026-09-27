using EscapeUNIFRANZ.Core;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class GameplayModeStateTests
    {
        [Test]
        public void NewState_StartsInExplore()
        {
            var state = new GameplayModeState();

            Assert.That(state.Current, Is.EqualTo(GameplayMode.Explore));
            Assert.That(state.AllowsGameplay, Is.True);
        }

        [Test]
        public void Transition_BlocksGameplay()
        {
            var state = new GameplayModeState();

            state.SetMode(GameplayMode.Transition);

            Assert.That(state.AllowsGameplay, Is.False);
        }

        [Test]
        public void ReturningToExplore_EnablesGameplay()
        {
            var state = new GameplayModeState();
            state.SetMode(GameplayMode.Transition);

            state.SetMode(GameplayMode.Explore);

            Assert.That(state.AllowsGameplay, Is.True);
        }

        [Test]
        public void Map_BlocksMovementAndInteraction_ButCanBeClosed()
        {
            var state = new GameplayModeState();

            state.SetMode(GameplayMode.Map);

            Assert.That(state.AllowsMovement, Is.False);
            Assert.That(state.AllowsInteraction, Is.False);
            Assert.That(state.AllowsMapToggle, Is.True);
        }

        [Test]
        public void Encounter_AllowsGameplay_ButBlocksMap()
        {
            var state = new GameplayModeState();

            state.SetMode(GameplayMode.Encounter);

            Assert.That(state.AllowsMovement, Is.True);
            Assert.That(state.AllowsInteraction, Is.True);
            Assert.That(state.AllowsMapToggle, Is.False);
        }

        [TestCase(GameplayMode.Presentation)]
        [TestCase(GameplayMode.Pause)]
        [TestCase(GameplayMode.Defeat)]
        [TestCase(GameplayMode.Ending)]
        public void PresentationModes_BlockGameplay(GameplayMode mode)
        {
            var state = new GameplayModeState();

            state.SetMode(mode);

            Assert.That(state.AllowsMovement, Is.False);
            Assert.That(state.AllowsInteraction, Is.False);
            Assert.That(state.AllowsMapToggle, Is.False);
        }
    }
}
