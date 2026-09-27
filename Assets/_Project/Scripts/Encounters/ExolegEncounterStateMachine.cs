namespace EscapeUNIFRANZ.Encounters
{
    public enum ExolegEncounterState
    {
        Dormant = 0,
        Warning = 1,
        Active = 2,
        Overloaded = 3,
        Disabled = 4
    }

    public sealed class ExolegEncounterStateMachine
    {
        private readonly int requiredPoints;

        public ExolegEncounterStateMachine(int requiredPoints = 3)
        {
            this.requiredPoints = requiredPoints < 1 ? 1 : requiredPoints;
        }

        public ExolegEncounterState Current { get; private set; } = ExolegEncounterState.Dormant;
        public int ActivatedPoints { get; private set; }

        public bool BeginWarning()
        {
            if (Current != ExolegEncounterState.Dormant)
            {
                return false;
            }

            Current = ExolegEncounterState.Warning;
            return true;
        }

        public bool BeginActive()
        {
            if (Current != ExolegEncounterState.Warning)
            {
                return false;
            }

            Current = ExolegEncounterState.Active;
            return true;
        }

        public bool ActivateOverloadPoint()
        {
            if (Current != ExolegEncounterState.Active)
            {
                return false;
            }

            ActivatedPoints++;
            if (ActivatedPoints >= requiredPoints)
            {
                Current = ExolegEncounterState.Overloaded;
            }

            return true;
        }

        public bool Disable()
        {
            if (Current != ExolegEncounterState.Overloaded)
            {
                return false;
            }

            Current = ExolegEncounterState.Disabled;
            return true;
        }

        public void RestoreDisabled()
        {
            Current = ExolegEncounterState.Disabled;
            ActivatedPoints = requiredPoints;
        }

        public bool Reset()
        {
            if (Current == ExolegEncounterState.Disabled)
            {
                return false;
            }

            Current = ExolegEncounterState.Dormant;
            ActivatedPoints = 0;
            return true;
        }
    }
}
