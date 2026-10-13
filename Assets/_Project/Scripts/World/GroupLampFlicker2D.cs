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
        [SerializeField] private float phase = 7.3f;
        [SerializeField, Min(0f)] private float intensity = 0.32f;
        [SerializeField, Min(0f)] private float minimumIntensity=0.015f;
        [SerializeField] private int randomSeed;
        [Header("Intermittent power")]
        [SerializeField] private Vector2 onSeconds = new Vector2(2.8f, 7.2f);
        [SerializeField] private Vector2 offSeconds = new Vector2(0.12f, 0.45f);
        [SerializeField, Range(0f, 0.25f)] private float shimmer = 0.08f;
        [SerializeField, Min(0.1f)] private float shimmerSpeed = 0.8f;
        [SerializeField, Range(0f,1f)] private float failureProbability=0.75f;
        [SerializeField, Range(0f,0.2f)] private float briefCutChance=0.06f;

        private System.Random random;
        private float nextTransitionAt;
        private bool powered;
        private bool overridden;
        private float overrideIntensity;
        public void SetOverride(bool value,float level=0f) { overridden=value; overrideIntensity=level; }
        [SerializeField] private AudioSource optionalAudio;
        [SerializeField] private AudioClip failureSound;

        private void OnEnable()
        {
            // Local random stream: different phases and instances never share a flicker clock.
            random = new System.Random(randomSeed!=0 ? randomSeed : unchecked(Mathf.RoundToInt(phase * 1000f) ^ GetInstanceID()));
            powered = true;
            nextTransitionAt = Time.time + Duration(onSeconds);
            ApplyPower();
        }

        private void Update()
        {
            if (overridden) { if (lamp!=null) lamp.intensity=overrideIntensity; return; }
            if (Time.time >= nextTransitionAt)
            {
                powered = powered ? random.NextDouble()>=failureProbability : true;
                if (!powered && optionalAudio!=null && failureSound!=null) optionalAudio.PlayOneShot(failureSound);
                if (!powered) EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.Flicker);
                float duration=Duration(powered ? onSeconds : offSeconds);
                if (!powered && random.NextDouble()<briefCutChance) duration=0.065f;
                nextTransitionAt = Time.time + duration;
            }
            ApplyPower();
        }

        private void ApplyPower()
        {
            float noise = Mathf.PerlinNoise(phase, Time.time * shimmerSpeed);
            float level = powered ? 1f - shimmer * noise : 0f;
            int frame = powered ? (noise > 0.65f ? 1 : 0) : 3;
            if (lamp != null) lamp.intensity = powered ? intensity * level : Mathf.Min(intensity,minimumIntensity);
            if (lampRenderer != null && frames != null && frames.Length > frame)
                lampRenderer.sprite = frames[frame];
        }

        private float Duration(Vector2 range)
        {
            float minimum = Mathf.Max(0.05f, range.x);
            return Mathf.Lerp(minimum, Mathf.Max(minimum, range.y), (float)random.NextDouble());
        }

        private void OnValidate()
        {
            onSeconds.x = Mathf.Max(0.5f, onSeconds.x);
            onSeconds.y = Mathf.Max(onSeconds.x, onSeconds.y);
            offSeconds.x = Mathf.Max(0.05f, offSeconds.x);
            offSeconds.y = Mathf.Max(offSeconds.x, offSeconds.y);
        }
    }
}
