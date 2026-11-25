using System;
using System.Linq;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class PracticeSceneSetup
    {
        public const string ScenePath = "Assets/_Project/Scenes/Zones/Z00_Practica.unity";
        private const string Floor = "Assets/_Project/Art/Levels/PisoPractica.png";
        private const string Terminal = "Assets/_Project/Art/Levels/TerminalPractica.png";
        private const string Tiles = "Assets/_Project/Art/Environment/Tiles/";
        private const string Items = "Assets/_Project/Data/Narrative/Items/";

        public static void Install()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Guarda las escenas abiertas.");
                var setup = EditorSceneManager.GetSceneManagerSetup();
                try
                {
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    BuildRoom();
                    if (!EditorSceneManager.SaveScene(scene, ScenePath))
                        throw new InvalidOperationException("No se pudo guardar la sala de práctica.");
                }
                finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
            }
            RegisterScene();
        }

        public static void RefinePresentation()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Guarda las escenas abiertas.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                CampaignReleaseSetup.Edit(ScenePath, scene => ApplyPresentation(scene.GetRootGameObjects().Single().transform));
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void BuildRoom()
        {
            FinalUiAssets.WriteSprite(Floor, 448, 256, 32, Vector4.zero, (x, y) =>
            {
                if (x % 32 == 0 || y % 32 == 0) return new Color32(39, 47, 50, 255);
                if ((x == 41 || x == 406) && y > 20 && y < 235) return new Color32(123, 77, 42, 255);
                int tint = (x / 32 * 7 + y / 32 * 11) % 16;
                return new Color32((byte)(62 + tint), (byte)(69 + tint), (byte)(70 + tint), 255);
            });
            var root = new GameObject("Sala de calibración").transform;
            var zone = root.gameObject.AddComponent<ZoneContext>();
            var settings = new SerializedObject(zone);
            settings.FindProperty("zoneId").stringValue = EntranceTutorialController.PracticeZoneId;
            settings.FindProperty("displayName").stringValue = "SUBSUELO · CALIBRACIÓN";
            settings.FindProperty("defaultCheckpointSpawnId").stringValue = EntranceTutorialController.PracticeSpawnId;
            settings.FindProperty("minimapSize").vector2Value = new Vector2(14, 8);
            settings.ApplyModifiedPropertiesWithoutUndo();
            var spawn = Node(root, "Aparición", new Vector2(-5, -1.5f)).gameObject.AddComponent<SpawnPoint>();
            SetString(spawn, "spawnId", EntranceTutorialController.PracticeSpawnId);
            Draw(root, "Piso de servicio", Vector2.zero, new Vector2(14, 8), Floor, "Ground");
            AddWall(root, "Muro norte", new Vector2(0, 4), new Vector2(14, .6f));
            AddWall(root, "Muro sur", new Vector2(0, -4), new Vector2(14, .6f));
            AddWall(root, "Muro oeste", new Vector2(-7, 0), new Vector2(.6f, 8));
            AddWall(root, "Muro este", new Vector2(7, 0), new Vector2(.6f, 8));
            AddWall(root, "Separador del rincón oscuro", new Vector2(-1.1f, 2.55f), new Vector2(.45f, 2.4f));

            var flashlight = Draw(root, "Linterna de práctica", new Vector2(-3.4f, 0), new Vector2(.65f, .65f),
                "Assets/_Project/Art/Items/linterna.png", "World");
            var pickup = flashlight.gameObject.AddComponent<ItemPickupInteractable>();
            pickup.pickupId = "practica_linterna";
            pickup.item = AssetDatabase.LoadAssetAtPath<ItemData>(Items + "flashlight.asset");
            AddInteractionCollider(flashlight);

            var target = CreateRobot(root);
            var supplies = Draw(root, "Bandeja de latas", new Vector2(.1f, -.95f), new Vector2(.65f, .65f),
                "Assets/_Project/Art/Items/lata_naranja.png", "World");
            supplies.gameObject.AddComponent<PracticeStation>().target = target;
            AddInteractionCollider(supplies);
            var console = Draw(root, "Terminal de emergencia", new Vector2(5.3f, 2.3f), new Vector2(1.2f, .85f),
                Tiles + "pared_gris.png", "World");
            console.GetComponent<SpriteRenderer>().color = new Color32(111, 176, 169, 255);
            console.gameObject.AddComponent<PracticeStation>();
            AddInteractionCollider(console);

            MakeLight(root, "Luz ambiental", Light2D.LightType.Global, Vector2.zero, 0f, .09f,
                new Color32(191, 211, 212, 255));
            MakeLight(root, "Servicio", Light2D.LightType.Point, new Vector2(-4, -.7f), 3.8f, .55f,
                new Color32(244, 215, 166, 255));
            MakeLight(root, "Banco de pruebas", Light2D.LightType.Point, new Vector2(2.7f, -.3f), 3.3f, .45f,
                new Color32(255, 179, 140, 255));
            MakeLight(root, "Terminal", Light2D.LightType.Point, new Vector2(5.3f, 2.3f), 1.8f, .67f,
                new Color32(145, 247, 204, 255));
            ApplyPresentation(root);
        }

        private static void ApplyPresentation(Transform room)
        {
            FinalUiAssets.WriteSprite(Terminal, 30, 24, 32, Vector4.zero, (x, y) =>
            {
                if (x < 2 || x > 27 || y < 2 || y > 21 || (x + y < 6)) return Color.clear;
                if (x <= 3 || x >= 26 || y <= 3 || y >= 20) return new Color32(29, 39, 45, 255);
                if (x >= 5 && x <= 23 && y >= 10 && y <= 18)
                {
                    if (x >= 7 && x <= 21 && (y == 13 || y == 16) && x + (y % 6) <= 24)
                        return new Color32(108, 220, 168, 255);
                    return new Color32(21, 64, 60, 255);
                }
                if (y >= 5 && y <= 7 && x >= 7 && x <= 23 && x % 4 != 0)
                    return x > 20 ? new Color32(228, 159, 69, 255) : new Color32(50, 65, 67, 255);
                return new Color32(97, 112, 108, 255);
            });

            var terminal = room.Find("Terminal de emergencia").GetComponent<SpriteRenderer>();
            terminal.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Terminal);
            terminal.color = Color.white;
            SizeAsSingle(terminal, 1.15f);
            SizeAsSingle(room.Find("Linterna de práctica").GetComponent<SpriteRenderer>(), .7f);
            SizeAsSingle(room.Find("Bandeja de latas").GetComponent<SpriteRenderer>(), .62f);
            room.Find("Luz ambiental").GetComponent<Light2D>().intensity = .15f;
            foreach (var light in room.GetComponentsInChildren<Light2D>()) light.shadowsEnabled = false;
        }

        private static void SizeAsSingle(SpriteRenderer renderer, float size)
        {
            renderer.drawMode = SpriteDrawMode.Simple;
            Vector3 bounds = renderer.sprite.bounds.size;
            renderer.transform.localScale = Vector3.one * (size / Mathf.Max(bounds.x, bounds.y));
            var trigger = renderer.GetComponent<BoxCollider2D>();
            trigger.size = Vector2.one / renderer.transform.localScale.x;
        }

        private static MiniAutoController CreateRobot(Transform root)
        {
            var anchor = Node(root, "Unidad de calibración", new Vector2(3, -.83f));
            var rigid = anchor.gameObject.AddComponent<Rigidbody2D>();
            rigid.bodyType = RigidbodyType2D.Kinematic;
            var body = anchor.gameObject.AddComponent<BoxCollider2D>();
            body.size = new Vector2(.7f, .65f);
            var mover = anchor.gameObject.AddComponent<PatrolMover2D>();
            var sensor = Node(anchor, "Sensor sellado", Vector2.zero).gameObject.AddComponent<DetectionSensor2D>();
            sensor.enabled = false;
            sensor.GetComponent<CircleCollider2D>().enabled = false;
            var robot = anchor.gameObject.AddComponent<MiniAutoController>();
            var data = new SerializedObject(robot);
            data.FindProperty("mover").objectReferenceValue = mover;
            data.FindProperty("sensor").objectReferenceValue = sensor;
            data.FindProperty("maxHits").intValue = 3;
            data.ApplyModifiedPropertiesWithoutUndo();
            CampaignReleaseSetup.ConfigureMiniAuto(robot, "garra", 0);
            robot.GetComponentInChildren<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/Levels/nivel2_aulas/NuevoMapa/Generated/Robot_garra.asset");
            return robot;
        }

        private static void AddInteractionCollider(Transform obj)
        {
            var trigger = obj.gameObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = Vector2.one;
        }

        private static void AddWall(Transform root, string name, Vector2 pos, Vector2 size)
        {
            var wall = Draw(root, name, pos, size, Tiles + "pared_gris.png", "World");
            wall.gameObject.AddComponent<BoxCollider2D>().size = size;
        }

        private static Transform Draw(Transform parent, string name, Vector2 position, Vector2 size, string spritePath, string layer)
        {
            var obj = Node(parent, name, position);
            var sprite = obj.gameObject.AddComponent<SpriteRenderer>();
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            sprite.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Levels/GroupWorldLit.mat");
            sprite.sortingLayerName = layer;
            sprite.drawMode = SpriteDrawMode.Tiled;
            sprite.size = size;
            return obj;
        }

        private static void MakeLight(Transform root, string name, Light2D.LightType type, Vector2 position,
            float radius, float intensity, Color color)
        {
            var light = Node(root, name, position).gameObject.AddComponent<Light2D>();
            light.lightType = type;
            light.intensity = intensity;
            light.color = color;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * .25f;
            var data = new SerializedObject(light);
            var layers = data.FindProperty("m_ApplyToSortingLayers");
            layers.arraySize = SortingLayer.layers.Length;
            for (int i = 0; i < layers.arraySize; i++) layers.GetArrayElementAtIndex(i).intValue = SortingLayer.layers[i].id;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform Node(Transform parent, string name, Vector2 position)
        {
            var obj = new GameObject(name).transform;
            obj.SetParent(parent, false);
            obj.localPosition = position;
            return obj;
        }

        private static void SetString(UnityEngine.Object obj, string property, string value)
        {
            var data = new SerializedObject(obj);
            data.FindProperty(property).stringValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterScene()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalog>("Assets/_Project/Data/Zones/ZoneCatalog.asset");
            if (!catalog.TryResolveScene(EntranceTutorialController.PracticeZoneId, out _, out _))
            {
                var data = new SerializedObject(catalog);
                var zones = data.FindProperty("zones");
                var entry = zones.GetArrayElementAtIndex(zones.arraySize++);
                entry.FindPropertyRelative("zoneId").stringValue = EntranceTutorialController.PracticeZoneId;
                entry.FindPropertyRelative("scenePath").stringValue = ScenePath;
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
            if (!EditorBuildSettings.scenes.Any(scene => scene.path == ScenePath && scene.enabled))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath)
                    .Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
        }
    }
}
