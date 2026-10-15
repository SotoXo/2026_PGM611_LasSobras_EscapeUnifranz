using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed partial class NarrativeController
    {
        public void ShowEnvironmentalControl(EnvironmentalControlInteractable owner)
        {
            if (!Open(GameplayMode.Puzzle)) return;
            string action=owner.action==EnvironmentalAction.PowerCut ? "CORTAR ALIMENTACIÓN" : owner.action==EnvironmentalAction.ChargingLock ? "BLOQUEAR ESTACIÓN" : "ACTIVAR DISTRACCIÓN";
            view.Options("CONTROL DEL SECTOR",owner.explanation,new[]{action},_=>
            { string message=owner.Execute(); Close(); session.ShowMessage(message); },"Evita el contacto. La desactivación comienza a avanzar cuando cierras este panel.");
        }
    }
}
