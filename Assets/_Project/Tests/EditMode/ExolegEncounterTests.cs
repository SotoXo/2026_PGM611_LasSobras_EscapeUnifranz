using EscapeUNIFRANZ.Encounters;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class ExolegEncounterTests
    {
        [Test]
        public void FullStateFlow_ReachesDisabled()
        {
            var machine = new ExolegEncounterStateMachine(2);

            Assert.That(machine.BeginWarning(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(ExolegEncounterState.Warning));
            Assert.That(machine.BeginActive(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(ExolegEncounterState.Active));
            Assert.That(machine.ActivateOverloadPoint(), Is.True);
            Assert.That(machine.ActivateOverloadPoint(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(ExolegEncounterState.Overloaded));
            Assert.That(machine.Disable(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(ExolegEncounterState.Disabled));
        }

        [Test]
        public void DisabledState_DoesNotReset()
        {
            var machine = new ExolegEncounterStateMachine(1);
            machine.BeginWarning();
            machine.BeginActive();
            machine.ActivateOverloadPoint();
            machine.Disable();

            Assert.That(machine.Reset(), Is.False);
            Assert.That(machine.Current, Is.EqualTo(ExolegEncounterState.Disabled));
        }
    }
}
