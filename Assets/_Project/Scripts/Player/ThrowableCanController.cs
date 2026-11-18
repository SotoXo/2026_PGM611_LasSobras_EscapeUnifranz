using System.Collections;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeUNIFRANZ.Player
{
    /// <summary>
    /// [G] lanza una lata en la dirección en que mira el jugador. Al caer hace ruido y los
    /// carritos (MiniAutoController) cercanos van a investigar ese punto. La lata queda en el
    /// piso y se puede volver a recoger con [E].
    /// </summary>
    public sealed class ThrowableCanController : MonoBehaviour
    {
        [SerializeField] private string itemId = "soda";
        [SerializeField] private Sprite canSprite;
        [SerializeField] private float throwDistance = 4.5f;
        [SerializeField] private float flightSpeed = 9f;
        [SerializeField] private float noiseRadius = 10f;
        [SerializeField] private float distractSeconds = 5f;
        [SerializeField] private float cooldownSeconds = 0.6f;

        private GameSessionController session;
        private GameplayModeController mode;
        private PlayerVisualController visual;
        private float nextThrowAt;

        private void Awake() { visual = GetComponentInChildren<PlayerVisualController>(true); }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.gKey.wasPressedThisFrame || Time.time < nextThrowAt) return;
            if (session == null) session = FindFirstObjectByType<GameSessionController>();
            if (mode == null) mode = FindFirstObjectByType<GameplayModeController>();
            if (session == null || session.State == null || mode == null || !mode.AllowsInteraction) return;

            nextThrowAt = Time.time + cooldownSeconds;
            if (!session.State.HasItem(itemId))
            {
                session.ShowMessage("No tienes nada que lanzar. Compra una lata en la expendedora.");
                return;
            }
            if (!session.State.TryUseItem(itemId)) return;
            Throw();
        }

        private Vector2 Facing()
        {
            if (visual == null) return Vector2.down;
            switch (visual.CurrentDirection)
            {
                case PlayerVisualController.FacingDirection.Back: return Vector2.up;
                case PlayerVisualController.FacingDirection.Left: return Vector2.left;
                case PlayerVisualController.FacingDirection.Right: return Vector2.right;
                default: return Vector2.down;
            }
        }

        private void Throw()
        {
            Vector2 dir = Facing();
            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.35f;
            float distance = throwDistance;

            var filter = ContactFilter2D.noFilter; filter.useTriggers = false;
            var hits = new RaycastHit2D[8];
            int count = Physics2D.Raycast(origin, dir, filter, hits, throwDistance);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider == null || hits[i].collider.transform.IsChildOf(transform)) continue;
                distance = Mathf.Min(distance, Mathf.Max(0.3f, hits[i].distance - 0.25f));
            }

            ProjectAudioHooks.Emit(AudioCue.Interaction);
            StartCoroutine(Fly(origin, origin + dir * distance, distance));
        }

        private IEnumerator Fly(Vector2 from, Vector2 to, float distance)
        {
            var can = new GameObject("LataLanzada");
            var zone = FindFirstObjectByType<ZoneContext>();
            if (zone != null) can.transform.SetParent(zone.transform, true);
            var sr = can.AddComponent<SpriteRenderer>();
            sr.sprite = canSprite; sr.sortingLayerName = "World";
            can.transform.localScale = Vector3.one * 0.7f;

            float duration = Mathf.Max(0.15f, distance / flightSpeed), t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float k = Mathf.Clamp01(t);
                Vector2 ground = Vector2.Lerp(from, to, k);
                can.transform.position = ground + Vector2.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
                can.transform.rotation = Quaternion.Euler(0f, 0f, 720f * k);
                sr.sortingOrder = WorldYSort2D.Order(ground.y);
                yield return null;
            }

            can.transform.position = to;
            can.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            sr.sortingOrder = WorldYSort2D.Order(to.y);
            var trigger = can.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(0.8f, 0.8f);
            var pickup = can.AddComponent<ThrownCanPickup>(); pickup.itemId = itemId; pickup.BindSession(session);

            ProjectAudioHooks.Emit(AudioCue.ObjectUsed);
            foreach (var robot in FindObjectsByType<MiniAutoController>(FindObjectsSortMode.None))
                if (Vector2.Distance(robot.transform.position, to) <= noiseRadius) robot.DistractAt(to, distractSeconds);
        }
    }
}
