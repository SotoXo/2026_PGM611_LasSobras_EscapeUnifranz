using System.Collections.Generic;
using UnityEngine;

namespace EscapeUNIFRANZ.Interaction
{
    /// <summary>
    /// Selects one valid candidate by priority, distance and stable registration order.
    /// </summary>
    public static class InteractionCandidateResolver
    {
        public static IInteractable Resolve(
            IReadOnlyList<InteractionCandidate> candidates,
            Vector2 origin)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            IInteractable bestInteractable = null;
            int bestPriority = int.MinValue;
            float bestDistanceSqr = float.PositiveInfinity;
            long bestStableOrder = long.MaxValue;

            for (int index = 0; index < candidates.Count; index++)
            {
                InteractionCandidate candidate = candidates[index];
                IInteractable interactable = candidate.Interactable;

                if (interactable == null || !interactable.CanInteract)
                {
                    continue;
                }

                int priority = interactable.Priority;
                float distanceSqr = (candidate.Position - origin).sqrMagnitude;

                bool isBetter = priority > bestPriority ||
                    priority == bestPriority && distanceSqr < bestDistanceSqr ||
                    priority == bestPriority && distanceSqr == bestDistanceSqr &&
                    candidate.StableOrder < bestStableOrder;

                if (!isBetter)
                {
                    continue;
                }

                bestInteractable = interactable;
                bestPriority = priority;
                bestDistanceSqr = distanceSqr;
                bestStableOrder = candidate.StableOrder;
            }

            return bestInteractable;
        }
    }
}
