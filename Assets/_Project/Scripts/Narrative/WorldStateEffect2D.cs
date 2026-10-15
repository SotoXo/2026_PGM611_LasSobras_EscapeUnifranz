using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed class WorldStateEffect2D : MonoBehaviour, IZoneRuntimeInitializable
    {
        public GameFlagId flag;
        public Light2D[] restoredLights;
        public SpriteRenderer[] monitors;
        public UnityEvent onRestored=new UnityEvent();
        private GameState state;
        private bool applied;
        public void Initialize(ZoneRuntimeContext context) { state=context.Session.State; state.FlagChanged+=Changed; Apply(); }
        private void Changed(GameFlagId changed) { if (changed==flag) Apply(); }
        private void Apply()
        {
            bool on=state.HasFlag(flag);
            foreach(var light in restoredLights ?? new Light2D[0])
            {
                if (light==null) continue;
                if (light.TryGetComponent<GroupLampFlicker2D>(out var flicker)) flicker.SetExternalPower(on ? 0.48f : 0f);
                light.intensity=on ? 0.48f : 0f;
            }
            foreach(var monitor in monitors ?? new SpriteRenderer[0])
                if (monitor!=null) monitor.color=on ? new Color(0.6f,1f,0.85f) : new Color(0.3f,0.4f,0.5f);
            if (on && !applied) { applied=true; onRestored.Invoke(); }
        }
        private void OnDestroy() { if (state!=null) state.FlagChanged-=Changed; }
    }
}
