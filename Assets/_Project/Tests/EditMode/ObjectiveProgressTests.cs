using EscapeUNIFRANZ.UI;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class ObjectiveProgressTests
    {
        [TestCase(0, "Desconecta los nodos [0/2]")]
        [TestCase(1, "Desconecta los nodos [1/2]")]
        [TestCase(2, "Desconecta los nodos [2/2]")]
        public void FormatsProgressFromZeroToComplete(int progress, string expected)
        {
            Assert.That(ObjectiveProgressFormatter.Format(
                "Desconecta los nodos", progress, 2), Is.EqualTo(expected));
        }

        [Test]
        public void SimpleObjective_RemainsSimple()
        {
            Assert.That(ObjectiveProgressFormatter.Format(
                "Explora el Hall.", 0, 0), Is.EqualTo("Explora el Hall."));
        }
    }
}
