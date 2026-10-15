using System.Collections.Generic;
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Player;
using TMPro;
using UnityEngine;
namespace EscapeUNIFRANZ.UI
{
    public enum TutorialAction { None=-1, Movement, Interaction, Inventory, Flashlight, Journal, Scanner, Map, AvoidThreat, PowerCut }
    // Pending presentation is local; completed/shown knowledge belongs to the session.
    public sealed class EntranceTutorialProgress
    {
        private readonly GameState state;
        private readonly List<TutorialAction> pending=new List<TutorialAction>();
        public bool IsEntrance { get; private set; }
        public EntranceTutorialProgress(GameState state) { this.state=state; }
        public void SetZone(string id) { IsEntrance=id=="entrada"; pending.Clear(); }
        public bool Completed(TutorialAction action)=>state.IsWorldItemDiscovered("onboarding_done",action.ToString());
        public bool Offer(TutorialAction action)
        {
            if(!IsEntrance || action==TutorialAction.None || Completed(action) || state.IsWorldItemDiscovered("onboarding_shown",action.ToString()) || pending.Contains(action)) return false;
            if(action==TutorialAction.Inventory && !state.HasItem("battery") && !state.IsWorldItemDiscovered("acquired","battery") || action==TutorialAction.Flashlight && !state.HasItem("flashlight") || action==TutorialAction.Map && !state.HasFlag(GameFlagId.CampusPlanRecovered) || action==TutorialAction.Journal && !state.DiscoveredWorldItems.Any(id=>id.StartsWith("clue:",System.StringComparison.Ordinal)) || (action==TutorialAction.Scanner || action==TutorialAction.AvoidThreat || action==TutorialAction.PowerCut) && !state.HasFlag(GameFlagId.FirstThreatActivated)) return false;
            if(action==TutorialAction.Inventory || action==TutorialAction.Flashlight || action==TutorialAction.Map || action==TutorialAction.AvoidThreat) pending.Insert(0,action); else pending.Add(action); return true;
        }
        public void Complete(TutorialAction action) { if(!IsEntrance) return; state.DiscoverWorldItem("onboarding_done",action.ToString()); pending.Remove(action); }
        public TutorialAction TakeNext()
        {
            foreach(var action in pending.ToArray())
            {
                if(Completed(action)) { pending.Remove(action); continue; }
                if(action==TutorialAction.Interaction && !Completed(TutorialAction.Movement)) continue;
                pending.Remove(action); state.DiscoverWorldItem("onboarding_shown",action.ToString()); return action;
            }
            return TutorialAction.None;
        }
    }
    [DefaultExecutionOrder(200)]
    public sealed class EntranceTutorialController : MonoBehaviour
    {
        [SerializeField] private GameSessionController session;
        [SerializeField] private GameplayModeController mode;
        [SerializeField] private PlayerMovement2D player;
        [SerializeField] private PlayerInteraction interaction;
        [SerializeField] private TMP_Text label;
        private EntranceTutorialProgress progress;
        private TutorialAction current=TutorialAction.None;
        private Vector2 initialPosition;
        private bool positionReady;
        private float visibleSeconds;
        private string zoneId;
        public bool IsEntrance=>zoneId=="entrada";
        private void OnEnable() { session.StateChanged+=Bind; }
        private void Start() { Bind(session.State); }
        private void Bind(GameState state) { if(state==null) return; progress=new EntranceTutorialProgress(state); SetZone(zoneId); }
        public void SetZone(string id)
        {
            zoneId=id; progress?.SetZone(id); current=TutorialAction.None; positionReady=false; Hide();
            if(IsEntrance) progress?.Offer(TutorialAction.Movement);
        }
        public void Offer(TutorialAction action)
        {
            if(progress?.Offer(action)!=true) return;
            if(current!=TutorialAction.Movement && current!=TutorialAction.Interaction && (action==TutorialAction.Inventory || action==TutorialAction.Flashlight || action==TutorialAction.Map || action==TutorialAction.AvoidThreat)) { current=TutorialAction.None; Hide(); }
        }
        public void Complete(TutorialAction action) { progress?.Complete(action); if(current==action) { current=TutorialAction.None; Hide(); } }
        private void Update()
        {
            if(!IsEntrance || progress==null || mode==null || !mode.AllowsMovement) { Hide(); return; }
            if(!positionReady) { initialPosition=player.transform.position; positionReady=true; }
            if(Vector2.Distance(initialPosition,player.transform.position)>.4f) Complete(TutorialAction.Movement);
            if(interaction.CurrentTarget!=null) Offer(TutorialAction.Interaction);
            if(current!=TutorialAction.None && progress.Completed(current)) current=TutorialAction.None;
            if(current==TutorialAction.None)
            {
                current=progress.TakeNext(); visibleSeconds=0;
                if(current!=TutorialAction.None) ProjectAudioHooks.Emit(AudioCue.TutorialNotification);
            }
            if(current==TutorialAction.None) { Hide(); return; }
            visibleSeconds+=Time.deltaTime;
            if(visibleSeconds>8f && current!=TutorialAction.Movement && current!=TutorialAction.Interaction) { current=TutorialAction.None; Hide(); return; }
            if(label!=null) { label.text=Caption(current); label.gameObject.SetActive(true); }
        }
        private static string Caption(TutorialAction action)
        {
            switch(action)
            {
                case TutorialAction.Movement:return "WASD\nMoverse";
                case TutorialAction.Interaction:return "E\nInteractuar con el objeto cercano";
                case TutorialAction.Inventory:return "I — Inventario\nRevisa la batería que acabas de recoger";
                case TutorialAction.Flashlight:return "F — Linterna\nEncender / apagar";
                case TutorialAction.Journal:return "J — Pistas\nConsultar documentos leídos";
                case TutorialAction.Scanner:return "TAB — Scanner\nResalta dispositivos cercanos";
                case TutorialAction.Map:return "M — Mapa\nPlano del campus recuperado";
                case TutorialAction.AvoidThreat:return "Evita su visión\nUsa las paredes como cobertura";
                case TutorialAction.PowerCut:return "Puedes cortar su alimentación\nUsa el panel de energía del robot";
                default:return "";
            }
        }
        private void Hide() { if(label!=null) label.gameObject.SetActive(false); }
        private void OnDisable() { session.StateChanged-=Bind; Hide(); }
    }
}
