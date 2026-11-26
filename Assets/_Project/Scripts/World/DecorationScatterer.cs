using System.Collections.Generic;
using System.Linq;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    /// <summary>
    /// Cuarta capa del mapa: "Decoración". Reparte sprites sin colisión sobre el suelo libre,
    /// lejos de puertas, ascensores, spawns, objetos interactivos y rutas de patrulla.
    /// Usar el menú contextual del componente: Scatter / Clear (determinista por 'seed').
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DecorationScatterer : MonoBehaviour
    {
        public const string LayerObjectName = "Decoración";
        public Sprite[] sprites;
        public Material litMaterial;
        [Min(0)] public int count = 40;
        public int seed = 1;
        public Rect area = new Rect(-10, -10, 20, 20);
        public float clearRadius = 1.4f;
        public float patrolClearance = 1.2f;
        public float minSpacing = 1.0f;
        public string floorSortingLayer = "Decals";
        public string wallSortingLayer = "World";
        [Tooltip("Genera la decoración al iniciar la zona, cuando ya existen los colliders.")] public bool scatterOnStart = true;

        private void Start() { if (scatterOnStart && transform.Find(LayerObjectName) == null) Scatter(); }

        [ContextMenu("Scatter")]
        public void Scatter()
        {
            Clear();
            Physics2D.SyncTransforms();
            if (sprites == null || sprites.Length == 0) return;
            var keepClear = CollectKeepClear(out var patrolSegments);
            var root = new GameObject(LayerObjectName).transform;
            root.SetParent(transform, false);
            var placed = new List<Vector2>();
            var rng = new System.Random(seed);
            int attempts = count * 40, made = 0;
            while (made < count && attempts-- > 0)
            {
                Vector2 p = new Vector2(area.xMin + (float)rng.NextDouble() * area.width, area.yMin + (float)rng.NextDouble() * area.height);
                Sprite sprite = sprites[rng.Next(sprites.Length)];
                if (sprite == null) continue;
                bool wallLike = sprite.name.Contains("telarana") || sprite.name.Contains("senal");
                if (Physics2D.OverlapPointAll(p).Any(c=>!c.isTrigger)) continue;
                bool nearSolid = Physics2D.OverlapCircleAll(p,.6f).Any(c=>!c.isTrigger);
                if (wallLike != nearSolid) continue;                                  // telarañas y señales junto a paredes; el resto en suelo abierto
                if (TooClose(p, keepClear, clearRadius) || TooClose(p, placed, minSpacing) || NearSegments(p, patrolSegments, patrolClearance)) continue;
                var go = new GameObject(sprite.name);
                go.transform.SetParent(root, false);
                go.transform.position = p;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                if(litMaterial!=null)sr.sharedMaterial=litMaterial;
                sr.flipX = rng.Next(2) == 0;
                if (wallLike) { sr.sortingLayerName = wallSortingLayer; sr.sortingOrder = WorldYSort2D.Order(p.y); }
                else { sr.sortingLayerName = floorSortingLayer; sr.sortingOrder = 20; go.transform.rotation = Quaternion.Euler(0, 0, rng.Next(4) * 90f); }
                placed.Add(p); made++;
            }
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            Transform old = transform.Find(LayerObjectName);
            if (old != null) { if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject); }
        }

        private List<Vector2> CollectKeepClear(out List<(Vector2, Vector2)> segments)
        {
            var points = new List<Vector2>(); segments = new List<(Vector2, Vector2)>();
            Transform scope = transform.parent != null ? transform.parent : transform;
            foreach (var e in scope.GetComponentsInChildren<ZoneExitInteractable>(true)) points.Add(e.transform.position);
            foreach (var e in scope.GetComponentsInChildren<ElevatorDoorAnimation>(true)) { points.Add(e.cabinCenter); points.Add(e.transform.position); }
            foreach (var i in scope.GetComponentsInChildren<InteractableBehaviour>(true)) points.Add(i.transform.position);
            foreach (var spawn in scope.GetComponentsInChildren<SpawnPoint>(true)) points.Add(spawn.Position);
            Transform spawns = scope.Find("SpawnPoints");
            if (spawns != null) foreach (Transform s in spawns) points.Add(s.position);
            foreach (var path in scope.GetComponentsInChildren<PatrolPath2D>(true))
                for (int i = 0; i < path.Count; i++)
                {
                    Vector2 a = path.GetPoint(i); points.Add(a);
                    if (i + 1 < path.Count) segments.Add((a, path.GetPoint(i + 1)));
                }
            return points;
        }

        private static bool TooClose(Vector2 p, List<Vector2> list, float r) { float r2 = r * r; foreach (var q in list) if ((q - p).sqrMagnitude < r2) return true; return false; }
        private static bool NearSegments(Vector2 p, List<(Vector2, Vector2)> segs, float r)
        {
            foreach (var (a, b) in segs)
            {
                Vector2 ab = b - a; float t = ab.sqrMagnitude < .0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                if ((a + ab * t - p).sqrMagnitude < r * r) return true;
            }
            return false;
        }
    }
}
