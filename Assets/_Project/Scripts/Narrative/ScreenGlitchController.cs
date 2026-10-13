using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed class ScreenGlitchController : MonoBehaviour
    {
        public Image dim;
        public Image[] bands;
        public TMP_Text intrusion;
        public AudioSource optionalAudio;
        public AudioClip glitchSound;
        private float until, started, nextAllowed;
        private bool red, words;
        public void Pulse(bool alarm=false, bool showWords=false)
        {
            if (Time.unscaledTime<nextAllowed) return;
            started=Time.unscaledTime; until=started+0.32f; nextAllowed=started+12f;
            red=alarm; words=showWords;
            ProjectAudioHooks.Emit(AudioCue.TerrorInterference);
            if (optionalAudio!=null && glitchSound!=null) optionalAudio.PlayOneShot(glitchSound);
        }
        private void Update()
        {
            float elapsed=Time.unscaledTime-started;
            bool active=Time.unscaledTime<until;
            if (dim!=null) dim.color=active ? new Color(red ? 0.38f : 0f,0f,0f,0.06f) : Color.clear;
            if (intrusion!=null) { intrusion.text="TE VEO"; intrusion.enabled=active && words && elapsed<0.15f; }
            if (bands==null) return;
            for(int i=0;i<bands.Length;i++) if (bands[i]!=null)
            {
                bands[i].color=active ? new Color(0.5f,0.7f,0.72f,0.045f) : Color.clear;
                bands[i].rectTransform.anchoredPosition=new Vector2(active ? Mathf.Sin(elapsed*17f+i)*3f : 0f, 190f-i*70f);
            }
        }
        private void OnDisable()
        {
            if (dim!=null) dim.color=Color.clear;
            if (intrusion!=null) intrusion.enabled=false;
            if (bands!=null) foreach(var band in bands) if (band!=null) band.color=Color.clear;
        }
    }
}
