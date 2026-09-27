using EscapeUNIFRANZ.Encounters;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class RobotDogEncounterTests
    {
        [Test]
        public void DetectionAndSearch_ReturnToPatrol()
        {
            var machine = new RobotDogEncounterStateMachine();

            Assert.That(machine.Detect(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(RobotDogEncounterState.Alert));
            Assert.That(machine.LoseTarget(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(RobotDogEncounterState.Search));
            Assert.That(machine.FinishSearch(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(RobotDogEncounterState.Patrol));
        }

        [Test]
        public void TwoNetworkNodes_DisableDog()
        {
            var machine = new RobotDogEncounterStateMachine();

            Assert.That(machine.DisableNode(0), Is.True);
            Assert.That(machine.Current, Is.EqualTo(RobotDogEncounterState.Patrol));
            Assert.That(machine.DisableNode(1), Is.True);
            Assert.That(machine.Current, Is.EqualTo(RobotDogEncounterState.Disabled));
        }

        [Test]
        public void DisabledDog_DoesNotReturnToPatrol()
        {
            var machine = new RobotDogEncounterStateMachine();
            machine.DisableNode(0);
            machine.DisableNode(1);

            Assert.That(machine.Reset(), Is.False);
            Assert.That(machine.Detect(), Is.False);
            Assert.That(machine.Current, Is.EqualTo(RobotDogEncounterState.Disabled));
        }
    }
}
