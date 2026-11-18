using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Puzzles;
using UnityEngine;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed partial class NarrativeController
    {
        [SerializeField] private ClueData[] journalCatalog;
        private int journalPage;
        public void BeginCable(string id)
        {
            if (model==null || activePuzzle==null || !activePuzzle.puzzle.IsConnectionPuzzle) return;
            selectedPort=id; view.SelectPort(id);
        }
        public void EndCable(string id)
        {
            if (selectedPort!=null && id!=null && selectedPort!=id) SelectPort(id);
            else { selectedPort=null; view.SelectPort(null); }
        }
        public void ShowJournal()
        { if (!Open(GameplayMode.Dialogue)) return; session.GetComponent<EscapeUNIFRANZ.UI.EntranceTutorialController>()?.Complete(EscapeUNIFRANZ.UI.TutorialAction.Journal); journalPage=0; JournalPage(); }
        private void JournalPage()
        {
            var found=journalCatalog.Where(c=>c!=null && state.IsWorldItemDiscovered("clue",c.id)).ToArray();
            journalPage=Mathf.Clamp(journalPage,0,Mathf.Max(0,(found.Length-1)/6));
            var current=found.Skip(journalPage*6).Take(6).ToArray();
            var labels = current.Select(c => c.title).Concat(new[] { "ANTERIOR", "SIGUIENTE", "AYUDA DEL OBJETIVO" }).ToArray();
            view.Options("REGISTRO DE PISTAS",found.Length==0 ? "Todavía no has encontrado documentos. Examina notas y terminales." :
                "Solo se guardan documentos leídos. Página "+(journalPage+1)+" • "+found.Length+" registros.",labels,index=>
            {
                if (index<current.Length) { view.ShowJournalDocument(current[index]); journalReading=true; }
                else if (index==current.Length) { journalPage--; JournalPage(); }
                else if (index==current.Length+1) { journalPage++; JournalPage(); }
                else if (index==current.Length+2) ShowObjectiveHelp(false);
            });
            journalReading=false;
        }
        private void ShowObjectiveHelp(bool detail)
        {
            var hints = state.CurrentObjectiveId switch
            {
                "entrada_bateria" => ("Alguien dejó un repuesto en la sala dental.", "Desde recepción, toma el corredor hacia la hilera de sillones azules y busca el repuesto entre los equipos."),
                "entrada_cctv" or "entrada_codigo" => ("La cámara que apunta al Hall registró una placa.", "Vuelve al terminal CCTV, abre el feed del Hall y lleva el número que ves al lector de la puerta."),
                "hall_llave" => ("El personal de admisión dejó un aviso.", "Busca las mesas anaranjadas de la cafetería. La llave de ARCA está en ese sector."),
                "hall_gabinete" or "sim_salida" => ("El gabinete de comunicaciones está en un sector anterior.", "Vuelve al Hall con la tarjeta de mantenimiento de Simulación. Lee el documento junto al gabinete."),
                "sim_exoleg" => ("El Exoleg avisa antes de activar el área de peligro.", "Actúa cuando el halo diga EN ESPERA. Desactiva sus dos actuadores antes del punto CONTROL."),
                "tec_red" => ("La ruta marcada como comprometida aún lleva órdenes de FRANCIS.", "Examina la pizarra y el rack. Pasa por el switch que mantiene el enlace directo al laboratorio."),
                "lab_stop" => ("La orden del proceso abrió una ventana en los actuadores.", "Busca la etiqueta EMERGENCY STOP y espera el estado EN ESPERA antes de cruzar."),
                "core_escape" => ("La salida está en el camino por el que entraste.", "Vuelve al acceso del laboratorio por el corredor. Usa la acción Salir por la vía de emergencia."),
                _ => ("Consulta los documentos del sector y examina el dispositivo.", "Acércate al control y usa el escáner. Lee las etiquetas y comprueba energía, red y autorización.")
            };
            string[] actions = detail ? new[] { "VOLVER AL DIARIO" } : new[] { "VER PISTA ADICIONAL", "VOLVER AL DIARIO" };
            view.Options("AYUDA DEL OBJETIVO", detail ? hints.Item2 : hints.Item1, actions, index =>
            {
                if (!detail && index == 0) ShowObjectiveHelp(true);
                else JournalPage();
            });
            journalReading = true;
        }

        private bool journalReading;
        private void LateUpdate()
        { if (modalOpen && journalReading && UnityEngine.InputSystem.Keyboard.current?.jKey.wasPressedThisFrame==true) JournalPage(); }
        public void ShowSecurity(SecurityTerminalInteractable terminal)
        { if (Open(GameplayMode.Puzzle)) SecurityPage(terminal,0); }
        private void SecurityPage(SecurityTerminalInteractable terminal,int feedIndex)
        {
            terminal.Read(feedIndex); var feed=terminal.feeds[feedIndex];
            string status=feed.noSignal ? "NO SIGNAL" : feed.camera!=null ? feed.camera.Status : "ONLINE / ENLACE REMOTO";
            string body=feed.name+" — "+status+"\n"+feed.schematic+(terminal.observedUnit!=null ? "\nUNIDAD LOCAL: "+terminal.observedUnit.ReadableState : "");
            var operationCodes=new System.Collections.Generic.List<int>();
            if (feed.camera!=null && !feed.noSignal) operationCodes.AddRange(new[]{0,1,2});
            if (terminal.vigilanceUnit!=null) operationCodes.Add(3);
            var captions=new[]{"GIRAR CÁMARA / 12 s","PAUSAR CÁMARA / 15 s","REINICIAR ENLACE","AISLAR VIGILANCIA / 10 s"};
            var labels=terminal.feeds.Select(f=>f.name).Concat(operationCodes.Select(c=>captions[c])).ToArray();
            view.Options("CCTV — ESQUEMA DE SEGURIDAD",body,labels,index=>
            {
                if (index<terminal.feeds.Length) { SecurityPage(terminal,index); return; }
                int operation=operationCodes[index-terminal.feeds.Length];
                if (operation==0) { bool ok=terminal.Rotate(feedIndex); SecurityPage(terminal,feedIndex); view.Feedback(ok ? "Orientación desviada durante 12 segundos al cerrar el terminal." : "Rotación agotada o cámara remota sin acceso.",!ok); }
                else if (operation==1)
                { bool ok=terminal.Disable(feedIndex); SecurityPage(terminal,feedIndex); view.Feedback(ok ? "Cámara suspendida. Cierra el terminal: tienes 15 segundos para cruzar." : "Control agotado o cámara remota sin acceso. Solo una suspensión por terminal.",!ok); }
                else if (operation==2) { bool ok=terminal.ResetLink(feedIndex); SecurityPage(terminal,feedIndex); view.Feedback(ok ? "ENLACE REINICIADO. Pausa de 3 segundos y barrido original." : "ERROR: control agotado o enlace físico perdido.",!ok); }
                else { bool ok=terminal.IsolateUnit(); SecurityPage(terminal,feedIndex); view.Feedback(ok ? "Vigilancia aislada. Cierra el terminal: dispones de 10 segundos." : "Este enlace no controla una unidad de vigilancia o ya se utilizó.",!ok); }
            },"Vista esquemática • Cambia de feed • La suspensión tiene un solo uso por terminal.");
        }
    }
}
