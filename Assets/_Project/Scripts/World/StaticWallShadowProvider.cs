using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EscapeUNIFRANZ.World
{
    [Serializable]
    public sealed class StaticWallShadowProvider : ShadowShape2DProvider
    {
        [NonSerialized] private bool built;

        public override bool IsShapeSource(Component source) =>
            source is CompositeCollider2D collider && collider.geometryType == CompositeCollider2D.GeometryType.Outlines;

        public override void Enabled(Component source, ShadowShape2D shape) => built = false;
        public override void OnPersistantDataCreated(Component source, ShadowShape2D shape) => built = false;

        public override void OnBeforeRender(Component source, Bounds visibleBounds, ShadowShape2D shape)
        {
            if (built || !(source is CompositeCollider2D collider) || collider.pathCount == 0) return;

            // The contours are authored and static. Camera movement does not change their mesh.
            var paths = new Vector2[collider.pathCount][];
            int vertexCount = 0;
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = new Vector2[collider.GetPathPointCount(i)];
                vertexCount += collider.GetPath(i, paths[i]);
            }

            var points = new Vector3[vertexCount];
            var lines = new int[vertexCount * 2];
            int offset = 0;
            foreach (var path in paths)
            {
                for (int i = 0; i < path.Length; i++)
                {
                    int index = offset + i;
                    points[index] = path[i];
                    lines[index * 2] = index;
                    lines[index * 2 + 1] = offset + (i + 1) % path.Length;
                }
                offset += path.Length;
            }

            using var vertices = new NativeArray<Vector3>(points, Allocator.Temp);
            using var edges = new NativeArray<int>(lines, Allocator.Temp);
            shape.SetShape(vertices, edges, ShadowShape2D.OutlineTopology.Lines,
                ShadowShape2D.WindingOrder.CounterClockwise, createInteriorGeometry: true);
            built = true;
        }
    }
}
