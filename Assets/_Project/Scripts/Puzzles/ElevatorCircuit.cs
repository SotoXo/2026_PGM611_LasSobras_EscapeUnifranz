namespace EscapeUNIFRANZ.Puzzles
{
    public sealed class ElevatorCircuit
    {
        public const string ItemId = "elevator_circuit_piece";
        public const string PuzzleId = "elevator_circuit";
        public const int Missing = 6;
        public const int Count = 16;

        // Each tile has one conductor, except the curved crossings which have two.
        private const int North = 1, East = 2, South = 4, West = 8;
        private static readonly int[] Conductors =
        {
            South | West, East | West, North | East, East | South,
            East | West, North | West, 15, North | East,
            North | West, South | West, North | South, East | West,
            East | West, East | South, North | West, North | South
        };
        private readonly int[] rotations = new int[Count];
        public bool Installed { get; private set; }
        public int Rotation(int index) => rotations[index];
        public void Install() => Installed = true;
        public void Rotate(int index) => rotations[index] = (rotations[index] + 1) % 4;

        public bool IsPowered()
        {
            if (!Installed) return false;
            var visited = new bool[Count * 4];
            return Trace(0, West, visited);
        }

        private bool Trace(int tile, int incoming, bool[] visited)
        {
            int direction = incoming == North ? 0 : incoming == East ? 1 : incoming == South ? 2 : 3;
            int visit = tile * 4 + direction;
            if (visited[visit]) return false;
            visited[visit] = true;
            int turn = rotations[tile];
            int conductor = Turn(Conductors[tile], turn);
            if ((conductor & incoming) == 0)
            {
                if (tile != 2 && tile != 8) return false;
                conductor ^= 15;
            }
            int outgoing = conductor & ~incoming;
            if (tile == 15 && (outgoing & East) != 0) return true;
            return ((outgoing & North) != 0 && tile >= 4 && Trace(tile - 4, South, visited))
                || ((outgoing & East) != 0 && tile % 4 != 3 && Trace(tile + 1, West, visited))
                || ((outgoing & South) != 0 && tile < 12 && Trace(tile + 4, North, visited))
                || ((outgoing & West) != 0 && tile % 4 != 0 && Trace(tile - 1, East, visited));
        }

        private static int Turn(int mask, int count) => ((mask << count) | (mask >> (4 - count))) & 15;
    }
}
