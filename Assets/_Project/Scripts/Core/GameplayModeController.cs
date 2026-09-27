using System;
using EscapeUNIFRANZ.Player;
using UnityEngine;

namespace EscapeUNIFRANZ.Core
{
    /// <summary>
    /// Applies the exclusive gameplay mode to movement and interaction.
    /// </summary>
    public sealed class GameplayModeController : MonoBehaviour
    {
        [SerializeField] private PlayerMovement2D playerMovement;
        [SerializeField] private PlayerInteraction playerInteraction;

        private readonly GameplayModeState state = new GameplayModeState();

        public GameplayMode CurrentMode => state.Current;
        public bool AllowsGameplay => state.AllowsGameplay;
        public bool AllowsMovement => state.AllowsMovement;
        public bool AllowsInteraction => state.AllowsInteraction;
        public bool AllowsMapToggle => state.AllowsMapToggle;
        public event Action<GameplayMode> ModeChanged;

        private void Awake()
        {
            ApplyMode();
        }

        public void SetMode(GameplayMode mode)
        {
            if (!state.SetMode(mode))
            {
                ApplyMode();
                return;
            }

            ApplyMode();
            ModeChanged?.Invoke(mode);
        }

        private void ApplyMode()
        {
            playerMovement?.SetMovementEnabled(state.AllowsMovement);
            playerInteraction?.SetInteractionEnabled(state.AllowsInteraction);
        }
    }
}
