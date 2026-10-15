#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.World;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Encounters;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Aseprite;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EscapeUNIFRANZ.EditorTools
{
    /// <summary>Editor-only integration of authored PNG layers; never enters Play Mode.</summary>
    public static class GroupArtIntegrationBuilder
    {
        private const string Art = "Assets/_Project/Art/";
        private const string Zones = "Assets/_Project/Scenes/Zones/";
        private const string Official = Art + "Characters/Player/Official/";
        private const string Generated = Official + "Generated/";
        private const string MaterialPath = Art + "Levels/GroupWorldLit.mat";
        private static Material lit;
        private static Sprite[] shadowFrames;
        private static Sprite[] lampFrames;
        private static HashSet<string> originallyLoaded;
        private static readonly string[] LevelNames = {
            "nivel1_planta_baja", "nivel2_piso1_cocina_simulacion", "nivel3_tecnologia", "nivel4_aulas" };
        private static readonly string[] SceneNames = {
            "Z00_Entrada", "Z01_Hall", "Z02_ARCA", "Z03_Piso2", "Z04_Piso3", "Z05_LabSoftware", "Z06_NucleoIA" };
        private static readonly int[] LevelForScene = { 0, 0, 0, 1, 2, 2, 3 };

        [Serializable] private class PixelRect
        {
            public int x, y, w, h;
            public string asset, color;
            public Vector2 Center => new Vector2((x + w * 0.5f) / 64f, (y + h * 0.5f) / 64f);
            public Rect Rect => new Rect(x / 64f, y / 64f, w / 64f, h / 64f);
        }
        [Serializable] private class LevelData
        {
            public string level;
            public int width, height, ppu;
            public PixelRect[] collisions, objects, triggers, characters, items, lights;
        }

        [MenuItem("Escape UNIFRANZ/Art/Integrate Group Assets (No Play Mode)")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("This integration only runs in Edit Mode.");
            var originalActive = SceneManager.GetActiveScene();
            originallyLoaded = new HashSet<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i); originallyLoaded.Add(scene.path);
                if (scene.isDirty && scene.path != "Assets/_Project/Scenes/UI/MainMenu.unity")
                    throw new InvalidOperationException("Save changes to the affected scene before integrating: " + scene.path);
            }
            try
            {
                AssetDatabase.Refresh();
                ConfigureSorting();
                ConfigureSourceImporters();
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                if (shader == null) throw new InvalidOperationException("The project's URP sprite-lit shader is missing.");
                lit = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (lit == null) { lit = new Material(shader); AssetDatabase.CreateAsset(lit, MaterialPath); }
                shadowFrames = SliceStrip(Art + "Characters/Enemies/Shadow/sombra_spritesheet_12x1.png", 12);
                lampFrames = SliceStrip(Art + "Lighting/luz_parpadeo_spritesheet_4x1.png", 4);
                var data = LevelNames.Select(ReadLevel).ToArray();
                var prefabs = data.Select(BuildLevelPrefab).ToArray();
                var controller = BuildOfficialAnimations(out Sprite idle);
                UpdatePlayer(controller, idle);
                var report = new List<string>();
                for (int i = 0; i < SceneNames.Length; i++)
                    IntegrateScene(i, data[LevelForScene[i]], prefabs[LevelForScene[i]], report);
                NarrativeCampaignBuilder.BuildAll();
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("reports");
                File.WriteAllLines("reports/group-art-placement.txt", report);
                Debug.Log("Group art integration completed in Edit Mode. All seven zone scenes saved; no gameplay or aesthetic review performed.");
            }
            finally { if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive); }
        }

        private static LevelData ReadLevel(string name) => JsonUtility.FromJson<LevelData>(
            File.ReadAllText(Art + "Levels/" + name + "/LevelGeometry.json"));

        [MenuItem("Escape UNIFRANZ/Art/Apply Horror Vision and Lighting Only")]
        public static void ApplyHorrorLightingOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Apply lighting in Edit Mode.");
            const string bootstrapPath = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
            var originalActive = SceneManager.GetActiveScene();
            originallyLoaded = new HashSet<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i); originallyLoaded.Add(s.path);
                if (s.path == bootstrapPath && s.isDirty)
                    throw new InvalidOperationException("Save Bootstrap changes before applying its lighting.");
            }
            lit = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            lampFrames = AssetDatabase.LoadAllAssetsAtPath(Art + "Lighting/luz_parpadeo_spritesheet_4x1.png")
                .OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (lampFrames.Length != 4) throw new InvalidOperationException("Expected four authored lamp frames.");
            try
            {
                var bootstrap = OpenScene(bootstrapPath);
                var player = bootstrap.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<PlayerVisionController>(true)).First();
                ConfigureHorrorVision(player.gameObject);
                EditorSceneManager.MarkSceneDirty(bootstrap); EditorSceneManager.SaveScene(bootstrap);
                CloseScene(bootstrap);
                foreach (string name in LevelNames)
                {
                    string path = Art + "Levels/" + name + "/" + name + ".prefab";
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try { ConfigureAuthoredLamps(root, name); PrefabUtility.SaveAsPrefabAsset(root,path); }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                AssetDatabase.SaveAssets();
                Debug.Log("Horror vision and four shared level lighting prefabs saved in Edit Mode. No gameplay review.");
            }
            finally { if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive); }
        }

        internal static void ConfigureHorrorVision(GameObject player)
        {
            var vision = player.GetComponent<PlayerVisionController>() ?? player.AddComponent<PlayerVisionController>();
            Set(vision, "visualController", player.GetComponent<PlayerVisualController>());
            Set(vision, "immediateRadius", 1.1f); Set(vision, "immediateIntensity", 0.38f);
            Set(vision, "forwardRange", 6.8f); Set(vision, "forwardAngle", 88f); Set(vision, "innerAngle", 60f);
            Set(vision, "forwardIntensity", 1.15f); Set(vision, "forwardFalloff", 0.55f);
            Set(vision, "immediateFalloff", 0.72f); Set(vision, "forwardOffset", 0.18f);
            Set(vision, "emitterOrigin", new Vector3(0f,0.3f,0f));
            vision.ApplyLightConfiguration();
            foreach (var light in player.GetComponentsInChildren<Light2D>(true))
                light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
        }

        private static void ConfigureAuthoredLamps(GameObject root, string levelName)
        {
            int map = Array.IndexOf(LevelNames, levelName);
            foreach (var light in root.GetComponentsInChildren<Light2D>(true))
            {
                if (!light.name.StartsWith("AuthoredLamp_",StringComparison.Ordinal)) continue;
                int index = TrailingNumber(light.name);
                light.intensity = index == 2 ? 0f : 0.32f;
                light.pointLightOuterAngle = 360f; light.pointLightInnerAngle = 360f;
                var flicker = light.GetComponent<GroupLampFlicker2D>();
                if (index != 0 && index != 3)
                { if (flicker != null) flicker.enabled = false; continue; }
                if (flicker == null) flicker = light.gameObject.AddComponent<GroupLampFlicker2D>();
                flicker.enabled = true;
                Set(flicker,"lamp",light); Set(flicker,"intensity",0.32f);
                Set(flicker,"phase",13.7f + map * 19.3f + index * 6.1f);
                Set(flicker,"onSeconds",index == 0 ? new Vector2(2.8f,7.2f) : new Vector2(8f,15f));
                Set(flicker,"offSeconds",index == 0 ? new Vector2(0.12f,0.45f) : new Vector2(0.08f,0.18f));
                Set(flicker,"shimmer",index == 0 ? 0.08f : 0.04f);
                Set(flicker,"shimmerSpeed",index == 0 ? 0.8f : 0.55f);
                Set(flicker,"failureProbability",index == 0 ? 0.75f : 0.4f);
                Set(flicker,"briefCutChance",index == 0 ? 0.06f : 0.02f);
                var sr = light.GetComponent<SpriteRenderer>(); if (sr != null) Set(flicker,"lampRenderer",sr);
                SetArray(flicker,"frames",lampFrames);
            }
        }

        private static void ConfigureSorting()
        {
            var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = manager.FindProperty("m_SortingLayers");
            foreach (string name in new[] { "Ground", "Decals", "World", "Characters", "Foreground", "Effects" })
            {
                bool found = false;
                for (int i = 0; i < layers.arraySize; i++)
                    found |= layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name;
                if (found) continue;
                int index = layers.arraySize; layers.InsertArrayElementAtIndex(index);
                var layer = layers.GetArrayElementAtIndex(index);
                layer.FindPropertyRelative("name").stringValue = name;
                layer.FindPropertyRelative("uniqueID").intValue = 18001000 + index;
                layer.FindPropertyRelative("locked").boolValue = false;
            }
            manager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSourceImporters()
        {
            foreach (string p in Directory.GetFiles(Art, "*.png", SearchOption.AllDirectories))
            {
                string path = p.Replace('\\', '/');
                if (path.Contains("/UI/")) continue;
                ConfigurePng(path);
            }
            // Keep original authoring files, but don't instantiate their generated models.
            foreach (string p in Directory.GetFiles(Art, "*.aseprite", SearchOption.AllDirectories))
            {
                string path = p.Replace('\\', '/');
                if (!path.Contains("/Levels/") && !path.Contains("/Environment/") &&
                    !path.Contains("/Items/") && !path.Contains("/Lighting/") &&
                    !path.Contains("/Official/") && !path.Contains("/Group/") && !path.Contains("/Enemies/")) continue;
                var importer = AssetImporter.GetAtPath(path) as AsepriteImporter;
                if (importer == null) continue;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = path.Contains("/Official/") ? 32f : 64f;
                importer.generateModelPrefab = false;
                importer.generateAnimationClips = false;
                importer.generateSpriteAtlas = false;
                importer.generatePhysicsShape = false;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigurePng(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("PNG importer missing: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = Vector2.zero;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings);
            }
            importer.SaveAndReimport();
        }

        private static Sprite[] Slice(string path, PixelRect[] rects, string prefix)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var old = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var slices = rects.Select((r, i) => new SpriteRect {
                name = prefix + "_" + i.ToString("D3"), rect = new Rect(r.x, r.y, r.w, r.h),
                pivot = new Vector2(0.5f, 0f), alignment = SpriteAlignment.Custom,
                spriteID = old.TryGetValue(prefix + "_" + i.ToString("D3"), out GUID id) ? id : GUID.Generate()
            }).ToArray();
            provider.SetSpriteRects(slices);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names.SetNameFileIdPairs(slices.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }

        private static Sprite[] SliceStrip(string path, int count)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int w = texture.width / count;
            return Slice(path, Enumerable.Range(0, count).Select(i => new PixelRect {
                x = i * w, y = 0, w = w, h = texture.height }).ToArray(), "Frame");
        }

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();

        private static SpriteRenderer Draw(Transform root, string name, Sprite sprite, Vector2 feet, string layer, int order = 0)
        {
            var obj = new GameObject(name); obj.transform.SetParent(root, false); obj.transform.localPosition = feet;
            var sr = obj.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sharedMaterial = lit;
            sr.sortingLayerName = layer; sr.sortingOrder = order;
            return sr;
        }

        private static GameObject BuildLevelPrefab(LevelData d)
        {
            string folder = Art + "Levels/" + d.level + "/";
            var preview = EditorSceneManager.NewPreviewScene();
            var root = new GameObject(d.level);
            SceneManager.MoveGameObjectToScene(root, preview);
            try
            {
                Draw(root.transform, "Suelo", LoadSprite(folder + "Layers/Suelo.png"), Vector2.zero, "Ground");
                Draw(root.transform, "Decals", LoadSprite(folder + "Layers/Decals.png"), Vector2.zero, "Decals");
                // Horizontal strips preserve the complete wall image while sharing Y depth with actors.
                var bands = new List<PixelRect>();
                for (int y = 0; y < d.height; y += 32)
                    bands.Add(new PixelRect { x = 0, y = y, w = d.width, h = Math.Min(32, d.height - y) });
                var walls = Slice(folder + "Layers/Paredes.png", bands.ToArray(), "Wall");
                for (int i = 0; i < walls.Length; i++)
                    Draw(root.transform, walls[i].name, walls[i], new Vector2(d.width / 128f, bands[i].y / 64f),
                        "World", WorldYSort2D.Order(bands[i].y / 64f));
                var props = Slice(folder + "Layers/Objetos.png", d.objects, "Prop");
                for (int i = 0; i < props.Length; i++)
                    Draw(root.transform, props[i].name, props[i],
                        new Vector2((d.objects[i].x + d.objects[i].w * 0.5f) / 64f, d.objects[i].y / 64f),
                        "World", WorldYSort2D.Order(d.objects[i].y / 64f));
                var collisionRoot = new GameObject("WorldCollision_Authored").transform;
                collisionRoot.SetParent(root.transform, false);
                for (int i = 0; i < d.collisions.Length; i++)
                {
                    var r = d.collisions[i]; var obj = new GameObject("Solid_" + i.ToString("D3"));
                    obj.transform.SetParent(collisionRoot, false); obj.transform.localPosition = r.Center;
                    var collider = obj.AddComponent<BoxCollider2D>(); collider.size = new Vector2(r.w / 64f, r.h / 64f);
                    ConfigureShadow(obj, collider.size);
                }
                foreach (var item in d.items)
                {
                    // Recreate only recognized real PNG items at guide locations; never render Items_guia.
                    Sprite sprite = LoadSprite(Art + "Items/" + item.asset + ".png");
                    Draw(root.transform, "ItemVisual_" + item.asset, sprite,
                        item.Center - new Vector2(sprite.bounds.size.x, sprite.bounds.size.y) * 0.5f,
                        "World", WorldYSort2D.Order(item.y / 64f));
                }
                for (int i = 0; i < d.lights.Length; i++)
                {
                    var obj = new GameObject("AuthoredLamp_" + i); obj.transform.SetParent(root.transform, false);
                    obj.transform.localPosition = d.lights[i].Center;
                    var light = obj.AddComponent<Light2D>(); light.lightType = Light2D.LightType.Point;
                    light.color = new Color(0.68f, 0.75f, 0.85f); light.intensity = i == 2 ? 0f : 0.32f;
                    light.pointLightOuterRadius = 1.7f; light.pointLightInnerRadius = 0.4f;
                    light.shadowsEnabled = true; light.shadowIntensity = 0.93f;
                    light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
                    if (i == 0)
                    {
                        var sr = obj.AddComponent<SpriteRenderer>(); sr.sprite = lampFrames[0]; sr.sharedMaterial = lit;
                        sr.sortingLayerName = "Effects";
                        var flicker = obj.AddComponent<GroupLampFlicker2D>();
                        Set(flicker, "lamp", light); Set(flicker, "lampRenderer", sr); SetArray(flicker, "frames", lampFrames);
                    }
                }
                ConfigureAuthoredLamps(root, d.level);
                return PrefabUtility.SaveAsPrefabAsset(root, folder + d.level + ".prefab");
            }
            finally { Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static void ConfigureShadow(GameObject obj, Vector2 size)
        {
            var caster = obj.AddComponent<ShadowCaster2D>(); caster.castsShadows = true; caster.selfShadows = false;
            var so = new SerializedObject(caster);
            so.FindProperty("m_ShadowCastingSource").intValue = 1; // ShapeEditor in the installed URP version.
            var path = so.FindProperty("m_ShapePath"); path.arraySize = 4;
            Vector2 half = size * 0.5f;
            Vector3[] points = { new Vector3(-half.x,-half.y), new Vector3(-half.x,half.y),
                new Vector3(half.x,half.y), new Vector3(half.x,-half.y) };
            for (int i = 0; i < 4; i++) path.GetArrayElementAtIndex(i).vector3Value = points[i];
            so.FindProperty("m_ShapePathHash").intValue = size.GetHashCode();
            var layers = so.FindProperty("m_ApplyToSortingLayers"); layers.arraySize = SortingLayer.layers.Length;
            for (int i = 0; i < layers.arraySize; i++) layers.GetArrayElementAtIndex(i).intValue = SortingLayer.layers[i].id;
            so.ApplyModifiedPropertiesWithoutUndo(); caster.Update();
        }

        private static AnimatorController BuildOfficialAnimations(out Sprite front)
        {
            Directory.CreateDirectory(Generated); AssetDatabase.Refresh();
            string path = Generated + "PlayerOfficial.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                AssetDatabase.CopyAsset(Art + "Characters/Player/Generated/PlayerDirectional.controller", path);
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            }
            var dirs = new Dictionary<string, string> { { "Front", "Sprite-delanteActual" },
                { "Back", "Sprite-detrasActual" }, { "Left", "IzquierdaActual" }, { "Right", "derechaActual" } };
            front = null;
            foreach (var pair in dirs)
            {
                string ase = Official + pair.Value + ".aseprite";
                var provider = (ISpriteEditorDataProvider)AssetImporter.GetAtPath(ase);
                provider.InitSpriteEditorDataProvider();
                var rects = provider.GetSpriteRects();
                foreach (var r in rects) { r.pivot = new Vector2(0.5f, 0f); r.alignment = SpriteAlignment.Custom; }
                provider.SetSpriteRects(rects); provider.Apply(); AssetImporter.GetAtPath(ase).SaveAndReimport();
                Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(ase).OfType<Sprite>().OrderBy(s => s.name).ToArray();
                if (sprites.Length < 3) throw new InvalidOperationException("Official direction has fewer than 3 frames: " + ase);
                if (pair.Key == "Front") front = sprites[0];
                foreach (string mode in new[] { "Idle", "Walk" })
                {
                    string name = mode + pair.Key; string clipPath = Generated + name + ".anim";
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
                    clip.name = name; clip.ClearCurves(); clip.frameRate = mode == "Walk" ? 8f : 1f;
                    Sprite[] frames = mode == "Walk" ? sprites : new[] { sprites[0] };
                    var keys = frames.Select((s, i) => new ObjectReferenceKeyframe { time = i / clip.frameRate, value = s }).ToList();
                    keys.Add(new ObjectReferenceKeyframe { time = frames.Length / clip.frameRate, value = frames[0] });
                    AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { type = typeof(SpriteRenderer),
                        path = "", propertyName = "m_Sprite" }, keys.ToArray());
                    var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip);
                    var state = controller.layers[0].stateMachine.states.First(s => s.state.name == name).state;
                    state.motion = clip; EditorUtility.SetDirty(state);
                }
            }
            EditorUtility.SetDirty(controller); return controller;
        }

        private static void UpdatePlayer(AnimatorController controller, Sprite idle)
        {
            var scene = OpenScene("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity");
            var movement = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerMovement2D>(true)).First();
            var player = movement.gameObject; Set(movement, "moveSpeed", 4.5f);
            var body = player.GetComponent<Rigidbody2D>(); body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation; body.linearDamping = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Art + "Characters/Player/PlayerFeet.physicsMaterial2D");
            if (material == null) { material = new PhysicsMaterial2D("PlayerFeet"); AssetDatabase.CreateAsset(material, Art + "Characters/Player/PlayerFeet.physicsMaterial2D"); }
            material.friction = 0f; material.bounciness = 0f; EditorUtility.SetDirty(material);
            var collider = player.GetComponent<CapsuleCollider2D>(); collider.size = new Vector2(0.38f,0.22f);
            collider.offset = new Vector2(0f,0.13f); collider.sharedMaterial = material;
            var visual = player.transform.Find("Visual"); visual.localScale = Vector3.one;
            visual.localPosition = Vector3.zero;
            var sr = visual.GetComponent<SpriteRenderer>(); sr.sprite = idle; sr.sharedMaterial = lit;
            sr.sortingLayerName = "World"; sr.sortingOrder = WorldYSort2D.Order(player.transform.position.y);
            visual.GetComponent<Animator>().runtimeAnimatorController = controller;
            Set(player.GetComponent<PlayerVisualController>(), "centerVisualOnPlayer", false);
            var sorting = visual.GetComponent<WorldYSort2D>() ?? visual.gameObject.AddComponent<WorldYSort2D>(); Set(sorting, "feet", player.transform);
            var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).First(); camera.orthographicSize = 4.5f;
            var follow = camera.GetComponent<PlayerCameraFollow2D>() ?? camera.gameObject.AddComponent<PlayerCameraFollow2D>();
            Set(follow, "target", player.transform); Set(follow, "smoothTime", 0.06f);
            foreach (var light in player.GetComponentsInChildren<Light2D>(true))
                light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
            ConfigureHorrorVision(player);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            CloseScene(scene);
        }

        private static void IntegrateScene(int index, LevelData d, GameObject prefab, List<string> report)
        {
            var scene = OpenScene(Zones + SceneNames[index] + ".unity");
            var zone = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ZoneContext>(true)).First();
            var root = zone.transform;
            // Keep old layout available for recovery; it cannot render or collide during gameplay.
            foreach (string name in new[] { "Environment", "WorldCollision", "PresentationDecor" })
            {
                var child = root.Find(name); if (child != null) child.gameObject.SetActive(false);
            }
            var previous = root.Find("GroupLevelArt"); if (previous != null) Object.DestroyImmediate(previous.gameObject);
            var level = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            level.name = "GroupLevelArt"; level.transform.SetParent(root, false);
            Set(zone, "minimapCenter", new Vector2(d.width / 128f, d.height / 128f));
            Set(zone, "minimapSize", new Vector2(d.width / 64f, d.height / 64f));
            foreach (var light in root.GetComponentsInChildren<Light2D>(true))
            {
                light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
                if (light.lightType == Light2D.LightType.Global) { light.intensity = 0.16f; light.color = new Color(0.26f,0.32f,0.42f); }
            }
            // Relocate existing behavior owners, including referenced patrol waypoints.
            var transforms = root.GetComponentsInChildren<Transform>(true).Where(t =>
                t != root && !t.IsChildOf(level.transform) && !InLegacyEnvironment(t, root)).ToArray();
            var oldPositions = transforms.ToDictionary(t => t, t => t.position);
            foreach (var t in transforms.OrderBy(Depth))
            {
                if (t.GetComponent<SpawnPoint>() != null || t.GetComponent<InteractableBehaviour>() != null ||
                    t.GetComponent<PatrolMover2D>() != null || t.GetComponent<TimedHazardArea2D>() != null ||
                    t.GetComponent<ExolegEncounterController>() != null || t.GetComponent<RobotArmsEncounterController>() != null ||
                    t.GetComponent<FrancisBossController>() != null || IsWaypoint(t))
                {
                    Vector2 desired = Placement(index, t, d, oldPositions[t]);
                    Vector2 position = SafePosition(d, desired);
                    t.position = new Vector3(position.x, position.y, 0f);
                    report.Add(SceneNames[index] + " / " + t.name + " = " + position.ToString("F3"));
                }
            }
            // Replace placeholder appearance without replacing its logic or physics hierarchy.
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.transform.IsChildOf(level.transform) || InLegacyEnvironment(renderer.transform, root)) continue;
                if (renderer.GetComponent<TimedHazardArea2D>() != null)
                { renderer.sortingLayerName = "Decals"; continue; } // Preserve phase telegraphs.
                renderer.enabled = false;
            }
            foreach (var text in root.GetComponentsInChildren<TextMeshPro>(true)) text.enabled = false;
            foreach (var interaction in root.GetComponentsInChildren<InteractableBehaviour>(true))
            {
                var box = interaction.GetComponent<BoxCollider2D>();
                if (box != null) { box.isTrigger = true; box.size = new Vector2(0.6f,0.6f); }
                // Doorways/checkpoints are already represented by authored map art.
                if (interaction is ZoneExitInteractable) continue;
                string art = interaction is ProgressionFlagInteractable || interaction is RobotArmsPanelInteractable
                    ? "Environment/Props/desk_pc.png" : "Items/bateria.png";
                ReplaceVisual(interaction.transform, LoadSprite(Art + art));
            }
            foreach (var mover in root.GetComponentsInChildren<PatrolMover2D>(true))
            {
                ReplaceVisual(mover.transform, shadowFrames[0]);
                var col = mover.GetComponent<BoxCollider2D>(); if (col != null) col.size = new Vector2(0.45f,0.35f);
                var oldCaster = mover.GetComponent<ShadowCaster2D>(); if (oldCaster != null) oldCaster.enabled = false;
            }
            // Arena triggers fit authored rooms; no huge graybox area stretches across walls.
            foreach (var arena in root.GetComponentsInChildren<ExolegEncounterController>(true))
                arena.GetComponent<BoxCollider2D>().size = new Vector2(5.5f,4f);
            foreach (var arena in root.GetComponentsInChildren<RobotArmsEncounterController>(true))
                arena.GetComponent<BoxCollider2D>().size = new Vector2(7f,4f);
            foreach (var arena in root.GetComponentsInChildren<FrancisBossController>(true))
                arena.GetComponent<BoxCollider2D>().size = new Vector2(3f,2f);
            foreach (var hazard in root.GetComponentsInChildren<TimedHazardArea2D>(true))
            {
                var box = hazard.GetComponent<BoxCollider2D>(); if (box != null) box.size = new Vector2(0.65f,2.4f);
                var sr = hazard.GetComponent<SpriteRenderer>(); if (sr != null) sr.size = new Vector2(0.65f,2.4f);
            }
            foreach (var box in root.GetComponentsInChildren<BoxCollider2D>(true))
            {
                if (box.transform.IsChildOf(level.transform) || InLegacyEnvironment(box.transform,root)) continue;
                if (!box.isTrigger && box.GetComponent<PatrolMover2D>() == null)
                {
                    // Decorative equipment uses the map's colliders, avoiding duplicate graybox solids.
                    if (box.GetComponent<InteractableBehaviour>() == null) box.enabled = false;
                }
                var caster = box.GetComponent<ShadowCaster2D>(); if (caster != null) caster.enabled = false;
            }
            ValidateReferences(zone, d);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            report.Add(SceneNames[index] + " -> " + d.level + "; authored colliders=" + d.collisions.Length);
            CloseScene(scene);
        }

        private static Scene OpenScene(string path)
        {
            var scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene); return scene;
        }
        private static void CloseScene(Scene scene)
        {
            if (!originallyLoaded.Contains(scene.path)) EditorSceneManager.CloseScene(scene,true);
        }

        private static void ReplaceVisual(Transform owner, Sprite sprite)
        {
            var old = owner.Find("GroupVisual"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var sr = Draw(owner, "GroupVisual", sprite, Vector2.zero, "World", WorldYSort2D.Order(owner.position.y));
            // Native PNG assets use bottom-left; place their canvas around the object's feet.
            sr.transform.localPosition = new Vector3(-sprite.bounds.center.x, -sprite.bounds.min.y, 0f);
            var sort = sr.gameObject.AddComponent<WorldYSort2D>(); Set(sort,"feet",owner);
        }

        private static bool InLegacyEnvironment(Transform t, Transform root)
        {
            foreach (string n in new[] { "Environment", "WorldCollision", "PresentationDecor" })
            { var p = root.Find(n); if (p != null && (t == p || t.IsChildOf(p))) return true; }
            return false;
        }
        private static int Depth(Transform t) { int d=0; while(t.parent != null) { d++; t=t.parent; } return d; }
        private static bool IsWaypoint(Transform t) => t.parent != null && t.parent.GetComponent<PatrolPath2D>() != null;

        private static Vector2 Placement(int scene, Transform t, LevelData d, Vector2 old)
        {
            Vector2 start = d.triggers.First(r => r.color == "255,0,255").Center;
            var spawn = t.GetComponent<SpawnPoint>();
            var exit = t.GetComponent<ZoneExitInteractable>();
            string id = spawn != null ? spawn.SpawnId : exit != null ? exit.TargetZoneId : "";
            if (scene < 3)
            {
                if (spawn != null)
                {
                    if (scene == 0 && id == "entrada_inicio") return start;
                    if (id.Contains("arca")) return new Vector2(4.5f,9.5f);
                    if (id.Contains("piso2")) return new Vector2(22f,11.8f);
                    if (scene == 2) return new Vector2(4.5f,9.5f);
                    if (id.Contains("hall") || id.Contains("entrada")) return new Vector2(10.4f,3.5f);
                    return start;
                }
                if (exit != null)
                    return scene == 2 ? new Vector2(4f,8.5f) : id == "piso2" ? new Vector2(22f,12f) : id == "arca"
                        ? new Vector2(4.5f,9.7f) : new Vector2(10.8f,3.5f);
                if (t.GetComponent<ProgressionFlagInteractable>() != null) return new Vector2(4.5f,10f);
            }
            if (scene == 3)
            {
                if (spawn != null) return id.Contains("piso3") ? new Vector2(13f,9.8f)
                    : id.Contains("antes") ? new Vector2(16f,5f) : start;
                if (exit != null) return id == "piso3" ? new Vector2(13f,10f) : new Vector2(11f,2.5f);
                if (t.GetComponent<ExolegEncounterController>() != null) return new Vector2(19f,7f);
                if (t.GetComponent<ExolegOverloadInteractable>() != null)
                    return new Vector2(17.5f + (TrailingNumber(t.name)-1)*1.6f,4.2f);
                if (t.GetComponent<TimedHazardArea2D>() != null)
                    return new Vector2(t.name.Contains("Left") ? 17.5f : t.name.Contains("Right") ? 21f : 19.2f,6f);
                if (t.GetComponent<PatrolMover2D>() != null || IsWaypoint(t))
                    return IsWaypoint(t) ? new Vector2(TrailingNumber(t.name) == 1 ? 7f : 5f,
                        TrailingNumber(t.name) == 2 ? 3.1f : 2.4f) : new Vector2(5f,2.4f);
            }
            if (scene == 4 || scene == 5)
            {
                if (spawn != null) return scene == 5 && id == "from_piso3" ? new Vector2(11f,3.5f)
                    : id.Contains("lab") ? new Vector2(11f,3.5f)
                    : id.Contains("nucleo") ? new Vector2(27f,2.5f) : id.Contains("antes") ? new Vector2(13f,2.5f) : start;
                if (exit != null) return id == "piso2" ? new Vector2(2f,2.5f)
                    : id == "nucleo_ia" ? new Vector2(27f,2.5f) : new Vector2(11f,3f);
                if (t.GetComponent<RobotDogNetworkNodeInteractable>() != null)
                    return new Vector2(t.name.EndsWith("A") ? 15.5f : 24.5f,4.5f);
                if (t.GetComponent<RobotArmsPanelInteractable>() != null) return new Vector2(2.5f,6f);
                if (t.GetComponent<RobotArmsEncounterController>() != null) return new Vector2(6f,6.7f);
                if (t.GetComponent<TimedHazardArea2D>() != null) return new Vector2(3.8f+(TrailingNumber(t.name)-1)*2f,7f);
                if (t.GetComponent<PatrolMover2D>() != null || IsWaypoint(t))
                {
                    bool mini = t.name.Contains("MiniAuto") || (t.parent != null && t.parent.name.Contains("MiniAuto"));
                    int point = IsWaypoint(t) ? TrailingNumber(t.name) : 0;
                    return new Vector2((mini ? 8f : 16f) + (point == 0 ? 0f : point == 1 ? 2f : 4f),2.5f);
                }
            }
            if (scene == 6)
            {
                if (spawn != null) return id.Contains("francis") ? new Vector2(14f,2.5f) : start;
                if (exit != null) return new Vector2(2f,2.5f);
                if (t.GetComponent<FrancisBossController>() != null) return new Vector2(14f,2.5f);
                if (t.GetComponent<FrancisSystemNodeInteractable>() != null)
                {
                    var so=new SerializedObject(t.GetComponent<FrancisSystemNodeInteractable>());
                    int phase=so.FindProperty("phase").intValue, step=so.FindProperty("stepIndex").intValue;
                    return new Vector2(18f+step*3.5f,2f+phase*0.55f);
                }
                if (t.GetComponent<PatrolMover2D>() != null || IsWaypoint(t))
                {
                    int point = IsWaypoint(t) ? TrailingNumber(t.name) : 0;
                    return new Vector2(point == 1 ? 24f : 21f,point == 2 ? 2.1f : 3f);
                }
                if (t.GetComponent<TimedHazardArea2D>() != null) return new Vector2(23f,2.7f);
            }
            return start;
        }
        private static int TrailingNumber(string name) => int.TryParse(name.Split('_').Last(),out int n) ? n : 1;

        private static Vector2 SafePosition(LevelData d, Vector2 desired)
        {
            // Geometry-only placement: allow a 0.38 x 0.22 feet capsule, not the full sprite silhouette.
            for (int ring = 0; ring < 240; ring++)
                for (int x = -ring; x <= ring; x++)
                    for (int y = -ring; y <= ring; y++)
                    {
                        if (ring > 0 && Math.Abs(x) != ring && Math.Abs(y) != ring) continue;
                        Vector2 p = desired + new Vector2(x,y)/16f;
                        Rect feet = new Rect(p.x-0.22f,p.y-0.02f,0.44f,0.3f);
                        if (p.x < 0.3f || p.y < 0.3f || p.x > d.width/64f-0.3f || p.y > d.height/64f-0.6f) continue;
                        if (!d.collisions.Any(r => r.Rect.Overlaps(feet))) return p;
                    }
            throw new InvalidOperationException("No walkable placement near " + desired + " in " + d.level);
        }

        private static void ValidateReferences(ZoneContext zone, LevelData d)
        {
            var spawns = zone.GetComponentsInChildren<SpawnPoint>(true);
            if (spawns.Select(s => s.SpawnId).Distinct().Count() != spawns.Length)
                throw new InvalidOperationException("Duplicate spawn IDs in " + zone.name);
            foreach (var s in spawns)
                if (Vector2.Distance(s.Position,SafePosition(d,s.Position)) > 0.001f)
                    throw new InvalidOperationException("Spawn overlaps authored collision: " + s.name);
            foreach (var behaviour in zone.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour == null) throw new InvalidOperationException("Missing script in " + zone.name);
        }

        private static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field);
            if (p == null) throw new InvalidOperationException("Missing serialized field " + field + " on " + target.name);
            if (value is Object obj) p.objectReferenceValue = obj;
            else if (value is float f) p.floatValue=f;
            else if (value is bool b) p.boolValue=b;
            else if (value is Vector2 v) p.vector2Value=v;
            else if (value is Vector3 v3) p.vector3Value=v3;
            else throw new InvalidOperationException("Unsupported field value.");
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetArray(Object target, string field, Sprite[] value)
        {
            var so = new SerializedObject(target); var p=so.FindProperty(field); p.arraySize=value.Length;
            for(int i=0;i<value.Length;i++) p.GetArrayElementAtIndex(i).objectReferenceValue=value[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
