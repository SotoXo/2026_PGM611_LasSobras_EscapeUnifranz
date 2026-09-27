using EscapeUNIFRANZ.Core;

namespace EscapeUNIFRANZ.Interaction
{
    public enum ScanCategory
    {
        Interactable = 0,
        Terminal = 1,
        Door = 2,
        Sensor = 3,
        Actuator = 4,
        NetworkNode = 5,
        Checkpoint = 6
    }

    public interface IScanStateProvider
    {
        string ScanStateText { get; }
    }

    public sealed class ScannerTimer
    {
        private readonly float duration;
        private readonly float cooldown;
        private float activeUntil;
        private float nextAllowedAt;

        public ScannerTimer(float durationSeconds = 2.5f, float cooldownSeconds = 4f)
        {
            duration = durationSeconds > 0f ? durationSeconds : 2.5f;
            cooldown = cooldownSeconds > 0f ? cooldownSeconds : 4f;
        }

        public bool IsActive { get; private set; }
        public float ActiveUntil => activeUntil;
        public float NextAllowedAt => nextAllowedAt;

        public bool TryActivate(float now)
        {
            if (IsActive || now < nextAllowedAt)
            {
                return false;
            }

            IsActive = true;
            activeUntil = now + duration;
            nextAllowedAt = now + cooldown;
            return true;
        }

        public bool Tick(float now)
        {
            if (!IsActive || now < activeUntil)
            {
                return false;
            }

            IsActive = false;
            return true;
        }
    }

    public static class ScanLogic
    {
        public static bool CanReveal(
            GameState state,
            bool secret,
            GameFlagId revealFlag)
        {
            if (!secret)
            {
                return true;
            }

            return revealFlag != GameFlagId.None && state != null && state.HasFlag(revealFlag);
        }

        public static string CategoryLabel(ScanCategory category)
        {
            switch (category)
            {
                case ScanCategory.Interactable: return "INTERACTUABLE";
                case ScanCategory.Terminal: return "TERMINAL";
                case ScanCategory.Door: return "PUERTA";
                case ScanCategory.Sensor: return "SENSOR";
                case ScanCategory.Actuator: return "ACTUADOR";
                case ScanCategory.NetworkNode: return "NODO DE RED";
                case ScanCategory.Checkpoint: return "CHECKPOINT";
                default: return category.ToString().ToUpperInvariant();
            }
        }
    }
}
