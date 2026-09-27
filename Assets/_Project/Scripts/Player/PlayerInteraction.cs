using EscapeUNIFRANZ.Input;
using EscapeUNIFRANZ.Interaction;
using UnityEngine;

namespace EscapeUNIFRANZ.Player
{
    /// <summary>
    /// Coordinates target selection, prompt updates and one interaction per E press.
    /// </summary>
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private InteractionSensor sensor;
        [SerializeField] private InteractionPromptView promptView;

        private IInteractable currentTarget;
        private bool interactionEnabled = true;

        public IInteractable CurrentTarget => currentTarget;
        public bool InteractionEnabled => interactionEnabled;

        private void OnEnable()
        {
            if (inputReader == null || sensor == null || promptView == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerInteraction)} on '{name}' requires input, sensor and prompt references.",
                    this);
                enabled = false;
                return;
            }

            inputReader.InteractPressed += OnInteractPressed;
        }

        private void Update()
        {
            RefreshTarget();
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.InteractPressed -= OnInteractPressed;
            }

            SetCurrentTarget(null);
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            interactionEnabled = isEnabled;

            if (interactionEnabled)
            {
                RefreshTarget();
            }
            else
            {
                SetCurrentTarget(null);
            }
        }

        private void OnInteractPressed()
        {
            if (!interactionEnabled)
            {
                return;
            }

            RefreshTarget();
            IInteractable target = currentTarget;

            if (target == null || !target.CanInteract)
            {
                return;
            }

            target.Interact();
            RefreshTarget();
        }

        private void RefreshTarget()
        {
            IInteractable resolvedTarget = interactionEnabled
                ? sensor.ResolveCurrent(transform.position)
                : null;

            SetCurrentTarget(resolvedTarget);
        }

        private void SetCurrentTarget(IInteractable target)
        {
            if (ReferenceEquals(currentTarget, target))
            {
                return;
            }

            currentTarget = target;

            if (currentTarget != null && currentTarget.CanInteract)
            {
                promptView.Show(currentTarget.Prompt);
            }
            else
            {
                promptView.Hide();
            }
        }
    }
}
