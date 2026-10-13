using UnityEngine;
using UnityEngine.EventSystems;
namespace EscapeUNIFRANZ.Narrative
{
    // Clicks still work through Button; dragging commits only at a destination port.
    public sealed class CablePortDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string portId;
        public NarrativeController controller;
        public void OnBeginDrag(PointerEventData data) { controller.BeginCable(portId); }
        public void OnDrag(PointerEventData data) { }
        public void OnEndDrag(PointerEventData data)
        {
            var destination=data.pointerCurrentRaycast.gameObject;
            var port=destination!=null ? destination.GetComponentInParent<CablePortDrag>() : null;
            controller.EndCable(port!=null ? port.portId : null);
        }
    }
}
