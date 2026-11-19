using EscapeUNIFRANZ.Player;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    /// <summary>Keeps the player readable behind tall furniture without changing its physics.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(200)]
    public sealed class WorldObjectOcclusion2D : MonoBehaviour, IZoneRuntimeInitializable
    {
        private SpriteRenderer artwork, character;
        private Color original;
        private void Awake() { artwork = GetComponent<SpriteRenderer>(); original = artwork.color; }
        public void Initialize(ZoneRuntimeContext context)
        {
            var player = context.Session.GetComponentInChildren<PlayerVisualController>(true);
            if (player != null) character = player.GetComponentInChildren<SpriteRenderer>(true);
        }
        private void LateUpdate()
        {
            float target = IsOccluding(artwork, character) ? .25f : original.a;
            var color = original; color.a = Mathf.MoveTowards(artwork.color.a, target, Time.deltaTime * 8f);
            artwork.color = color;
        }
        public static bool IsOccluding(SpriteRenderer foreground, SpriteRenderer player)
        {
            if (foreground == null || player == null || foreground.sprite == null || player.sprite == null) return false;
            int layer = SortingLayer.GetLayerValueFromID(foreground.sortingLayerID);
            int actorLayer = SortingLayer.GetLayerValueFromID(player.sortingLayerID);
            if (layer < actorLayer || layer == actorLayer && foreground.sortingOrder <= player.sortingOrder) return false;
            var bounds = player.bounds;
            return OpaqueAt(foreground, bounds.center) ||
                OpaqueAt(foreground, new Vector3(bounds.center.x, bounds.max.y - bounds.size.y * .15f, bounds.center.z));
        }
        private static bool OpaqueAt(SpriteRenderer renderer, Vector3 point)
        {
            if (!renderer.bounds.Contains(point)) return false;
            var sprite = renderer.sprite; var texture = sprite.texture;
            if (!texture.isReadable) return true;
            var local = renderer.transform.InverseTransformPoint(point);
            int x = Mathf.FloorToInt(local.x * sprite.pixelsPerUnit + sprite.pivot.x);
            int y = Mathf.FloorToInt(local.y * sprite.pixelsPerUnit + sprite.pivot.y);
            if (x < 0 || y < 0 || x >= sprite.rect.width || y >= sprite.rect.height) return false;
            return texture.GetPixel((int)sprite.rect.x + x, (int)sprite.rect.y + y).a > .1f;
        }
        private void OnDisable() { if (artwork != null) artwork.color = original; }
    }
}
