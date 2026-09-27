using EscapeUNIFRANZ.Hazards;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class ThreatAlertIndicatorTests
    {
        [Test]
        public void Patrol_HasNoIndicator()
        {
            Assert.That(ThreatAlertIndicatorLogic.SymbolFor(
                ThreatAwarenessState.Patrol), Is.Empty);
        }

        [Test]
        public void Alert_ShowsDetectionIndicator()
        {
            Assert.That(ThreatAlertIndicatorLogic.SymbolFor(
                ThreatAwarenessState.Alert), Is.EqualTo("!"));
        }

        [Test]
        public void Disabled_ClearsIndicator()
        {
            Assert.That(ThreatAlertIndicatorLogic.SymbolFor(
                ThreatAwarenessState.Disabled), Is.Empty);
        }
    }
}
