using System.Linq;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed partial class NarrativeController
    {
        private bool reviewingPuzzleHint;
        public void ReviewPuzzleHint()
        {
            if (activePuzzle==null || model==null) return;
            var ids=new[]{activePuzzle.puzzle.requiredClueId}.Concat(activePuzzle.puzzle.prerequisiteClues ?? new string[0]);
            var found=journalCatalog.Where(c=>c!=null && ids.Contains(c.id) && state.IsWorldItemDiscovered("clue",c.id)).ToArray();
            reviewingPuzzleHint=true; selectedPort=null;
            if (found.Length>0) view.ShowFoundHint(found[0]);
            else view.Options("PISTA ENCONTRADA","Aún no has leído una pista para este panel.",new[]{"VOLVER AL PUZZLE"},_=>ReturnToPuzzle());
        }
        public void HandleClose() { if (reviewingPuzzleHint) ReturnToPuzzle(); else if (examiningItem) InventoryPage(); else if(journalReading) JournalPage(); else Close(); }
        private void ReturnToPuzzle()
        {
            if (activePuzzle==null || model==null) return;
            reviewingPuzzleHint=false; view.ShowPuzzle(activePuzzle.puzzle); view.RenderConnections(model.Connected);
            if (!activePuzzle.puzzle.IsConnectionPuzzle) view.RenderCommandProgress(model.CompletedCommands);
            view.Feedback("Progreso: "+model.Progress+" / "+model.Total,false);
        }
    }
}
