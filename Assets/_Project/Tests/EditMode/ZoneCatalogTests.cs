using System.Collections.Generic;
using EscapeUNIFRANZ.World;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class ZoneCatalogTests
    {
        [Test]
        public void TryCreateLookup_ValidZone_ResolvesScene()
        {
            var definitions = new List<ZoneDefinition>
            {
                new ZoneDefinition("hall", "Assets/_Project/Scenes/Zones/Z01_Hall.unity")
            };

            bool success = ZoneCatalog.TryCreateLookup(definitions, out var lookup, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(lookup["hall"], Does.EndWith("Z01_Hall.unity"));
        }

        [Test]
        public void TryResolveScene_MissingZone_ReturnsClearError()
        {
            var definitions = new List<ZoneDefinition>
            {
                new ZoneDefinition("hall", "Assets/_Project/Scenes/Zones/Z01_Hall.unity")
            };

            bool success = ZoneCatalog.TryResolveScene(
                definitions,
                "arca",
                out string scenePath,
                out string error);

            Assert.That(success, Is.False);
            Assert.That(scenePath, Is.Null);
            Assert.That(error, Does.Contain("ZoneId 'arca' does not exist"));
        }

        [Test]
        public void TryCreateLookup_DuplicateId_ReturnsClearError()
        {
            var definitions = new List<ZoneDefinition>
            {
                new ZoneDefinition("hall", "Assets/A.unity"),
                new ZoneDefinition("hall", "Assets/B.unity")
            };

            bool success = ZoneCatalog.TryCreateLookup(definitions, out _, out string error);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("Duplicate ZoneId 'hall'"));
        }
    }
}
