using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Player;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    /// <summary>
    /// Explicit safe checkpoint identified independently of its GameObject name.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class RespawnPoint2D : MonoBehaviour, IZoneRuntimeInitializable, IScanStateProvider
    {
        [SerializeField] private string zoneId;
        [SerializeField] private string respawnId;

        private GameState state;
        private SpriteRenderer indicator;
        private SpriteRenderer halo;
        private SpriteRenderer core;
        private Vector3 haloBaseScale = Vector3.one;

        public string ZoneId => zoneId;
        public string RespawnId => respawnId;
        public Vector2 Position => transform.position;
        public string ScanStateText => IsCurrent ? "[ACTIVO]" : "[DISPONIBLE]";
        private bool IsCurrent => EscapeUNIFRANZ.Map.MinimapLogic.IsCurrentCheckpoint(
            state, zoneId, respawnId);

        public void Initialize(ZoneRuntimeContext context)
        {
            state = context.Session.State;
            state.CheckpointChanged += OnCheckpointChanged;
            GetComponent<Collider2D>().isTrigger = true;
            indicator = GetComponent<SpriteRenderer>();

            if (string.IsNullOrWhiteSpace(zoneId))
            {
                zoneId = context.Zone.ZoneId;
            }

            EnsureBeaconVisual();
            RefreshVisual();
        }

        private void Update()
        {
            if (halo == null)
            {
                return;
            }

            float pulse = IsCurrent
                ? 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.08f
                : 1f;
            halo.transform.localScale = haloBaseScale * pulse;
        }

        private void OnDestroy()
        {
            if (state != null)
            {
                state.CheckpointChanged -= OnCheckpointChanged;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayerPhysicsContact2D.IsPlayerBody(other))
            {
                return;
            }

            Activate(state);
        }

        public bool Activate(GameState gameState)
        {
            bool changed = Activate(gameState, zoneId, respawnId);
            if (changed)
            {
                Debug.Log($"Checkpoint actualizado: {respawnId}", this);
                RefreshVisual();
            }

            return changed;
        }

        public static bool Activate(GameState gameState, string pointZoneId, string pointRespawnId)
        {
            return gameState != null && gameState.SetCheckpoint(pointZoneId, pointRespawnId);
        }

        private void OnCheckpointChanged(string _, string __)
        {
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            bool active = IsCurrent;
            if (indicator != null)
            {
                indicator.color = active
                    ? new Color(0.1f, 1f, 0.35f, 0.9f)
                    : new Color(0.1f, 1f, 0.35f, 0.32f);
            }

            if (halo != null)
            {
                halo.color = active
                    ? new Color(0.3f, 0.89f, 0.54f, 0.8f)
                    : new Color(0.3f, 0.89f, 0.54f, 0.2f);
            }

            if (core != null)
            {
                core.color = active
                    ? new Color(0.78f, 1f, 0.88f, 1f)
                    : new Color(0.3f, 0.89f, 0.54f, 0.55f);
            }
        }

        private void EnsureBeaconVisual()
        {
            Transform legacyLabel = transform.Find("CheckpointLabel");
            if (legacyLabel != null)
            {
                legacyLabel.gameObject.SetActive(false);
            }

            halo = EnsureBeaconPart("BeaconHalo", new Vector2(0.9f, 0.9f), 0);
            core = EnsureBeaconPart("BeaconCore", new Vector2(0.28f, 0.28f), 1);
            haloBaseScale = halo.transform.localScale;
            core.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private SpriteRenderer EnsureBeaconPart(string childName, Vector2 size, int orderOffset)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                child = new GameObject(childName).transform;
                child.SetParent(transform, false);
            }

            child.localPosition = new Vector3(0f, 0f, -0.05f * orderOffset);
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = indicator != null ? indicator.sprite : null;
            renderer.drawMode = SpriteDrawMode.Simple;
            Vector2 sourceSize = renderer.sprite != null ? renderer.sprite.bounds.size : Vector2.one;
            child.localScale = new Vector3(size.x / sourceSize.x, size.y / sourceSize.y, 1f);
            renderer.sortingOrder = (indicator != null ? indicator.sortingOrder : 1) + orderOffset;
            return renderer;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.1f, 1f, 0.25f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.45f);
        }
    }
}
