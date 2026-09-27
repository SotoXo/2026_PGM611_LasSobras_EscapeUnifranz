using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace EscapeUNIFRANZ.Input
{
    /// <summary>
    /// Reads the configured gameplay actions and exposes player intent as state or events.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Assign InputSystem_Actions/Player/Move (Value, Vector2).")]
        private InputActionReference moveAction;

        [SerializeField]
        [Tooltip("Assign InputSystem_Actions/Player/Interact (Button, E).")]
        private InputActionReference interactAction;

        [SerializeField]
        [Tooltip("Assign InputSystem_Actions/Player/ToggleMap (Button, M).")]
        private InputActionReference toggleMapAction;

        [SerializeField]
        [Tooltip("Assign InputSystem_Actions/Player/Scan (Button, Tab).")]
        private InputActionReference scanAction;

        private InputAction subscribedMoveAction;
        private InputAction subscribedInteractAction;
        private InputAction subscribedToggleMapAction;
        private InputAction subscribedScanAction;
        private bool enabledMoveActionLocally;
        private bool enabledInteractActionLocally;
        private bool enabledToggleMapActionLocally;
        private bool enabledScanActionLocally;

        public Vector2 MoveInput { get; private set; }
        public event Action InteractPressed;
        public event Action ToggleMapPressed;
        public event Action ScanPressed;

        private void OnEnable()
        {
            if (moveAction == null || moveAction.action == null ||
                interactAction == null || interactAction.action == null ||
                toggleMapAction == null || toggleMapAction.action == null ||
                scanAction == null || scanAction.action == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerInputReader)} on '{name}' requires Move, Interact, ToggleMap and Scan InputActionReferences.",
                    this);
                enabled = false;
                return;
            }

            subscribedMoveAction = moveAction.action;
            subscribedInteractAction = interactAction.action;
            subscribedToggleMapAction = toggleMapAction.action;
            subscribedScanAction = scanAction.action;

            subscribedMoveAction.performed += OnMoveChanged;
            subscribedMoveAction.canceled += OnMoveChanged;
            subscribedInteractAction.started += OnInteractStarted;
            subscribedToggleMapAction.started += OnToggleMapStarted;
            subscribedScanAction.started += OnScanStarted;

            enabledMoveActionLocally = !subscribedMoveAction.enabled;
            if (enabledMoveActionLocally)
            {
                subscribedMoveAction.Enable();
            }

            enabledInteractActionLocally = !subscribedInteractAction.enabled;
            if (enabledInteractActionLocally)
            {
                subscribedInteractAction.Enable();
            }

            enabledToggleMapActionLocally = !subscribedToggleMapAction.enabled;
            if (enabledToggleMapActionLocally)
            {
                subscribedToggleMapAction.Enable();
            }

            enabledScanActionLocally = !subscribedScanAction.enabled;
            if (enabledScanActionLocally)
            {
                subscribedScanAction.Enable();
            }

            MoveInput = subscribedMoveAction.ReadValue<Vector2>();
        }

        private void OnDisable()
        {
            MoveInput = Vector2.zero;

            if (subscribedMoveAction != null)
            {
                subscribedMoveAction.performed -= OnMoveChanged;
                subscribedMoveAction.canceled -= OnMoveChanged;

                if (enabledMoveActionLocally)
                {
                    subscribedMoveAction.Disable();
                }
            }

            if (subscribedInteractAction != null)
            {
                subscribedInteractAction.started -= OnInteractStarted;

                if (enabledInteractActionLocally)
                {
                    subscribedInteractAction.Disable();
                }
            }

            if (subscribedToggleMapAction != null)
            {
                subscribedToggleMapAction.started -= OnToggleMapStarted;

                if (enabledToggleMapActionLocally)
                {
                    subscribedToggleMapAction.Disable();
                }
            }

            if (subscribedScanAction != null)
            {
                subscribedScanAction.started -= OnScanStarted;

                if (enabledScanActionLocally)
                {
                    subscribedScanAction.Disable();
                }
            }

            subscribedMoveAction = null;
            subscribedInteractAction = null;
            subscribedToggleMapAction = null;
            subscribedScanAction = null;
            enabledMoveActionLocally = false;
            enabledInteractActionLocally = false;
            enabledToggleMapActionLocally = false;
            enabledScanActionLocally = false;
        }

        private void OnMoveChanged(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
        }

        private void OnInteractStarted(InputAction.CallbackContext context)
        {
            if (context.control is KeyControl key && key.keyCode == Key.E)
            {
                InteractPressed?.Invoke();
            }
        }

        private void OnToggleMapStarted(InputAction.CallbackContext context)
        {
            if (context.control is KeyControl key && key.keyCode == Key.M)
            {
                ToggleMapPressed?.Invoke();
            }
        }

        private void OnScanStarted(InputAction.CallbackContext context)
        {
            if (context.control is KeyControl key && key.keyCode == Key.Tab)
            {
                ScanPressed?.Invoke();
            }
        }
    }
}
