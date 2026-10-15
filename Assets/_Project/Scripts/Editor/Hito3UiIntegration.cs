#if UNITY_EDITOR
using System;
using System.Linq;
using EscapeUNIFRANZ.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Aseprite;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EscapeUNIFRANZ.EditorTools
{
    /// <summary>Applies the team's UI artwork to existing views, preserving their button bindings.</summary>
    public static partial class Hito3UiIntegration
    {
        private const string Ui = "Assets/_Project/Art/UI/";
        private const string Menu = Ui + "MainMenu/";
        private const string MenuScene = "Assets/_Project/Scenes/UI/MainMenu.unity";
        private const string BootstrapScene = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        private static readonly Color Blood = new Color(0.85f, 0.13f, 0.12f);
        private static readonly Color Pale = new Color(0.94f, 0.88f, 0.85f);
        private static readonly Color Surface = new Color(0.055f, 0.045f, 0.05f, 0.97f);
        private const string Team =
            "<b>LAS SOBRAS</b>\n\n" +
            "<b>Limber Soto Cazu</b>\nProgramación, arquitectura y mecánicas\n\n" +
            "<b>Abril Michelle Cayllagua Marca</b>\nMenú principal y arte de escenarios\n\n" +
            "<b>Marco Antonio Calderon Rojas</b>\nGame Over, victoria y personaje\n\n" +
            "<b>Edson Teddy Tito Chuquimia</b>\nMenú de pausa\n\n" +
            "Programación Gráfica y Multimedia · UNIFRANZ";

        [MenuItem("Escape UNIFRANZ/UI/Integrate Hito 3 Artwork")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before integrating UI artwork.");

            EnsureFolder(Menu);
            EnsureFolder(Ui + "GameOver");
            EnsureFolder(Ui + "Victory");
            foreach (string name in new[] { "btn_jugar.aseprite", "btn_controles.aseprite",
                         "btn_nueva_partida.aseprite", "btn_salida.aseprite", "menu_botones.aseprite",
                         "PirataOne-Regular.ttf", "vista_previa.png", "paleta_sangre_gris.gpl" })
                Move("Assets/" + name, Menu + name);
            foreach (string name in new[] { "pantalla gameover.png", "Cursor gameOver.png" })
                Move("Assets/_Project/Art/" + name, Ui + "GameOver/" + name);
            foreach (string name in new[] { "PantallaVictoria.png", "CursorPantallaVictoria.png" })
                Move("Assets/_Project/Art/" + name, Ui + "Victory/" + name);

            foreach (string name in new[] { "btn_jugar", "btn_controles", "btn_nueva_partida",
                         "btn_salida", "menu_botones" })
            {
                var importer = (AsepriteImporter)AssetImporter.GetAtPath(Menu + name + ".aseprite");
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.generateModelPrefab = false;
                importer.generateAnimationClips = false;
                importer.SaveAndReimport();
            }

            ImportSprite(Menu + "vista_previa.png");
            ImportSprite(Menu + "CreditsPlate.png");
            ImportSprite(Ui + "GameOver/pantalla gameover.png");
            ImportSprite(Ui + "Victory/PantallaVictoria.png");
            // The supplied cursor PNGs contain a small pointer on a full-size transparent canvas.
            // Slice the occupied pixels in import metadata; keep the original files unchanged.
            ImportSprite(Ui + "GameOver/Cursor gameOver.png", new Rect(27, 941 - 522, 58, 67));
            ImportSprite(Ui + "Victory/CursorPantallaVictoria.png", new Rect(547, 941 - 694, 50, 52));

            TMP_FontAsset titleFont = LoadTitleFont();
            Scene original = SceneManager.GetActiveScene();
            UpdateScene(MenuScene, scene => ApplyMenu(scene, titleFont));
            UpdateScene(BootstrapScene, scene => ApplyOutcomes(scene, titleFont));
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            AssetDatabase.SaveAssets();
            Debug.Log("Hito 3 UI artwork integrated: Abril's main menu and Marco's existing outcome screens.");
        }

        private static void ApplyMenu(Scene scene, TMP_FontAsset font)
        {
            MainMenuController controller = Find<MainMenuController>(scene);
            var fields = new SerializedObject(controller);
            Transform canvas = controller.transform;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            Transform main = Ref<GameObject>(fields, "mainPanel").transform;
            TMP_Text title = main.Find("TitleEscape").GetComponent<TMP_Text>();
            SetText(title, "ESCAPE", font, 112, Pale);
            Position(title.rectTransform, new Vector2(0, 1), new Vector2(115, -86), new Vector2(720, 160), new Vector2(0, 1));
            TMP_Text institution = main.Find("TitleInstitution").GetComponent<TMP_Text>();
            SetText(institution, "UNIFRANZ", font, 72, Blood);
            institution.characterSpacing = 5;
            Position(institution.rectTransform, new Vector2(0, 1), new Vector2(120, -228), new Vector2(720, 110), new Vector2(0, 1));
            TMP_Text kicker = main.Find("Kicker").GetComponent<TMP_Text>();
            kicker.color = Blood;
            Position(kicker.rectTransform, new Vector2(0, 1), new Vector2(120, -68), new Vector2(760, 30), new Vector2(0, 1));
            main.Find("Subtitle").GetComponent<TMP_Text>().color = Pale;
            Position((RectTransform)main.Find("Subtitle"), new Vector2(0, 1), new Vector2(120, -350), new Vector2(760, 32), new Vector2(0, 1));

            Button play = Ref<Button>(fields, "newGameButton");
            Button controls = Ref<Button>(fields, "controlsButton");
            Button credits = Ref<Button>(fields, "creditsButton");
            Button quit = Ref<Button>(fields, "quitButton");
            SkinAuthoredButton(play, "btn_jugar", new Vector2(424, 60));
            SkinAuthoredButton(controls, "btn_controles", new Vector2(424, -80));
            SkinPlateButton(credits, "CRÉDITOS", font, Blood);
            Position((RectTransform)credits.transform, new Vector2(0, 0.5f), new Vector2(424, -220), new Vector2(616, 126));
            SkinAuthoredButton(quit, "btn_salida", new Vector2(424, -360));
            NavigateVertical(play, controls, credits, quit);
            Find<EventSystem>(scene).firstSelectedGameObject = play.gameObject;

            foreach (Image graphic in canvas.GetComponentsInChildren<Image>(true))
            {
                if (graphic.name == "Accent" || graphic.name == "TopSignalRail" ||
                    graphic.name == "MenuRail" || graphic.name == "MenuRailAlert") graphic.color = Blood;
            }
            Position((RectTransform)main.Find("MenuRail"), new Vector2(0, 0.5f), new Vector2(90, -150), new Vector2(3, 552));
            main.Find("Footer").GetComponent<TMP_Text>().color = Blood;

            Transform controlsCard = Ref<GameObject>(fields, "controlsPanel").transform.Find("ControlsCard");
            Position((RectTransform)controlsCard, Vector2.one * 0.5f, Vector2.zero, new Vector2(900, 800));
            SetText(controlsCard.Find("Title").GetComponent<TMP_Text>(), "CONTROLES", font, 48, Pale);
            controlsCard.Find("Title").GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 90);
            TMP_Text extra = Text(controlsCard, "AdditionalControls");
            SetText(extra, "I  INVENTARIO     J  DIARIO\nF  ENCENDER / APAGAR LINTERNA", null, 20, Pale);
            extra.alignment = TextAlignmentOptions.Center;
            Position(extra.rectTransform, Vector2.one * 0.5f, new Vector2(0, -220), new Vector2(760, 70));
            Button controlsBack = Ref<Button>(fields, "controlsBackButton");
            SkinPlateButton(controlsBack, "VOLVER", font, Blood);
            Position((RectTransform)controlsBack.transform, Vector2.one * 0.5f, new Vector2(0, -330), new Vector2(320, 66));
            Transform creditsCard = Ref<GameObject>(fields, "creditsPanel").transform.Find("CreditsCard");
            ApplyCredits(creditsCard, Ref<Button>(fields, "creditsBackButton"), font);
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(GraphicTitle))) ApplyGraphicTitle(scene);
        }

        private static void ApplyOutcomes(Scene scene, TMP_FontAsset font)
        {
            GameFlowView view = Find<GameFlowView>(scene);
            var fields = new SerializedObject(view);
            Transform defeat = Ref<GameObject>(fields, "defeatRoot").transform;
            Transform victory = Ref<GameObject>(fields, "victoryRoot").transform;
            Transform defeatCard = Ref<Button>(fields, "retryButton").transform.parent;
            Transform victoryCard = Ref<Button>(fields, "victoryMenuButton").transform.parent;
            Transform defeatLayout = OutcomeLayout(defeat, defeatCard, Ui + "GameOver/pantalla gameover.png");
            Transform victoryLayout = OutcomeLayout(victory, victoryCard, Ui + "Victory/PantallaVictoria.png");

            Button retry = Ref<Button>(fields, "retryButton");
            Button defeatMenu = Ref<Button>(fields, "defeatMenuButton");
            TMP_Text reason = Ref<TMP_Text>(fields, "defeatReason");
            KeepOnly(defeatCard, retry.transform, defeatMenu.transform, reason.transform);
            SkinHitbox(retry, new Rect(100, 433, 550, 112), Blood);
            SkinHitbox(defeatMenu, new Rect(950, 433, 680, 112), Blood);
            SetText(reason, "El sistema de seguridad neutralizó al jugador.", null, 20, Pale);
            reason.alignment = TextAlignmentOptions.Center;
            Pixels(reason.rectTransform, new Rect(350, 578, 1000, 44));
            TMP_Text hint = Text(defeatCard, "CheckpointHint");
            SetText(hint, "REINTENTAR DESDE CHECKPOINT", null, 18, new Color(0.94f, 0.4f, 0.4f));
            hint.alignment = TextAlignmentOptions.Center;
            Pixels(hint.rectTransform, new Rect(100, 544, 550, 28));
            NavigateHorizontal(retry, defeatMenu);
            Assign(fields, "defeatSelectionCursor", Marker(defeatLayout, Ui + "GameOver/Cursor gameOver.png"));

            Button victoryCredits = Ref<Button>(fields, "victoryCreditsButton");
            Button victoryMenu = Ref<Button>(fields, "victoryMenuButton");
            TMP_Text body = victoryCard.Find("Body").GetComponent<TMP_Text>();
            KeepOnly(victoryCard, victoryCredits.transform, victoryMenu.transform, body.transform);
            SetText(body, "Lograste escapar. La IA ha quedado aislada.", null, 23, new Color(0.7f, 1, 0.65f));
            body.alignment = TextAlignmentOptions.Center;
            Pixels(body.rectTransform, new Rect(350, 566, 1000, 48));
            SkinHitbox(victoryMenu, new Rect(610, 625, 570, 100), new Color(0.1f, 1, 0.05f));
            SkinPlateButton(victoryCredits, "VER CRÉDITOS", font, new Color(0.2f, 1, 0.1f));
            Pixels((RectTransform)victoryCredits.transform, new Rect(595, 765, 500, 102));
            NavigateVertical(victoryMenu, victoryCredits);
            Assign(fields, "victorySelectionCursor", Marker(victoryLayout, Ui + "Victory/CursorPantallaVictoria.png"));
            fields.ApplyModifiedPropertiesWithoutUndo();

            Transform credits = Ref<GameObject>(fields, "creditsRoot").transform.Find("CreditsCard");
            ApplyCredits(credits, Ref<Button>(fields, "creditsMenuButton"), font);
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(PauseArtwork))) ApplyPauseAndControls(scene, font);
        }

        private static Transform OutcomeLayout(Transform screen, Transform card, string path)
        {
            screen.GetComponent<Image>().color = Color.black;
            foreach (Transform child in screen)
                if (child != card && child.name != "AuthoredLayout") child.gameObject.SetActive(false);
            RectTransform layout = Rect(screen, "AuthoredLayout");
            AspectRatioFitter fit = layout.GetComponent<AspectRatioFitter>() ?? layout.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 1672f / 941f;
            Image image = Picture(layout, "MarcoArtwork");
            Stretch(image.rectTransform);
            image.sprite = Sprite(path);
            image.color = Color.white;
            image.preserveAspect = true;
            image.transform.SetAsFirstSibling();
            card.SetParent(layout, false);
            Stretch((RectTransform)card);
            Image surface = card.GetComponent<Image>();
            surface.color = Color.clear;
            surface.raycastTarget = false;
            return layout;
        }

        private static void ApplyCredits(Transform card, Button back, TMP_FontAsset font)
        {
            Position((RectTransform)card, Vector2.one * 0.5f, Vector2.zero, new Vector2(1050, 870));
            card.GetComponent<Image>().color = Surface;
            TMP_Text title = card.Find("Title").GetComponent<TMP_Text>();
            SetText(title, "CRÉDITOS", font, 48, Pale);
            title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 90);
            TMP_Text body = card.Find("Body").GetComponent<TMP_Text>();
            SetText(body, Team, null, 23, Pale);
            body.alignment = TextAlignmentOptions.Center;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 0;
            Position(body.rectTransform, Vector2.one * 0.5f, new Vector2(0, -5), new Vector2(900, 570));
            SkinPlateButton(back, "VOLVER AL MENÚ", font, Blood);
            Position((RectTransform)back.transform, Vector2.one * 0.5f, new Vector2(0, -365), new Vector2(390, 80));
        }

        private static void SkinAuthoredButton(Button button, string asset, Vector2 position)
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(Menu + asset + ".aseprite")
                .OfType<Sprite>().OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
            if (frames.Length != 3) throw new InvalidOperationException("Expected three authored UI states: " + asset);
            Image image = button.GetComponent<Image>();
            image.sprite = frames[0];
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = frames[1], selectedSprite = frames[1],
                pressedSprite = frames[2], disabledSprite = frames[0]
            };
            foreach (Transform child in button.transform) child.gameObject.SetActive(false);
            Position((RectTransform)button.transform, new Vector2(0, 0.5f), position, new Vector2(616, 126));
        }

        private static void SkinPlateButton(Button button, string label, TMP_FontAsset font, Color accent)
        {
            // CreditsPlate is a lossless export of Abril's metal/blood layers, with its text layer hidden.
            Image image = button.GetComponent<Image>();
            image.sprite = Sprite(Menu + "CreditsPlate.png");
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = colors.selectedColor = new Color(1.4f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.65f, 0.45f, 0.45f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            foreach (Transform child in button.transform) child.gameObject.SetActive(false);
            TMP_Text text = Text(button.transform, "Label");
            SetText(text, label, font, 70, accent);
            text.enableAutoSizing = true;
            text.fontSizeMin = 22;
            text.fontSizeMax = 70;
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(35, 5);
            text.rectTransform.offsetMax = new Vector2(-35, -5);
        }

        private static void SkinHitbox(Button button, Rect bounds, Color accent)
        {
            Image image = button.GetComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.clear;
            colors.highlightedColor = colors.selectedColor = new Color(accent.r, accent.g, accent.b, 0.15f);
            colors.pressedColor = new Color(accent.r, accent.g, accent.b, 0.3f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            foreach (Transform child in button.transform) child.gameObject.SetActive(false);
            Pixels((RectTransform)button.transform, bounds);
        }

        private static Image Marker(Transform parent, string path)
        {
            Image marker = Picture(parent, "SelectionCursor");
            marker.sprite = Sprite(path);
            marker.preserveAspect = true;
            marker.rectTransform.sizeDelta = new Vector2(50, 56);
            return marker;
        }

        private static void NavigateVertical(params Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length],
                    selectOnDown = buttons[(i + 1) % buttons.Length] };
        }

        private static void NavigateHorizontal(Button first, Button second)
        {
            first.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnRight = second, selectOnLeft = second, selectOnUp = second, selectOnDown = second };
            second.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnRight = first, selectOnLeft = first, selectOnUp = first, selectOnDown = first };
        }

        private static void ImportSprite(string path, Rect? slice = null)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing UI texture: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = slice.HasValue ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            if (slice.HasValue)
            {
                var factories = new SpriteDataProviderFactories();
                factories.Init();
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                var old = provider.GetSpriteRects().FirstOrDefault();
                var id = old != null ? old.spriteID : GUID.Generate();
                provider.SetSpriteRects(new[] { new SpriteRect { name = "Pointer", rect = slice.Value,
                    pivot = Vector2.one * 0.5f, alignment = SpriteAlignment.Center, spriteID = id } });
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                    .SetNameFileIdPairs(new[] { new SpriteNameFileIdPair("Pointer", id) });
                provider.Apply();
            }
            importer.SaveAndReimport();
        }

        private static TMP_FontAsset LoadTitleFont()
        {
            string path = Menu + "PirataOne SDF.asset";
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null && (font.material == null || font.atlasTextures.Any(atlas => atlas == null)))
            {
                AssetDatabase.DeleteAsset(path);
                font = null;
            }
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(Menu + "PirataOne-Regular.ttf"));
                font.name = "PirataOne SDF";
                AssetDatabase.CreateAsset(font, path);
            }
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            foreach (Texture2D atlas in font.atlasTextures)
                if (!AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
            if (!AssetDatabase.Contains(font.material)) AssetDatabase.AddObjectToAsset(font.material, font);
            font.TryAddCharacters(string.Concat(Enumerable.Range(32, 95).Select(c => (char)c)) + "ÁÉÍÓÚÜÑáéíóúüñ¿¡·");
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(font);
            return font;
        }

        private static void SetText(TMP_Text text, string value, TMP_FontAsset font, float size, Color color)
        {
            text.gameObject.SetActive(true);
            text.enabled = true;
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSharedMaterial = text.font.material;
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = false;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static void UpdateScene(string path, Action<Scene> apply)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            // Save existing in-memory scene edits before applying this focused update.
            if (scene.isDirty && !EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not preserve existing scene changes: " + path);
            apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Cannot save " + path);
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
        }

        private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
        private static T Ref<T>(SerializedObject fields, string name) where T : Object =>
            fields.FindProperty(name).objectReferenceValue as T ?? throw new InvalidOperationException("Missing UI binding: " + name);
        private static void Assign(SerializedObject fields, string name, Object value) => fields.FindProperty(name).objectReferenceValue = value;
        private static Sprite Sprite(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault()
            ?? throw new InvalidOperationException("Missing imported Sprite: " + path);
        private static void KeepOnly(Transform parent, params Transform[] keep)
        {
            foreach (Transform child in parent) child.gameObject.SetActive(keep.Contains(child));
        }
        private static RectTransform Rect(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) { child.gameObject.SetActive(true); return (RectTransform)child; }
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return (RectTransform)obj.transform;
        }
        private static Image Picture(Transform parent, string name)
        {
            RectTransform rect = Rect(parent, name);
            Image image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            return image;
        }
        private static TMP_Text Text(Transform parent, string name)
        {
            RectTransform rect = Rect(parent, name);
            return rect.GetComponent<TMP_Text>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
        private static void Position(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? Vector2.one * 0.5f;
            rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
        }
        private static void Pixels(RectTransform rect, Rect bounds)
        {
            rect.anchorMin = new Vector2(bounds.xMin / 1672f, 1 - bounds.yMax / 941f);
            rect.anchorMax = new Vector2(bounds.xMax / 1672f, 1 - bounds.yMin / 941f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = Vector2.one * 0.5f;
        }
        private static void EnsureFolder(string path)
        {
            path = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, split));
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }
        private static void Move(string source, string destination)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(destination))) return;
            string error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        }
    }
}
#endif
