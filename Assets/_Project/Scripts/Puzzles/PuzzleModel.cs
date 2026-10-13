using System;
using System.Collections.Generic;
using System.Linq;
namespace EscapeUNIFRANZ.Puzzles
{
    public enum PuzzleSubmission { Rejected, Accepted, Completed }
    /// <summary>Only validates data and scratch progress; never changes session flags.</summary>
    public sealed class PuzzleModel
    {
        private readonly PuzzleData data;
        private readonly List<CableConnection> connected = new List<CableConnection>();
        private int commandIndex;
        public IReadOnlyList<CableConnection> Connected => connected;
        public string[] CompletedCommands => data.IsConnectionPuzzle ? new string[0] : data.commandOrder.Take(commandIndex).ToArray();
        public int Progress => data.IsConnectionPuzzle ? connected.Count : commandIndex;
        public int Total => data.IsConnectionPuzzle ? data.connections.Length : data.commandOrder.Length;
        public bool IsComplete => Progress == Total;
        public PuzzleModel(PuzzleData definition)
        {
            if (!Validate(definition,out string error)) throw new ArgumentException(error);
            data=definition;
        }
        public static bool Validate(PuzzleData definition,out string error)
        {
            error=null;
            if (definition == null || definition.ports == null || definition.ports.Length < 2)
                error="Puzzle requires named ports or commands.";
            else if (definition.ports.Any(p => p == null || string.IsNullOrWhiteSpace(p.id)) ||
                definition.ports.Select(p=>p.id).Distinct().Count() != definition.ports.Length)
                error="Ports must have unique nonempty IDs.";
            else
            {
                var ids=new HashSet<string>(definition.ports.Select(p=>p.id));
                if (definition.IsConnectionPuzzle)
                {
                    if (definition.connections==null || definition.connections.Length==0 ||
                        definition.connections.Any(c=>c==null || c.from==c.to || !ids.Contains(c.from) || !ids.Contains(c.to)) ||
                        definition.connections.Select(c=>c.from+"\n"+c.to).Distinct().Count()!=definition.connections.Length)
                        error="Cable solution must contain distinct valid directed connections.";
                }
                else if (definition.commandOrder==null || definition.commandOrder.Length==0 ||
                    definition.commandOrder.Any(c=>!ids.Contains(c))) error="Command solution must reference declared options.";
            }
            return error==null;
        }
        public PuzzleSubmission Connect(string from,string to)
        {
            if (!data.IsConnectionPuzzle || IsComplete || from==to || connected.Any(c=>c.from==from&&c.to==to))
                return PuzzleSubmission.Rejected;
            int index=Array.FindIndex(data.connections,c=>c.from==from&&c.to==to);
            if (index<0 || (data.connectInOrder && index!=connected.Count)) return PuzzleSubmission.Rejected;
            connected.Add(new CableConnection(from,to));
            return IsComplete ? PuzzleSubmission.Completed : PuzzleSubmission.Accepted;
        }
        public PuzzleSubmission Command(string id)
        {
            if (data.IsConnectionPuzzle || IsComplete) return PuzzleSubmission.Rejected;
            if (data.commandOrder[commandIndex]!=id) { if (!data.preserveCommandProgress) commandIndex=0; return PuzzleSubmission.Rejected; }
            commandIndex++;
            return IsComplete ? PuzzleSubmission.Completed : PuzzleSubmission.Accepted;
        }
        public void Reset() { connected.Clear(); commandIndex=0; }
    }
}
