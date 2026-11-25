using System;
using System.IO;
using System.Linq;
using System.Reflection;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class CampaignPolishSetup
    {
        private const string Art = "Assets/_Project/Art/UI/MainMenu/";
        private const string Audio = "Assets/_Project/Audio/";
        private const string MixerFile = Audio + "EscapeUNIFRANZ.mixer";
        private const string PaperFile = "Assets/_Project/Art/UI/DocumentPaper.png";
        private const string SourceBackgrounds = Art + "Backgrounds/HallwayFlicker/";
        private const string PlayerBackgrounds = Art + "Backgrounds/HallwayCompressed/";
        private const string LetterMaterial = Art + "PirataOne Menu.mat";
        private static readonly Color Ink = new Color32(0xE9, 0xD8, 0xC6, 255);
        private static readonly Color Accent = new Color32(0xF0, 0x69, 0x59, 255);

        [MenuItem("Escape UNIFRANZ/Campaña/Aplicar presentación y rendimiento")]
        public static void Apply()
        {
            CreateMixer();
            ConfigureImporters();
            CreatePaper();
            CampaignReleaseSetup.Apply();
            foreach (var level in EditorBuildSettings.scenes.Where(scene => scene.enabled && scene.path.Contains("/Zones/")))
                CampaignReleaseSetup.Edit(level.path, ConfigureZoneLights);
            CampaignReleaseAudit.Run();
            CampaignPerformanceAudit.FinalSnapshot();
        }

        public static void ApplyAndPreview()
        {
            Apply();
            CampaignPerformanceAudit.RenderMenuPreviews();
        }

        public static void RefineMenuPreviews()
        {
            CampaignReleaseSetup.Edit(CampaignReleaseSetup.MainMenuPath, scene =>
                ConfigureMenu(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MainMenuController>()).Single()));
            CampaignReleaseAudit.Run();
            CampaignPerformanceAudit.FinalSnapshot();
            CampaignPerformanceAudit.RenderMenuPreviews();
        }

        public static void InstallSharedOptions()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Guarda las escenas abiertas.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Art + "PirataOne SDF.asset");
            var plate = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "CreditsPlate.png");
            try
            {
                CampaignReleaseSetup.Edit(CampaignReleaseSetup.MainMenuPath, scene =>
                {
                    var menu = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MainMenuController>(true)).Single();
                    var data = new SerializedObject(menu);
                    var card = (GameObject)data.FindProperty("optionsPanel").objectReferenceValue;
                    data.FindProperty("optionsBackButton").objectReferenceValue =
                        ConfigureOptions((RectTransform)card.transform, font, plate);
                    data.ApplyModifiedPropertiesWithoutUndo();
                });
                CampaignReleaseSetup.Edit(CampaignReleaseSetup.BootstrapPath, scene =>
                {
                    var flow = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameFlowView>(true)).Single();
                    var data = new SerializedObject(flow);
                    var pause = (GameObject)data.FindProperty("pauseRoot").objectReferenceValue;
                    var actions = pause.transform.Find("PauseCard/PauseActions");
                    string[] fields = { "pauseRestartButton", "pauseInventoryButton", "pauseMapButton", "pauseJournalButton" };
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var action = (Button)data.FindProperty(fields[i]).objectReferenceValue;
                        Place((RectTransform)action.transform, Vector2.one * .5f, Vector2.one * .5f,
                            new Vector2(0, 155 - i * 94), new Vector2(490, 84));
                    }
                    var optionsButton = actions.Find("PauseOptions")?.GetComponent<Button>();
                    if (optionsButton == null)
                        optionsButton = Command(actions, "PauseOptions", "Ajustes", font, plate, new Vector2(0, -221), 490);
                    var overlay = pause.transform.parent.Find("OptionsScreen") as RectTransform;
                    if (overlay == null) overlay = Rect(pause.transform.parent, "OptionsScreen", Vector2.zero, Vector2.zero);
                    overlay.anchorMin = Vector2.zero;
                    overlay.anchorMax = Vector2.one;
                    overlay.offsetMin = overlay.offsetMax = Vector2.zero;
                    Ensure<Image>(overlay.gameObject).color = new Color(.012f, .008f, .012f, .92f);
                    var card = overlay.Find("SharedOptions") as RectTransform ??
                        Rect(overlay, "SharedOptions", Vector2.zero, Vector2.zero);
                    var back = ConfigureOptions(card, font, plate);
                    card.gameObject.SetActive(true);
                    overlay.gameObject.SetActive(false);
                    data.FindProperty("optionsRoot").objectReferenceValue = overlay.gameObject;
                    data.FindProperty("pauseOptionsButton").objectReferenceValue = optionsButton;
                    data.FindProperty("optionsBackButton").objectReferenceValue = back;
                    data.ApplyModifiedPropertiesWithoutUndo();
                });
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void CreateMixer()
        {
            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerFile) != null) return;
            // Unity 6000.3 exposes mixer group authoring through its editor's native controller.
            // The generated mixer and group references are regular assets in the player.
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController", true);
            const BindingFlags members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var mixer = (AudioMixer)type.GetMethod("CreateMixerControllerAtPath", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { MixerFile });
            var master = (AudioMixerGroup)type.GetProperty("masterGroup", members).GetValue(mixer);
            var names = Enum.GetNames(typeof(VolumeBus));
            var groups = new AudioMixerGroup[names.Length];
            groups[0] = master;
            for (int i = 1; i < names.Length; i++)
            {
                groups[i] = (AudioMixerGroup)type.GetMethod("CreateNewGroup", members)
                    .Invoke(mixer, new object[] { names[i], false });
                type.GetMethod("AddChildToParent", members).Invoke(mixer, new object[] { groups[i], master });
            }

            var exposed = type.GetProperty("exposedParameters", members);
            var entryType = exposed.PropertyType.GetElementType();
            var entries = Array.CreateInstance(entryType, groups.Length);
            for (int i = 0; i < groups.Length; i++)
            {
                object entry = Activator.CreateInstance(entryType);
                var guid = groups[i].GetType().GetMethod("GetGUIDForVolume", members).Invoke(groups[i], null);
                entryType.GetField("guid", members).SetValue(entry, guid);
                entryType.GetField("name", members).SetValue(entry, GameOptions.Parameter((VolumeBus)i));
                entries.SetValue(entry, i);
                EditorUtility.SetDirty(groups[i]);
            }

            exposed.SetValue(mixer, entries);
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MixerFile, ImportAssetOptions.ForceUpdate);
        }

        internal static void RouteAudio(ProjectAudioHooks hooks)
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerFile);
            if (mixer == null) throw new InvalidOperationException("Falta el AudioMixer de campaña.");
            AudioMixerGroup Bus(VolumeBus bus) => mixer.FindMatchingGroups(bus.ToString()).Single(g => g.name == bus.ToString());
            hooks.mixer = mixer;
            hooks.musicBus = Bus(VolumeBus.Music);
            hooks.ambienceBus = Bus(VolumeBus.Ambience);
            hooks.sfxBus = Bus(VolumeBus.SFX);
            hooks.uiBus = Bus(VolumeBus.UI);
            hooks.voicesBus = Bus(VolumeBus.Voices);
            hooks.music.outputAudioMixerGroup = hooks.musicBus;
            hooks.effects.outputAudioMixerGroup = hooks.sfxBus;
            if (hooks.ambience != null) hooks.ambience.outputAudioMixerGroup = hooks.ambienceBus;
        }

        private static void ConfigureImporters()
        {
            for (int i = 1; i <= 4; i++)
            {
                string path = PlayerBackgrounds + $"Hallway_{i:00}.png";
                CreateBackgroundVariant(SourceBackgrounds + $"MenuHallway_Light_{i:00}.png", path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.filterMode = FilterMode.Bilinear;
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                standalone.overridden = true;
                standalone.format = TextureImporterFormat.BC7;
                standalone.compressionQuality = 100;
                importer.SetPlatformTextureSettings(standalone);
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.SaveAndReimport();
            }

            foreach (string name in new[] { "menu", "exploration", "tension", "chase", "core", "discovery", "victory", "defeat" })
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(Audio + $"Campaign/{name}.wav");
                var sample = importer.defaultSampleSettings;
                sample.loadType = AudioClipLoadType.Streaming;
                sample.compressionFormat = AudioCompressionFormat.Vorbis;
                sample.quality = .63f;
                importer.defaultSampleSettings = sample;
                importer.SaveAndReimport();
            }
        }

        private static void CreateBackgroundVariant(string source, string path)
        {
            if (File.Exists(path)) return;
            Directory.CreateDirectory(PlayerBackgrounds);
            var decoded = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!ImageConversion.LoadImage(decoded, File.ReadAllBytes(source)))
                throw new InvalidOperationException("No se pudo leer " + source);

            // Whole BC7 blocks at exactly 16:9, using a center crop of the original art.
            const int width = 1664, height = 936;
            var crop = new Texture2D(width, height, TextureFormat.RGB24, false);
            crop.SetPixels(decoded.GetPixels((decoded.width - width) / 2, (decoded.height - height) / 2, width, height));
            crop.Apply();
            File.WriteAllBytes(path, ImageConversion.EncodeToPNG(crop));
            Object.DestroyImmediate(decoded);
            Object.DestroyImmediate(crop);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigureZoneLights(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var roomFill = roots.SelectMany(root => root.GetComponentsInChildren<RoomLightingController>(true))
                .SelectMany(room => room.fillLights).Where(light => light != null).ToHashSet();
            foreach (var light in roots.SelectMany(root => root.GetComponentsInChildren<Light2D>(true)))
                light.shadowsEnabled = roomFill.Contains(light);
        }

        internal static void ConfigureMenu(MainMenuController menu)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Art + "PirataOne SDF.asset");
            if (font == null) throw new InvalidOperationException("Falta PirataOne SDF.");
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚÜÑáéíóúüñ¿¡0123456789 .,:;!?%/+-";
            string missing = new string(letters.Where(character => !font.HasCharacter((int)character)).ToArray());
            if (!string.IsNullOrEmpty(missing))
                throw new InvalidOperationException("Faltan caracteres en el atlas de PirataOne: " + missing);

            var plate = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "CreditsPlate.png");
            var data = new SerializedObject(menu);
            var main = (GameObject)data.FindProperty("mainPanel").objectReferenceValue;
            Button GetButton(string field) => (Button)data.FindProperty(field).objectReferenceValue;
            var buttons = new[] { GetButton("newGameButton"), GetButton("continueButton"), GetButton("optionsButton"),
                GetButton("controlsButton"), GetButton("creditsButton"), GetButton("quitButton") };
            var labels = new[] { "", "Continuar", "Opciones", "", "Créditos", "" };

            foreach (string name in new[] { "MenuRail", "MenuRailAlert", "TopSignalRail", "BottomSignalRail",
                         "SurveillanceScanline", "StatusReadout", "TopSignal", "MenuIndex", "ThreatRail",
                         "TopVignette", "BottomVignette", "LeftReadabilityShade" })
                SetInactive(menu.transform, name);

            var ambience = new SerializedObject(menu.GetComponent<MainMenuAmbienceController>());
            var backgroundMotion = (RectTransform)ambience.FindProperty("background").objectReferenceValue;
            var canvas = Ensure<Canvas>(backgroundMotion.gameObject);
            canvas.overrideSorting = false;
            using (var flicker = new SerializedObject(backgroundMotion.GetComponent<HallwayFlicker>()))
            {
                var lightingStates = flicker.FindProperty("lightingStates");
                lightingStates.arraySize = 4;
                for (int i = 0; i < lightingStates.arraySize; i++)
                    lightingStates.GetArrayElementAtIndex(i).objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<Sprite>(PlayerBackgrounds + $"Hallway_{i + 1:00}.png");
                var first = (Sprite)lightingStates.GetArrayElementAtIndex(0).objectReferenceValue;
                foreach (string key in new[] { "primaryImage", "blendImage" })
                    ((Image)flicker.FindProperty(key).objectReferenceValue).sprite = first;
                flicker.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (string name in new[] { "scanline", "alertPulse", "statusReadout" })
            {
                var property = ambience.FindProperty(name);
                if (property.objectReferenceValue is Component graphic) graphic.gameObject.SetActive(false);
                property.objectReferenceValue = null;
            }
            ambience.ApplyModifiedPropertiesWithoutUndo();

            var camera = menu.gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()).Single();
            camera.allowHDR = false;
            camera.allowMSAA = false;

            var title = (RectTransform)menu.GetComponentsInChildren<Transform>(true).First(t => t.name == "GraphicTitle");
            title.SetParent(main.transform, false);
            title.gameObject.SetActive(true);
            title.GetComponent<Image>().preserveAspect = true;
            Place(title, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -54f), new Vector2(780f, 167f));
            var subtitle = main.transform.Find("CampaignSubtitle") as RectTransform;
            var caption = subtitle == null ? Text(main.transform, "CampaignSubtitle", "Último turno", font, Vector2.zero, new Vector2(740, 44), 33) :
                subtitle.GetComponent<TMP_Text>();
            caption.color = Ink;
            Place(caption.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                new Vector2(0f, -226f), new Vector2(740f, 46f));

            foreach (Transform child in main.transform)
                if (child != title && child != caption.transform && !buttons.Any(button => button.transform == child))
                    child.gameObject.SetActive(false);

            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                button.gameObject.SetActive(true);
                Place((RectTransform)button.transform, new Vector2(0f, .5f), new Vector2(0f, .5f),
                    new Vector2(100f, 207f - i * 102f), new Vector2(480f, 92f));
                foreach (Transform child in button.transform) child.gameObject.SetActive(false);
                if (labels[i].Length > 0) SkinPlate(button, labels[i], font, plate, 53f);
                else
                {
                    var graphic = (Image)button.targetGraphic;
                    graphic.preserveAspect = false;
                    var states = button.spriteState;
                    states.disabledSprite = graphic.sprite;
                    button.spriteState = states;
                }
                button.navigation = new Navigation { mode = Navigation.Mode.Vertical };
                Ensure<MenuButtonFeedback>(button.gameObject);
            }
            buttons[1].interactable = false;
            buttons[1].GetComponent<MenuButtonFeedback>().label.color = new Color32(135, 113, 109, 255);

            var options = (GameObject)data.FindProperty("optionsPanel").objectReferenceValue;
            var optionsBack = ConfigureOptions((RectTransform)options.transform, font, plate);
            data.FindProperty("optionsBackButton").objectReferenceValue = optionsBack;
            var controls = (GameObject)data.FindProperty("controlsPanel").objectReferenceValue;
            var controlsBack = ConfigureControls((RectTransform)controls.transform, font, plate);
            data.FindProperty("controlsBackButton").objectReferenceValue = controlsBack;
            data.ApplyModifiedPropertiesWithoutUndo();
            var creditsBack = GetButton("creditsBackButton");
            SkinPlate(creditsBack, "Volver", font, plate, 32f);
        }

        private static Button ConfigureOptions(RectTransform card, TMP_FontAsset font, Sprite plate)
        {
            card.gameObject.SetActive(false);
            ClearChildren(card);
            Place(card, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1030, 690));
            Ensure<Image>(card.gameObject).color = new Color(.037f, .042f, .052f, .974f);
            Text(card, "Title", "Ajustes", font, new Vector2(0, 281), new Vector2(910, 78), 59).color = Accent;
            Ensure<Outline>(card.gameObject).effectColor = new Color32(107, 48, 49, 255);
            Text(card, "AudioHeading", "Sonido", font, new Vector2(-247, 212), new Vector2(410, 48), 40);
            Text(card, "VideoHeading", "Gráficos", font, new Vector2(247, 212), new Vector2(410, 48), 40);

            var view = Ensure<GameOptionsView>(card.gameObject);
            view.volumeSliders = new Slider[6];
            view.volumeLabels = new TMP_Text[6];
            for (int i = 0; i < view.volumeSliders.Length; i++)
            {
                float y = 147 - i * 53;
                view.volumeLabels[i] = Text(card, "VolumeLabel" + i, "", font,
                    new Vector2(-247, y + 20), new Vector2(360, 33), 29);
                view.volumeSliders[i] = VolumeSlider(card, "Volume" + i, new Vector2(-247, y - 8));
            }
            view.mute = Command(card, "Mute", "Sonido activo", font, plate, new Vector2(-247, -181), 375);
            view.muteLabel = view.mute.GetComponentInChildren<TMP_Text>(true);
            view.brightnessLabel = Text(card, "BrightnessLabel", "Brillo 50 %", font,
                new Vector2(247, 167), new Vector2(360, 33), 29);
            view.brightness = VolumeSlider(card, "Brightness", new Vector2(247, 135));
            view.flashes = Command(card, "Flashes", "Destellos normales", font, plate, new Vector2(247, 69), 375);
            view.vsync = Command(card, "VSync", "VSync activado", font, plate, new Vector2(247, -1), 375);
            view.frameLimit = Command(card, "FrameLimit", "60 FPS", font, plate, new Vector2(247, -71), 375);
            view.flashLabel = view.flashes.GetComponentInChildren<TMP_Text>(true);
            view.vsyncLabel = view.vsync.GetComponentInChildren<TMP_Text>(true);
            view.frameLimitLabel = view.frameLimit.GetComponentInChildren<TMP_Text>(true);
            view.confirmation = Text(card, "Confirmation", "", TMP_Settings.defaultFontAsset,
                new Vector2(0, -237), new Vector2(900, 30), 24);
            view.defaults = Command(card, "Defaults", "Restaurar valores", font, plate, new Vector2(-210, -286), 365);
            return Command(card, "Back", "Volver", font, plate, new Vector2(210, -286), 365);
        }

        private static Button ConfigureControls(RectTransform panel, TMP_FontAsset font, Sprite plate)
        {
            panel.gameObject.SetActive(false);
            ClearChildren(panel);
            var card = Rect(panel, "ControlsCard", Vector2.zero, new Vector2(1030, 650));
            card.gameObject.AddComponent<Image>().color = new Color(.037f, .042f, .052f, .974f);
            Text(card, "Title", "Controles", font, new Vector2(0, 253), new Vector2(910, 78), 59).color = Accent;
            var body = Text(card, "Actions",
                "WASD o flechas  ·  Moverse\nE  ·  Interactuar. Mantén cuando se indique.\nF  ·  Linterna       G  ·  Lanzar lata\nI  ·  Inventario       J  ·  Diario\nM  ·  Mapa       Tab  ·  Escáner\nEsc  ·  Pausa o cerrar el panel",
                TMP_Settings.defaultFontAsset, new Vector2(0, 14), new Vector2(820, 345), 35);
            body.lineSpacing = 26f;
            return Command(card, "ControlsBack", "Volver", font, plate, new Vector2(0, -236), 360);
        }

        private static Slider VolumeSlider(Transform parent, string name, Vector2 position)
        {
            var rect = Rect(parent, name, position, new Vector2(370, 36));
            rect.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0);
            var track = Rect(rect, "Track", Vector2.zero, new Vector2(370, 7)).gameObject.AddComponent<Image>();
            track.color = HorrorPalette.Steel;
            track.raycastTarget = false;
            var area = Rect(rect, "HandleArea", Vector2.zero, new Vector2(345, 14));
            var handle = Rect(area, "Handle", Vector2.zero, new Vector2(24, 23));
            var graphic = handle.gameObject.AddComponent<Image>();
            graphic.color = Accent;
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.handleRect = handle;
            slider.targetGraphic = graphic;
            return slider;
        }

        private static Button Command(Transform parent, string name, string title, TMP_FontAsset font, Sprite plate,
            Vector2 position, float width)
        {
            var rect = Rect(parent, name, position, new Vector2(width, 65));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.gameObject.AddComponent<Image>();
            SkinPlate(button, title, font, plate, 39f);
            return button;
        }

        private static void SkinPlate(Button button, string value, TMP_FontAsset font, Sprite plate, float size)
        {
            var image = (Image)button.targetGraphic;
            image.sprite = plate;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = colors.selectedColor = new Color32(255, 179, 166, 255);
            colors.pressedColor = new Color32(180, 83, 76, 255);
            colors.disabledColor = new Color32(153, 149, 156, 210);
            colors.fadeDuration = .09f;
            button.colors = colors;
            var label = button.transform.Find("GothicLabel")?.GetComponent<TMP_Text>();
            if (label == null)
            {
                foreach (Transform child in button.transform)
                    if (child.GetComponent<TMP_Text>() != null) child.gameObject.SetActive(false);
                label = Text(button.transform, "GothicLabel", value, font, Vector2.zero, ((RectTransform)button.transform).sizeDelta, size);
            }
            label.gameObject.SetActive(true);
            label.font = font;
            label.text = value;
            label.color = new Color32(220, 67, 54, 255);
            label.fontSharedMaterial = ButtonLettering(font);
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = size * .67f;
            label.fontSizeMax = size;
            Ensure<MenuButtonFeedback>(button.gameObject).label = label;
        }

        private static Material ButtonLettering(TMP_FontAsset font)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(LetterMaterial);
            if (material != null) return material;
            material = new Material(font.material) { name = "PirataOne Menu" };
            material.SetFloat("_OutlineWidth", .18f);
            material.SetColor("_OutlineColor", new Color32(24, 13, 15, 255));
            AssetDatabase.CreateAsset(material, LetterMaterial);
            return material;
        }

        private static void CreatePaper()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(PaperFile) != null) return;
            const int width = 256, height = 128;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            var random = new System.Random(15);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int distance = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                int shade = random.Next(0, 10) + Mathf.Max(0, 17 - distance);
                pixels[y * width + x] = new Color32((byte)(211 - shade), (byte)(192 - shade), (byte)(157 - shade), 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(PaperFile, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(PaperFile, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(PaperFile);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        internal static void ConfigureDocuments(Scene scene)
        {
            var view = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NarrativePanelView>(true)).Single();
            using var data = new SerializedObject(view);
            var body = (TMP_Text)data.FindProperty("body").objectReferenceValue;
            var surface = body.transform.parent.Find("DocumentSurface") as RectTransform;
            if (surface == null)
                surface = Rect(body.transform.parent, "DocumentSurface", new Vector2(0, 38), new Vector2(910, 347));
            surface.SetSiblingIndex(body.transform.GetSiblingIndex());
            if (surface.GetSiblingIndex() > body.transform.GetSiblingIndex()) surface.SetSiblingIndex(body.transform.GetSiblingIndex());
            var image = Ensure<Image>(surface.gameObject);
            image.raycastTarget = false;
            image.gameObject.SetActive(false);
            data.FindProperty("documentSurface").objectReferenceValue = image;
            data.FindProperty("paper").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(PaperFile);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInactive(Transform root, string name)
        {
            var match = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            if (match != null) match.gameObject.SetActive(false);
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static TMP_Text Text(Transform parent, string name, string value, TMP_FontAsset font,
            Vector2 position, Vector2 size, float fontSize)
        {
            var text = Rect(parent, name, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = Ink;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            return rect;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static T Ensure<T>(GameObject owner) where T : Component
        {
            var result = owner.GetComponent<T>();
            return result != null ? result : owner.AddComponent<T>();
        }
    }
}
