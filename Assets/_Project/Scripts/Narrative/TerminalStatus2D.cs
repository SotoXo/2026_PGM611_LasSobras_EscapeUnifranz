using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.World;
using UnityEngine;
namespace EscapeUNIFRANZ.Narrative
{
    public enum TerminalState { Off, Booting, Online, Error, Locked, Compromised }
    public sealed class TerminalStatus2D : MonoBehaviour, IZoneRuntimeInitializable, IScanStateProvider
    {
        public GameFlagId powerFlag, repairedFlag;
        public string credentialItem;
        public bool compromisedUntilRepair;
        public string puzzleId;
        public SpriteRenderer monitor;
        [TextArea] public string diagnostic="Dos cables fueron retirados manualmente. PWR y DATA sin continuidad.";
        private GameState state;
        private float bootUntil;
        private bool Repaired => state!=null && ((repairedFlag!=GameFlagId.None && state.HasFlag(repairedFlag)) ||
            (!string.IsNullOrEmpty(puzzleId) && state.IsWorldItemDiscovered("puzzle",puzzleId)) || (repairedFlag==GameFlagId.None && string.IsNullOrEmpty(puzzleId)));
        public TerminalState Current => state==null || (powerFlag!=GameFlagId.None && !state.HasFlag(powerFlag)) ? TerminalState.Off :
            Time.time<bootUntil ? TerminalState.Booting : !Repaired && !string.IsNullOrEmpty(credentialItem) && !state.HasItem(credentialItem) ? TerminalState.Locked :
            !Repaired ? (compromisedUntilRepair ? TerminalState.Compromised : TerminalState.Error) : TerminalState.Online;
        public string ScanStateText => "["+Current.ToString().ToUpperInvariant()+"] "+diagnostic;
        public void Initialize(ZoneRuntimeContext context)
        { state=context.Session.State; state.FlagChanged+=Changed; Apply(); }
        private void Changed(GameFlagId flag) { if (flag==powerFlag || flag==repairedFlag) { bootUntil=Time.time+0.8f; Apply(); } }
        private void Update() { Apply(); }
        private void Apply()
        { if (monitor!=null) monitor.color=Current==TerminalState.Off ? new Color(0.3f,0.3f,0.35f) : Current==TerminalState.Online ? new Color(0.6f,1f,0.85f) : new Color(1f,0.7f,0.45f); }
        private void OnDestroy() { if (state!=null) state.FlagChanged-=Changed; }
    }
}
