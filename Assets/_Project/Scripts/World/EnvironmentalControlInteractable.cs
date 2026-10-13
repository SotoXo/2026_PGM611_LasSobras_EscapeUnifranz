using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Narrative;
using UnityEngine;
namespace EscapeUNIFRANZ.World
{
    public enum EnvironmentalAction { PowerCut, Distraction, ChargingLock }
    public sealed class EnvironmentalControlInteractable : InteractableBehaviour, IZoneRuntimeInitializable, IScanStateProvider
    {
        public EnvironmentalAction action;
        public MiniAutoController mini;
        public RobotDogEncounterController dog;
        public Transform destination;
        public float disabledSeconds=20f, cooldownSeconds=30f, captureRadius=1.8f;
        [TextArea] public string explanation;
        private NarrativeController narrative;
        private float readyAt;
        public string ScanStateText => Time.time<readyAt ? "[RECARGANDO]" : action==EnvironmentalAction.ChargingLock ? "[ESTACIÓN DE CARGA / CIERRE LOCAL]" : "[ALIMENTACIÓN EXTERNA / CONTROL LOCAL]";
        public void Initialize(ZoneRuntimeContext context) { narrative=context.Session.GetComponent<NarrativeController>(); }
        protected override void PerformInteraction() { narrative.ShowEnvironmentalControl(this); }
        public string Execute()
        {
            if (Time.time<readyAt) return "El panel se está reiniciando. Espera "+Mathf.CeilToInt(readyAt-Time.time)+" s.";
            if (action==EnvironmentalAction.ChargingLock)
            {
                if (dog==null || destination==null || Vector2.Distance(dog.UnitPosition,destination.position)>captureRadius) return "La estación está vacía. Atrae al perro hasta la luz amarilla y activa el cierre.";
                if (!dog.CaptureAtStation()) return "La unidad ya está desconectada.";
                readyAt=Time.time+cooldownSeconds; return "ESTACIÓN BLOQUEADA. Perro robot desconectado.";
            }
            if (action==EnvironmentalAction.Distraction)
            { if (destination!=null) { mini?.DistractAt(destination.position,5f); dog?.DistractAt(destination.position,5f); } readyAt=Time.time+cooldownSeconds; return "SEÑAL DE PRUEBA ACTIVA. La unidad revisará ese punto."; }
            mini?.SuspendFor(disabledSeconds); dog?.SuspendFor(disabledSeconds); readyAt=Time.time+cooldownSeconds;
            return "ALIMENTACIÓN CORTADA. Unidad desactivada "+disabledSeconds+" s al cerrar el panel.";
        }
    }
}
