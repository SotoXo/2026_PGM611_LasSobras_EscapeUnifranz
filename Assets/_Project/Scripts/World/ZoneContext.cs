using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.UI;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    /// <summary>
    /// Declares one zone and initializes its local objects from the persistent session.
    /// </summary>
    public sealed class ZoneContext : MonoBehaviour
    {
        [SerializeField] private string zoneId;
        [SerializeField] private string displayName;
        [SerializeField] private string defaultCheckpointSpawnId;
        [SerializeField] private string objectiveId;
        [SerializeField, TextArea] private string objectiveText;
        [SerializeField, Min(0)] private int objectiveTotal;
        [SerializeField] private Vector2 minimapCenter = Vector2.zero;
        [SerializeField] private Vector2 minimapSize = new Vector2(16f, 9f);

        private SpawnPoint[] spawnPoints;
        private readonly List<string> spawnIds = new List<string>();

        public string ZoneId => zoneId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? zoneId : displayName;
        public string DefaultCheckpointSpawnId => defaultCheckpointSpawnId;
        public Rect MinimapBounds => new Rect(minimapCenter - minimapSize * 0.5f, minimapSize);

        public bool Initialize(
            GameSessionController session,
            SceneFlowController sceneFlow,
            ObjectiveController objectiveController,
            out string error)
        {
            if (session == null || sceneFlow == null || string.IsNullOrWhiteSpace(zoneId))
            {
                error = $"ZoneContext '{name}' is missing its runtime or ZoneId.";
                return false;
            }

            CacheSpawns();
            if (spawnPoints.Length == 0)
            {
                error = $"Zone '{zoneId}' has no SpawnPoint components.";
                return false;
            }

            ZoneExitInteractable[] exits = GetComponentsInChildren<ZoneExitInteractable>(true);
            for (int index = 0; index < exits.Length; index++)
            {
                exits[index].Bind(sceneFlow, session);
            }

            ZoneRuntimeContext runtime = new ZoneRuntimeContext(
                session,
                sceneFlow,
                sceneFlow.ModeController,
                this);
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IZoneRuntimeInitializable initializable)
                {
                    initializable.Initialize(runtime);
                }
            }

            GameFlagId visitedFlag = ResolveVisitedFlag(zoneId);

            if (session.State.SetFlag(visitedFlag))
            {
                Debug.Log($"Flag set: {visitedFlag}", this);
            }

            objectiveController?.SetCurrent(objectiveId, objectiveText, 0, objectiveTotal);
            session.BindZonePresentation(this);
            error = null;
            return true;
        }

        public static GameFlagId ResolveVisitedFlag(string id)
        {
            switch (id)
            {
                case "entrada": return GameFlagId.EntranceVisited;
                case "hall": return GameFlagId.HallVisited;
                case "arca": return GameFlagId.ArcaVisited;
                case "piso2": return GameFlagId.Piso2Visited;
                case "piso3": return GameFlagId.Piso3Visited;
                case "lab_software": return GameFlagId.LabSoftwareVisited;
                case "nucleo_ia": return GameFlagId.NucleoVisited;
                default: return GameFlagId.None;
            }
        }

        public bool TryGetSpawn(string spawnId, out SpawnPoint spawnPoint, out string error)
        {
            CacheSpawns();
            if (SpawnIdResolver.TryResolveIndex(spawnIds, spawnId, out int index, out error))
            {
                spawnPoint = spawnPoints[index];
                return true;
            }

            spawnPoint = null;
            error = $"Zone '{zoneId}': {error}";
            return false;
        }

        private void CacheSpawns()
        {
            spawnPoints = GetComponentsInChildren<SpawnPoint>(true);
            spawnIds.Clear();

            for (int index = 0; index < spawnPoints.Length; index++)
            {
                spawnIds.Add(spawnPoints[index].SpawnId);
            }
        }
    }
}
