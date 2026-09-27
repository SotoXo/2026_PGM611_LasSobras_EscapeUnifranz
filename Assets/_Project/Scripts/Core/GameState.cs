using System;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeUNIFRANZ.Core
{
    /// <summary>
    /// Serializable session state. It contains no scene objects or presentation logic.
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        [SerializeField] private string currentZoneId = string.Empty;
        [SerializeField] private string currentSpawnId = string.Empty;
        [SerializeField] private string currentObjectiveId = string.Empty;
        [SerializeField] private int currentObjectiveProgress;
        [SerializeField] private int currentObjectiveTotal;
        [SerializeField] private string checkpointZoneId = string.Empty;
        [SerializeField] private string checkpointSpawnId = string.Empty;
        [SerializeField] private List<GameFlagId> completedFlags = new List<GameFlagId>();
        [SerializeField] private List<string> discoveredWorldItems = new List<string>();

        public string CurrentZoneId => currentZoneId;
        public string CurrentSpawnId => currentSpawnId;
        public string CurrentObjectiveId => currentObjectiveId;
        public int CurrentObjectiveProgress => currentObjectiveProgress;
        public int CurrentObjectiveTotal => currentObjectiveTotal;
        public string CheckpointZoneId => checkpointZoneId;
        public string CheckpointSpawnId => checkpointSpawnId;
        public bool HasCheckpoint =>
            !string.IsNullOrWhiteSpace(checkpointZoneId) &&
            !string.IsNullOrWhiteSpace(checkpointSpawnId);
        public IReadOnlyList<GameFlagId> CompletedFlags => completedFlags;
        public IReadOnlyList<string> DiscoveredWorldItems => discoveredWorldItems;
        public event Action<GameFlagId> FlagChanged;
        public event Action LocationChanged;
        public event Action ObjectiveChanged;
        public event Action<string, string> CheckpointChanged;
        public event Action MapKnowledgeChanged;

        public bool HasFlag(GameFlagId flag)
        {
            return flag != GameFlagId.None && completedFlags.Contains(flag);
        }

        public bool SetFlag(GameFlagId flag)
        {
            if (flag == GameFlagId.None || completedFlags.Contains(flag))
            {
                return false;
            }

            completedFlags.Add(flag);
            FlagChanged?.Invoke(flag);
            return true;
        }

        public void SetLocation(string zoneId, string spawnId)
        {
            string nextZoneId = zoneId ?? string.Empty;
            string nextSpawnId = spawnId ?? string.Empty;
            if (currentZoneId == nextZoneId && currentSpawnId == nextSpawnId)
            {
                return;
            }

            currentZoneId = nextZoneId;
            currentSpawnId = nextSpawnId;
            LocationChanged?.Invoke();
        }

        public bool SetCurrentObjective(string objectiveId, int progress = 0, int total = 0)
        {
            string nextId = objectiveId ?? string.Empty;
            int nextTotal = Mathf.Max(0, total);
            int nextProgress = nextTotal > 0
                ? Mathf.Clamp(progress, 0, nextTotal)
                : 0;

            if (currentObjectiveId == nextId &&
                currentObjectiveProgress == nextProgress &&
                currentObjectiveTotal == nextTotal)
            {
                return false;
            }

            currentObjectiveId = nextId;
            currentObjectiveProgress = nextProgress;
            currentObjectiveTotal = nextTotal;
            ObjectiveChanged?.Invoke();
            return true;
        }

        public bool SetObjectiveProgress(int progress, int total)
        {
            return SetCurrentObjective(currentObjectiveId, progress, total);
        }

        public bool SetCheckpoint(string zoneId, string spawnId)
        {
            if (string.IsNullOrWhiteSpace(zoneId) || string.IsNullOrWhiteSpace(spawnId))
            {
                return false;
            }

            if (checkpointZoneId == zoneId && checkpointSpawnId == spawnId)
            {
                return false;
            }

            checkpointZoneId = zoneId;
            checkpointSpawnId = spawnId;
            CheckpointChanged?.Invoke(checkpointZoneId, checkpointSpawnId);
            return true;
        }

        public bool DiscoverWorldItem(string zoneId, string itemId)
        {
            string key = CreateWorldItemKey(zoneId, itemId);
            if (string.IsNullOrEmpty(key) || discoveredWorldItems.Contains(key))
            {
                return false;
            }

            discoveredWorldItems.Add(key);
            MapKnowledgeChanged?.Invoke();
            return true;
        }

        public bool IsWorldItemDiscovered(string zoneId, string itemId)
        {
            string key = CreateWorldItemKey(zoneId, itemId);
            return !string.IsNullOrEmpty(key) && discoveredWorldItems.Contains(key);
        }

        private static string CreateWorldItemKey(string zoneId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(zoneId) || string.IsNullOrWhiteSpace(itemId))
            {
                return string.Empty;
            }

            return zoneId.Trim() + ":" + itemId.Trim();
        }
    }
}
