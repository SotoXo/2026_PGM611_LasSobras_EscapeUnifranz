using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Interaction;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    /// <summary>
    /// Reusable contextual exit configured with stable destination IDs.
    /// </summary>
    public sealed class ZoneExitInteractable : InteractableBehaviour, IScanStateProvider
    {
        [SerializeField] private string targetZoneId;
        [SerializeField] private string targetSpawnId;
        [SerializeField] private GameFlagId requiredFlag;
        [SerializeField, TextArea] private string lockedMessage = "Acceso restringido.";

        private SceneFlowController sceneFlow;
        private GameSessionController session;

        public GameFlagId RequiredFlag => requiredFlag;
        public string TargetZoneId => targetZoneId;
        public bool IsUnlocked => requiredFlag == GameFlagId.None ||
            (session != null && session.State.HasFlag(requiredFlag));
        public string ScanStateText => IsUnlocked ? "[ACCESO DISPONIBLE]" : "[ACCESO BLOQUEADO]";

        public void Bind(SceneFlowController controller, GameSessionController gameSession)
        {
            sceneFlow = controller;
            session = gameSession;
        }

        protected override void PerformInteraction()
        {
            if (sceneFlow == null)
            {
                Debug.LogError($"{nameof(ZoneExitInteractable)} '{name}' is not bound to SceneFlow.", this);
                return;
            }


            if (!IsUnlocked)
            {
                session?.ShowMessage(lockedMessage);
                return;
            }

            sceneFlow.GoToZone(targetZoneId, targetSpawnId);
        }
    }
}
