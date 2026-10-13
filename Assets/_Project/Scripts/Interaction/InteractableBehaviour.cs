using UnityEngine;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;

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
        [SerializeField] private GameFlagId[] requiredFlags;
        [SerializeField] private string requiredItem;
        [SerializeField] private string requirementMessage = "El dispositivo todavía no está habilitado.";
        protected GameSessionController BoundSession { get; private set; }
        public bool RequirementsMet => DoorAccessRules.CanOpen(BoundSession?.State, requiredFlags, requiredItem);
        public void BindSession(GameSessionController session) { BoundSession = session; }

        public string Prompt => prompt;
        public int Priority => priority;
        public virtual bool CanInteract =>
            interactionAvailable && isActiveAndEnabled && gameObject.activeInHierarchy;

        public void Interact()
        {
            if (CanInteract)
            {
                EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.Interaction);
                if (!RequirementsMet) { OnRequirementsBlocked(requirementMessage); return; }
                PerformInteraction();
            }
        }

        protected abstract void PerformInteraction();
        protected virtual void OnRequirementsBlocked(string message) { BoundSession?.ShowMessage(message); }
    }
}
