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
            EscapeUNIFRANZ.UI.PresentationTheme.StyleText(promptLabel);
            Hide();
        }

        public void Show(string prompt)
        {
            if (promptRoot == null || promptLabel == null || string.IsNullOrWhiteSpace(prompt))
            {
                Hide();
                return;
            }

            string display = Application.isMobilePlatform
                ? prompt.Replace("[E] ", "").Replace("[E]", "").Replace("Mantén E", "Mantén Interactuar")
                : prompt;
            if (promptLabel.text != display) promptLabel.text = display;
            if (!promptRoot.activeSelf) promptRoot.SetActive(true);
        }

        public void Hide()
        {
            if (promptRoot != null && promptRoot.activeSelf)
            {
                promptRoot.SetActive(false);
            }
        }
    }
}
