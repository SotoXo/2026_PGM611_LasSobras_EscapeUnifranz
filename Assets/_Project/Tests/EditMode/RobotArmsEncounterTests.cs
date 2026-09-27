using EscapeUNIFRANZ.Encounters;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class RobotArmsEncounterTests
    {
        [Test]
        public void FullStateFlow_ReachesDisabled()
        {
            var machine = new RobotArmsEncounterStateMachine();

            Assert.That(machine.BeginSequence(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(RobotArmsEncounterState.Sequence));
            Assert.That(machine.OpenVulnerability(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(RobotArmsEncounterState.Vulnerable));
            Assert.That(machine.Disable(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(RobotArmsEncounterState.Disabled));
        }

        [Test]
        public void PanelCannotDisableOutsideVulnerableState()
        {
            var machine = new RobotArmsEncounterStateMachine();

            Assert.That(machine.Disable(), Is.False);
            Assert.That(machine.Current, Is.EqualTo(RobotArmsEncounterState.Idle));
        }

        [Test]
        public void DisabledArms_DoNotReset()
        {
            var machine = new RobotArmsEncounterStateMachine();
            machine.BeginSequence();
            machine.OpenVulnerability();
            machine.Disable();

            Assert.That(machine.Reset(), Is.False);
            Assert.That(machine.Current, Is.EqualTo(RobotArmsEncounterState.Disabled));
        }
    }
}
