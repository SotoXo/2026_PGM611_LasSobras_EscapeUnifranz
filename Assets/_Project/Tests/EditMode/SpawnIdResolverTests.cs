using EscapeUNIFRANZ.World;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class SpawnIdResolverTests
    {
        [Test]
        public void TryResolveIndex_ExistingSpawn_ReturnsIndex()
        {
            string[] ids = { "from_bootstrap", "from_arca" };

            bool success = SpawnIdResolver.TryResolveIndex(
                ids,
                "from_arca",
                out int index,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void TryResolveIndex_MissingSpawn_ReturnsClearError()
        {
            string[] ids = { "from_bootstrap" };

            bool success = SpawnIdResolver.TryResolveIndex(
                ids,
                "missing",
                out int index,
                out string error);

            Assert.That(success, Is.False);
            Assert.That(index, Is.EqualTo(-1));
            Assert.That(error, Does.Contain("SpawnId 'missing' was not found"));
        }
    }
}
