using System;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;
using UnityEngine;
namespace EscapeUNIFRANZ.Narrative
{
    [Serializable] public class NarrativeObjectiveStep
    { public string id,text,untilItem; public GameFlagId untilFlag; }
    public sealed class NarrativeZoneObjectives : MonoBehaviour, IZoneRuntimeInitializable
    {
        public NarrativeObjectiveStep[] steps;
        private GameSessionController session;
        public void Initialize(ZoneRuntimeContext context)
        { session=context.Session; session.State.FlagChanged+=OnFlag; session.State.InventoryChanged+=Refresh; Refresh(); }
        private void OnFlag(GameFlagId _) { Refresh(); }
        private void Refresh()
        {
            if (session==null || steps==null) return;
            foreach(var step in steps)
            {
                bool complete=step.untilFlag!=GameFlagId.None && session.State.HasFlag(step.untilFlag) ||
                    !string.IsNullOrWhiteSpace(step.untilItem) && session.State.HasItem(step.untilItem);
                if (!complete) { session.Objectives.SetCurrent(step.id,step.text); return; }
            }
        }
        private void OnDestroy()
        { if (session!=null) { session.State.FlagChanged-=OnFlag; session.State.InventoryChanged-=Refresh; } }
    }
}
