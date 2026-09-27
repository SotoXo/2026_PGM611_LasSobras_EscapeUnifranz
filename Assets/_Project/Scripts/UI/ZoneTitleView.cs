using System.Collections;
using TMPro;
using UnityEngine;

namespace EscapeUNIFRANZ.UI
{
    public sealed class ZoneTitleView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;
        [SerializeField, Min(0.1f)] private float visibleSeconds = 1.8f;

        private Coroutine routine;
        private bool countdownPending;

        public void Show(string displayName)
        {
            if (root == null || label == null || string.IsNullOrWhiteSpace(displayName))
            {
                Hide();
                return;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            label.text = displayName.Trim().ToUpperInvariant();
            root.SetActive(true);
            countdownPending = true;
            StartCountdownIfVisible();
        }

        public void Hide()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            countdownPending = false;
            root?.SetActive(false);
        }

        private void OnEnable()
        {
            StartCountdownIfVisible();
        }

        private void OnDisable()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        private void StartCountdownIfVisible()
        {
            if (!countdownPending || routine != null || !isActiveAndEnabled ||
                root == null || !root.activeInHierarchy)
            {
                return;
            }

            routine = StartCoroutine(HideLater());
        }

        private IEnumerator HideLater()
        {
            yield return new WaitForSecondsRealtime(visibleSeconds);
            routine = null;
            countdownPending = false;
            root.SetActive(false);
        }
    }
}
