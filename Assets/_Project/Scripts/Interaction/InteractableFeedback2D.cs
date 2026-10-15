using EscapeUNIFRANZ.UI;
using TMPro;
using UnityEngine;
namespace EscapeUNIFRANZ.Interaction
{
    [DisallowMultipleComponent]
    public sealed class InteractableFeedback2D : MonoBehaviour
    {
        private TMP_Text cue;
        private IInteractable interactable;
        public void SetSelected(bool value)
        {
            if (!value) { if(cue!=null) cue.gameObject.SetActive(false); return; }
            foreach(var component in GetComponents<MonoBehaviour>()) if(component.isActiveAndEnabled && component is IInteractable candidate) { interactable=candidate; break; }
            if(cue==null) { var obj=new GameObject("ProximityCue"); obj.transform.SetParent(transform,false); obj.transform.localPosition=new Vector3(0,1.05f,-.2f); cue=obj.AddComponent<TextMeshPro>(); cue.font=PresentationTheme.Font; cue.fontSize=1.5f; cue.alignment=TextAlignmentOptions.Center; cue.rectTransform.sizeDelta=new Vector2(4.2f,.65f); obj.GetComponent<MeshRenderer>().sortingOrder=81; }
            cue.text=interactable?.Prompt; cue.gameObject.SetActive(true);
        }
        private void Update() { if(cue!=null && cue.gameObject.activeSelf) { cue.text=interactable?.Prompt; cue.color=new Color(1f,.9f,.6f,.78f+.18f*Mathf.Sin(Time.time*4f)); } }
        private void OnDisable() { SetSelected(false); }
    }
}
