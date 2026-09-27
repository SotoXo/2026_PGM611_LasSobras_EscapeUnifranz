using EscapeUNIFRANZ.UI;
using NUnit.Framework;

namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class NotificationQueueTests
    {
        [Test]
        public void Queue_PreservesOrderAndShowsOneAtATime()
        {
            var queue = new NotificationQueue(2f);
            queue.Enqueue("Primero");
            queue.Enqueue("Segundo");

            Assert.That(queue.Tick(0f, out string first), Is.True);
            Assert.That(first, Is.EqualTo("Primero"));
            Assert.That(queue.Tick(1f, out _), Is.False);
            Assert.That(queue.Tick(2f, out string second), Is.True);
            Assert.That(second, Is.EqualTo("Segundo"));
        }

        [Test]
        public void IdenticalCheckpointNotification_IsNotRepeated()
        {
            var queue = new NotificationQueue();

            Assert.That(queue.Enqueue("Punto de control actualizado"), Is.True);
            queue.Tick(0f, out _);
            Assert.That(queue.Enqueue("Punto de control actualizado"), Is.False);
        }
    }
}
