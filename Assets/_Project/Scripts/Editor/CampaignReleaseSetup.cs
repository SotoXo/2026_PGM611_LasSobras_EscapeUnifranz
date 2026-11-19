using System;
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.World;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class CampaignReleaseSetup
    {
        internal const string MainMenuPath = "Assets/_Project/Scenes/UI/MainMenu.unity";
        internal const string BootstrapPath = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        private const string AudioPath = "Assets/_Project/Audio/Campaign/";

        [MenuItem("Escape UNIFRANZ/Campaña/Conectar audio y opciones")]
        public static void Apply()
        {
            var bindings = Enum.GetValues(typeof(AudioCue)).Cast<AudioCue>().Select(Binding).ToArray();
            Edit(MainMenuPath, scene =>
            {
                var menu = InScene<MainMenuController>(scene).Single();
                var audio = Ensure<ProjectAudioHooks>(menu.gameObject);
                if (audio.effects == null) audio.effects = NewSource(menu.transform, "MenuEffects");
                if (audio.music == null) audio.music = NewSource(menu.transform, "MenuMusic");
                audio.bindings = bindings.Where(b => b.cue == AudioCue.MusicMenu || b.cue == AudioCue.Button ||
                    b.cue == AudioCue.MenuFocus).ToArray();
                CampaignPolishSetup.RouteAudio(audio);
                EnsureListener(scene);
                CampaignPolishSetup.ConfigureMenu(menu);
            });

            Edit(BootstrapPath, scene =>
            {
                var campaignAudio = InScene<ProjectAudioHooks>(scene).Single();
                campaignAudio.bindings = bindings;
                CampaignPolishSetup.RouteAudio(campaignAudio);
                CampaignPolishSetup.ConfigureDocuments(scene);
                EnsureListener(scene);
                var view = InScene<GameFlowView>(scene).Single();
                var flow = new SerializedObject(view);
                var victory = ((GameObject)flow.FindProperty("victoryRoot").objectReferenceValue).transform;
                if (victory.Find("Epilogue") == null)
                {
                    var font = victory.GetComponentsInChildren<TMP_Text>(true).First().font;
                    var epilogue = Label(victory, "Epilogue",
                        "La puerta del laboratorio cedió. Por primera vez en la noche, el altavoz guardó silencio.",
                        font, new Vector2(0, 105), new Vector2(980, 74), 30);
                    epilogue.color = HorrorPalette.SpectralWhite;
                }
            });
            Debug.Log($"Campaña: {bindings.Length} eventos de audio conectados.");
        }

        [MenuItem("Escape UNIFRANZ/Campaña/Conectar arte de carritos")]
        public static void ApplyThreatArt()
        {
            const string root = "Assets/_Project/Scenes/Zones/";
            Edit(root + "Z00_Entrada.unity", scene =>
            {
                ConfigureMiniAuto(InScene<MiniAutoController>(scene).Single(), "garra", 0);
            });
            Edit(root + "Z03_Piso2.unity", scene =>
            {
                ConfigureMiniAuto(InScene<MiniAutoController>(scene).Single(), "rampa", 1);
                InstallShelter(InScene<ZoneContext>(scene).Single());
            });
            Edit(root + "Z04_Piso3.unity", scene =>
            {
                foreach (var robot in InScene<MiniAutoController>(scene))
                    ConfigureMiniAuto(robot, robot.name == "SurveillanceRobot" ? "garra" : "sierra",
                        robot.name == "SurveillanceRobot" ? 1 : 2);
            });
        }

        private static void InstallShelter(ZoneContext zone)
        {
            var parent = zone.transform.Find("NarrativeContent");
            if (parent.Find("Cobertura_Mostrador") != null) return;
            var spawn = zone.GetComponentsInChildren<SpawnPoint>(true)
                .Single(point => point.SpawnId == zone.DefaultCheckpointSpawnId);
            var solids = zone.GetComponentsInChildren<Collider2D>()
                .Where(collider => CampaignReleaseAudit.IsMapCollider(collider) &&
                    collider.GetComponentInParent<PatrolMover2D>() == null &&
                    collider.GetComponentInParent<DoorPanel2D>() == null &&
                    collider.GetComponentInParent<KeyedPassage>() == null).ToArray();
            var map = new CampaignReleaseAudit.WalkableGrid(zone.MinimapBounds, solids, new Vector2(.38f, .22f), new Vector2(0, .13f));
            if (map.Fill(spawn.Position) == 0) throw new InvalidOperationException("No hay paso hacia la cocina.");
            var spot = new GameObject("Cobertura_Mostrador");
            spot.transform.SetParent(parent, false);
            spot.transform.position = map.Nearest(new Vector2(3.92f, 4.28f));
            spot.AddComponent<ShelterInteractable>();
            spot.AddComponent<InteractableFeedback2D>();
            var trigger = spot.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = .55f;
        }

        private static void ConfigureMiniAuto(MiniAutoController robot, string style, int wear)
        {
            const string folder = "Assets/_Project/Art/Characters/Enemies/MiniAutos/";
            var node = robot.transform.Find("CarPixelArt");
            if (node == null)
            {
                node = new GameObject("CarPixelArt").transform;
                node.SetParent(robot.transform, false);
            }
            var sprite = Ensure<SpriteRenderer>(node.gameObject);
            sprite.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Levels/GroupWorldLit.mat");
            sprite.sortingLayerName = "World";
            sprite.sortingOrder = WorldYSort2D.Order(robot.transform.position.y);
            sprite.enabled = true;
            node.localPosition = Vector3.zero;
            node.localScale = Vector3.one;
            var sort = Ensure<WorldYSort2D>(node.gameObject);
            sort.enabled = true;

            var original = robot.GetComponent<SpriteRenderer>();
            if (original != null) original.enabled = false;
            var art = robot.transform.Find("GroupVisual")?.GetComponent<SpriteRenderer>();
            if (art != null) art.enabled = false;

            using var data = new SerializedObject(robot);
            data.FindProperty("carVisual").objectReferenceValue = sprite;
            data.FindProperty("movementArt").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + style + "_movimiento.png");
            data.FindProperty("verticalArt").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + style + "_vertical.png");
            data.FindProperty("impactArt").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + style + "_impacto.png");
            data.FindProperty("wear").enumValueIndex = wear;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ConnectGroundFloorAndThreatArt()
        {
            CampaignGroundFloor.Apply();
            ApplyThreatArt();
            CampaignReleaseAudit.Run();
        }

        private static AudioCueBinding Binding(AudioCue cue)
        {
            (string clip, float volume) = cue switch
            {
                AudioCue.ElectricalHum => ("current", 0.12f),
                AudioCue.HVAC => ("ventilation", 0.12f),
                AudioCue.BuildingAmbience => ("building", 0.086f),
                AudioCue.MusicMenu => ("menu", 0.72f),
                AudioCue.MusicExplore => ("exploration", 0.49f),
                AudioCue.MusicTension => ("tension", 0.42f),
                AudioCue.MusicChase => ("chase", 0.5f),
                AudioCue.MusicCore => ("core", 0.58f),
                AudioCue.MusicDiscovery => ("discovery", 0.48f),
                AudioCue.MusicVictory => ("victory", 0.42f),
                AudioCue.MusicDefeat => ("defeat", 0.56f),
                AudioCue.MenuFocus => ("focus", 0.26f),
                AudioCue.PaperOpen => ("paper", 0.35f),
                AudioCue.TerminalOpen => ("terminal", 0.35f),
                AudioCue.InventoryOpen or AudioCue.InventoryClose => ("inventory", 0.26f),
                AudioCue.AttackImpact => ("attack", 0.53f),
                AudioCue.FootstepFloor => ("steps_floor", 0.27f),
                AudioCue.FootstepTile => ("steps_tile", 0.24f),
                AudioCue.Switch or AudioCue.CableConnect or AudioCue.FlashlightOn or AudioCue.FlashlightOff => ("switch", 0.23f),
                AudioCue.Pickup or AudioCue.ObjectUsed => ("pickup", 0.28f),
                AudioCue.PuzzleSolved or AudioCue.DoorUnlock or AudioCue.Checkpoint or AudioCue.TutorialNotification => ("access_ok", 0.25f),
                AudioCue.ObjectError or AudioCue.CableError or AudioCue.TerminalError => ("access_error", 0.33f),
                AudioCue.Fluorescent or AudioCue.Electricity or AudioCue.LightRestore or AudioCue.TerminalBoot => ("power", 0.37f),
                AudioCue.ElectricBuzz or AudioCue.Flicker => ("flicker", 0.2f),
                AudioCue.Blackout or AudioCue.RobotOff => ("blackout", 0.35f),
                AudioCue.DoorLock => ("lock", 0.48f),
                AudioCue.DoorOpen or AudioCue.DoorClose or AudioCue.ElectronicDoor => ("door", 0.43f),
                AudioCue.RobotServo or AudioCue.RobotWheels or AudioCue.RobotPatrol or AudioCue.RobotRestart or AudioCue.CameraMotor => ("servo", 0.43f),
                AudioCue.RobotAlert or AudioCue.RobotDetection or AudioCue.CameraDetection => ("robot_alert", 0.52f),
                AudioCue.RobotChase => ("robot_chase", 0.58f),
                AudioCue.ElectronicInterference or AudioCue.CameraFeed or AudioCue.CameraSignalLost or AudioCue.TerrorInterference => ("interference", 0.28f),
                AudioCue.DistantImpact or AudioCue.MetalCreak or AudioCue.MetallicNoise or AudioCue.Stinger => ("distant_impact", 0.45f),
                AudioCue.RedAlarm => ("alarm", 0.53f),
                AudioCue.CanThrow or AudioCue.Breathing => ("can_throw", 0.23f),
                AudioCue.CanImpact => ("can_impact", 0.58f),
                AudioCue.FrancisBroadcast => ("francis_broadcast", 0.46f),
                AudioCue.FrancisFarewell => ("francis_farewell", 0.55f),
                AudioCue.Button or AudioCue.Interaction or AudioCue.MapOpen or AudioCue.MapClose => ("button", 0.19f),
                _ => throw new InvalidOperationException($"Falta el audio {cue}")
            };

            var asset = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + clip + ".wav");
            if (asset == null) throw new InvalidOperationException($"No se importó {clip}.wav");
            return new AudioCueBinding { cue = cue, clip = asset, volume = volume };
        }

        private static TMP_Text Label(Transform parent, string name, string text, TMP_FontAsset font,
            Vector2 position, Vector2 size, float fontSize)
        {
            var rect = Rect(parent, name, position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = HorrorPalette.SpectralWhite;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static T Ensure<T>(GameObject owner) where T : Component
        {
            var component = owner.GetComponent<T>();
            return component != null ? component : owner.AddComponent<T>();
        }

        private static AudioSource NewSource(Transform parent, string name)
        {
            var source = new GameObject(name).AddComponent<AudioSource>();
            source.transform.SetParent(parent, false);
            source.playOnAwake = false;
            return source;
        }

        private static void EnsureListener(Scene scene)
        {
            if (InScene<AudioListener>(scene).Any()) return;
            InScene<Camera>(scene).First(camera => camera.CompareTag("MainCamera")).gameObject.AddComponent<AudioListener>();
        }

        private static T[] InScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        internal static void Edit(string path, Action<Scene> edit)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            edit(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException($"No se pudo guardar {path}");
        }
    }
}
