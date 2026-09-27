namespace EscapeUNIFRANZ.Encounters
{
    public enum RobotDogEncounterState
    {
        Patrol = 0,
        Alert = 1,
        Search = 2,
        Disabled = 3
    }

    public sealed class RobotDogEncounterStateMachine
    {
        private readonly bool[] nodes = new bool[2];

        public RobotDogEncounterState Current { get; private set; } = RobotDogEncounterState.Patrol;
        public int DisabledNodeCount => (nodes[0] ? 1 : 0) + (nodes[1] ? 1 : 0);

        public bool Detect()
        {
            if (Current == RobotDogEncounterState.Disabled)
            {
                return false;
            }

            Current = RobotDogEncounterState.Alert;
            return true;
        }

        public bool LoseTarget()
        {
            if (Current != RobotDogEncounterState.Alert)
            {
                return false;
            }

            Current = RobotDogEncounterState.Search;
            return true;
        }

        public bool FinishSearch()
        {
            if (Current != RobotDogEncounterState.Search)
            {
                return false;
            }

            Current = RobotDogEncounterState.Patrol;
            return true;
        }

        public bool DisableNode(int index)
        {
            if (Current == RobotDogEncounterState.Disabled || index < 0 || index >= nodes.Length || nodes[index])
            {
                return false;
            }

            nodes[index] = true;
            if (nodes[0] && nodes[1])
            {
                Current = RobotDogEncounterState.Disabled;
            }

            return true;
        }

        public void RestoreDisabled()
        {
            nodes[0] = true;
            nodes[1] = true;
            Current = RobotDogEncounterState.Disabled;
        }

        public bool Reset()
        {
            if (Current == RobotDogEncounterState.Disabled)
            {
                return false;
            }

            nodes[0] = false;
            nodes[1] = false;
            Current = RobotDogEncounterState.Patrol;
            return true;
        }
    }
}
