using System.Collections;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace EscapeUNIFRANZ.World
{
    public sealed class LightSequenceTrigger : MonoBehaviour, IZoneRuntimeInitializable
    {
        public string eventId;
        public GameFlagId requiredFlag;
        public Light2D[] lights;
        public float stepSeconds=0.65f, blackoutSeconds=1.8f;
        public AudioSource optionalAudio;
        public Light2D distantEyes;
        private GameSessionController session;
        private void Awake() { GetComponent<Collider2D>().isTrigger=true; }
        public void Initialize(ZoneRuntimeContext context) { session=context.Session; }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (session==null || !PlayerPhysicsContact2D.IsPlayerBody(other) ||
                (requiredFlag!=GameFlagId.None && !session.State.HasFlag(requiredFlag)) ||
                !session.State.DiscoverWorldItem("event",eventId)) return;
            StartCoroutine(Sequence());
        }
        private IEnumerator Sequence()
        {
            var original=new float[lights.Length]; var flickers=new GroupLampFlicker2D[lights.Length]; var active=new bool[lights.Length];
            if (optionalAudio!=null && optionalAudio.clip!=null) optionalAudio.Play();
            EscapeUNIFRANZ.Narrative.ProjectAudioHooks.Emit(EscapeUNIFRANZ.Narrative.AudioCue.Blackout);
            for(int i=0;i<lights.Length;i++)
            {
                if (lights[i]==null) continue;
                original[i]=lights[i].intensity; flickers[i]=lights[i].GetComponent<GroupLampFlicker2D>();
                active[i]=flickers[i]!=null && flickers[i].enabled;
                if (flickers[i]!=null) flickers[i].enabled=false;
                lights[i].intensity=0f; yield return new WaitForSeconds(stepSeconds);
            }
            session.ShowMessage("IA: LA INFRAESTRUCTURA PERMANECE BAJO CONTROL.");
            session.GetComponentInChildren<EscapeUNIFRANZ.Narrative.ScreenGlitchController>(true)?.Pulse();
            if (distantEyes!=null) distantEyes.enabled=true;
            yield return new WaitForSeconds(blackoutSeconds);
            if (distantEyes!=null) distantEyes.enabled=false;
            for(int i=0;i<lights.Length;i++)
                if (lights[i]!=null) { lights[i].intensity=original[i]; if (flickers[i]!=null) flickers[i].enabled=active[i]; }
        }
    }
}
