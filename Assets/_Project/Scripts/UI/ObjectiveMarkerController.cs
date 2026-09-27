using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.UI
{
    public readonly struct ObjectiveMarkerPlacement
    {
        public ObjectiveMarkerPlacement(Vector2 viewportPosition, bool offScreen, float angle)
        {
            ViewportPosition = viewportPosition;
            OffScreen = offScreen;
            Angle = angle;
        }

        public Vector2 ViewportPosition { get; }
        public bool OffScreen { get; }
        public float Angle { get; }

        public static ObjectiveMarkerPlacement Resolve(Vector3 viewport, float margin)
        {
            float safeMargin = Mathf.Clamp(margin, 0.01f, 0.45f);
            Vector2 point = new Vector2(viewport.x, viewport.y);
            bool inside = viewport.z > 0f &&
                point.x >= safeMargin && point.x <= 1f - safeMargin &&
                point.y >= safeMargin && point.y <= 1f - safeMargin;
            if (inside)
            {
                return new ObjectiveMarkerPlacement(point, false, 0f);
            }

            Vector2 direction = point - new Vector2(0.5f, 0.5f);
            if (viewport.z <= 0f)
            {
                direction = -direction;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.up;
            }

            float horizontalScale = (0.5f - safeMargin) / Mathf.Max(Mathf.Abs(direction.x), 0.0001f);
            float verticalScale = (0.5f - safeMargin) / Mathf.Max(Mathf.Abs(direction.y), 0.0001f);
            float scale = Mathf.Min(horizontalScale, verticalScale);
            Vector2 edge = new Vector2(0.5f, 0.5f) + direction * scale;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return new ObjectiveMarkerPlacement(edge, true, angle);
        }
    }

    public sealed class ObjectiveMarkerController : MonoBehaviour
    {
        [SerializeField] private GameplayModeController gameplayMode;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private ObjectiveMarkerView view;
        [SerializeField, Range(0.01f, 0.3f)] private float viewportMargin = 0.075f;

        private GameState state;
        private ZoneContext zone;
        private Transform target;

        private void OnEnable()
        {
            if (gameplayMode != null)
            {
                gameplayMode.ModeChanged += OnModeChanged;
            }
        }

        private void OnDisable()
        {
            if (gameplayMode != null)
            {
                gameplayMode.ModeChanged -= OnModeChanged;
            }

            view?.Hide();
        }

        private void LateUpdate()
        {
            if (!CanShow() || target == null || worldCamera == null || canvasRect == null)
            {
                view?.Hide();
                return;
            }

            ObjectiveMarkerPlacement placement = ObjectiveMarkerPlacement.Resolve(
                worldCamera.WorldToViewportPoint(target.position),
                viewportMargin);
            Vector2 anchored = new Vector2(
                (placement.ViewportPosition.x - 0.5f) * canvasRect.rect.width,
                (placement.ViewportPosition.y - 0.5f) * canvasRect.rect.height);
            view?.Render(anchored, placement.OffScreen, placement.Angle);
        }

        public void Bind(GameState gameState)
        {
            if (state != null)
            {
                state.ObjectiveChanged -= RefreshTarget;
            }

            state = gameState;
            if (state != null)
            {
                state.ObjectiveChanged += RefreshTarget;
            }

            RefreshTarget();
        }

        public void SetZone(ZoneContext zoneContext)
        {
            zone = zoneContext;
            RefreshTarget();
        }

        private void RefreshTarget()
        {
            target = null;
            if (zone == null || state == null)
            {
                return;
            }

            MinimapTrackable2D[] candidates = zone.GetComponentsInChildren<MinimapTrackable2D>(true);
            for (int index = 0; index < candidates.Length; index++)
            {
                if (candidates[index].ObjectiveId == state.CurrentObjectiveId)
                {
                    target = candidates[index].transform;
                    break;
                }
            }
        }

        private bool CanShow()
        {
            return gameplayMode != null &&
                (gameplayMode.CurrentMode == GameplayMode.Explore ||
                 gameplayMode.CurrentMode == GameplayMode.Encounter);
        }

        private void OnModeChanged(GameplayMode _)
        {
            if (!CanShow())
            {
                view?.Hide();
            }
        }
    }
}
