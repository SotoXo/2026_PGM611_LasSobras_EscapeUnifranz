using UnityEngine;

namespace EscapeUNIFRANZ.UI
{
    public sealed class NotificationController : MonoBehaviour
    {
        [SerializeField] private MessageToastView view;
        [SerializeField, Min(0.1f)] private float visibleSeconds = 2f;

        private NotificationQueue queue;

        private void Awake()
        {
            queue = new NotificationQueue(visibleSeconds);
            view?.Hide();
        }

        private void Update()
        {
            Advance();
        }

        public bool Enqueue(string message)
        {
            EnsureQueue();
            bool added = queue.Enqueue(message);
            if (added)
            {
                Advance();
            }

            return added;
        }

        public void Clear()
        {
            EnsureQueue();
            queue.Clear();
            view?.Hide();
        }

        private void Advance()
        {
            EnsureQueue();
            if (queue.Tick(Time.unscaledTime, out string message))
            {
                view?.Show(message);
            }
            else if (queue.Current == null)
            {
                view?.Hide();
            }
        }

        private void EnsureQueue()
        {
            queue ??= new NotificationQueue(visibleSeconds);
        }
    }
}
