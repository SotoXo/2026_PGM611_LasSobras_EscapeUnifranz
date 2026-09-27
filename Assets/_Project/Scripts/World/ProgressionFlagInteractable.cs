using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Interaction;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    /// <summary>
    /// Small graybox interaction for a single explicit progression gate such as ARCA access.
    /// </summary>
    public sealed class ProgressionFlagInteractable : InteractableBehaviour, IZoneRuntimeInitializable, IScanStateProvider
    {
        [SerializeField] private GameFlagId completionFlag;
        [SerializeField] private GameFlagId unlockedFlag;
        [SerializeField, TextArea] private string successMessage;
        [SerializeField] private string nextObjectiveId;
        [SerializeField, TextArea] private string nextObjectiveText;

        private GameSessionController session;

        public string ScanStateText => session != null && session.State.HasFlag(completionFlag)
            ? "[RESTABLECIDO]"
            : "[ERROR]";

        public override bool CanInteract => base.CanInteract && session != null &&
            !session.State.HasFlag(completionFlag);

        public void Initialize(ZoneRuntimeContext context)
        {
            session = context.Session;
        }

        protected override void PerformInteraction()
        {
            if (session.State.SetFlag(completionFlag))
            {
                session.State.SetFlag(unlockedFlag);
                session.ShowMessage(successMessage);
                session.Objectives?.SetCurrent(nextObjectiveId, nextObjectiveText);
                Debug.Log($"Zone unlocked by flag: {unlockedFlag}", this);
            }
        }
    }
}
