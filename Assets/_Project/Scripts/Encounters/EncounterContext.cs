using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Encounters
{
    /// <summary>
    /// Owns only the local reset boundary and checkpoint of one encounter.
    /// </summary>
    public sealed class EncounterContext : MonoBehaviour, IZoneRuntimeInitializable
    {
        [SerializeField] private string checkpointSpawnId;

        private readonly List<IEncounterResettable> resettableObjects =
            new List<IEncounterResettable>();
        private ZoneRuntimeContext runtime;
        private bool initialized;

        public string CheckpointSpawnId => checkpointSpawnId;
        public bool IsActive { get; private set; }
        public bool IsCompleted { get; private set; }

        public void Initialize(ZoneRuntimeContext context)
        {
            runtime = context;
            initialized = true;
            CacheResettableObjects();
        }

        public void BeginEncounter()
        {
            if (!initialized || IsCompleted)
            {
                return;
            }

            IsActive = true;
            runtime.Session.State.SetCheckpoint(runtime.Zone.ZoneId, checkpointSpawnId);
            runtime.GameplayMode.SetMode(GameplayMode.Encounter);
            Debug.Log($"Encounter started: {name}", this);
        }

        public bool RequestRespawn(string reason = "Contacto con una amenaza activa.")
        {
            if (initialized && !IsCompleted)
            {
                return runtime.Session.RequestDefeat(this, reason);
            }

            return false;
        }

        public void ResetLocalState()
        {
            for (int index = 0; index < resettableObjects.Count; index++)
            {
                resettableObjects[index].ResetEncounter();
            }

            IsActive = false;
            Debug.Log($"Encounter reset: {name}", this);
        }

        public void CompleteEncounter()
        {
            if (IsCompleted)
            {
                return;
            }

            IsCompleted = true;
            IsActive = false;
            runtime.GameplayMode.SetMode(GameplayMode.Explore);
            Debug.Log($"Encounter completed: {name}", this);
        }

        private void CacheResettableObjects()
        {
            resettableObjects.Clear();
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IEncounterResettable resettable &&
                    !ReferenceEquals(resettable, this))
                {
                    resettableObjects.Add(resettable);
                }
            }
        }
    }
}
