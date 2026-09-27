using UnityEngine;

namespace EscapeUNIFRANZ.Player
{
    /// <summary>
    /// Maps top-down movement to the four directional animation states.
    /// The renderer can live on a scaled child so physics remain on the Player root.
    /// </summary>
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

        private HorizontalFacing lastHorizontalFacing = HorizontalFacing.Right;

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

            CurrentDirection = initialDirection;
            UpdateHorizontalFacing(CurrentDirection);
            ApplyAnimatorState(false);
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
            CenterVisual();
        }

        private void OnDisable()
        {
            ApplyAnimatorState(false);
        }

        public static FacingDirection ResolveDirection(
            Vector2 movementInput,
            FacingDirection previousDirection)
        {
            if (movementInput.sqrMagnitude <= DirectionThresholdSqr)
            {
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
            position.y = -bounds.center.y * scale.y;
            visual.localPosition = position;
        }
    }
}
