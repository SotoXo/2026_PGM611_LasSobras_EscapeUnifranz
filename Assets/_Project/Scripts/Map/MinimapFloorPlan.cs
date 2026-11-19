using System.Collections.Generic;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Map
{
    internal static class MinimapFloorPlan
    {
        private const float PixelsPerUnit = 12f;
        private static readonly Color32 Walkable = new(56, 25, 34, 255);
        private static readonly Color32 Walls = new(165, 82, 94, 255);
        private static readonly Color32 SolidInterior = new(32, 13, 21, 255);
        private static readonly Color32 Void = new(13, 8, 11, 255);

        public static Texture2D Build(ZoneContext zone, Rect bounds)
        {
            if (bounds.width <= 0f || bounds.height <= 0f)
                return null;

            int width = Mathf.Clamp(Mathf.CeilToInt(bounds.width * PixelsPerUnit), 1, 384);
            int height = Mathf.Clamp(Mathf.CeilToInt(bounds.height * PixelsPerUnit), 1, 384);
            var occupied = new bool[width * height];
            Physics2D.SyncTransforms();
            foreach (var collider in zone.GetComponentsInChildren<Collider2D>())
            {
                if (!collider.enabled || collider.isTrigger || collider.GetComponentInParent<DoorPanel2D>() != null ||
                    (collider.attachedRigidbody != null && collider.attachedRigidbody.bodyType != RigidbodyType2D.Static))
                    continue;

                Rasterize(collider, bounds, width, height, occupied);
            }

            bool[] footprint = ReadFootprint(zone.MinimapFloorMask, width, height);
            bool[] floor = footprint == null
                ? FindReachableFloor(zone, bounds, width, height, occupied)
                : new bool[occupied.Length];
            if (footprint != null)
                for (int i = 0; i < floor.Length; i++)
                    floor[i] = footprint[i] && !occupied[i];

            var pixels = new Color32[occupied.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % width, y = i / width;
                bool edge = occupied[i] && (
                    (x > 0 && floor[i - 1]) || (x < width - 1 && floor[i + 1]) ||
                    (y > 0 && floor[i - width]) || (y < height - 1 && floor[i + width]));
                pixels[i] = floor[i] ? Walkable : edge ? Walls :
                    (footprint != null && footprint[i] ? SolidInterior : Void);
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Minimap " + zone.ZoneId,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static bool[] ReadFootprint(Texture2D mask, int width, int height)
        {
            if (mask == null || !mask.isReadable)
                return null;

            var floor = new bool[width * height];
            Color32[] source = mask.GetPixels32();
            for (int y = 0; y < height; y++)
            {
                int sourceY = (2 * y + 1) * mask.height / (2 * height);
                for (int x = 0; x < width; x++)
                {
                    int sourceX = (2 * x + 1) * mask.width / (2 * width);
                    floor[y * width + x] = source[sourceY * mask.width + sourceX].a >= 128;
                }
            }
            return floor;
        }

        private static bool[] FindReachableFloor(ZoneContext zone, Rect bounds, int width, int height, bool[] occupied)
        {
            var floor = new bool[occupied.Length];
            var pending = new Queue<int>();
            foreach (var spawn in zone.GetComponentsInChildren<SpawnPoint>())
            {
                Vector2 uv = MinimapLogic.NormalizePosition(spawn.Position, bounds);
                int x = Mathf.Clamp((int)(uv.x * width), 0, width - 1);
                int y = Mathf.Clamp((int)(uv.y * height), 0, height - 1);
                // Seed around the spawn in case a thin collider covers its exact map pixel.
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        Visit(x + dx, y + dy, width, height, occupied, floor, pending);
            }

            while (pending.Count != 0)
            {
                int cell = pending.Dequeue();
                int x = cell % width, y = cell / width;
                Visit(x - 1, y, width, height, occupied, floor, pending);
                Visit(x + 1, y, width, height, occupied, floor, pending);
                Visit(x, y - 1, width, height, occupied, floor, pending);
                Visit(x, y + 1, width, height, occupied, floor, pending);
            }

            return floor;
        }

        private static void Rasterize(Collider2D collider, Rect world, int width, int height, bool[] occupied)
        {
            Bounds bounds = collider.bounds;
            if (bounds.size.x <= 0f || bounds.size.y <= 0f ||
                !world.Overlaps(Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y)))
                return;

            Vector2 min = MinimapLogic.NormalizePosition(bounds.min, world);
            Vector2 max = MinimapLogic.NormalizePosition(bounds.max, world);
            int left = Mathf.FloorToInt(min.x * width), right = Mathf.CeilToInt(max.x * width);
            int bottom = Mathf.FloorToInt(min.y * height), top = Mathf.CeilToInt(max.y * height);
            bool axisAligned = collider is BoxCollider2D &&
                Mathf.Abs(Mathf.DeltaAngle(collider.transform.eulerAngles.z, 0f)) < 0.01f;
            for (int y = bottom; y < top; y++)
            {
                for (int x = left; x < right; x++)
                {
                    Vector2 sample = new(world.xMin + (x + 0.5f) * world.width / width,
                        world.yMin + (y + 0.5f) * world.height / height);
                    if (axisAligned || collider.OverlapPoint(sample))
                        occupied[y * width + x] = true;
                }
            }
        }

        private static void Visit(int x, int y, int width, int height, bool[] occupied, bool[] floor, Queue<int> pending)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return;
            int cell = y * width + x;
            if (occupied[cell] || floor[cell])
                return;
            floor[cell] = true;
            pending.Enqueue(cell);
        }
    }
}
