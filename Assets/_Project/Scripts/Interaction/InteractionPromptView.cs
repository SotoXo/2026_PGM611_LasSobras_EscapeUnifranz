using TMPro;
using UnityEngine;

namespace EscapeUNIFRANZ.Interaction
{
    /// <summary>
    /// Displays the prompt for the single interaction target selected by the Player.
    /// </summary>
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TMP_Text promptLabel;

        private void Awake()
        {
            Hide();
        }

        public void Show(string prompt)
        {
            if (promptRoot == null || promptLabel == null || string.IsNullOrWhiteSpace(prompt))
            {
                Hide();
                return;
            }

            promptLabel.text = prompt;
            promptRoot.SetActive(true);
        }

        public void Hide()
        {
            if (promptRoot != null)
            {
                promptRoot.SetActive(false);
            }
        }
    }
}
