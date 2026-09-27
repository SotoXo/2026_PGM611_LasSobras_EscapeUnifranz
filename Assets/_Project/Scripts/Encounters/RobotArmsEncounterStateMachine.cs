namespace EscapeUNIFRANZ.Encounters
{
    public enum RobotArmsEncounterState
    {
        Idle = 0,
        Sequence = 1,
        Vulnerable = 2,
        Disabled = 3
    }

    public sealed class RobotArmsEncounterStateMachine
    {
        public RobotArmsEncounterState Current { get; private set; } = RobotArmsEncounterState.Idle;

        public bool BeginSequence()
        {
            if (Current != RobotArmsEncounterState.Idle && Current != RobotArmsEncounterState.Vulnerable)
            {
                return false;
            }

            Current = RobotArmsEncounterState.Sequence;
            return true;
        }

        public bool OpenVulnerability()
        {
            if (Current != RobotArmsEncounterState.Sequence)
            {
                return false;
            }

            Current = RobotArmsEncounterState.Vulnerable;
            return true;
        }

        public bool Disable()
        {
            if (Current != RobotArmsEncounterState.Vulnerable)
            {
                return false;
            }

            Current = RobotArmsEncounterState.Disabled;
            return true;
        }

        public void RestoreDisabled()
        {
            Current = RobotArmsEncounterState.Disabled;
        }

        public bool Reset()
        {
            if (Current == RobotArmsEncounterState.Disabled)
            {
                return false;
            }

            Current = RobotArmsEncounterState.Idle;
            return true;
        }
    }
}
