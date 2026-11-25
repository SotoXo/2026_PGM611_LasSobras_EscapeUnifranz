using System.Collections;
using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
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
        public const string EmptyCanItemId = "empty_can";
        public static string ChooseCan(GameState state)
        {
            if(state==null)return null;
            if((state.SelectedItemId=="soda"||state.SelectedItemId==EmptyCanItemId)&&state.HasItem(state.SelectedItemId))return state.SelectedItemId;
            return state.HasItem(EmptyCanItemId) ? EmptyCanItemId : state.HasItem("soda") ? "soda" : null;
        }

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
            if (ChooseCan(session.State) == null)
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
            MiniAutoController hitRobot = null;

            var filter = ContactFilter2D.noFilter; filter.useTriggers = false;
            hits.Clear();
            Physics2D.CircleCast(origin, 0.07f, dir, filter, hits, throwDistance);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                float stop = Mathf.Max(0f, hit.distance - 0.08f);
                if (stop < distance) { distance = stop; hitRobot = hit.collider.GetComponentInParent<MiniAutoController>(); }
            }

            if (distance < 0.2f)
            {
                session.ShowMessage("Necesitas espacio para lanzar la lata.");
                return;
            }

            string thrownItem=ChooseCan(session.State);
            bool selected = session.State.SelectedItemId == thrownItem;
            if (thrownItem==null || !session.State.TryUseItem(thrownItem)) return;
            if (visual != null) visual.PlayAction(PlayerVisualController.ActionPose.Throw);
            ProjectAudioHooks.Emit(AudioCue.CanThrow);
            StartCoroutine(Fly(origin, origin + dir * distance, distance, hitRobot, thrownItem, selected));
        }

        private IEnumerator Fly(Vector2 from, Vector2 to, float distance, MiniAutoController hitRobot, string thrownItem, bool selected)
        {
            bool spill = thrownItem == itemId;
            var can = new GameObject("LataLanzada");
            var zone = FindFirstObjectByType<ZoneContext>();
            if (zone != null)
                can.transform.SetParent(zone.transform, true);
            else
                can.transform.SetParent(transform.parent, true);
            var sr = can.AddComponent<SpriteRenderer>();
            sr.sprite = ItemData.Find(thrownItem)?.icon ?? canSprite;
            sr.sortingLayerName = "World";
            can.transform.localScale = Vector3.one * 0.7f;

            // Sombra en el piso: da profundidad al vuelo y al rebote.
            var shadow = new GameObject("SombraLata");
            shadow.transform.SetParent(can.transform.parent, true);
            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = InteractionFx.Circle; shadowRenderer.sortingLayerName = "World";
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.35f);
            shadow.transform.position = from;

            try
            {
                float duration = Mathf.Max(0.15f, distance / Mathf.Max(0.1f, flightSpeed)), t = 0f;
                Vector2 previousGround = from;
                while (t < 1f)
                {
                    if (can == null) yield break;
                    if (mode != null && !mode.AllowsMovement) { yield return null; continue; }
                    t += Time.deltaTime / duration;
                    float k = Mathf.Clamp01(t);
                    Vector2 ground = Vector2.Lerp(from, to, k);
                    Vector2 step = ground - previousGround;
                    if (step.sqrMagnitude > .000001f)
                    {
                        var flightFilter = ContactFilter2D.noFilter; flightFilter.useTriggers = false;
                        hits.Clear();
                        Physics2D.CircleCast(previousGround, .07f, step.normalized, flightFilter, hits, step.magnitude);
                        RaycastHit2D nearest = default;
                        foreach (var hit in hits)
                        {
                            if (hit.collider == null || hit.transform.IsChildOf(transform) ||
                                hit.transform.IsChildOf(can.transform)) continue;
                            if (nearest.collider == null || hit.distance < nearest.distance) nearest = hit;
                        }
                        if (nearest.collider != null)
                        {
                            to = previousGround + step.normalized * Mathf.Max(0f, nearest.distance - 0.08f);
                            hitRobot = nearest.collider.GetComponentInParent<MiniAutoController>();
                            break;
                        }
                    }
                    previousGround = ground;
                    float height = Mathf.Sin(k * Mathf.PI);
                    can.transform.position = ground + Vector2.up * height * 0.6f;
                    can.transform.rotation = Quaternion.Euler(0f, 0f, 720f * k);
                    sr.sortingOrder = WorldYSort2D.Order(ground.y);
                    shadow.transform.position = ground;
                    shadow.transform.localScale = new Vector3(0.45f - 0.15f * height, 0.2f - 0.07f * height, 1f);
                    shadowRenderer.sortingOrder = sr.sortingOrder - 1;
                    yield return null;
                }

                if (can == null) yield break;
                can.transform.position = to;
                can.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                sr.sortingOrder = WorldYSort2D.Order(to.y);
                shadow.transform.position = to;
                shadow.transform.localScale = new Vector3(0.45f, 0.2f, 1f);
                shadowRenderer.sortingOrder = sr.sortingOrder - 1;
                // Golpe, polvo y distracción ocurren en el instante del impacto.
                ProjectAudioHooks.EmitAt(AudioCue.CanImpact, to);
                InteractionFx.Puff(to, new Color(0.8f, 0.8f, 0.8f, 1f));
                bool robotAffected = false;
                if (hitRobot != null && Vector2.Distance(hitRobot.transform.position, to) < .7f &&
                    DetectionSensor2D.HasLineOfSight(to, hitRobot.transform.position, ~0, can.transform, hitRobot.transform, hits))
                    robotAffected = ApplyRobotImpact(hitRobot, spill);
                if (robotAffected && session.IsPractice) session.Tutorial?.PracticeCanHit(spill, selected);
                if (spill) CanSpill2D.Create(to, can.transform.parent, mode, robotAffected ? hitRobot : null);
                foreach (var dog in FindObjectsByType<RobotDogEncounterController>(FindObjectsSortMode.None))
                    if ((dog.UnitPosition - to).sqrMagnitude <= noiseRadius * noiseRadius &&
                        DetectionSensor2D.HasLineOfSight(dog.UnitPosition, to, ~0, dog.UnitTransform, null, hits))
                        dog.DistractAt(to, distractSeconds);
                foreach (var robot in FindObjectsByType<MiniAutoController>(FindObjectsSortMode.None))
                {
                    Vector2 eye = (Vector2)robot.transform.position + Vector2.up * 0.12f;
                    if (Vector2.Distance(eye, to) <= noiseRadius &&
                        DetectionSensor2D.HasLineOfSight(eye, to, ~0, robot.transform, null, hits))
                        robot.DistractAt(to, distractSeconds);
                }
                if (!spill && robotAffected)
                {
                    Destroy(can); // An empty can is spent when it successfully damages a robot.
                    yield break;
                }
                sr.sprite = ItemData.Find(EmptyCanItemId)?.icon ?? sr.sprite;
                var trigger = can.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(0.8f, 0.8f);
                var pickup = can.AddComponent<ThrownCanPickup>(); pickup.itemId = EmptyCanItemId; pickup.BindSession(session);

                // Pequeño rebote solo visual (sin sonido extra) para que el golpe se sienta.
                const float bounceSeconds = 0.22f;
                float bounce = 0f;
                while (bounce < 1f)
                {
                    if (can == null) yield break;
                    bounce += Time.deltaTime / bounceSeconds;
                    can.transform.position = to + Vector2.up * Mathf.Sin(Mathf.Clamp01(bounce) * Mathf.PI) * 0.16f;
                    yield return null;
                }

                if (can != null) can.transform.position = to;
            }
            finally
            {
                if (shadow != null) Destroy(shadow);
            }
        }
        public static bool ApplyRobotImpact(MiniAutoController robot,bool full)
        {
            if(robot==null)return false;
            if(!full)return robot.ReceiveCanHit();
            if(!robot.CanBeStunned)return false;
            robot.SuspendFor(CanSpill2D.StunSeconds);return true;
        }
    }
}
