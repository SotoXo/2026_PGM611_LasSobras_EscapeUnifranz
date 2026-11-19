using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.Map
{
    public sealed class MinimapController : MonoBehaviour
    {
        private sealed class MarkerBinding
        {
            public MinimapTrackable2D Source;
            public InteractableBehaviour Interaction;
            public Scannable2D Scanner;
            public Transform Target;
            public MinimapMarkerKind Kind;
            public MinimapMarkerView View;
        }

        [SerializeField] private GameplayModeController gameplayMode;
        [SerializeField] private Transform player;
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private MinimapMarkerView markerTemplate;
        [SerializeField] private TMP_Text zoneLabel;
        [SerializeField] private RawImage floorPlan;
        [SerializeField] private TMP_Text nearbyLabel;
        [SerializeField, Min(0.01f)] private float playerSmoothTime = 0.08f;

        private readonly List<MarkerBinding> markers = new List<MarkerBinding>();
        private ZoneContext zone;
        private Rect bounds;
        private Vector2 displayedPlayerPosition;
        private Vector2 playerVelocity;
        private Vector2 mapArea;
        private PlayerVisionController vision;
        private Texture2D planTexture;
        private float nextMarkerRefresh;

        private void OnEnable()
        {
            if (gameplayMode != null)
            {
                gameplayMode.ModeChanged += OnModeChanged;
            }

            RefreshVisibility();
        }

        private void OnDisable()
        {
            if (gameplayMode != null)
            {
                gameplayMode.ModeChanged -= OnModeChanged;
            }

            root?.SetActive(false);
        }

        private void Update()
        {
            if (root == null || !root.activeSelf || zone == null || content == null)
            {
                return;
            }

            UpdatePlayerMarker();
            if (Time.unscaledTime < nextMarkerRefresh)
                return;

            nextMarkerRefresh = Time.unscaledTime + 0.1f;
            MarkerBinding nearest = null;
            float nearestDistance = float.PositiveInfinity;
            int threats = 0;
            int cameras = 0;
            for (int index = 0; index < markers.Count; index++)
            {
                MarkerBinding marker = markers[index];
                if (!UpdateMarker(marker))
                    continue;
                if (marker.Kind == MinimapMarkerKind.Threat)
                    threats++;
                if (marker.Kind == MinimapMarkerKind.Camera)
                    cameras++;
                if (player == null || marker.Interaction == null)
                    continue;

                float distance = ((Vector2)(marker.Target.position - player.position)).sqrMagnitude;
                if (distance < nearestDistance && distance <= 4.25f * 4.25f)
                {
                    nearest = marker;
                    nearestDistance = distance;
                }
            }

            if (nearbyLabel != null)
                RefreshStatus(nearest, threats, cameras);
        }

        private void OnDestroy()
        {
            if (planTexture != null)
                Destroy(planTexture);
        }

        public void SetZone(ZoneContext zoneContext)
        {
            ClearMarkers();
            zone = zoneContext;
            if (planTexture != null)
                Destroy(planTexture);
            planTexture = null;
            if (zone == null)
            {
                if (floorPlan != null) floorPlan.texture = null;
                RefreshVisibility();
                return;
            }

            bounds = zone.MinimapBounds;
            vision = player != null ? player.GetComponent<PlayerVisionController>() : null;
            nextMarkerRefresh = 0f;
            if (nearbyLabel != null) nearbyLabel.text = "SIN CONTACTOS";
            if (floorPlan != null && content != null)
            {
                if (mapArea == Vector2.zero) mapArea = content.rect.size;
                if (bounds.width > 0f && bounds.height > 0f)
                {
                    float scale = Mathf.Min(mapArea.x / bounds.width, mapArea.y / bounds.height);
                    content.sizeDelta = bounds.size * scale;
                }
                planTexture = MinimapFloorPlan.Build(zone, bounds);
                floorPlan.texture = planTexture;
            }

            if (zoneLabel != null)
            {
                zoneLabel.text = zone.DisplayName.ToUpperInvariant();
            }

            MinimapTrackable2D[] sources = zone.GetComponentsInChildren<MinimapTrackable2D>(true);
            for (int index = 0; index < sources.Length; index++)
            {
                MinimapTrackable2D source = sources[index];
                var interaction = source.GetComponent<InteractableBehaviour>();
                if (source.ShowAsExit) AddMarker(source, interaction, MinimapMarkerKind.Exit);
                if (!string.IsNullOrWhiteSpace(source.ObjectiveId)) AddMarker(source, interaction, MinimapMarkerKind.Objective);
                if (!string.IsNullOrWhiteSpace(source.CheckpointId)) AddMarker(source, null, MinimapMarkerKind.Checkpoint);
                if (source.ShowAsThreat)
                    AddMarker(source, null, source.pointKind == KnownPointKind.Camera ? MinimapMarkerKind.Camera : MinimapMarkerKind.Threat);
                else if (!source.ShowAsExit && string.IsNullOrEmpty(source.CheckpointId))
                    AddMarker(source, interaction, KindFor(interaction));
            }
            foreach (var interaction in zone.GetComponentsInChildren<InteractableBehaviour>(true))
                if (interaction.GetComponent<MinimapTrackable2D>() == null)
                    AddMarker(null, interaction, KindFor(interaction));

            if (playerMarker != null) playerMarker.SetAsLastSibling();

            displayedPlayerPosition = GetPlayerPosition();
            playerVelocity = Vector2.zero;
            RefreshVisibility();
        }

        private static MinimapMarkerKind KindFor(InteractableBehaviour interaction) => interaction switch
        {
            ZoneExitInteractable or InteriorDoorInteractable => MinimapMarkerKind.Exit,
            ItemPickupInteractable => MinimapMarkerKind.Item,
            _ => MinimapMarkerKind.Point
        };

        private void AddMarker(MinimapTrackable2D source, InteractableBehaviour interaction, MinimapMarkerKind kind)
        {
            if (markerTemplate == null || content == null)
            {
                return;
            }

            Transform target = source != null ? source.transform : interaction.transform;
            MinimapMarkerView view = Instantiate(markerTemplate, content);
            view.name = kind + "_" + target.name;
            view.gameObject.SetActive(false);
            markers.Add(new MarkerBinding
            {
                Source = source, Interaction = interaction, Scanner = target.GetComponent<Scannable2D>(),
                Target = target, Kind = kind, View = view
            });
        }

        private void UpdatePlayerMarker()
        {
            if (playerMarker == null)
            {
                return;
            }

            Vector2 target = GetPlayerPosition();
            displayedPlayerPosition = Vector2.SmoothDamp(
                displayedPlayerPosition,
                target,
                ref playerVelocity,
                playerSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            playerMarker.anchoredPosition = displayedPlayerPosition;

            if (vision != null)
                playerMarker.localRotation = Quaternion.Euler(0f, 0f,
                    PlayerVisionController.ResolveRotationDegrees(vision.CurrentDirection));
        }

        private Vector2 GetPlayerPosition()
        {
            if (player == null || content == null)
            {
                return Vector2.zero;
            }

            Vector2 normalized = MinimapLogic.NormalizePosition(player.position, bounds);
            return new Vector2(
                (normalized.x - 0.5f) * content.rect.width,
                (normalized.y - 0.5f) * content.rect.height);
        }

        private bool UpdateMarker(MarkerBinding binding)
        {
            if (binding.View == null)
                return false;
            if (binding.Target == null || !binding.Target.gameObject.activeInHierarchy)
            {
                binding.View.Hide();
                return false;
            }

            bool visible;
            MinimapTrackable2D source = binding.Source;
            MinimapExitState exitState = MinimapExitState.Discovered;
            switch (binding.Kind)
            {
                case MinimapMarkerKind.Exit:
                    if (binding.Interaction is InteriorDoorInteractable door)
                    {
                        visible = CanSeeNearby(binding);
                        exitState = door.IsOpen ? MinimapExitState.Available : MinimapExitState.Conditional;
                    }
                    else if (source != null)
                    {
                        exitState = source.ExitState;
                        if (exitState == MinimapExitState.Unknown && !source.IsSecret &&
                            binding.Interaction is ZoneExitInteractable nearbyExit && CanSeeNearby(binding))
                            exitState = nearbyExit.MapExitState;
                        visible = exitState != MinimapExitState.Unknown;
                    }
                    else
                    {
                        var exit = binding.Interaction as ZoneExitInteractable;
                        visible = exit != null && CanSeeNearby(binding);
                        if (visible) exitState = exit.MapExitState;
                    }
                    break;
                case MinimapMarkerKind.Objective:
                    visible = source.IsObjectiveActive && source.IsDiscovered;
                    break;
                case MinimapMarkerKind.Checkpoint:
                    visible = source.IsCurrentCheckpoint && source.IsDiscovered;
                    break;
                case MinimapMarkerKind.Threat:
                case MinimapMarkerKind.Camera:
                    visible = source.IsThreatVisible(vision);
                    break;
                case MinimapMarkerKind.Point:
                case MinimapMarkerKind.Item:
                    visible = (source == null || (!source.IsSecret && !(source.IsObjectiveActive && source.IsDiscovered))) &&
                        (binding.Interaction == null || binding.Interaction.CanInteract) && CanSeeNearby(binding);
                    break;
                default:
                    visible = false;
                    break;
            }

            if (!visible)
            {
                binding.View.Hide();
                return false;
            }

            binding.View.Render(
                binding.Kind,
                exitState,
                MinimapLogic.NormalizePosition(binding.Target.position, bounds),
                content);
            return true;
        }

        private bool CanSeeNearby(MarkerBinding binding)
        {
            if (binding.Scanner != null && binding.Scanner.IsRevealed)
                return true;
            return vision != null && ((Vector2)(binding.Target.position - player.position)).sqrMagnitude <= 4.25f * 4.25f &&
                vision.CanSeePoint(binding.Target.position, binding.Target);
        }

        private void RefreshStatus(MarkerBinding nearby, int threats, int cameras)
        {
            string label = nearby?.Interaction switch
            {
                ItemPickupInteractable pickup => ItemData.Label(pickup.ItemId),
                InteriorDoorInteractable => "Puerta",
                _ => nearby?.Source?.displayName
            };
            string status = threats + cameras == 0 ? "SIN AMENAZAS DETECTADAS" : $"ENEMIGOS: {threats}  ·  CCTV: {cameras}";
            string text = string.IsNullOrWhiteSpace(label) ? status : $"{status}  |  {label}";
            if (nearbyLabel.text != text) nearbyLabel.text = text;
        }

        private void ClearMarkers()
        {
            for (int index = 0; index < markers.Count; index++)
            {
                if (markers[index].View != null)
                {
                    Destroy(markers[index].View.gameObject);
                }
            }

            markers.Clear();
        }

        private void OnModeChanged(GameplayMode _)
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (root == null)
            {
                return;
            }

            bool show = zone != null && gameplayMode != null &&
                (gameplayMode.CurrentMode == GameplayMode.Explore ||
                 gameplayMode.CurrentMode == GameplayMode.Encounter);
            root.SetActive(show);
        }
    }
}
