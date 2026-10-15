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

        protected string RequiredItem => requiredItem;
        public virtual string Prompt => !string.IsNullOrWhiteSpace(requiredItem) && BoundSession?.State.HasItem(requiredItem)==true ? "[E] Usar "+EscapeUNIFRANZ.Narrative.ItemData.Label(requiredItem) : prompt;
        public int Priority => priority;
        public virtual bool CanInteract =>
            interactionAvailable && isActiveAndEnabled && gameObject.activeInHierarchy;
        public void NoticeNearby()
        {
            var scanner=GetComponent<Scannable2D>();
            var tutorial=BoundSession?.GetComponent<EscapeUNIFRANZ.UI.EntranceTutorialController>();
            tutorial?.Offer(EscapeUNIFRANZ.UI.TutorialAction.Interaction);
            if(GetComponent<EnvironmentalControlInteractable>()?.action==EnvironmentalAction.PowerCut && BoundSession?.State.HasFlag(GameFlagId.FirstThreatActivated)==true) tutorial?.Offer(EscapeUNIFRANZ.UI.TutorialAction.PowerCut);
            if (scanner!=null && scanner.Category==ScanCategory.Terminal && BoundSession?.State.HasFlag(GameFlagId.FirstThreatActivated)==true)
                tutorial?.Offer(EscapeUNIFRANZ.UI.TutorialAction.Scanner);
        }

        public void Interact()
        {
            if (CanInteract)
            {
                EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.Interaction);
                BoundSession?.GetComponent<EscapeUNIFRANZ.UI.EntranceTutorialController>()?.Complete(EscapeUNIFRANZ.UI.TutorialAction.Interaction);
                if (!string.IsNullOrWhiteSpace(requiredItem) && BoundSession?.State.CheckContextItem(requiredItem)==ContextItemResult.WrongSelection)
                { BoundSession.ShowMessage("Eso no sirve aquí. Quita la selección o elige "+EscapeUNIFRANZ.Narrative.ItemData.Label(requiredItem)+"."); EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.ObjectError); return; }
                if (!RequirementsMet) { OnRequirementsBlocked(requirementMessage); return; }
                if (!string.IsNullOrWhiteSpace(requiredItem)) EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.ObjectUsed);
                PerformInteraction();
            }
        }

        protected abstract void PerformInteraction();
        protected virtual void OnRequirementsBlocked(string message) { BoundSession?.ShowMessage(message); }
    }
}
