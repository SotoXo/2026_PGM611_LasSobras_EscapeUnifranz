using System.Collections.Generic;

namespace EscapeUNIFRANZ.World
{
    public static class SpawnIdResolver
    {
        public static bool TryResolveIndex(
            IReadOnlyList<string> spawnIds,
            string requestedId,
            out int index,
            out string error)
        {
            index = -1;

            if (spawnIds == null || string.IsNullOrWhiteSpace(requestedId))
            {
                error = "Spawn list or requested SpawnId is invalid.";
                return false;
            }

            for (int candidateIndex = 0; candidateIndex < spawnIds.Count; candidateIndex++)
            {
                if (spawnIds[candidateIndex] == requestedId)
                {
                    index = candidateIndex;
                    error = null;
                    return true;
                }
            }

            error = $"SpawnId '{requestedId}' was not found.";
            return false;
        }
    }
}
