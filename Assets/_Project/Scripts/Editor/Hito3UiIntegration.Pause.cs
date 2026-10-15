#if UNITY_EDITOR
using System;
using System.Linq;
using EscapeUNIFRANZ.UI;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.EditorTools
{
    public static partial class Hito3UiIntegration
    {
        private const string GraphicTitle = Menu + "titulo_escape_unifranz.aseprite";
        private const string PauseArtwork = Ui + "Pause/menu_pausa_controles.png";

        [MenuItem("Escape UNIFRANZ/UI/Complete Title and Pause (Edit Mode Only)")]
        public static void CompleteInterfaces()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("UI integration requires Edit Mode.");

            EnsureFolder(Ui + "Pause");
            Move("Assets/UI/titulo_escape_unifranz.aseprite", GraphicTitle);
            Move("Assets/_Project/Art/menu_pausa_controles.png", PauseArtwork);
            var importer = (AsepriteImporter)AssetImporter.GetAtPath(GraphicTitle);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.generateModelPrefab = false;
            importer.generateAnimationClips = false;
            importer.SaveAndReimport();
            ImportSprite(PauseArtwork);

            TMP_FontAsset font = LoadTitleFont();
            Scene original = SceneManager.GetActiveScene();
            UpdateScene(MenuScene, ApplyGraphicTitle);
            UpdateScene(BootstrapScene, scene => ApplyPauseAndControls(scene, font));
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            AssetDatabase.SaveAssets();
            Debug.Log("Hito 3: graphic title and Edson's pause UI saved in Edit Mode.");
        }

        private static void ApplyGraphicTitle(Scene scene)
        {
            var menu = new SerializedObject(Find<MainMenuController>(scene));
            Transform panel = Ref<GameObject>(menu, "mainPanel").transform;
            panel.Find("TitleEscape").gameObject.SetActive(false);
            panel.Find("TitleInstitution").gameObject.SetActive(false);

            Image title = Picture(panel, "GraphicTitle");
            title.sprite = Sprite(GraphicTitle);
            title.preserveAspect = true;
            float width = 450;
            Position(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -20),
                new Vector2(width, width * title.sprite.rect.height / title.sprite.rect.width), new Vector2(0.5f, 1));
            TMP_Text subtitle = panel.Find("Subtitle").GetComponent<TMP_Text>();
            subtitle.alignment = TextAlignmentOptions.Center;
            Position(subtitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -365),
                new Vector2(900, 34), new Vector2(0.5f, 1));
        }

        private static void ApplyPauseAndControls(Scene scene, TMP_FontAsset font)
        {
            var fields = new SerializedObject(Find<GameFlowView>(scene));
            Transform screen = Ref<GameObject>(fields, "pauseRoot").transform;
            Image scrim = screen.GetComponent<Image>();
            scrim.color = new Color(0.012f, 0.008f, 0.012f, 0.94f);
            scrim.raycastTarget = true;
            Transform card = screen.Find("PauseCard");
            Position((RectTransform)card, Vector2.one * 0.5f, Vector2.zero, new Vector2(1400, 820));
            card.GetComponent<Image>().color = Color.clear;
            Outline outline = card.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;

            Button resume = Ref<Button>(fields, "resumeButton");
            Button restart = Ref<Button>(fields, "pauseRestartButton");
            Button controls = Ref<Button>(fields, "pauseControlsButton");
            Button menu = Ref<Button>(fields, "pauseMenuButton");
            Button inventory = Ref<Button>(fields, "pauseInventoryButton");
            Button map = Ref<Button>(fields, "pauseMapButton");
            Button journal = Ref<Button>(fields, "pauseJournalButton");
            foreach (Transform child in card)
                if (child.name != "EdsonArtwork" && child.name != "PauseActions") child.gameObject.SetActive(false);

            // Keep Edson's entire original panel, including its three printed button labels.
            Image artwork = Picture(card, "EdsonArtwork");
            artwork.sprite = Sprite(PauseArtwork);
            artwork.preserveAspect = true;
            Position(artwork.rectTransform, Vector2.one * 0.5f, new Vector2(-300, 0), new Vector2(800, 720));
            artwork.transform.SetAsFirstSibling();
            PauseHitbox(resume, artwork.transform, new Rect(40, 57, 80, 17));
            PauseHitbox(controls, artwork.transform, new Rect(40, 80, 80, 17));
            PauseHitbox(menu, artwork.transform, new Rect(40, 103, 80, 17));
            TMP_Text caption = Text(card, "MenuDestination");
            SetText(caption, "SALIR AL MENÚ PRINCIPAL", null, 18, Pale);
            caption.alignment = TextAlignmentOptions.Center;
            Position(caption.rectTransform, Vector2.one * 0.5f, new Vector2(-300, -385), new Vector2(700, 30));

            RectTransform actions = Rect(card, "PauseActions");
            Position(actions, Vector2.one * 0.5f, new Vector2(400, 0), new Vector2(520, 720));
            TMP_Text heading = Text(actions, "Title");
            SetText(heading, "RECURSOS", font, 48, Blood);
            heading.alignment = TextAlignmentOptions.Center;
            Position(heading.rectTransform, Vector2.one * 0.5f, new Vector2(0, 300), new Vector2(500, 90));
            TMP_Text description = Text(actions, "Hint");
            SetText(description, "EL TIEMPO PERMANECE DETENIDO", null, 17, Pale);
            description.alignment = TextAlignmentOptions.Center;
            Position(description.rectTransform, Vector2.one * 0.5f, new Vector2(0, 235), new Vector2(500, 30));

            Button[] buttons = { restart, inventory, map, journal };
            string[] labels = { "REINICIAR CHECKPOINT", "INVENTARIO", "MAPA", "DIARIO" };
            for (int index = 0; index < buttons.Length; index++)
            {
                Button button = buttons[index];
                button.transform.SetParent(actions, false);
                button.gameObject.SetActive(true);
                SkinPlateButton(button, labels[index], font, Pale);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                label.fontSizeMax = 40;
                label.fontSize = 40;
                Position((RectTransform)button.transform, Vector2.one * 0.5f,
                    new Vector2(0, 120 - index * 120), new Vector2(490, 100));
            }
            NavigateVertical(resume, restart, controls, inventory, map, journal, menu);
            TMP_Text escape = Text(actions, "EscapeHint");
            SetText(escape, "ESC  ·  REANUDAR", null, 18, Pale);
            escape.alignment = TextAlignmentOptions.Center;
            Position(escape.rectTransform, Vector2.one * 0.5f, new Vector2(0, -335), new Vector2(500, 30));

            Transform controlsScreen = Ref<GameObject>(fields, "controlsRoot").transform;
            controlsScreen.GetComponent<Image>().color = scrim.color;
            controlsScreen.GetComponent<Image>().raycastTarget = true;
            Transform controlsCard = controlsScreen.Find("ControlsCard");
            Position((RectTransform)controlsCard, Vector2.one * 0.5f, Vector2.zero, new Vector2(1000, 820));
            controlsCard.GetComponent<Image>().color = Surface;
            Outline border = controlsCard.GetComponent<Outline>();
            if (border != null) border.effectColor = Blood;
            TMP_Text title = controlsCard.Find("Title").GetComponent<TMP_Text>();
            SetText(title, "CONTROLES", font, 48, Pale);
            Position(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(850, 90), new Vector2(0.5f, 1));
            // Earlier builders left the original instruction rows active behind the current table.
            foreach (Transform child in controlsCard)
                if (child.name == "Kicker" || child.name == "Subtitle" || child.name == "TopBorder" ||
                    child.name.StartsWith("Action", StringComparison.Ordinal) || child.name.StartsWith("Key", StringComparison.Ordinal))
                    child.gameObject.SetActive(false);

            string[] keys = { "WASD / FLECHAS", "E", "F", "I", "J", "TAB", "M", "ESC" };
            string[] instructions = { "Moverse", "Interactuar / usar objeto", "Linterna · requiere recogerla",
                "Inventario · seleccionar / examinar", "Diario · documentos leídos", "Escáner con línea de visión",
                "Mapa · requiere recuperar el plano", "Pausa / cerrar / volver" };
            for (int index = 0; index < keys.Length; index++)
            {
                RectTransform row = Rect(controlsCard, "Row_" + index);
                row.gameObject.SetActive(true);
                Image background = row.GetComponent<Image>();
                if (background != null) { background.color = new Color(0.12f, 0.075f, 0.075f); background.raycastTarget = false; }
                Position(row, Vector2.one * 0.5f, new Vector2(0, 220 - index * 54), new Vector2(880, 46));
                TMP_Text key = Text(row, "Key");
                SetText(key, keys[index], null, 19, Pale);
                key.alignment = TextAlignmentOptions.Left;
                Position(key.rectTransform, new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(250, 42), new Vector2(0, 0.5f));
                TMP_Text action = Text(row, "Action");
                SetText(action, instructions[index], null, 19, Pale);
                action.alignment = TextAlignmentOptions.Left;
                Position(action.rectTransform, new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(555, 42), new Vector2(1, 0.5f));
            }
            Button back = Ref<Button>(fields, "controlsBackButton");
            SkinPlateButton(back, "VOLVER A PAUSA", font, Pale);
            Position((RectTransform)back.transform, Vector2.one * 0.5f, new Vector2(0, -320), new Vector2(440, 90));
        }

        private static void PauseHitbox(Button button, Transform parent, Rect bounds)
        {
            button.transform.SetParent(parent, false);
            button.gameObject.SetActive(true);
            // SkinHitbox configures the existing Button without touching its listeners.
            SkinHitbox(button, bounds, Blood);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(bounds.xMin / 160f, 1 - bounds.yMax / 144f);
            rect.anchorMax = new Vector2(bounds.xMax / 160f, 1 - bounds.yMin / 144f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
