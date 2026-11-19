#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EscapeUNIFRANZ.EditorTools
{
    /// <summary>Imports the authored Aula 2 annex without replacing the shared four-room floor.</summary>
    public static class Aula2MapBuilder
    {
        private const string Source = "Assets/_Project/Art/Levels/nivel4_aulas/NuevoMapa/";
        private const string ScenePath = "Assets/_Project/Scenes/Zones/Z07_Aula2.unity";
        private const string CorePath = "Assets/_Project/Scenes/Zones/Z06_NucleoIA.unity";
        private const float Ppu = 32f;
        private const int Cell = 1;
        private static bool[,] walkable;
        private static Vector2 Entry;
        private static Vector2 Return;
        // Door-frame highlights protrude into several authored gaps (7–14 px).
        // Keep their artwork intact, but give the feet collider a 16 px passage.
        private static readonly RectInt[] DoorClearances = {
            Portal(229,248,265),Portal(282,248,265),Portal(374,248,265),Portal(469,248,265),Portal(582,248,265),Portal(727,248,265),
            Portal(254,280,299),Portal(539,280,299),Portal(576,280,299),Portal(634,280,299),Portal(720,390,408)
        };
        private static RectInt Portal(int center,int top,int bottom)=>new RectInt(center-8,576-bottom,16,bottom-top);

        [MenuItem("Escape UNIFRANZ/Art/Integrate Aula 2 New Map")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Integrate Aula 2 in Edit Mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before integrating Aula 2.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                AssetDatabase.Refresh();
                var floor = Import("PisoActual.png");
                var walls = Import("murosActuall.png");
                var objects = Import("ObjetosActual.png");
                var elevatorDoor = ElevatorMapIntegration.FindDoor(objects, true);
                Return = new Vector2(elevatorDoor.center.x / Ppu, (elevatorDoor.yMax + 14f) / Ppu);
                Entry = Return + Vector2.up * .65f;
                objects = ElevatorMapIntegration.WithoutDoor(objects, elevatorDoor);
                // The original wall layer also contains a fixed elevator door.
                // Replace that artwork and its collision with the animated cabin opening.
                var cabinOpening = new RectInt(elevatorDoor.x, elevatorDoor.y, elevatorDoor.width, elevatorDoor.height + 8);
                walls = ElevatorMapIntegration.WithoutDoor(walls, cabinOpening, "MurosSinPuerta");
                if (floor.width != 960 || floor.height != 576 || walls.width != floor.width ||
                    walls.height != floor.height || objects.width != floor.width || objects.height != floor.height)
                    throw new InvalidOperationException("Aula 2 layers must share the authored 960 x 576 canvas.");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("Zone_Aula2");
                var zone = root.AddComponent<ZoneContext>();
                Set(zone, "zoneId", "aula2"); Set(zone, "displayName", "Aula 2 · Nuevo mapa");
                Set(zone, "defaultCheckpointSpawnId", "from_nucleo");
                Set(zone, "objectiveId", "aula2_explore");
                Set(zone, "objectiveText", "Explora el aula 2. Regresa por el acceso junto a las escaleras.");
                Set(zone, "minimapCenter", new Vector2(15f, 9f));
                Set(zone, "minimapSize", new Vector2(30f, 18f));
                var art = new GameObject("GroupLevelArt").transform; art.SetParent(root.transform, false);
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Levels/GroupWorldLit.mat");
                Draw(art, "PisoActual", floor, new Rect(0, 0, 960, 576), material, "Ground", 0);
                for (int y = 0; y < floor.height; y += 8)
                    Draw(art, "Muros_" + y, walls, new Rect(0, y, 960, 8), material, "World", WorldYSort2D.Order(y / Ppu));
                var furniture = Furniture(objects);
                for (int i = 0; i < furniture.Count; i++)
                {
                    var sprite = Draw(art, "Mueble_" + i, objects, furniture[i], material, "World", WorldYSort2D.Order(furniture[i].y / Ppu));
                    if (furniture[i].height >= 10) sprite.gameObject.AddComponent<WorldObjectOcclusion2D>();
                }
                var blocked = CollisionMask(floor, walls, objects);
                walkable = WalkableMask(blocked);
                var collision = new GameObject("WorldCollision_Authored").transform;
                collision.SetParent(root.transform, false);
                BuildColliders(collision, blocked);
                Spawn(root.transform, "from_nucleo", Entry);
                var cabin = ElevatorMapIntegration.CabinCenter(elevatorDoor, Ppu);
                Spawn(root.transform, "from_elevator", cabin);
                var elevator = Exit(root.transform, "Ascensor a planta baja", Return, "entrada", "from_aula2_elevator").GetComponent<ZoneExitInteractable>();
                ElevatorMapIntegration.Configure(elevator, art, elevatorDoor, Ppu, true);
                var ambient = new GameObject("Ambient_Aula2"); ambient.transform.SetParent(root.transform, false);
                var light = ambient.AddComponent<Light2D>(); light.lightType = Light2D.LightType.Global;
                light.intensity = .65f; light.color = new Color(.8f, .88f, 1f);
                light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
                ValidateWalkable(blocked, Entry, Return);
                ValidateWalkable(blocked, cabin, Return);
                foreach(var pixel in new[]{new Vector2(228,235),new Vector2(280,235),new Vector2(372,235),new Vector2(470,235),new Vector2(582,235),new Vector2(685,235),new Vector2(250,310),new Vector2(542,310)})
                    ValidateWalkable(blocked,cabin,FindOpenPosition(new Vector2(pixel.x/Ppu,(576-pixel.y)/Ppu)));
                Aula2TeamContentBuilder.PlaceRobots(zone, material);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save Aula 2.");
                AddEntrance();
                var catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalog>("Assets/_Project/Data/Zones/ZoneCatalog.asset");
                var serialized = new SerializedObject(catalog); var zones = serialized.FindProperty("zones");
                int index = -1;
                for (int i = 0; i < zones.arraySize; i++)
                    if (zones.GetArrayElementAtIndex(i).FindPropertyRelative("zoneId").stringValue == "aula2") index = i;
                if (index < 0) { index = zones.arraySize; zones.arraySize++; }
                var entry = zones.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("zoneId").stringValue = "aula2";
                entry.FindPropertyRelative("scenePath").stringValue = ScenePath;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var builds = EditorBuildSettings.scenes.ToList();
                builds.RemoveAll(s => s.path == ScenePath); builds.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = builds.ToArray();
                AssetDatabase.SaveAssets();
                VendingMachineBuilder.InstallInAula2();
                Directory.CreateDirectory("reports");
                File.WriteAllText("reports/AULA2_MAPA.md", "# Aula 2\n\nMapa independiente: Z07_Aula2.\n" +
                    "Fuente: nivel4_aulas/NuevoMapa; capas PisoActual, murosActuall y ObjetosActual.\n" +
                    "Escala: 32 píxeles por unidad; tamaño: 30 × 18 unidades.\n" +
                    "Acceso principal: ascensor de planta baja tras reparar el circuito. Regreso: ascensor central.\n" +
                    "Colisiones: precisión de un píxel, bases de muebles y geometría compuesta. Robots de Limber y expendedora de Abril junto al acceso.\n" +
                    "Verificación automática: dimensiones, puntos de llegada libres y conexión hasta el regreso.\n");
                Debug.Log("AULA2_MAP_OK: independent scene, two-way travel, collisions and spawn connectivity verified.");
            }
            finally
            {
                if (setup.Any(s => s.isLoaded && s.isActive && !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        public static void AddEntrance()
        {
            var scene = SceneManager.GetSceneByPath(CorePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(CorePath, OpenSceneMode.Additive);
            var zone = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ZoneContext>(true)).Single();
            foreach (string name in new[] { "Aula2AnnexAccess", "Spawn_from_aula2" })
            {
                var old = zone.transform.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            Exit(zone.transform, "Entrar al nuevo mapa del aula 2", new Vector2(10.8f, 5.6f), "aula2", "from_nucleo").name = "Aula2AnnexAccess";
            Spawn(zone.transform, "from_aula2", new Vector2(10.8f, 5.1f));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }

        private static Texture2D Import(string file)
        {
            string path = Source + file;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Ppu; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false; importer.isReadable = true;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
            importer.alphaIsTransparency = true; importer.spritePivot = Vector2.zero;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static SpriteRenderer Draw(Transform parent, string name, Texture2D texture, Rect rect, Material material, string layer, int order)
        {
            // Persistent sprite subassets are authored below; no transient sprites in saved scenes.
            string path = Source + name + ".asset";
            var sprite = Sprite.Create(texture, rect, Vector2.zero, Ppu, 0, SpriteMeshType.FullRect);
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing == null) AssetDatabase.CreateAsset(sprite, path);
            else { EditorUtility.CopySerialized(sprite, existing); Object.DestroyImmediate(sprite); sprite = existing; }
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(rect.x / Ppu, rect.y / Ppu);
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sharedMaterial = material; renderer.sortingLayerName = layer; renderer.sortingOrder = order;
            return renderer;
        }

        private static bool[,] CollisionMask(Texture2D floor, Texture2D walls, Texture2D objects)
        {
            var f = floor.GetPixels32(); var w = walls.GetPixels32(); var o = objects.GetPixels32();
            var wallMask=w.Select(pixel=>pixel.a>128).ToArray();
            foreach(var door in DoorClearances)
                if(door.xMax<=floor.width&&door.yMax<=floor.height)
                    for(int y=door.yMin;y<door.yMax;y++)for(int x=door.xMin;x<door.xMax;x++)wallMask[y*floor.width+x]=false;
            var furniture = Furniture(objects);
            var bases = new bool[o.Length];
            foreach (var rect in furniture)
                for (int y = (int)rect.y; y < (int)rect.y + Mathf.Max(2, Mathf.RoundToInt(rect.height * .4f)); y++)
                    for (int x = (int)rect.x; x < rect.xMax; x++)
                        if (o[y * floor.width + x].a > 128) bases[y * floor.width + x] = true;
            var mask = new bool[floor.width / Cell, floor.height / Cell];
            for (int y = 0; y < mask.GetLength(1); y++)
                for (int x = 0; x < mask.GetLength(0); x++)
                {
                    int solid = 0, outside = 0;
                    for (int dy = 0; dy < Cell; dy++) for (int dx = 0; dx < Cell; dx++)
                    {
                        int i = (y * Cell + dy) * floor.width + x * Cell + dx;
                        if (wallMask[i] || bases[i]) solid++;
                        if (f[i].a < 128 || f[i].r == 255 && f[i].g == 255 && f[i].b == 255) outside++;
                    }
                    mask[x, y] = solid > 0 || outside > 0;
                }
            return mask;
        }

        private static List<Rect> Furniture(Texture2D texture)
        {
            var pixels = texture.GetPixels32(); int width = texture.width, height = texture.height;
            var seen = new bool[pixels.Length]; var queue = new Queue<int>(); var rectangles = new List<Rect>();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (seen[i] || pixels[i].a <= 128) continue;
                int minX = width, minY = height, maxX = 0, maxY = 0; seen[i] = true; queue.Enqueue(i);
                while (queue.Count > 0)
                {
                    int p = queue.Dequeue(), x = p % width, y = p / width;
                    minX = Math.Min(minX,x); minY = Math.Min(minY,y); maxX = Math.Max(maxX,x); maxY = Math.Max(maxY,y);
                    foreach (var d in new[] {Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
                    {
                        int nx = x+d.x, ny = y+d.y, n = ny*width+nx;
                        if (nx>=0 && nx<width && ny>=0 && ny<height && !seen[n] && pixels[n].a>128) {seen[n]=true;queue.Enqueue(n);}
                    }
                }
                rectangles.Add(new Rect(minX,minY,maxX-minX+1,maxY-minY+1));
            }
            for (int a=0;a<rectangles.Count;a++) for (int b=a+1;b<rectangles.Count;b++)
                if (rectangles[a].Overlaps(rectangles[b]))
                {
                    var x=rectangles[a];var y=rectangles[b];
                    rectangles[a]=Rect.MinMaxRect(Math.Min(x.xMin,y.xMin),Math.Min(x.yMin,y.yMin),Math.Max(x.xMax,y.xMax),Math.Max(x.yMax,y.yMax));
                    rectangles.RemoveAt(b);a=-1;break;
                }
            return rectangles;
        }

        private static bool[,] WalkableMask(bool[,] blocked)
        {
            int width=blocked.GetLength(0),height=blocked.GetLength(1);
            var sum=new int[width+1,height+1]; var result=new bool[width,height];
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
                sum[x+1,y+1]=(blocked[x,y]?1:0)+sum[x,y+1]+sum[x+1,y]-sum[x,y];
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                int left=Mathf.FloorToInt(x-.19f*Ppu),right=Mathf.CeilToInt(x+.19f*Ppu);
                int bottom=Mathf.FloorToInt(y+.02f*Ppu),top=Mathf.CeilToInt(y+.24f*Ppu);
                if(left<0||right>width||bottom<0||top>height) continue;
                result[x,y]=sum[right,top]-sum[left,top]-sum[right,bottom]+sum[left,bottom]==0;
            }
            return result;
        }

        public static Vector2 FindOpenPosition(Vector2 desired, bool patrolLane = false)
        {
            Vector2Int center=new Vector2Int(Mathf.RoundToInt(desired.x*Ppu),Mathf.RoundToInt(desired.y*Ppu));
            float best=float.MaxValue; var result=Vector2.zero;
            for(int y=Math.Max(0,center.y-64);y<Math.Min(walkable.GetLength(1),center.y+65);y++)
                for(int x=Math.Max(0,center.x-64);x<Math.Min(walkable.GetLength(0),center.x+65);x++)
                {
                    if(!walkable[x,y])continue;
                    if(patrolLane)
                    {
                        bool free=true;
                        for(int yy=y-3;yy<=y+3&&free;yy++) for(int xx=x-14;xx<=x+14;xx++)
                            if(xx<0||yy<0||xx>=walkable.GetLength(0)||yy>=walkable.GetLength(1)||!walkable[xx,yy]){free=false;break;}
                        if(!free)continue;
                    }
                    float distance=(new Vector2(x,y)-(Vector2)center).sqrMagnitude;
                    if(distance<best){best=distance;result=new Vector2(x/Ppu,y/Ppu);}
                }
            if(best==float.MaxValue)throw new InvalidOperationException("No accessible location for team content near "+desired);
            return result;
        }

        private static void BuildColliders(Transform parent, bool[,] mask)
        {
            parent.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var composite = parent.gameObject.AddComponent<CompositeCollider2D>();
            composite.generationType = CompositeCollider2D.GenerationType.Manual;
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.vertexDistance = .001f; composite.offsetDistance = 0;
            var paths = new List<Vector2[]>();
            var used = new bool[mask.GetLength(0), mask.GetLength(1)];
            int count = 0;
            for (int y = 0; y < mask.GetLength(1); y++) for (int x = 0; x < mask.GetLength(0); x++)
            {
                if (!mask[x, y] || used[x, y]) continue;
                int width = 1, height = 1;
                while (x + width < mask.GetLength(0) && mask[x + width, y] && !used[x + width, y]) width++;
                while (y + height < mask.GetLength(1) && Enumerable.Range(x, width).All(i => mask[i, y + height] && !used[i, y + height])) height++;
                for (int j = y; j < y + height; j++) for (int i = x; i < x + width; i++) used[i, j] = true;
                count++;
                float left = x / Ppu, right = (x + width) / Ppu, bottom = y / Ppu, top = (y + height) / Ppu;
                paths.Add(new[] {new Vector2(left,bottom),new Vector2(right,bottom),new Vector2(right,top),new Vector2(left,top)});
            }
            var source = new GameObject("PixelRuns"); source.transform.SetParent(parent, false);
            var polygon = source.AddComponent<PolygonCollider2D>(); polygon.pathCount = paths.Count;
            for (int i = 0; i < paths.Count; i++) polygon.SetPath(i, paths[i]);
            polygon.compositeOperation = Collider2D.CompositeOperation.Merge;
            composite.GenerateGeometry();
            if (composite.pathCount == 0) throw new InvalidOperationException("Aula 2 collision geometry is empty.");
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
        }

        private static void ValidateWalkable(bool[,] mask, Vector2 start, Vector2 destination)
        {
            Vector2Int CellAt(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x * Ppu / Cell), Mathf.FloorToInt(p.y * Ppu / Cell));
            bool Free(Vector2Int p)
            {
                return p.x >= 0 && p.y >= 0 && p.x < walkable.GetLength(0) && p.y < walkable.GetLength(1) && walkable[p.x,p.y];
            }
            var a = CellAt(start); var b = CellAt(destination);
            if (!Free(a) || !Free(b)) throw new InvalidOperationException("Aula 2 spawn or return overlaps collision.");
            var seen = new HashSet<Vector2Int> { a }; var queue = new Queue<Vector2Int>(); queue.Enqueue(a);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue(); if (p == b) return;
                foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    if (!seen.Contains(p + d) && Free(p + d)) { seen.Add(p + d); queue.Enqueue(p + d); }
            }
            throw new InvalidOperationException("No walkable route in Aula 2 from "+start+" to "+destination+" (source pixel "+new Vector2(destination.x*Ppu,576-destination.y*Ppu)+").");
        }

        private static void Spawn(Transform parent, string id, Vector2 position)
        {
            var obj = new GameObject("Spawn_" + id); obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
            Set(obj.AddComponent<SpawnPoint>(), "spawnId", id);
        }

        private static GameObject Exit(Transform parent, string label, Vector2 position, string zone, string spawn)
        {
            var obj = new GameObject(label); obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
            var collider = obj.AddComponent<BoxCollider2D>(); collider.isTrigger = true; collider.size = new Vector2(.8f, .8f);
            var exit = obj.AddComponent<ZoneExitInteractable>(); Set(exit, "targetZoneId", zone); Set(exit, "targetSpawnId", spawn);
            Set(exit, "priority", 10);
            var marker = obj.AddComponent<MinimapTrackable2D>(); marker.displayName = label;
            Set(marker, "markerId", "exit_" + zone); Set(marker, "showAsExit", true);
            var text = new GameObject("AccessLabel"); text.transform.SetParent(obj.transform, false); text.transform.localPosition = Vector3.up * .5f;
            var tmp = text.AddComponent<TextMeshPro>(); tmp.text = "[E] " + label; tmp.fontSize = 2f;
            tmp.alignment = TextAlignmentOptions.Center; tmp.rectTransform.sizeDelta = new Vector2(5f, .6f);
            tmp.GetComponent<MeshRenderer>().sortingLayerName = "Effects";
            return obj;
        }

        private static void Set(Object obj, string field, object value)
        {
            var serialized = new SerializedObject(obj); var property = serialized.FindProperty(field);
            if (value is string s) property.stringValue = s;
            else if (value is Vector2 v) property.vector2Value = v;
            else if (value is bool b) property.boolValue = b;
            else if (value is int i) property.intValue = i;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
