using System.Collections.Generic;

namespace EscapeUNIFRANZ.UI
{
    public sealed class NotificationQueue
    {
        private readonly Queue<string> pending = new Queue<string>();
        private readonly float visibleSeconds;
        private string current;
        private float visibleUntil;

        public NotificationQueue(float durationSeconds = 2f)
        {
            visibleSeconds = durationSeconds > 0f ? durationSeconds : 2f;
        }

        public string Current => current;
        public int PendingCount => pending.Count;

        public bool Enqueue(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            string normalized = message.Trim();
            if (current == normalized || pending.Contains(normalized))
            {
                return false;
            }

            pending.Enqueue(normalized);
            return true;
        }

        public bool Tick(float now, out string message)
        {
            if (current != null && now < visibleUntil)
            {
                message = current;
                return false;
            }

            current = null;
            if (pending.Count == 0)
            {
                message = null;
                return false;
            }

            current = pending.Dequeue();
            visibleUntil = now + visibleSeconds;
            message = current;
            return true;
        }

        public void Clear()
        {
            pending.Clear();
            current = null;
            visibleUntil = 0f;
        }
    }
}
