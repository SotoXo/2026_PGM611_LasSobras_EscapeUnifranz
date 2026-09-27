using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId;

        public string SpawnId => spawnId;
        public Vector2 Position => transform.position;
    }
}
