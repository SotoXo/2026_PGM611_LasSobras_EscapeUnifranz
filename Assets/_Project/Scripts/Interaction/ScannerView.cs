using TMPro;
using UnityEngine;

namespace EscapeUNIFRANZ.Interaction
{
    public sealed class ScannerView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;

        public void Show(int revealedCount)
        {
            if (root == null || label == null)
            {
                return;
            }

            label.text = revealedCount > 0
                ? $"ESCÁNER ACTIVO — {revealedCount} SEÑALES"
                : "ESCÁNER ACTIVO — SIN SEÑALES";
            root.SetActive(true);
        }

        public void Hide()
        {
            root?.SetActive(false);
        }
    }
}
