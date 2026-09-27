using EscapeUNIFRANZ.Input;
using UnityEngine;

namespace EscapeUNIFRANZ.Player
{
    /// <summary>
    /// Applies normalized player movement through the Rigidbody2D simulation.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement2D : MonoBehaviour
    {
        private const float MovingThresholdSqr = 0.0001f;

        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField, Min(0f)] private float moveSpeed = 4f;

        private Rigidbody2D body;
        private Vector2 movementInput;
        private bool movementEnabled = true;

        public Vector2 MovementInput => movementInput;
        public bool MovementEnabled => movementEnabled;
        public bool IsMoving =>
            movementEnabled && moveSpeed > 0f && movementInput.sqrMagnitude > MovingThresholdSqr;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();

            if (inputReader == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerMovement2D)} on '{name}' requires a {nameof(PlayerInputReader)} reference.",
                    this);
            }
        }

        private void Update()
        {
            movementInput = movementEnabled && inputReader != null
                ? NormalizeMovementInput(inputReader.MoveInput)
                : Vector2.zero;
        }

        private void FixedUpdate()
        {
            body.linearVelocity = movementEnabled
                ? movementInput * moveSpeed
                : Vector2.zero;
        }

        private void OnDisable()
        {
            StopImmediately();
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
        }

        /// <summary>
        /// Enables or blocks movement. Blocking always stops the body immediately.
        /// </summary>
        public void SetMovementEnabled(bool isEnabled)
        {
            movementEnabled = isEnabled;

            if (!movementEnabled)
            {
                StopImmediately();
            }
        }

        public static Vector2 NormalizeMovementInput(Vector2 input)
        {
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private void StopImmediately()
        {
            movementInput = Vector2.zero;

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
