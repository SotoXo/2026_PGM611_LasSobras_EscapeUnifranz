using System.Collections.Generic;

namespace EscapeUNIFRANZ.Encounters
{
    public enum SequenceSubmissionResult
    {
        Correct = 0,
        Completed = 1,
        Incorrect = 2,
        AlreadyCompleted = 3
    }

    /// <summary>
    /// Pure ordered sequence used by Francis phases without owning presentation or GameState.
    /// </summary>
    public sealed class OrderedStepSequence<T>
    {
        private readonly IReadOnlyList<T> expected;
        private readonly IEqualityComparer<T> comparer;

        public OrderedStepSequence(IReadOnlyList<T> expected)
        {
            this.expected = expected;
            comparer = EqualityComparer<T>.Default;
        }

        public int Progress { get; private set; }
        public bool IsCompleted => expected != null && expected.Count > 0 && Progress >= expected.Count;

        public SequenceSubmissionResult Submit(T step, bool resetOnFailure)
        {
            if (IsCompleted)
            {
                return SequenceSubmissionResult.AlreadyCompleted;
            }

            if (expected == null || expected.Count == 0 ||
                !comparer.Equals(expected[Progress], step))
            {
                if (resetOnFailure)
                {
                    Progress = 0;
                }

                return SequenceSubmissionResult.Incorrect;
            }

            Progress++;
            return IsCompleted
                ? SequenceSubmissionResult.Completed
                : SequenceSubmissionResult.Correct;
        }

        public void Reset()
        {
            Progress = 0;
        }

        public void Complete()
        {
            Progress = expected?.Count ?? 0;
        }
    }
}
