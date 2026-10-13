using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    /// <summary>Use the object's feet as the shared depth reference for props and actors.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class WorldYSort2D : MonoBehaviour
    {
        [SerializeField] private Transform feet;
        private SpriteRenderer spriteRenderer;
        public static int Order(float y) => Mathf.Clamp(Mathf.RoundToInt(-y * 100f), -29000, 29000);
        private void Awake() { spriteRenderer = GetComponent<SpriteRenderer>(); }
        private void LateUpdate()
        {
            spriteRenderer.sortingOrder = Order(feet != null ? feet.position.y : transform.position.y);
        }
    }
}
