using System;
using EscapeUNIFRANZ.Core;
using UnityEngine;
namespace EscapeUNIFRANZ.Puzzles
{
    public enum PuzzleKind { Cables, CommandSequence, ServiceSelection, FuseReplacement }
    [Serializable] public class CablePort { public string id, label; public bool output, blocked; public int row; public Sprite icon; }
    [Serializable] public class CableConnection
    {
        public string from, to;
        public CableConnection(string source, string destination) { from=source; to=destination; }
    }
    [CreateAssetMenu(menuName="Escape UNIFRANZ/Puzzles/Definition")]
    public sealed class PuzzleData : ScriptableObject
    {
        public string id, title;
        public string objective;
        [TextArea] public string inspectionText;
        public EscapeUNIFRANZ.Narrative.ClueData environmentalClue;
        public bool IsConnectionPuzzle => kind==PuzzleKind.Cables || kind==PuzzleKind.FuseReplacement;
        [TextArea] public string instructions;
        public PuzzleKind kind;
        public CablePort[] ports;
        public CableConnection[] connections;
        public CableConnection[] diagramConnections;
        public string[] commandOrder;
        public bool connectInOrder = true;
        public bool matchingColumns, preserveCommandProgress;
        public string requiredClueId, requiredItem;
        public string[] prerequisiteClues;
        public bool consumeItem;
        public bool installBeforePuzzle;
        public GameFlagId installedFlag;
        public GameFlagId[] requiredFlags, completionFlags;
        public string[] rewardItems;
        public string successMessage;
    }
}
