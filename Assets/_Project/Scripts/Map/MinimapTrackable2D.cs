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
        public string displayName="Punto de interés";
        public KnownPointKind pointKind;
        private float cctvVisibleUntil;
        public void RevealThreatFor(float seconds) { cctvVisibleUntil=Time.time+seconds; }
        public bool IsThreatVisible(EscapeUNIFRANZ.Player.PlayerVisionController vision)=>showAsThreat && gameObject.activeInHierarchy && (Time.time<cctvVisibleUntil || vision!=null && vision.CanSeePoint(transform.position,transform));
        private Transform player;

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
        public MinimapExitState ExitState => zoneExit != null && IsDiscovered && !secret
            ? zoneExit.MapExitState
            : MinimapLogic.ResolveExitState(
            state,
            zoneExit != null ? zoneExit.RequiredFlag : GameFlagId.None,
            IsDiscovered,
            secret);

        public void Initialize(ZoneRuntimeContext context)
        {
            state = context.Session.State;
            zoneId = context.Zone.ZoneId;
            zoneExit = GetComponent<ZoneExitInteractable>();
            player=context.Session.GetComponentInChildren<EscapeUNIFRANZ.Player.PlayerVisionController>(true)?.transform;

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

            bool discovered=state.DiscoverWorldItem(zoneId, markerId); Record(); return discovered;
        }
        public void Record(string status=null)
        {
            if(state==null || secret || showAsThreat && pointKind!=KnownPointKind.Camera || !IsDiscovered) return;
            state.RecordMapPoint(zoneId,markerId,displayName,showAsExit ? KnownPointKind.Door : pointKind,status ?? (zoneExit!=null ? zoneExit.MapStatus : "Identificado"),zoneExit!=null ? (int)zoneExit.MapExitState : 1);
        }
        private void Update()
        {
            if((!showAsThreat || pointKind==KnownPointKind.Camera) && !IsDiscovered && player!=null && Vector2.Distance(player.position,transform.position)<=1.15f && EscapeUNIFRANZ.Hazards.DetectionSensor2D.HasLineOfSight(player.position,transform.position,~0,player,transform)) Discover();
            if(showAsExit && IsDiscovered) Record();
            else if(!string.IsNullOrEmpty(checkpointId) && IsDiscovered) Record(IsCurrentCheckpoint ? "Activo" : "Descubierto");
        }
    }
}
