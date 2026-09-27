using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEngine;

namespace EscapeUNIFRANZ.Map
{
    public sealed class MinimapController : MonoBehaviour
    {
        private sealed class MarkerBinding
        {
            public MinimapTrackable2D Source;
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
        [SerializeField, Min(0.01f)] private float playerSmoothTime = 0.08f;

        private readonly List<MarkerBinding> markers = new List<MarkerBinding>();
        private GameState state;
        private ZoneContext zone;
        private Rect bounds;
        private Vector2 displayedPlayerPosition;
        private Vector2 playerVelocity;

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
            for (int index = 0; index < markers.Count; index++)
            {
                UpdateMarker(markers[index]);
            }
        }

        public void Bind(GameState gameState)
        {
            state = gameState;
        }

        public void SetZone(ZoneContext zoneContext)
        {
            ClearMarkers();
            zone = zoneContext;
            if (zone == null)
            {
                RefreshVisibility();
                return;
            }

            bounds = zone.MinimapBounds;
            if (zoneLabel != null)
            {
                zoneLabel.text = zone.DisplayName.ToUpperInvariant();
            }

            MinimapTrackable2D[] sources = zone.GetComponentsInChildren<MinimapTrackable2D>(true);
            for (int index = 0; index < sources.Length; index++)
            {
                MinimapTrackable2D source = sources[index];
                if (source.ShowAsExit) AddMarker(source, MinimapMarkerKind.Exit);
                if (!string.IsNullOrWhiteSpace(source.ObjectiveId)) AddMarker(source, MinimapMarkerKind.Objective);
                if (!string.IsNullOrWhiteSpace(source.CheckpointId)) AddMarker(source, MinimapMarkerKind.Checkpoint);
                if (source.ShowAsThreat) AddMarker(source, MinimapMarkerKind.Threat);
            }

            displayedPlayerPosition = GetPlayerPosition();
            playerVelocity = Vector2.zero;
            RefreshVisibility();
        }

        private void AddMarker(MinimapTrackable2D source, MinimapMarkerKind kind)
        {
            if (markerTemplate == null || content == null)
            {
                return;
            }

            MinimapMarkerView view = Instantiate(markerTemplate, content);
            view.name = kind + "_" + source.MarkerId;
            view.gameObject.SetActive(false);
            markers.Add(new MarkerBinding { Source = source, Kind = kind, View = view });
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

            Rigidbody2D body = player != null ? player.GetComponent<Rigidbody2D>() : null;
            if (body != null && body.linearVelocity.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(body.linearVelocity.y, body.linearVelocity.x) * Mathf.Rad2Deg - 90f;
                playerMarker.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
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

        private void UpdateMarker(MarkerBinding binding)
        {
            if (binding.Source == null || binding.View == null)
            {
                return;
            }

            bool visible;
            MinimapExitState exitState = MinimapExitState.Discovered;
            switch (binding.Kind)
            {
                case MinimapMarkerKind.Exit:
                    exitState = binding.Source.ExitState;
                    visible = exitState != MinimapExitState.Unknown;
                    break;
                case MinimapMarkerKind.Objective:
                    visible = binding.Source.IsObjectiveActive && binding.Source.IsDiscovered;
                    break;
                case MinimapMarkerKind.Checkpoint:
                    visible = binding.Source.IsCurrentCheckpoint;
                    break;
                case MinimapMarkerKind.Threat:
                    visible = binding.Source.IsDiscovered;
                    break;
                default:
                    visible = false;
                    break;
            }

            if (!visible)
            {
                binding.View.Hide();
                return;
            }

            binding.View.Render(
                binding.Kind,
                exitState,
                MinimapLogic.NormalizePosition(binding.Source.WorldPosition, bounds),
                content);
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
