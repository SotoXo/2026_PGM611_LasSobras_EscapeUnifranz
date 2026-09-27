using UnityEngine;

namespace EscapeUNIFRANZ.Interaction
{
    /// <summary>
    /// Provides shared prompt, priority and availability data for world interactables.
    /// </summary>
    public abstract class InteractableBehaviour : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "[E] Interactuar";
        [SerializeField] private int priority;
        [SerializeField] private bool interactionAvailable = true;

        public string Prompt => prompt;
        public int Priority => priority;
        public virtual bool CanInteract =>
            interactionAvailable && isActiveAndEnabled && gameObject.activeInHierarchy;

        public void Interact()
        {
            if (CanInteract)
            {
                PerformInteraction();
            }
        }

        protected abstract void PerformInteraction();
    }
}
