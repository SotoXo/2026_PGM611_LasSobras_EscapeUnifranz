using EscapeUNIFRANZ.Input;
using UnityEngine;

namespace EscapeUNIFRANZ.Player
{
    /// <summary>
    /// Applies normalized player movement through the Rigidbody2D simulation.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement2D : MonoBehaviour
    {
        private const float MovingThresholdSqr = 0.0001f;

        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;

        private Rigidbody2D body;
        private Vector2 movementInput;
        private bool movementEnabled = true;
        private Transform cover;

        public bool IsConcealed => cover != null &&
            ((Vector2)(cover.position - transform.position)).sqrMagnitude <= 1.25f * 1.25f;
        public bool IsUsingCover(Transform target) => cover == target && IsConcealed;
        public void SetCover(Transform target) => cover = target;
        [SerializeField] private AudioSource optionalFootsteps;
        [SerializeField] private AudioClip footstepSound;
        [SerializeField] private float stepInterval=0.38f;
        private float nextStepAt;
        [SerializeField] private bool tiledSurface;
        private bool kitchenFloor;
        private Vector2 lastPosition;

        public void SetZoneSurface(string zoneId)
        {
            tiledSurface = zoneId == "entrada" || zoneId == "hall" || zoneId == "arca";
            kitchenFloor = zoneId == "piso2";
        }

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
            if (IsMoving) cover = null;
            Vector2 position = body.position;
            bool displaced = (position - lastPosition).sqrMagnitude > 0.0001f;
            lastPosition = position;
            if (IsMoving && displaced && Time.time >= nextStepAt)
            {
                if (optionalFootsteps != null && footstepSound != null) optionalFootsteps.PlayOneShot(footstepSound);
                bool tile = tiledSurface || (kitchenFloor && position.x < 10f);
                EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(tile
                    ? EscapeUNIFRANZ.Narrative.AudioCue.FootstepTile : EscapeUNIFRANZ.Narrative.AudioCue.FootstepFloor);
                nextStepAt = Time.time + stepInterval;
            }
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
