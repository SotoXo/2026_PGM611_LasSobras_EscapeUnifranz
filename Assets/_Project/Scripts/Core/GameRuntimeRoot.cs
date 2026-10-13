using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeUNIFRANZ.Core
{
    /// <summary>
    /// Keeps exactly one runtime hierarchy alive across Single scene loads.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameRuntimeRoot : MonoBehaviour
    {
        private static GameRuntimeRoot instance;
        private static bool teardownInProgress;

        public static GameRuntimeRoot Instance => instance;
        public static bool TeardownInProgress => teardownInProgress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instance = null;
            teardownInProgress = false;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            teardownInProgress = false;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Disables the complete persistent gameplay hierarchy, destroys it at the
        /// end of the frame, and only then loads the standalone menu scene.
        /// </summary>
        public bool TeardownAndLoadScene(string scenePath)
        {
            if (teardownInProgress || string.IsNullOrWhiteSpace(scenePath))
            {
                return false;
            }

            teardownInProgress = true;
            Time.timeScale = 1f;

            GameObject loaderObject = new GameObject("RuntimeTeardownLoader");
            DontDestroyOnLoad(loaderObject);
            RuntimeTeardownLoader loader = loaderObject.AddComponent<RuntimeTeardownLoader>();

            gameObject.SetActive(false);
            loader.Begin(gameObject, scenePath);
            return true;
        }
    }

    /// <summary>
    /// Short-lived owner for the teardown coroutine. It remains alive after the
    /// gameplay root is disabled and removes itself once the menu is loaded.
    /// </summary>
    internal sealed class RuntimeTeardownLoader : MonoBehaviour
    {
        public void Begin(GameObject runtimeRoot, string scenePath)
        {
            StartCoroutine(TeardownRoutine(runtimeRoot, scenePath));
        }

        private IEnumerator TeardownRoutine(GameObject runtimeRoot, string scenePath)
        {
            Destroy(runtimeRoot);
            yield return null;

            AsyncOperation load = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            if (load != null)
            {
                while (!load.isDone)
                {
                    yield return null;
                }
            }

            Destroy(gameObject);
        }
    }
}
