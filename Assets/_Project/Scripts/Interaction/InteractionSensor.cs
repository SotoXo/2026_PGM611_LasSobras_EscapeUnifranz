using System.Collections.Generic;
using UnityEngine;

namespace EscapeUNIFRANZ.Interaction
{
    /// <summary>
    /// Tracks interactables overlapping a trigger and resolves the current valid target.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class InteractionSensor : MonoBehaviour
    {
        private sealed class TrackedCandidate
        {
            public TrackedCandidate(
                MonoBehaviour owner,
                IInteractable interactable,
                long stableOrder)
            {
                Owner = owner;
                Interactable = interactable;
                StableOrder = stableOrder;
            }

            public MonoBehaviour Owner { get; }
            public IInteractable Interactable { get; }
            public long StableOrder { get; }
            public HashSet<int> ColliderIds { get; } = new HashSet<int>();
        }

        private readonly Dictionary<int, TrackedCandidate> trackedCandidates =
            new Dictionary<int, TrackedCandidate>();
        private readonly List<InteractionCandidate> candidateBuffer =
            new List<InteractionCandidate>();
        private readonly List<int> removalBuffer = new List<int>();

        private long nextStableOrder;

        public IInteractable ResolveCurrent(Vector2 origin)
        {
            RemoveInvalidOwners();
            candidateBuffer.Clear();

            foreach (TrackedCandidate tracked in trackedCandidates.Values)
            {
                candidateBuffer.Add(new InteractionCandidate(
                    tracked.Interactable,
                    tracked.Owner.transform.position,
                    tracked.StableOrder));
            }

            return InteractionCandidateResolver.Resolve(candidateBuffer, origin);
        }

        private void Awake()
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
        }

        private void OnDisable()
        {
            trackedCandidates.Clear();
            candidateBuffer.Clear();
            removalBuffer.Clear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!TryFindInteractable(other, out MonoBehaviour owner, out IInteractable interactable))
            {
                return;
            }

            int ownerId = owner.GetInstanceID();
            if (!trackedCandidates.TryGetValue(ownerId, out TrackedCandidate tracked))
            {
                tracked = new TrackedCandidate(owner, interactable, nextStableOrder++);
                trackedCandidates.Add(ownerId, tracked);
            }

            tracked.ColliderIds.Add(other.GetInstanceID());
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!TryFindInteractable(other, out MonoBehaviour owner, out _))
            {
                RemoveInvalidOwners();
                return;
            }

            int ownerId = owner.GetInstanceID();
            if (!trackedCandidates.TryGetValue(ownerId, out TrackedCandidate tracked))
            {
                return;
            }

            tracked.ColliderIds.Remove(other.GetInstanceID());
            if (tracked.ColliderIds.Count == 0)
            {
                trackedCandidates.Remove(ownerId);
            }
        }

        private void RemoveInvalidOwners()
        {
            removalBuffer.Clear();

            foreach (KeyValuePair<int, TrackedCandidate> pair in trackedCandidates)
            {
                MonoBehaviour owner = pair.Value.Owner;
                if (owner == null || !owner.isActiveAndEnabled || !owner.gameObject.activeInHierarchy)
                {
                    removalBuffer.Add(pair.Key);
                }
            }

            for (int index = 0; index < removalBuffer.Count; index++)
            {
                trackedCandidates.Remove(removalBuffer[index]);
            }
        }

        private static bool TryFindInteractable(
            Collider2D other,
            out MonoBehaviour owner,
            out IInteractable interactable)
        {
            MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>(true);

            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IInteractable candidate)
                {
                    owner = behaviours[index];
                    interactable = candidate;
                    return true;
                }
            }

            owner = null;
            interactable = null;
            return false;
        }
    }
}
