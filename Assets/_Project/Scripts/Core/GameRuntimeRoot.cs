using UnityEngine;

namespace EscapeUNIFRANZ.Core
{
    /// <summary>
    /// Keeps exactly one runtime hierarchy alive across Single scene loads.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameRuntimeRoot : MonoBehaviour
    {
        private static GameRuntimeRoot instance;

        public static GameRuntimeRoot Instance => instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instance = null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
