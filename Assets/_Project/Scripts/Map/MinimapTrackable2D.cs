using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Map
{
    public sealed class MinimapTrackable2D : MonoBehaviour, IZoneRuntimeInitializable
    {
        [SerializeField] private string markerId;
        [SerializeField] private bool initiallyDiscovered = true;
        [SerializeField] private bool secret;
        [SerializeField] private bool showAsExit;
        [SerializeField] private string objectiveId;
        [SerializeField] private string checkpointId;
        [SerializeField] private bool showAsThreat;

        private GameState state;
        private string zoneId;
        private ZoneExitInteractable zoneExit;

        public string MarkerId => markerId;
        public bool ShowAsExit => showAsExit;
        public string ObjectiveId => objectiveId;
        public string CheckpointId => checkpointId;
        public bool ShowAsThreat => showAsThreat;
        public Vector2 WorldPosition => transform.position;
        public bool IsDiscovered => initiallyDiscovered ||
            (state != null && state.IsWorldItemDiscovered(zoneId, markerId));
        public bool IsObjectiveActive => state != null &&
            !string.IsNullOrWhiteSpace(objectiveId) &&
            state.CurrentObjectiveId == objectiveId;
        public bool IsCurrentCheckpoint => MinimapLogic.IsCurrentCheckpoint(
            state, zoneId, checkpointId);
        public MinimapExitState ExitState => MinimapLogic.ResolveExitState(
            state,
            zoneExit != null ? zoneExit.RequiredFlag : GameFlagId.None,
            IsDiscovered,
            secret);

        public void Initialize(ZoneRuntimeContext context)
        {
            state = context.Session.State;
            zoneId = context.Zone.ZoneId;
            zoneExit = GetComponent<ZoneExitInteractable>();

            if (string.IsNullOrWhiteSpace(markerId))
            {
                markerId = name;
            }
        }

        public bool Discover()
        {
            if (state == null || secret)
            {
                return false;
            }

            return state.DiscoverWorldItem(zoneId, markerId);
        }
    }
}
