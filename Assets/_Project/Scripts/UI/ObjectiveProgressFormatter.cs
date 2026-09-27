using UnityEngine;

namespace EscapeUNIFRANZ.UI
{
    public static class ObjectiveProgressFormatter
    {
        public static string Format(string objective, int progress, int total)
        {
            if (string.IsNullOrWhiteSpace(objective))
            {
                return string.Empty;
            }

            if (total <= 0)
            {
                return objective.Trim();
            }

            int safeTotal = Mathf.Max(1, total);
            int safeProgress = Mathf.Clamp(progress, 0, safeTotal);
            return $"{objective.Trim()} [{safeProgress}/{safeTotal}]";
        }
    }
}
