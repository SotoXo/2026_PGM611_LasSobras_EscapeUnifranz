using EscapeUNIFRANZ.UI;
using TMPro;
using UnityEngine;

namespace EscapeUNIFRANZ.Interaction
{
    [DisallowMultipleComponent]
    public sealed class InteractableFeedback2D : MonoBehaviour
    {
        private TMP_Text caption;

        public void SetPrompt(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                if (caption != null) caption.gameObject.SetActive(false);
                return;
            }

            if (caption == null)
            {
                var holder = new GameObject("ProximityCue");
                holder.transform.SetParent(transform, false);
                holder.transform.localPosition = new Vector3(0, 1.05f, -.2f);
                caption = holder.AddComponent<TextMeshPro>();
                caption.font = PresentationTheme.Font;
                caption.fontSize = 1.5f;
                caption.alignment = TextAlignmentOptions.Center;
                caption.rectTransform.sizeDelta = new Vector2(4.2f, .65f);
                caption.color = new Color(1f, .9f, .6f, .92f);
                holder.GetComponent<MeshRenderer>().sortingOrder = 81;
            }

            caption.text = prompt;
            caption.gameObject.SetActive(true);
        }

        private void OnDisable() => SetPrompt(null);
    }
}
