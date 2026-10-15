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
            var labels=current.Select(c=>c.title).Concat(new[]{"ANTERIOR","SIGUIENTE"}).ToArray();
            view.Options("REGISTRO DE PISTAS",found.Length==0 ? "Todavía no has encontrado documentos. Examina notas y terminales." :
                "Solo se guardan documentos leídos. Página "+(journalPage+1)+" • "+found.Length+" registros.",labels,index=>
            {
                if (index<current.Length) { view.ShowJournalDocument(current[index]); journalReading=true; }
                else if (index==current.Length) { journalPage--; JournalPage(); }
                else if (index==current.Length+1) { journalPage++; JournalPage(); }
            });
            journalReading=false;
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
