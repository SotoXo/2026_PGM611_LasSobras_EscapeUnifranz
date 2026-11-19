using UnityEngine;
namespace EscapeUNIFRANZ.Hazards
{
    // Animate only the artwork; colliders and navigation remain on the stationary parent.
    public sealed class ThreatMotionView2D : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private PatrolMover2D mover;
        [SerializeField] private ThreatFeedback2D feedback;
        [SerializeField] private TimedHazardArea2D[] hazards;
        private Vector3 restPosition, restScale;
        private SpriteRenderer sprite;
        private void Awake()
        {
            if(visual==null || visual==transform) { enabled=false; return; }
            restPosition=visual.localPosition; restScale=visual.localScale; sprite=visual.GetComponent<SpriteRenderer>();
        }
        private void LateUpdate()
        {
            bool moving=mover!=null && mover.isActiveAndEnabled && mover.IsMoving;
            bool off=feedback!=null && feedback.State==ThreatVisualState.Disabled;
            bool attack=feedback!=null && feedback.State==ThreatVisualState.Attack;
            bool warning=feedback!=null && feedback.State==ThreatVisualState.Alert;
            if(hazards!=null) foreach(var h in hazards) if(h!=null) {attack|=h.IsActive; warning|=h.State==HazardWindowState.Telegraph;}
            if(off) {moving=attack=warning=false;}
            if(sprite!=null && moving && Mathf.Abs(mover.Facing.x)>.1f) sprite.flipX=mover.Facing.x<0f;
            float bob=moving ? Mathf.Abs(Mathf.Sin(Time.time*12f))*.025f : 0f;
            visual.localPosition=restPosition+Vector3.up*bob;
            float x=off?.94f:attack?1.08f:warning?1f+Mathf.Sin(Time.time*10f)*.025f:1f;
            visual.localScale=new Vector3(restScale.x*x,restScale.y*(attack?.92f:1f),restScale.z);
        }
        private void OnDisable() { if(visual!=null) {visual.localPosition=restPosition;visual.localScale=restScale;} }
    }
}
