using UnityEngine;

namespace EscapeUNIFRANZ.Interaction
{
    /// <summary>
    /// Temporary graybox interaction that writes a configured message to the Console.
    /// </summary>
    public sealed class DebugLogInteractable : InteractableBehaviour
    {
        [SerializeField, TextArea] private string logMessage;

        protected override void PerformInteraction()
        {
            Debug.Log(logMessage, this);
        }
    }
}
