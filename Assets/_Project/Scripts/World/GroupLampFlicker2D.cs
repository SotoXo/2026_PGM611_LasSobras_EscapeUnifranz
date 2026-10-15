using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace EscapeUNIFRANZ.World
{
    [DisallowMultipleComponent]
    public sealed class GroupLampFlicker2D : MonoBehaviour
    {
        [SerializeField] private Light2D lamp;
        [SerializeField] private SpriteRenderer lampRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float phase=7.3f;
        [SerializeField, Min(0)] private float intensity=.32f;
        [SerializeField] private float minimumIntensity;
        [SerializeField] private int randomSeed;
        [SerializeField] private bool startsOff;
        [SerializeField] private Vector2 onSeconds=new Vector2(4.2f,10f), offSeconds=new Vector2(.06f,.08f);
        [SerializeField] private float shimmer=.015f,shimmerSpeed=.8f,failureProbability=.5f,briefCutChance=.06f;
        [SerializeField] private AudioSource optionalAudio;
        [SerializeField] private AudioClip failureSound;
        private System.Random random;
        private float nextTransitionAt,overrideIntensity,externalIntensity;
        private bool overridden,external;
        private bool externalFault;
        private int burst;
        public RoomFailurePattern failurePattern=RoomFailurePattern.Double;
        private float weakUntil;
        public void SetAutomaticPower(float level) { intensity=Mathf.Max(0,level); ResumeFlicker(); }
        public void SetOverride(bool value,float level=0) { overridden=value; overrideIntensity=level; ApplyPower(); }
        public void SetExternalPower(float level,bool fault=false) { external=true; externalFault=fault; externalIntensity=Mathf.Max(0,level); ApplyPower(); }
        public void ResumeFlicker() { external=false; burst=0; nextTransitionAt=Time.time+Duration(); ApplyPower(); }
        private void OnEnable() { random=new System.Random(randomSeed!=0 ? randomSeed : unchecked(Mathf.RoundToInt(phase*1000)^GetInstanceID())); if(startsOff) { external=true; externalIntensity=0; } burst=0; nextTransitionAt=Time.time+Duration(); ApplyPower(); }
        private float Duration()=>Mathf.Lerp(Mathf.Max(4.2f,onSeconds.x),Mathf.Max(4.2f,onSeconds.y),(float)random.NextDouble());
        private void Update()
        {
            if(!external && !overridden && Time.time>=nextTransitionAt)
            {
                if(burst==0)
                {
                    if(random.NextDouble()<failureProbability) { burst=1; nextTransitionAt=Time.time+(failurePattern==RoomFailurePattern.Strong ? .4f : failurePattern==RoomFailurePattern.Mild ? .06f : .08f); EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.Flicker); if(optionalAudio!=null && failureSound!=null) optionalAudio.PlayOneShot(failureSound); }
                    else nextTransitionAt=Time.time+Duration();
                }
                else if(burst==1 && failurePattern==RoomFailurePattern.Mild) { burst=0; nextTransitionAt=Time.time+Duration(); }
                else if(burst==1) { burst=2; nextTransitionAt=Time.time+(failurePattern==RoomFailurePattern.Strong ? .3f : .12f); }
                else if(burst==2) { burst=3; nextTransitionAt=Time.time+(failurePattern==RoomFailurePattern.Strong ? .18f : Mathf.Clamp(offSeconds.y,.05f,.08f)); }
                else { burst=0; if(failurePattern==RoomFailurePattern.Strong) weakUntil=Time.time+6f; nextTransitionAt=Time.time+Duration()+briefCutChance; }
            }
            ApplyPower();
        }
        private void ApplyPower()
        {
            float level=overridden ? overrideIntensity : external ? externalIntensity : burst==1 || burst==3 ? Mathf.Min(0,minimumIntensity) : intensity*(Time.time<weakUntil ? .45f : 1f)*(1-Mathf.Clamp(shimmer,0,.03f)*Mathf.PerlinNoise(phase,Time.time*shimmerSpeed));
            if(lamp!=null) lamp.intensity=level;
            int frame=level<=0 ? 2 : external && externalFault && !overridden ? 0 : burst==2 && !external && !overridden ? 3 : 1;
            if(lampRenderer!=null && frames!=null && frames.Length>frame) lampRenderer.sprite=frames[frame];
        }
    }
}
