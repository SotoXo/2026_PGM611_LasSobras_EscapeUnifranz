namespace EscapeUNIFRANZ.Hazards
{
    /// <summary>
    /// Pure deterministic waypoint index progression for loop and ping-pong routes.
    /// </summary>
    public sealed class PatrolRouteState
    {
        public int Index { get; private set; }
        public int Direction { get; private set; } = 1;

        public void Reset()
        {
            Index = 0;
            Direction = 1;
        }

        public int Advance(int pointCount, bool pingPong)
        {
            if (pointCount <= 1)
            {
                Index = 0;
                return Index;
            }

            if (!pingPong)
            {
                Index = (Index + 1) % pointCount;
                return Index;
            }

            if (Index + Direction >= pointCount || Index + Direction < 0)
            {
                Direction *= -1;
            }

            Index += Direction;
            return Index;
        }
    }
}
