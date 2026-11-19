#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Puzzles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EscapeUNIFRANZ.EditorTools
{
    /// <summary>Imports the complete authored ground floor without rebuilding campaign logic.</summary>
    public static class CompleteGroundFloorBuilder
    {
        const string Folder = "Assets/_Project/Art/Levels/nivel1_planta_baja/";
        const string PrefabPath = Folder + "nivel1_planta_baja.prefab";
        // Preserve the campaign's 30-unit width, using uniform scaling for the new artwork.
        const float Ppu = 610f / 30f;
        static int width, height;
        static bool[] solid, reachable;
        static Material material;
        // Authored stair treads are walkable floor, not furniture footprints.
        // These rectangles use the source canvas's bottom-left pixel coordinates.
        static readonly RectInt[] StairTreads = {
            new RectInt(70, 126, 20, 16),
            new RectInt(249, 87, 30, 30),
            new RectInt(432, 47, 29, 28)
        };
        // The authored landing is nine pixels wide. A corner pixel and an
        // isolated wall-layer fleck cross it; they must not block the stairs.
        static readonly RectInt StairLanding = new RectInt(79, 115, 9, 13);

        [MenuItem("Escape UNIFRANZ/Art/Replace Ground Floor With Mapa Completo")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run in Edit Mode.");
            if (!SavePendingSceneChanges()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            AssetDatabase.Refresh();
            material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Levels/GroupWorldLit.mat");
            var floor = Import("mapa_suelo");
            var walls = Import("mapa_muros");
            var objects = Import("mapa_objetos");
            width = floor.width; height = floor.height;
            if (width != 610 || height != 285 || walls.width != width || walls.height != height || objects.width != width || objects.height != height)
                throw new InvalidOperationException("Expected three aligned 610 x 285 layers.");
            var fp = floor.GetPixels32(); var wp = walls.GetPixels32(); var op = objects.GetPixels32();
            var elevatorDoor = ElevatorMapIntegration.FindDoor(objects, false);
            ElevatorMapIntegration.ClearDoor(op, width, elevatorDoor);
            var wallMask = wp.Select(pixel => pixel.a >= 128).ToArray();
            for (int y = StairLanding.yMin; y < StairLanding.yMax; y++)
                for (int x = StairLanding.xMin; x < StairLanding.xMax; x++)
                    wallMask[y * width + x] = false;
            solid = new bool[width * height];
            for (int i = 0; i < solid.Length; i++) solid[i] = fp[i].a < 128 || wallMask[i];
            var props = Components(op);
            // Only opaque pixels at the furniture's base block movement. The
            // rectangular slice is for rendering and may contain empty gaps.
            foreach (var r in props)
                for (int y = r.yMin; y < Math.Min(r.yMax, r.yMin + Mathf.Max(2, Mathf.RoundToInt(r.height * .4f))); y++)
                    for (int x = r.xMin; x < r.xMax; x++)
                        if (op[y * width + x].a >= 128 && !StairLanding.Contains(new Vector2Int(x, y)) &&
                            !StairTreads.Any(stair => stair.Contains(new Vector2Int(x, y))))
                            solid[y * width + x] = true;
            ValidateStairsAndWall(fp, wp, wallMask);
            FindReachable();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                foreach (Transform t in root.transform.Cast<Transform>().ToArray())
                    if (t.name == "Suelo" || t.name == "Decals" || t.name.StartsWith("Wall_") || t.name.StartsWith("Prop_") || t.name == "WorldCollision_Authored" || t.name == "WallShadows" || t.name == "MapaCompleto")
                        Object.DestroyImmediate(t.gameObject);
                var visual = new GameObject("MapaCompleto").transform; visual.SetParent(root.transform, false);
                Draw(visual, Slice("mapa_suelo", new[] { new RectInt(0, 0, width, height) }, "Suelo")[0], new RectInt(0, 0, width, height), "Ground");
                var bands = new List<RectInt>();
                for (int y = 0; y < height; y += 8)
                    bands.Add(new RectInt(0, y, width, Math.Min(8, height - y)));
                var wallSprites = Slice("mapa_muros", bands, "Wall");
                for (int i = 0; i < bands.Count; i++) Draw(visual, wallSprites[i], bands[i], "World");
                var propSprites = Slice("mapa_objetos", props, "Prop");
                for (int i = 0; i < props.Count; i++)
                {
                    var rect = props[i];
                    bool stair = StairTreads.Any(s => s.Contains(new Vector2Int(Mathf.RoundToInt(rect.center.x), Mathf.RoundToInt(rect.center.y))));
                    var sprite = Draw(visual, propSprites[i], rect, stair ? "Decals" : "World");
                    if (!stair && rect.height >= 10) sprite.gameObject.AddComponent<WorldObjectOcclusion2D>();
                }
                var collision = BuildGeometry(root.transform, "WorldCollision_Authored", solid,
                    CompositeCollider2D.GeometryType.Polygons, out int collisionRectangles);

                // Only wall outlines cast shadows. Keep the shadow geometry out of movement queries.
                var wallsOnly = BuildGeometry(root.transform, "WallShadows", wallMask,
                    CompositeCollider2D.GeometryType.Outlines, out _);
                wallsOnly.isTrigger = true;
                wallsOnly.excludeLayers = ~0;
                var shadow = wallsOnly.gameObject.AddComponent<ShadowCaster2D>();
                shadow.castingOption = ShadowCaster2D.ShadowCastingOptions.CastAndSelfShadow;
                shadow.trimEdge = 0f;
                var shadowData = new SerializedObject(shadow);
                if (shadowData.FindProperty("m_ShadowShape2DComponent").objectReferenceValue != wallsOnly)
                    throw new InvalidOperationException("The shadow caster must use the wall outlines.");
                shadowData.FindProperty("m_ShadowShape2DProvider").managedReferenceValue = new StaticWallShadowProvider();
                shadowData.ApplyModifiedPropertiesWithoutUndo();

                var dentalRoom = new Rect(4f, 1.6f, 7.5f, 3.6f);
                foreach (Transform t in root.transform)
                {
                    if (t.name == "ItemVisual_llave")
                        t.localPosition = Safe(new Vector3(10.8f, 4.65f, t.localPosition.z), area: dentalRoom);
                    else if (t.name.StartsWith("ItemVisual_") || t.name.StartsWith("AuthoredLamp_"))
                        t.localPosition = Safe(t.localPosition);
                }
                ConfigureCleanPrefab(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save the ground floor prefab.");
                Directory.CreateDirectory("reports");
                File.WriteAllText("reports/mapa-completo-integration.txt", $"Artwork: {width} x {height}; {Ppu} pixels/unit; {width/Ppu} x {height/Ppu} units.\nProps: {props.Count}. Collision rectangles: {collisionRectangles} in one source collider. Composite paths: {collision.pathCount}. Shadow source: {wallsOnly.pathCount} wall outlines.\n");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            try
            {
                foreach (string name in new[] { "Z00_Entrada", "Z01_Hall", "Z02_ARCA" })
                {
                    var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Zones/" + name + ".unity", OpenSceneMode.Single);
                    var zone = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ZoneContext>(true)).First();
                    var old = zone.transform.Find("GroupLevelArt");
                    if (old == null) throw new InvalidOperationException("Missing level instance: " + name);
                    var art = old.gameObject;
                    var targets = zone.GetComponentsInChildren<Transform>(true).Where(t => t.gameObject.activeInHierarchy && !t.IsChildOf(art.transform) &&
                        (t.GetComponent<SpawnPoint>() != null || t.GetComponent<InteractableBehaviour>() != null || t.GetComponent<PatrolMover2D>() != null ||
                         t.GetComponent<TimedHazardArea2D>() != null || (t.parent != null && t.parent.GetComponent<PatrolPath2D>() != null)))
                        .Select(t => (transform:t, position:t.position)).ToArray();
                    var occupied = new List<Vector3>();
                    foreach (var target in targets)
                    {
                        var local = art.transform.InverseTransformPoint(target.position);
                        bool interactive = target.transform.GetComponent<InteractableBehaviour>() != null;
                        var safe = Safe(local, interactive ? occupied : null);
                        if (interactive) occupied.Add(safe);
                        target.transform.position = art.transform.TransformPoint(safe);
                        File.AppendAllText("reports/mapa-completo-integration.txt", $"{name}/{target.transform.name}: {local} -> {safe}\n");
                    }
                    var data = new SerializedObject(zone);
                    data.FindProperty("minimapCenter").vector2Value = new Vector2(width/Ppu/2,height/Ppu/2);
                    data.FindProperty("minimapSize").vector2Value = new Vector2(width/Ppu,height/Ppu);
                    data.ApplyModifiedPropertiesWithoutUndo();
                    foreach (var spawn in zone.GetComponentsInChildren<SpawnPoint>(true))
                    {
                        var local = art.transform.InverseTransformPoint(spawn.transform.position);
                        if (!IsSafe(Mathf.RoundToInt(local.x*Ppu), Mathf.RoundToInt(local.y*Ppu)))
                            throw new InvalidOperationException("Unsafe spawn: " + spawn.name);
                    }
                    ConfigureCleanScene(zone, art);
                    ConfigureKeyedPassage(zone, art.transform);
                    CampaignGroundFloor.Configure(zone, art);
                    if (name == "Z00_Entrada") ConfigureHallAccess(zone);
                    ConfigureElevator(zone, art.transform);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException("Could not save " + name);
                }
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (setup.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            Debug.Log("MAPA_COMPLETO_OK: replaced ground floor in three scenes; all spawn positions validated on connected walkable floor.");
        }

        static void ValidateStairsAndWall(Color32[] floor, Color32[] walls, bool[] wallMask)
        {
            for (int i = 0; i < walls.Length; i++)
                if (!StairLanding.Contains(new Vector2Int(i % width, i / width)) && wallMask[i] != (walls[i].a >= 128))
                    throw new InvalidOperationException("Wall collision must preserve every authored wall pixel.");
            foreach (var stair in StairTreads)
                for (int y = stair.yMin; y < stair.yMax; y++)
                    for (int x = stair.xMin; x < stair.xMax; x++)
                    {
                        int i = y * width + x;
                        if (floor[i].a >= 128 && !wallMask[i] && solid[i])
                            throw new InvalidOperationException("Stair treads must not have furniture collision.");
                    }
            Debug.Log("STAIRS_AND_WALL_OK: all three stair flights and the narrow landing are walkable; complete authored wall artwork is preserved.");
        }

        public static void CleanAndLight() => CampaignGroundFloor.Apply();

        static CompositeCollider2D BuildGeometry(Transform parent, string name, bool[] mask,
            CompositeCollider2D.GeometryType geometryType, out int rectangleCount)
        {
            var owner = new GameObject(name);
            owner.transform.SetParent(parent, false);
            owner.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var composite = owner.AddComponent<CompositeCollider2D>();
            composite.geometryType = geometryType;
            composite.generationType = CompositeCollider2D.GenerationType.Manual;
            composite.vertexDistance = 0.001f;
            composite.offsetDistance = 0f;

            // Store the pixel runs in one collider instead of creating a GameObject per run.
            var rectangles = new List<RectInt>();
            var runs = new Dictionary<Vector2Int, RectInt>();
            for (int y = 0; y <= height; y++)
            {
                var next = new Dictionary<Vector2Int, RectInt>();
                for (int x = 0; y < height && x < width; x++)
                {
                    if (!mask[y * width + x]) continue;
                    int start = x;
                    while (x + 1 < width && mask[y * width + x + 1]) x++;
                    var key = new Vector2Int(start, x - start + 1);
                    next[key] = runs.TryGetValue(key, out var previous)
                        ? new RectInt(start, previous.y, key.y, previous.height + 1)
                        : new RectInt(start, y, key.y, 1);
                }
                foreach (var pair in runs)
                    if (!next.ContainsKey(pair.Key)) rectangles.Add(pair.Value);
                runs = next;
            }

            // Keep the composite alone on its GameObject so URP selects it as the shadow source.
            var source = new GameObject("PixelRuns");
            source.transform.SetParent(owner.transform, false);
            var polygon = source.AddComponent<PolygonCollider2D>();
            polygon.pathCount = rectangleCount = rectangles.Count;
            for (int i = 0; i < rectangles.Count; i++)
            {
                var r = rectangles[i];
                polygon.SetPath(i, new[]
                {
                    new Vector2(r.xMin / Ppu, r.yMin / Ppu),
                    new Vector2(r.xMax / Ppu, r.yMin / Ppu),
                    new Vector2(r.xMax / Ppu, r.yMax / Ppu),
                    new Vector2(r.xMin / Ppu, r.yMax / Ppu)
                });
            }
            polygon.compositeOperation = Collider2D.CompositeOperation.Merge;
            composite.GenerateGeometry();
            if (composite.pathCount == 0)
                throw new InvalidOperationException("No geometry generated for " + name);
            return composite;
        }

        static bool SavePendingSceneChanges()
        {
            var dirty = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty) dirty.Add(string.IsNullOrEmpty(scene.path) ? "Escena sin guardar" : scene.path);
            }
            if (dirty.Count == 0) return true;
            if (Application.isBatchMode)
                throw new InvalidOperationException("Save modified scenes before batch integration: " + string.Join(", ", dirty));
            if (!EditorUtility.DisplayDialog("Guardar escenas antes de continuar",
                "El mapa necesita abrir otras escenas. Guarda primero los cambios pendientes:\n\n" + string.Join("\n", dirty),
                "Guardar y continuar", "Cancelar")) return false;
            if (!EditorSceneManager.SaveOpenScenes())
            {
                Debug.LogWarning("Integración cancelada: no se guardaron todas las escenas. No se aplicaron cambios al mapa.");
                return false;
            }
            return true;
        }

        static void ConfigureCleanPrefab(GameObject root)
        {
            var battery = root.transform.Find("ItemVisual_bateria");
            if (battery != null) Object.DestroyImmediate(battery.gameObject);
            foreach (var flicker in root.GetComponentsInChildren<GroupLampFlicker2D>(true)) flicker.enabled = false;
            foreach (var lamp in root.GetComponentsInChildren<Light2D>(true))
            {
                var oldFixture = lamp.GetComponent<SpriteRenderer>();
                if (oldFixture != null) oldFixture.enabled = false;
                lamp.shadowsEnabled = false;
                lamp.enabled = true; lamp.color = new Color(1f, .97f, .91f); lamp.intensity = .04f;
                lamp.pointLightOuterRadius = 3.5f; lamp.pointLightInnerRadius = 1f;
                lamp.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
            }
            foreach (var id in new[] { "linterna", "llave" })
            {
                var item = root.transform.Find("ItemVisual_" + id);
                if (item == null) throw new InvalidOperationException("Missing preserved item: " + id);
                string itemId = id == "linterna" ? "flashlight" : "key";
                var pickup = EnsureComponent<ItemPickupInteractable>(item.gameObject);
                pickup.itemId = itemId;
                pickup.item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Narrative/Items/" + itemId + ".asset");
                // Shared across all three scenes, so collected items never reappear after changing zone.
                pickup.pickupId = itemId == "flashlight" ? "entrada_linterna_personal" : "nivel1_mapa_completo_llave";
                var trigger = EnsureComponent<CircleCollider2D>(item.gameObject);
                trigger.enabled = true; trigger.isTrigger = true; trigger.radius = .4f;
                var renderer = item.GetComponent<SpriteRenderer>();
                if (renderer == null) throw new InvalidOperationException("Missing item sprite: " + item.name);
                renderer.enabled = true;
                renderer.sortingOrder = WorldYSort2D.Order(item.localPosition.y);
                item.gameObject.SetActive(true);
            }
        }

        static void ConfigureCleanScene(ZoneContext zone, GameObject art)
        {
            // Retain serialized legacy objects for recovery, but remove them from this map's gameplay.
            foreach (var interaction in zone.GetComponentsInChildren<InteractableBehaviour>(true))
                if (!interaction.transform.IsChildOf(art.transform) && !(interaction is ZoneExitInteractable) &&
                    !(interaction is ItemPickupInteractable pickup && pickup.ItemId == ElevatorCircuit.ItemId))
                    interaction.gameObject.SetActive(false);
            foreach (var renderer in zone.GetComponentsInChildren<Renderer>(true))
                if (!renderer.transform.IsChildOf(art.transform)) renderer.enabled = false;
            foreach (var collider in zone.GetComponentsInChildren<Collider2D>(true))
                if (!collider.transform.IsChildOf(art.transform) && !collider.isTrigger) collider.enabled = false;
            foreach (string name in new[] { "Hazards", "Environment", "WorldCollision", "PresentationDecor", "RoomLighting" })
            { var old = zone.transform.Find(name); if (old != null) old.gameObject.SetActive(false); }
            foreach (var controller in zone.GetComponentsInChildren<RoomLightingController>(true)) controller.gameObject.SetActive(false);
            foreach (var sequence in zone.GetComponentsInChildren<LightSequenceTrigger>(true)) sequence.gameObject.SetActive(false);
            foreach (var light in zone.gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light2D>(true)))
                if (!light.transform.IsChildOf(art.transform)) light.enabled = false;
            var lightRoot = zone.transform.Find("MapaCompleto_GlobalLight");
            if (lightRoot == null) { lightRoot = new GameObject("MapaCompleto_GlobalLight").transform; lightRoot.SetParent(zone.transform, false); }
            var fill = EnsureComponent<Light2D>(lightRoot.gameObject);
            fill.lightType = Light2D.LightType.Global; fill.color = new Color(1f, .98f, .95f);
            fill.intensity = .06f; fill.enabled = true;
            fill.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
        }

        static void ConfigureElevator(ZoneContext zone, Transform art)
        {
            const string folder = "Assets/_Project/Art/Puzzles/puzzle_circuito/";
            var exit = zone.GetComponentsInChildren<ZoneExitInteractable>(true)
                .SingleOrDefault(candidate => candidate.name == "ExitToPiso2");
            if (exit == null)
            {
                var owner = new GameObject("ExitToPiso2");
                owner.transform.SetParent(zone.transform, false);
                exit = owner.AddComponent<ZoneExitInteractable>();
            }
            var exitData = new SerializedObject(exit);
            exitData.FindProperty("targetZoneId").stringValue = "aula2";
            exitData.FindProperty("targetSpawnId").stringValue = "from_elevator";
            exitData.ApplyModifiedPropertiesWithoutUndo();
            var circuit = new ElevatorCircuit();
            int[] referenceTurns = { 0, 1, 2, 1, 1, 2, 0, 3, 1, 1, 0, 1, 0, 2, 1, 1 };
            for (int tile = 0; tile < referenceTurns.Length; tile++)
                for (int turn = 0; turn < referenceTurns[tile]; turn++)
                    circuit.Rotate(tile);
            if (circuit.IsPowered()) throw new InvalidOperationException("The circuit requires its missing piece.");
            circuit.Install();
            if (!circuit.IsPowered()) throw new InvalidOperationException("The reference circuit must reach the output.");
            exit.energyRequirement = GameFlagId.None;
            exit.networkRequirement = GameFlagId.None;
            if (exit.statusIndicator != null) exit.statusIndicator.enabled = true;
            // The car sits immediately east of the stairs. Its approach is
            // the narrow aisle between those stairs and the meeting room.
            var aisle = new Rect(14.2f, 4.2f, .55f, 1.9f);
            var landing = Safe(new Vector3(14.5f, 4.72f, 0f), area: aisle);
            exit.transform.position = art.TransformPoint(landing);
            var accessTrigger = EnsureComponent<BoxCollider2D>(exit.gameObject);
            accessTrigger.isTrigger = true;
            accessTrigger.size = new Vector2(.7f, 1.3f);
            var indicator = EnsureComponent<SpriteRenderer>(exit.gameObject);
            foreach (var spawn in zone.GetComponentsInChildren<SpawnPoint>(true))
                if (spawn.name == "Spawn_from_piso2")
                    spawn.transform.position = art.TransformPoint(Safe(landing + Vector3.up, area: aisle));
            exit.elevatorPanel = ImportCircuitSprite(folder + "panel.png", SpriteImportMode.Single);
            var missing = ImportCircuitSprite(folder + "pieza_faltante.png", SpriteImportMode.Single);
            indicator.sprite = missing;
            indicator.sharedMaterial = material;
            indicator.sortingLayerName = "World";
            indicator.sortingOrder = WorldYSort2D.Order(landing.y);
            indicator.enabled = true;
            indicator.transform.localScale = Vector3.one;
            // Keep the larger interaction trigger while drawing a small panel icon.
            indicator.drawMode = SpriteDrawMode.Sliced;
            indicator.size = Vector2.one * .22f;
            exit.statusIndicator = indicator;
            string sheet = folder + "piezas.png";
            ImportCircuitSprite(sheet, SpriteImportMode.Multiple);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath(sheet));
            provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
            var slices = new List<SpriteRect>();
            for (int i = 0; i < ElevatorCircuit.Count; i++)
            {
                if (i == ElevatorCircuit.Missing) continue;
                string name = "Circuit_" + i.ToString("D2");
                slices.Add(new SpriteRect
                {
                    name = name, rect = new Rect(36 + i % 4 * 30, 105 - i / 4 * 30, 30, 30),
                    alignment = SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
                    spriteID = previous.TryGetValue(name, out var id) ? id : GUID.Generate()
                });
            }
            provider.SetSpriteRects(slices.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(slices.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            AssetImporter.GetAtPath(sheet).SaveAndReimport();
            var tiles = AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().ToDictionary(sprite => sprite.name);
            exit.elevatorTiles = Enumerable.Range(0, ElevatorCircuit.Count)
                .Select(i => i == ElevatorCircuit.Missing ? missing : tiles["Circuit_" + i.ToString("D2")]).ToArray();

            const string itemPath = "Assets/_Project/Data/Narrative/Items/elevator_circuit_piece.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(itemPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, itemPath);
            }
            item.id = ElevatorCircuit.ItemId;
            item.displayName = "Pieza del circuito";
            item.description = "Conector del panel del ascensor.";
            item.examineText = "Conecta las cuatro direcciones del circuito.";
            item.icon = missing;
            EditorUtility.SetDirty(item);
            exit.elevatorPiece = item;
            EditorUtility.SetDirty(exit);

            var pickup = zone.GetComponentsInChildren<ItemPickupInteractable>(true)
                .FirstOrDefault(candidate => candidate.ItemId == ElevatorCircuit.ItemId);
            if (pickup == null)
            {
                var owner = new GameObject("PiezaCircuitoAscensor");
                owner.transform.SetParent(zone.transform, false);
                pickup = owner.AddComponent<ItemPickupInteractable>();
            }
            pickup.transform.position = art.TransformPoint(Safe(new Vector3(17.2f, 10.3f, 0f),
                area: new Rect(15f, 9f, 5f, 4f)));
            pickup.gameObject.SetActive(true);
            pickup.item = item;
            pickup.itemId = item.id;
            pickup.pickupId = item.id;
            var trigger = EnsureComponent<CircleCollider2D>(pickup.gameObject);
            trigger.isTrigger = true;
            trigger.radius = .38f;
            var renderer = EnsureComponent<SpriteRenderer>(pickup.gameObject);
            renderer.enabled = true;
            renderer.sprite = missing;
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = "World";
            renderer.sortingOrder = WorldYSort2D.Order(pickup.transform.position.y);
            pickup.transform.localScale = Vector3.one * .18f;
            trigger.radius /= pickup.transform.localScale.x;
            var returnSpawn = zone.GetComponentsInChildren<SpawnPoint>(true).FirstOrDefault(s => s.SpawnId == "from_aula2_elevator");
            if (returnSpawn == null)
            {
                var owner = new GameObject("Spawn_from_aula2_elevator"); owner.transform.SetParent(zone.transform, false);
                returnSpawn = owner.AddComponent<SpawnPoint>();
                var spawnData = new SerializedObject(returnSpawn); spawnData.FindProperty("spawnId").stringValue = "from_aula2_elevator";
                spawnData.ApplyModifiedPropertiesWithoutUndo();
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "MapaCompleto/mapa_objetos.png");
            var doorRect = ElevatorMapIntegration.FindDoor(texture, false);
            Vector2 cabin = ElevatorMapIntegration.CabinCenter(doorRect, Ppu);
            returnSpawn.transform.position = art.TransformPoint(Safe(cabin));
            ElevatorMapIntegration.Configure(exit, art, doorRect, Ppu, false);
            if (!exit.elevatorAnimation.cabinBounds.Contains(returnSpawn.Position))
                throw new InvalidOperationException("The elevator arrival spawn must be inside the ground-floor cabin.");
        }

        static void ConfigureHallAccess(ZoneContext zone)
        {
            // Keep the campaign's CCTV and power requirements alongside the key.
            var exit = zone.GetComponentsInChildren<ZoneExitInteractable>(true)
                .Single(candidate => candidate.name == "AccesoHall");
            var data = new SerializedObject(exit);
            data.FindProperty("requiredItem").stringValue = "key";
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureKeyedPassage(ZoneContext zone, Transform art)
        {
            // The three turnstiles span the corridor between the two wall ends.
            var gate = zone.transform.Find("CarrilConLlave");
            if (gate == null)
            {
                gate = new GameObject("CarrilConLlave").transform;
                gate.SetParent(zone.transform, false);
            }
            gate.position = art.TransformPoint(new Vector3(244f / Ppu, 140.5f / Ppu, 0f));
            var barrier = EnsureComponent<BoxCollider2D>(gate.gameObject);
            barrier.enabled = true;
            barrier.size = new Vector2(4f, 47f) / Ppu;
            EnsureComponent<KeyedPassage>(gate.gameObject);

            foreach (var exit in zone.GetComponentsInChildren<ZoneExitInteractable>(true))
                if (exit.name == "AccesoHall" || exit.name == "ExitToEntrada")
                    exit.transform.position = art.TransformPoint(Safe(new Vector3(13.2f, 6.9f, 0f)));
            foreach (var spawn in zone.GetComponentsInChildren<SpawnPoint>(true))
            {
                if (zone.ZoneId == "hall" && spawn.SpawnId == "from_entrada")
                    spawn.transform.position = art.TransformPoint(Safe(new Vector3(13.9f, 6.9f, 0f)));
                else if (zone.ZoneId == "entrada" && spawn.SpawnId == "from_hall")
                    spawn.transform.position = art.TransformPoint(Safe(new Vector3(11.2f, 6.9f, 0f)));
            }
        }

        static Sprite ImportCircuitSprite(string path, SpriteImportMode mode)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = mode;
            importer.spritePixelsPerUnit = Ppu;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return mode == SpriteImportMode.Single ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : null;
        }

        static T EnsureComponent<T>(GameObject owner) where T : Component
        {
            // Unity's missing/destroyed components can retain a managed reference.
            // Use Unity's overloaded null check; ?? would incorrectly reuse that reference.
            var component = owner.GetComponent<T>();
            if (component == null) component = owner.AddComponent<T>();
            if (component == null)
                throw new InvalidOperationException("Could not add " + typeof(T).Name + " to " + owner.name);
            return component;
        }

        static Texture2D Import(string name)
        {
            string path = Folder + "MapaCompleto/" + name + ".png";
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = Ppu; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed; ti.isReadable = true;
            ti.npotScale = TextureImporterNPOTScale.None; ti.alphaIsTransparency = true;
            ti.maxTextureSize = 2048; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Sprite[] Slice(string name, IList<RectInt> rects, string prefix)
        {
            string path = Folder + "MapaCompleto/" + name + ".png";
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath(path)); provider.InitSpriteEditorDataProvider();
            var old = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var slices = rects.Select((r,i) => { string n = prefix + "_" + i.ToString("D3"); return new SpriteRect { name=n, rect=new Rect(r.x,r.y,r.width,r.height), pivot=new Vector2(.5f,0), alignment=SpriteAlignment.Custom, spriteID=old.TryGetValue(n,out var id)?id:GUID.Generate() }; }).ToArray();
            provider.SetSpriteRects(slices);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(slices.Select(s => new SpriteNameFileIdPair(s.name,s.spriteID)));
            provider.Apply(); AssetImporter.GetAtPath(path).SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }
        static SpriteRenderer Draw(Transform parent, Sprite sprite, RectInt r, string layer)
        {
            var go = new GameObject(sprite.name); go.transform.SetParent(parent,false);
            go.transform.localPosition = new Vector3(r.center.x/Ppu,r.y/Ppu,0);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite=sprite; sr.sharedMaterial=material;
            sr.sortingLayerName=layer; sr.sortingOrder=layer=="Ground"?0:WorldYSort2D.Order(r.y/Ppu);
            return sr;
        }
        static List<RectInt> Components(Color32[] pixels)
        {
            var seen = new bool[pixels.Length]; var result = new List<RectInt>(); var queue = new Queue<int>();
            for (int i=0;i<pixels.Length;i++)
            {
                if (seen[i] || pixels[i].a<128) continue;
                int minX=width,minY=height,maxX=0,maxY=0; seen[i]=true; queue.Enqueue(i);
                while(queue.Count>0)
                {
                    int p=queue.Dequeue(),x=p%width,y=p/width; minX=Math.Min(minX,x); minY=Math.Min(minY,y); maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y);
                    foreach(var d in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
                    { int nx=x+d.x,ny=y+d.y,n=ny*width+nx; if(nx>=0&&nx<width&&ny>=0&&ny<height&&!seen[n]&&pixels[n].a>=128){seen[n]=true;queue.Enqueue(n);} }
                }
                result.Add(new RectInt(minX,minY,maxX-minX+1,maxY-minY+1));
            }
            // Rectangular sprite slices must not render pixels from neighbouring components twice.
            for (int a = 0; a < result.Count; a++)
                for (int b = a + 1; b < result.Count; b++)
                    if (result[a].Overlaps(result[b]))
                    {
                        var first = result[a]; var second = result[b];
                        result[a] = new RectInt(Math.Min(first.xMin, second.xMin), Math.Min(first.yMin, second.yMin),
                            Math.Max(first.xMax, second.xMax)-Math.Min(first.xMin, second.xMin),
                            Math.Max(first.yMax, second.yMax)-Math.Min(first.yMin, second.yMin));
                        result.RemoveAt(b); a = -1; break;
                    }
            return result;
        }
        static bool IsSafe(int x,int y)
        {
            if(x<5||y<5||x>=width-5||y>=height-5) return false;

            for (int dy = -4; dy <= 4; dy++)
                for (int dx = -4; dx <= 4; dx++)
                    if (solid[(y + dy) * width + x + dx]) return false;
            return true;
        }
        static void FindReachable()
        {
            var seen=new bool[solid.Length]; var best=new List<int>(); var queue=new Queue<int>();
            for(int i=0;i<seen.Length;i++)
            {
                if(seen[i]||!IsSafe(i%width,i/width)) continue;
                var component=new List<int>(); seen[i]=true; queue.Enqueue(i);
                while(queue.Count>0)
                {
                    int p=queue.Dequeue(); component.Add(p);
                    foreach(int n in new[]{p-1,p+1,p-width,p+width})
                        if(n>=0&&n<seen.Length&&!seen[n]&&IsSafe(n%width,n/width)){seen[n]=true;queue.Enqueue(n);}
                }
                if(component.Count>best.Count) best=component;
            }
            if(best.Count==0) throw new InvalidOperationException("No walkable floor.");
            reachable=new bool[solid.Length]; foreach(int i in best) reachable[i]=true;
        }
        static Vector3 Safe(Vector3 desired, List<Vector3> occupied = null, Rect? area = null)
        {
            float distance=float.MaxValue; Vector3 best=default;
            for(int i=0;i<reachable.Length;i++) if(reachable[i])
            { var p=new Vector3((i%width)/Ppu,(i/width)/Ppu,desired.z); if (area.HasValue && !area.Value.Contains(p)) continue; if (occupied != null && occupied.Any(o => (p-o).sqrMagnitude < .64f)) continue; float d=(p-desired).sqrMagnitude; if(d<distance){distance=d;best=p;} }
            if (distance == float.MaxValue) throw new InvalidOperationException("No free walkable position.");
            return best;
        }
    }
}
#endif
