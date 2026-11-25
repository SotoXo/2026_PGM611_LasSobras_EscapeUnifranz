using EscapeUNIFRANZ.Narrative;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class MenuButtonFeedback : MonoBehaviour, ISelectHandler, IDeselectHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        public TMP_Text label;
        private Button button;
        private bool pointed;
        private bool selected;
        private bool readable;

        public void UseReadableText()
        {
            readable = true;
            Refresh();
        }

        private void Awake()
        {
            button = GetComponent<Button>();
            Refresh();
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            Focus();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointed = true;
            Focus();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointed = false;
            Refresh();
        }

        private void OnDisable()
        {
            pointed = selected = false;
            Refresh();
        }

        private void Focus()
        {
            Refresh();
            if (button != null && button.IsInteractable()) ProjectAudioHooks.Emit(AudioCue.MenuFocus);
        }

        public void Refresh()
        {
            if (label == null || button == null) return;
            if (readable)
            {
                label.color = !button.IsInteractable() ? new Color32(170, 151, 151, 255) :
                    pointed || selected ? new Color32(255, 237, 223, 255) : new Color32(243, 211, 195, 255);
                return;
            }
            label.color = !button.IsInteractable() ? new Color32(135, 113, 109, 255) :
                pointed || selected ? new Color32(255, 128, 106, 255) : new Color32(220, 67, 54, 255);
        }
    }
}
