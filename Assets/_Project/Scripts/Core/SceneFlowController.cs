using System;
using System.Collections;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeUNIFRANZ.Core
{
    /// <summary>
    /// Runs one guarded Single-scene transition and initializes the destination zone.
    /// </summary>
    public sealed class SceneFlowController : MonoBehaviour
    {
        [SerializeField] private ZoneCatalog zoneCatalog;
        [SerializeField] private GameSessionController session;
        [SerializeField] private GameplayModeController gameplayMode;
        [SerializeField] private ObjectiveController objectiveController;
        [SerializeField] private PlayerMovement2D playerMovement;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private FadeView fadeView;

        public bool IsTransitioning { get; private set; }
        public GameplayModeController ModeController => gameplayMode;
        public event Action<bool> ZoneTransitionFinished;
        public event Action<bool> RespawnFinished;

        public bool GoToZone(string zoneId, string spawnId)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning("A zone transition is already active.", this);
                return false;
            }

            string scenePath = null;
            string error = "ZoneCatalog is not assigned.";
            if (zoneCatalog == null ||
                !zoneCatalog.TryResolveScene(zoneId, out scenePath, out error))
            {
                Debug.LogError($"Cannot load zone '{zoneId}': {error}", this);
                return false;
            }

            if (string.IsNullOrWhiteSpace(spawnId))
            {
                Debug.LogError($"Cannot load zone '{zoneId}' with an empty SpawnId.", this);
                return false;
            }

            StartCoroutine(TransitionRoutine(zoneId, spawnId, scenePath));
            return true;
        }

        public bool RequestRespawn(EncounterContext encounter = null)
        {
            if (IsTransitioning || session?.State == null ||
                !session.State.HasCheckpoint)
            {
                return false;
            }

            if (encounter == null)
            {
                EncounterContext[] encounters = UnityEngine.Object.FindObjectsByType<EncounterContext>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                encounter = ResolveCheckpointEncounter(
                    encounters,
                    session.State.CheckpointSpawnId);
            }

            StartCoroutine(RespawnRoutine(encounter));
            return true;
        }

        public static EncounterContext ResolveCheckpointEncounter(
            EncounterContext[] encounters,
            string checkpointSpawnId)
        {
            if (encounters == null || string.IsNullOrWhiteSpace(checkpointSpawnId))
            {
                return null;
            }

            EncounterContext checkpointFallback = null;
            EncounterContext activeFallback = null;
            EncounterContext soleIncompleteFallback = null;
            int incompleteCount = 0;
            for (int index = 0; index < encounters.Length; index++)
            {
                EncounterContext candidate = encounters[index];
                if (candidate == null || candidate.IsCompleted)
                {
                    continue;
                }

                incompleteCount++;
                soleIncompleteFallback = incompleteCount == 1 ? candidate : null;

                if (candidate.IsActive && activeFallback == null)
                {
                    activeFallback = candidate;
                }

                if (!string.Equals(
                    candidate.CheckpointSpawnId,
                    checkpointSpawnId,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                if (candidate.IsActive)
                {
                    return candidate;
                }

                checkpointFallback ??= candidate;
            }

            return checkpointFallback ?? activeFallback ?? soleIncompleteFallback;
        }

        private IEnumerator RespawnRoutine(EncounterContext encounter)
        {
            bool succeeded = false;
            IsTransitioning = true;
            gameplayMode.SetMode(GameplayMode.Transition);
            playerMovement.SetMovementEnabled(false);
            playerBody.linearVelocity = Vector2.zero;
            yield return fadeView.FadeOut();

            if (!TryFindSingleZoneContext(out ZoneContext zoneContext, out string contextError))
            {
                Debug.LogError(contextError, this);
            }
            else if (zoneContext.ZoneId != session.State.CheckpointZoneId)
            {
                Debug.LogError("Checkpoint belongs to a different zone.", this);
            }
            else if (!zoneContext.TryGetSpawn(
                session.State.CheckpointSpawnId,
                out SpawnPoint spawnPoint,
                out string spawnError))
            {
                Debug.LogError(spawnError, this);
            }
            else
            {
                playerBody.position = spawnPoint.Position;
                playerBody.linearVelocity = Vector2.zero;
                encounter?.ResetLocalState();
                succeeded = true;
            }

            yield return fadeView.FadeIn();
            gameplayMode.SetMode(GameplayMode.Explore);
            IsTransitioning = false;
            RespawnFinished?.Invoke(succeeded);
        }

        private IEnumerator TransitionRoutine(string zoneId, string spawnId, string scenePath)
        {
            IsTransitioning = true;
            gameplayMode.SetMode(GameplayMode.Transition);
            playerMovement.SetMovementEnabled(false);
            playerBody.linearVelocity = Vector2.zero;

            yield return fadeView.FadeOut();

            AsyncOperation loadOperation = BeginSceneLoad(scenePath);
            if (loadOperation == null)
            {
                Debug.LogError($"Unity could not begin loading scene '{scenePath}'.", this);
                yield return fadeView.FadeIn();
                gameplayMode.SetMode(GameplayMode.Explore);
                IsTransitioning = false;
                ZoneTransitionFinished?.Invoke(false);
                yield break;
            }

            yield return loadOperation;

            if (!TryFindSingleZoneContext(out ZoneContext zoneContext, out string contextError))
            {
                Debug.LogError(contextError, this);
                yield return fadeView.FadeIn();
                gameplayMode.SetMode(GameplayMode.Explore);
                IsTransitioning = false;
                ZoneTransitionFinished?.Invoke(false);
                yield break;
            }

            // Rebind after an Editor scene load as managed references can be reconstructed.
            objectiveController.Bind(session.State);

            if (!zoneContext.Initialize(session, this, objectiveController, out string initError))
            {
                Debug.LogError(initError, zoneContext);
                yield return fadeView.FadeIn();
                gameplayMode.SetMode(GameplayMode.Explore);
                IsTransitioning = false;
                ZoneTransitionFinished?.Invoke(false);
                yield break;
            }

            if (!zoneContext.TryGetSpawn(spawnId, out SpawnPoint spawnPoint, out string spawnError))
            {
                Debug.LogError(spawnError, zoneContext);
                yield return fadeView.FadeIn();
                gameplayMode.SetMode(GameplayMode.Explore);
                IsTransitioning = false;
                ZoneTransitionFinished?.Invoke(false);
                yield break;
            }

            playerBody.position = spawnPoint.Position;
            playerBody.linearVelocity = Vector2.zero;
            session.State.SetLocation(zoneId, spawnId);
            string checkpointId = string.IsNullOrWhiteSpace(zoneContext.DefaultCheckpointSpawnId)
                ? spawnId
                : zoneContext.DefaultCheckpointSpawnId;
            session.State.SetCheckpoint(zoneId, checkpointId);

            Debug.Log($"Loaded zone: {zoneId} | Spawn: {spawnId}", zoneContext);

            yield return fadeView.FadeIn();

            gameplayMode.SetMode(GameplayMode.Explore);
            IsTransitioning = false;
            ZoneTransitionFinished?.Invoke(true);
        }

        private static AsyncOperation BeginSceneLoad(string scenePath)
        {
#if UNITY_EDITOR
            if (!Application.CanStreamedLevelBeLoaded(scenePath))
            {
                return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    scenePath,
                    new LoadSceneParameters(LoadSceneMode.Single));
            }
#endif
            return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
        }

        private static bool TryFindSingleZoneContext(
            out ZoneContext zoneContext,
            out string error)
        {
            ZoneContext[] contexts = UnityEngine.Object.FindObjectsByType<ZoneContext>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (contexts.Length != 1)
            {
                zoneContext = null;
                error = $"The loaded zone must contain exactly one ZoneContext; found {contexts.Length}.";
                return false;
            }

            zoneContext = contexts[0];
            error = null;
            return true;
        }
    }
}
