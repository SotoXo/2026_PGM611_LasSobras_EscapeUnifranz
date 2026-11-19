using System.Collections;
using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
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
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

        private void Awake() { visual = GetComponentInChildren<PlayerVisualController>(true); }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.gKey.wasPressedThisFrame || Time.time < nextThrowAt) return;
            TryThrow();
        }

        public void TryThrow()
        {
            if (Time.time < nextThrowAt) return;
            if (session == null) session = GetComponentInParent<GameSessionController>();
            if (session == null) session = FindFirstObjectByType<GameSessionController>();
            if (mode == null) mode = FindFirstObjectByType<GameplayModeController>();
            if (session == null || session.State == null || mode == null || !mode.AllowsInteraction) return;

            nextThrowAt = Time.time + cooldownSeconds;
            if (!session.State.HasItem(itemId))
            {
                session.ShowMessage("No tienes nada que lanzar. Recoge una lata de la expendedora.");
                return;
            }
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
            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.12f;
            float distance = throwDistance;

            var filter = ContactFilter2D.noFilter; filter.useTriggers = false;
            Physics2D.CircleCast(origin, 0.07f, dir, filter, hits, throwDistance);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.08f));
            }

            if (distance < 0.2f)
            {
                session.ShowMessage("Necesitas espacio para lanzar la lata.");
                return;
            }

            if (!session.State.TryUseItem(itemId)) return;
            ProjectAudioHooks.Emit(AudioCue.CanThrow);
            StartCoroutine(Fly(origin, origin + dir * distance, distance));
        }

        private IEnumerator Fly(Vector2 from, Vector2 to, float distance)
        {
            var can = new GameObject("LataLanzada");
            var zone = FindFirstObjectByType<ZoneContext>();
            if (zone != null)
                can.transform.SetParent(zone.transform, true);
            else
                can.transform.SetParent(transform.parent, true);
            var sr = can.AddComponent<SpriteRenderer>();
            sr.sprite = canSprite; sr.sortingLayerName = "World";
            can.transform.localScale = Vector3.one * 0.7f;

            Vector2 previousGround = from;
            float duration = Mathf.Max(0.15f, distance / Mathf.Max(.1f,flightSpeed)), t = 0f;
            while (t < 1f)
            {
                if (can == null) yield break;
                if (mode != null && !mode.AllowsMovement) { yield return null; continue; }
                t += Time.deltaTime / duration;
                float k = Mathf.Clamp01(t);
                Vector2 ground = Vector2.Lerp(from, to, k);
                Vector2 segment=ground-previousGround;
                var filter=ContactFilter2D.noFilter; filter.useTriggers=false;
                Physics2D.CircleCast(previousGround,.07f,segment.normalized,filter,hits,segment.magnitude);
                float allowed=segment.magnitude;
                foreach(var hit in hits) if(hit.collider!=null && !hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(can.transform))
                    allowed=Mathf.Min(allowed,Mathf.Max(0f,hit.distance-.08f));
                if(allowed < segment.magnitude) {to=previousGround+segment.normalized*allowed;break;}
                previousGround=ground;
                can.transform.position = ground + Vector2.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
                can.transform.rotation = Quaternion.Euler(0f, 0f, 720f * k);
                sr.sortingOrder = WorldYSort2D.Order(ground.y);
                yield return null;
            }

            if (can == null) yield break;
            can.transform.position = to;
            can.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            sr.sortingOrder = WorldYSort2D.Order(to.y);
            var trigger = can.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(0.8f, 0.8f);
            var pickup = can.AddComponent<ThrownCanPickup>(); pickup.itemId = itemId; pickup.BindSession(session);

            ProjectAudioHooks.EmitAt(AudioCue.CanImpact, to);
            foreach (var dog in FindObjectsByType<RobotDogEncounterController>(FindObjectsSortMode.None))
                if ((dog.UnitPosition-to).sqrMagnitude <= noiseRadius*noiseRadius && DetectionSensor2D.HasLineOfSight(dog.UnitPosition,to,~0,dog.UnitTransform,null,hits))
                    dog.DistractAt(to,distractSeconds);
            foreach (var robot in FindObjectsByType<MiniAutoController>(FindObjectsSortMode.None))
            {
                Vector2 eye = (Vector2)robot.transform.position + Vector2.up * 0.12f;
                if (Vector2.Distance(eye, to) <= noiseRadius &&
                    DetectionSensor2D.HasLineOfSight(eye, to, ~0, robot.transform, null, hits))
                    robot.DistractAt(to, distractSeconds);
            }
        }
    }
}
