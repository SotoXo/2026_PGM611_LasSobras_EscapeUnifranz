using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace EscapeUNIFRANZ.Hazards
{
    public enum ThreatVisualState { Patrol, Alert, Attack, Disabled }
    public sealed class ThreatFeedback2D : MonoBehaviour
    {
        public TMP_Text marker;
        public Light2D led;
        public SpriteRenderer[] bodyRenderers;
        private Color[] original;
        private ThreatVisualState state;
        private void Awake()
        { original=new Color[bodyRenderers.Length]; for(int i=0;i<original.Length;i++) original[i]=bodyRenderers[i].color; }
        public void Show(ThreatVisualState value)
        {
            state=value;
            if (marker!=null)
            { marker.text=value==ThreatVisualState.Disabled ? "OFF" : value==ThreatVisualState.Patrol ? "" : "!"; marker.color=value==ThreatVisualState.Alert ? new Color(1f,0.8f,0.25f) : value==ThreatVisualState.Disabled ? new Color(0.65f,0.85f,0.85f) : new Color(1f,0.2f,0.12f); }
            if (bodyRenderers!=null && original!=null) for(int i=0;i<bodyRenderers.Length;i++) if (bodyRenderers[i]!=null)
                bodyRenderers[i].color=value==ThreatVisualState.Disabled ? original[i]*new Color(0.38f,0.45f,0.5f,1f) : original[i];
        }
        private void LateUpdate()
        {
            if (led==null) return;
            led.color=new Color(1f,0.05f,0.03f);
            led.intensity=state==ThreatVisualState.Disabled ? 0f : state==ThreatVisualState.Alert ? 0.6f+0.1f*Mathf.Sin(Time.time*8f) : state==ThreatVisualState.Attack ? 0.8f : 0.2f;
        }
    }
}
