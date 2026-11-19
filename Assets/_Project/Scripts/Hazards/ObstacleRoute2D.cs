using System.Collections.Generic;
using UnityEngine;

namespace EscapeUNIFRANZ.Hazards
{
    // A bounded, reusable local grid. Runs only when a direct route is obstructed.
    internal sealed class ObstacleRoute2D
    {
        private const int Width = 61, Center = Width / 2, Limit = 1800;
        private readonly int[] queue = new int[Width * Width];
        private readonly int[] parent = new int[Width * Width];
        private readonly Collider2D[] overlaps = new Collider2D[32];
        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        private readonly List<Vector2> route = new List<Vector2>(128);
        private Transform owner, target;
        private Vector2 size;
        private ContactFilter2D filter;
        private int cursor;
        private Vector2 plannedGoal;
        private float nextPlanAt;
        public void Clear() { route.Clear(); cursor = 0; nextPlanAt = 0f; }

        public Vector2 Next(Vector2 start, Vector2 goal, Vector2 footprint, Transform unit, Transform ignoredTarget, float cell, float now)
        {
            owner = unit; target = ignoredTarget; size = footprint;
            filter = ContactFilter2D.noFilter; filter.useTriggers = false;
            if (ClearSegment(start, goal)) { route.Clear(); cursor = 0; return goal; }
            if (now >= nextPlanAt && (cursor >= route.Count || (goal - plannedGoal).sqrMagnitude > cell * cell))
            {
                plannedGoal = goal;
                Plan(start, goal, cell);
                nextPlanAt = now + (route.Count == 0 ? 1.5f : 0.6f);
            }
            while (cursor < route.Count && (route[cursor] - start).sqrMagnitude < cell * cell * .16f) cursor++;
            if (cursor >= route.Count) return start;
            if (!ClearSegment(start, route[cursor])) { route.Clear(); cursor = 0; return start; }
            return route[cursor];
        }

        private bool Ignore(Transform hit) => hit.IsChildOf(owner) || (target != null && hit.IsChildOf(target));
        private bool ClearSegment(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            int count = Physics2D.BoxCast(from, size, 0f, delta.normalized, filter, hits, delta.magnitude);
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++) if (hits[i].collider != null && !Ignore(hits[i].transform)) return false;
            return true;
        }
        private bool Free(Vector2 point)
        {
            int count = Physics2D.OverlapBox(point, size, 0f, filter, overlaps);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++) if (overlaps[i] != null && !Ignore(overlaps[i].transform)) return false;
            return true;
        }
        private void Plan(Vector2 origin, Vector2 goal, float cell)
        {
            route.Clear(); cursor = 0;
            for (int i = 0; i < parent.Length; i++) parent[i] = -2;
            int first = Center * Width + Center, head = 0, tail = 1, found = -1;
            queue[0] = first; parent[first] = -1;
            while (head < tail && head < Limit)
            {
                int index = queue[head++], x = index % Width, y = index / Width;
                Vector2 point = origin + new Vector2(x - Center, y - Center) * cell;
                if ((point - goal).sqrMagnitude <= cell * cell * 2f && ClearSegment(point, goal)) { found = index; break; }
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0);
                    int ny = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                    if (nx < 0 || ny < 0 || nx >= Width || ny >= Width) continue;
                    int next = ny * Width + nx;
                    if (parent[next] != -2) continue;
                    Vector2 candidate = origin + new Vector2(nx - Center, ny - Center) * cell;
                    if (!Free(candidate) || !ClearSegment(point, candidate)) continue;
                    parent[next] = index; queue[tail++] = next;
                }
            }
            if (found < 0) return;
            for (int index = found; index != first; index = parent[index])
                route.Add(origin + new Vector2(index % Width - Center, index / Width - Center) * cell);
            route.Reverse(); route.Add(goal);
        }
    }
}
