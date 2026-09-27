using TMPro;
using UnityEngine;

namespace EscapeUNIFRANZ.UI
{
    /// <summary>
    /// Shows short non-modal feedback without owning gameplay state.
    /// </summary>
    public sealed class MessageToastView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;

        private void Awake()
        {
            Hide();
        }

        public void Show(string message)
        {
            if (root == null || label == null || string.IsNullOrWhiteSpace(message))
            {
                Hide();
                return;
            }

            label.text = message;
            root.SetActive(true);

        }

        public void Hide()
        {
            root?.SetActive(false);
        }
    }
}
