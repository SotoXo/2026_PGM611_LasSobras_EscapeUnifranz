using UnityEngine;

namespace EscapeUNIFRANZ.Player
{
    /// <summary>
    /// Maps top-down movement to the four directional animation states.
    /// The renderer can live on a scaled child so physics remain on the Player root.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class PlayerVisualController : MonoBehaviour
    {
        private const float DirectionThresholdSqr = 0.0001f;

        public const string IsWalkingParameter = "IsWalking";
        public const string DirectionParameter = "Direction";

        public enum FacingDirection
        {
            Front = 0,
            Back = 1,
            Left = 2,
            Right = 3
        }

        // Kept as a compatibility API for the existing movement logic tests.
        public enum HorizontalFacing
        {
            Left,
            Right
        }

        private static readonly int IsWalkingHash = Animator.StringToHash(IsWalkingParameter);
        private static readonly int DirectionHash = Animator.StringToHash(DirectionParameter);

        [SerializeField] private PlayerMovement2D movement;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private FacingDirection initialDirection = FacingDirection.Front;
        [SerializeField] private bool centerVisualOnPlayer = true;

        /// <summary>Poses de acción con respuesta visual (sin arte nuevo: pulso de escala + triggers opcionales).</summary>
        public enum ActionPose
        {
            Interact = 0,
            Pickup = 1,
            Use = 2,
            Throw = 3
        }

        private static readonly string[] ActionTriggerNames = { "Interact", "Pickup", "Use", "Throw" };
        private static readonly float[] ActionStrength = { 0.08f, 0.14f, 0.10f, 0.16f };
        private const float ActionSeconds = 0.18f;

        private HorizontalFacing lastHorizontalFacing = HorizontalFacing.Right;
        private Vector3 baseScale = Vector3.one;
        private float actionStartedAt = -10f;
        private float actionStrengthNow;
        private bool punchApplied;

        /// <summary>Instancia activa del jugador, para que objetos y efectos le pidan reacciones.</summary>
        public static PlayerVisualController Active { get; private set; }

        public FacingDirection CurrentDirection { get; private set; }
        public HorizontalFacing CurrentFacing => lastHorizontalFacing;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (movement == null)
            {
                movement = GetComponent<PlayerMovement2D>();
            }

            if (movement == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerVisualController)} on '{name}' requires a {nameof(PlayerMovement2D)} reference.",
                    this);
            }

            if (spriteRenderer != null)
            {
                baseScale = spriteRenderer.transform.localScale;
            }

            CurrentDirection = initialDirection;
            UpdateHorizontalFacing(CurrentDirection);
            ApplyAnimatorState(false);
        }

        private void OnEnable()
        {
            Active = this;
        }

        private void Update()
        {
            Vector2 input = movement != null ? movement.MovementInput : Vector2.zero;
            CurrentDirection = ResolveDirection(input, CurrentDirection);
            UpdateHorizontalFacing(CurrentDirection);
            ApplyAnimatorState(movement != null && movement.IsMoving);
        }

        private void LateUpdate()
        {
            ApplyActionPunch();
            CenterVisual();
        }

        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }

            ApplyAnimatorState(false);
        }

        /// <summary>
        /// Gira al personaje hacia un punto del mundo (p. ej. el objeto con el que interactúa).
        /// Si el jugador se está moviendo, manda la dirección del movimiento.
        /// </summary>
        public void FaceTowards(Vector2 worldPoint)
        {
            if (movement != null && movement.IsMoving)
            {
                return;
            }

            Vector2 delta = worldPoint - (Vector2)transform.position;
            CurrentDirection = ResolveDirection(delta, CurrentDirection);
            UpdateHorizontalFacing(CurrentDirection);
            ApplyAnimatorState(false);
        }

        /// <summary>
        /// Respuesta visual de una acción. Dispara un trigger del Animator con el mismo nombre
        /// ("Interact", "Pickup", "Use", "Throw") si existe, y siempre hace un pulso de escala
        /// con los pies fijos, para que funcione aunque aún no haya clips dibujados.
        /// </summary>
        public void PlayAction(ActionPose pose)
        {
            int index = (int)pose;
            actionStartedAt = Time.time;
            actionStrengthNow = ActionStrength[index];

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            string triggerName = ActionTriggerNames[index];
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type == AnimatorControllerParameterType.Trigger &&
                    parameters[i].name == triggerName)
                {
                    animator.SetTrigger(triggerName);
                    return;
                }
            }
        }

        public static FacingDirection ResolveDirection(
            Vector2 movementInput,
            FacingDirection previousDirection)
        {
            if (movementInput.sqrMagnitude <= DirectionThresholdSqr)
            {
                return previousDirection;
            }

            // With equally strong diagonal axes, keep the last compatible direction.
            if (Mathf.Approximately(Mathf.Abs(movementInput.x), Mathf.Abs(movementInput.y)))
            {
                if ((previousDirection == FacingDirection.Left && movementInput.x < 0f) ||
                    (previousDirection == FacingDirection.Right && movementInput.x > 0f) ||
                    (previousDirection == FacingDirection.Back && movementInput.y > 0f) ||
                    (previousDirection == FacingDirection.Front && movementInput.y < 0f))
                    return previousDirection;
            }

            if (Mathf.Abs(movementInput.x) > Mathf.Abs(movementInput.y))
            {
                return movementInput.x > 0f
                    ? FacingDirection.Right
                    : FacingDirection.Left;
            }

            return movementInput.y > 0f
                ? FacingDirection.Back
                : FacingDirection.Front;
        }

        public static HorizontalFacing ResolveFacing(
            Vector2 movementInput,
            HorizontalFacing previousFacing)
        {
            if (movementInput.x > 0f)
            {
                return HorizontalFacing.Right;
            }

            if (movementInput.x < 0f)
            {
                return HorizontalFacing.Left;
            }

            return previousFacing;
        }

        private void ApplyAnimatorState(bool isWalking)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            animator.SetInteger(DirectionHash, (int)CurrentDirection);
            animator.SetBool(IsWalkingHash, isWalking);
        }

        private void UpdateHorizontalFacing(FacingDirection direction)
        {
            if (direction == FacingDirection.Left)
            {
                lastHorizontalFacing = HorizontalFacing.Left;
            }
            else if (direction == FacingDirection.Right)
            {
                lastHorizontalFacing = HorizontalFacing.Right;
            }
        }

        private void ApplyActionPunch()
        {
            // Nunca escala la raíz del jugador (lleva el Rigidbody2D); solo el hijo visual.
            if (spriteRenderer == null || spriteRenderer.transform == transform)
            {
                return;
            }

            float t = (Time.time - actionStartedAt) / ActionSeconds;
            if (t >= 1f)
            {
                // Sin acción en curso no se toca la escala, salvo para devolverla a su valor base.
                if (punchApplied)
                {
                    spriteRenderer.transform.localScale = baseScale;
                    punchApplied = false;
                }

                return;
            }

            punchApplied = true;
            float k = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            Vector3 scale = baseScale;
            scale.x *= 1f + actionStrengthNow * 0.5f * k;
            scale.y *= 1f - actionStrengthNow * k;
            spriteRenderer.transform.localScale = scale;
        }

        private void CenterVisual()
        {
            if (!centerVisualOnPlayer || spriteRenderer == null ||
                spriteRenderer.sprite == null || spriteRenderer.transform == transform)
            {
                return;
            }

            Transform visual = spriteRenderer.transform;
            Bounds bounds = spriteRenderer.sprite.bounds;
            Vector3 scale = visual.localScale;
            Vector3 position = visual.localPosition;
            position.x = -bounds.center.x * scale.x;
            // El segundo término mantiene los pies en su sitio mientras dura el pulso de escala.
            position.y = -bounds.center.y * scale.y + (scale.y - baseScale.y) * bounds.extents.y;
            visual.localPosition = position;
        }
    }
}
