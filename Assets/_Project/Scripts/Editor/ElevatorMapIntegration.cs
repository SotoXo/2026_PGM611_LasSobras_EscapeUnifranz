#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class ElevatorMapIntegration
    {
        private const string Animations = "Assets/_Project/Art/Levels/animaciones/";
        [Serializable] private sealed class Sheet { public Frame[] frames; }
        [Serializable] private sealed class Frame { public int duration; }

        [MenuItem("Escape UNIFRANZ/Art/Integrate Animated Elevators and Aula 2")]
        public static void Build()
        {
            CompleteGroundFloorBuilder.Build();
            Aula2MapBuilder.Build();
            Verify();
            Debug.Log("ANIMATED_ELEVATORS_OK: ground-floor circuit unlocks Aula 2; both authored door animations integrated.");
        }

        public static void Verify()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string name in new[] { "Z00_Entrada", "Z01_Hall", "Z02_ARCA", "Z07_Aula2" })
                {
                    var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Zones/" + name + ".unity");
                    var zone = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<ZoneContext>()).Single();
                    bool upstairs = zone.ZoneId == "aula2";
                    var exit = zone.GetComponentsInChildren<ZoneExitInteractable>(true).Single(e => e.elevatorAnimation != null);
                    if (exit.TargetZoneId != (upstairs ? "entrada" : "aula2") ||
                        exit.TargetSpawnId != (upstairs ? "from_aula2_elevator" : "from_elevator"))
                        throw new InvalidOperationException("Incorrect elevator destination in " + name);
                    if (!upstairs && (exit.elevatorPanel == null || exit.elevatorPiece == null || exit.elevatorTiles.Length != 16))
                        throw new InvalidOperationException("Ground-floor elevator must keep its circuit puzzle.");
                    var animation = exit.elevatorAnimation;
                    if (animation.frames.Length != 10 || animation.frames.Any(f => f == null) ||
                        animation.frameSeconds.Length != 10 || animation.frameSeconds.Any(t => t <= 0) || animation.doorRenderer == null)
                        throw new InvalidOperationException("Incomplete authored door animation in " + name);
                    string spawn = upstairs ? "from_elevator" : "from_aula2_elevator";
                    if (!zone.TryGetSpawn(spawn, out var arrival, out string error)) throw new InvalidOperationException(error);
                    if (!animation.cabinBounds.Contains(arrival.Position)) throw new InvalidOperationException("Arrival must start inside the elevator cabin in " + name);
                }
                var catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalog>("Assets/_Project/Data/Zones/ZoneCatalog.asset");
                if (!catalog.TryResolveScene("aula2", out string path, out _) || !EditorBuildSettings.scenes.Any(s => s.enabled && s.path == path))
                    throw new InvalidOperationException("Aula 2 must be registered in both catalog and build settings.");
                Directory.CreateDirectory("reports");
                File.WriteAllText("reports/elevator-validation.txt", "PASS: four elevator scenes; ten non-null frames and positive JSON timings per elevator; circuit puzzle retained; destination/spawn pairs valid; Aula 2 registered for builds.\n");
            }
            finally
            {
                if (setup.Any(s => s.isLoaded && s.isActive && !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        public static RectInt FindDoor(Texture2D map, bool upstairs)
        {
            var reference = Read(Animations + (upstairs ? "elevador_nivel4/" : "elevador_planta_baja/") + "cerrado.png");
            var pixels = reference.GetPixels32();
            int left = reference.width, bottom = reference.height, right = 0, top = 0;
            for (int y = 0; y < reference.height; y++) for (int x = 0; x < reference.width; x++)
                if (pixels[y * reference.width + x].a >= 128)
                { left = Math.Min(left, x); bottom = Math.Min(bottom, y); right = Math.Max(right, x + 1); top = Math.Max(top, y + 1); }
            var source = map.GetPixels32(); int rw = right - left, rh = top - bottom;
            int best = int.MaxValue; var result = new RectInt();
            for (int y = 0; y <= map.height - rh; y++) for (int x = 0; x <= map.width - rw; x++)
            {
                int errors = 0;
                for (int j = 0; j < rh && errors < best; j++) for (int i = 0; i < rw && errors < best; i++)
                {
                    var a = pixels[(bottom + j) * reference.width + left + i];
                    if (a.a < 128) continue;
                    var b = source[(y + j) * map.width + x + i];
                    if (b.a < 128 || Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b) > 24) errors++;
                }
                if (errors < best) { best = errors; result = new RectInt(x, y, rw, rh); }
            }
            if (best > rw * rh * .06f) throw new InvalidOperationException("Cannot locate the authored elevator in " + map.name);
            return result;
        }

        public static void ClearDoor(Color32[] pixels, int width, RectInt door)
        {
            for (int y = door.yMin; y < door.yMax; y++) for (int x = door.xMin; x < door.xMax; x++)
                pixels[y * width + x] = new Color32();
        }

        public static Texture2D WithoutDoor(Texture2D source, RectInt door, string name = "ObjetosSinPuerta")
        {
            string folder = "Assets/_Project/Art/Levels/nivel4_aulas/NuevoMapa/Generated";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), "Generated");
            string path = folder + "/" + name + ".asset";
            var pixels = source.GetPixels32(); ClearDoor(pixels, source.width, door);
            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            texture.name = name; texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels); texture.Apply();
            var previous = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (previous == null) AssetDatabase.CreateAsset(texture, path);
            else { EditorUtility.CopySerialized(texture, previous); Object.DestroyImmediate(texture); texture = previous; }
            return texture;
        }

        public static void Configure(ZoneExitInteractable exit, Transform art, RectInt door, float ppu, bool upstairs)
        {
            string folder = Animations + (upstairs ? "elevador_nivel4/" : "elevador_planta_baja/");
            string path = folder + "sprites.png";
            Read(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.spriteImportMode = SpriteImportMode.Multiple; importer.spritePixelsPerUnit = ppu;
            importer.SaveAndReimport();
            // Crop the transparent authoring canvas, preserving the map's exact door footprint.
            var reference = Read(folder + "cerrado.png"); var rp = reference.GetPixels32();
            int left = 80, bottom = 80, right = 0, top = 0;
            for (int y = 0; y < 80; y++) for (int x = 0; x < 80; x++)
                if (rp[y * 80 + x].a >= 128)
                { left = Math.Min(left, x); bottom = Math.Min(bottom, y); right = Math.Max(right, x + 1); top = Math.Max(top, y + 1); }
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var old = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = Enumerable.Range(0, 10).Select(i => new SpriteRect {
                name = "Door_" + i.ToString("D2"), rect = new Rect(i * 80 + left, bottom, right - left, top - bottom),
                pivot = Vector2.zero, alignment = SpriteAlignment.Custom,
                spriteID = old.TryGetValue("Door_" + i.ToString("D2"), out var id) ? id : GUID.Generate()
            }).ToArray();
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            var timing = JsonUtility.FromJson<Sheet>(File.ReadAllText(folder + "sprites.json"));
            if (frames.Length != 10 || timing.frames.Length != 10) throw new InvalidOperationException("Expected ten authored elevator frames.");
            var oldDoor = art.Find("AnimatedElevatorDoor"); if (oldDoor != null) Object.DestroyImmediate(oldDoor.gameObject);
            var obj = new GameObject("AnimatedElevatorDoor"); obj.transform.SetParent(art, false);
            obj.transform.localPosition = new Vector3(door.x / ppu, door.y / ppu);
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = frames[0]; renderer.sortingLayerName = "World";
            renderer.sortingOrder = WorldYSort2D.Order(door.y / ppu);
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Levels/GroupWorldLit.mat");
            var animation = exit.GetComponent<ElevatorDoorAnimation>() ?? exit.gameObject.AddComponent<ElevatorDoorAnimation>();
            animation.doorRenderer = renderer; animation.frames = frames;
            animation.frameSeconds = timing.frames.Select(f => f.duration / 1000f).ToArray();
            animation.cabinCenter = art.TransformPoint(CabinCenter(door, ppu));
            Vector2 cabinMin = art.TransformPoint(new Vector3((door.x + 3f) / ppu, (door.y + 2f) / ppu));
            Vector2 cabinMax = art.TransformPoint(new Vector3((door.xMax - 3f) / ppu, (door.yMax - 2f) / ppu));
            animation.cabinBounds = Rect.MinMaxRect(cabinMin.x, cabinMin.y, cabinMax.x, cabinMax.y);
            exit.elevatorAnimation = animation;
        }

        public static Vector2 CabinCenter(RectInt door, float ppu) => new Vector2(door.center.x / ppu, (door.y + 5.5f) / ppu);

        private static Texture2D Read(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { AssetDatabase.ImportAsset(path); importer = (TextureImporter)AssetImporter.GetAtPath(path); }
            importer.textureType = TextureImporterType.Sprite; importer.isReadable = true; importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None; importer.alphaIsTransparency = true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
#endif
