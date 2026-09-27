using UnityEngine;

namespace EscapeUNIFRANZ.Interaction
{
    /// <summary>
    /// Immutable data used to rank an interactable without depending on scene physics.
    /// </summary>
    public readonly struct InteractionCandidate
    {
        public InteractionCandidate(
            IInteractable interactable,
            Vector2 position,
            long stableOrder)
        {
            Interactable = interactable;
            Position = position;
            StableOrder = stableOrder;
        }

        public IInteractable Interactable { get; }
        public Vector2 Position { get; }
        public long StableOrder { get; }
    }
}
