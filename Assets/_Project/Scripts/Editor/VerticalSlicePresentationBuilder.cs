#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.EditorTools
{
    /// <summary>
    /// Idempotent presentation pass layered over the gameplay graybox builder.
    /// </summary>
    public static class VerticalSlicePresentationBuilder
    {
        private const string BootstrapPath = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        private const string TmpEssentialResourcesGuid = "ce4ff17ca867d2b48b5c8a4181611901";

        private static readonly string[] ZonePaths =
        {
            "Assets/_Project/Scenes/Zones/Z00_Entrada.unity",
            "Assets/_Project/Scenes/Zones/Z01_Hall.unity",
            "Assets/_Project/Scenes/Zones/Z02_ARCA.unity",
            "Assets/_Project/Scenes/Zones/Z03_Piso2.unity",
            "Assets/_Project/Scenes/Zones/Z04_Piso3.unity",
            "Assets/_Project/Scenes/Zones/Z05_LabSoftware.unity",
            "Assets/_Project/Scenes/Zones/Z06_NucleoIA.unity"
        };

        private static readonly Color Background = Hex("06101C");
        private static readonly Color Surface = Hex("0B1D2B");
        private static readonly Color Raised = Hex("102A3B");
        private static readonly Color Border = Hex("245166");
        private static readonly Color Cyan = Hex("39D7F2");
        private static readonly Color TextPrimary = Hex("F4F8FB");
        private static readonly Color TextMuted = Hex("9CB1BE");
        private static readonly Color Success = Hex("4DE38A");
        private static readonly Color Warning = Hex("FFB84A");
        private static readonly Color Danger = Hex("FF5364");
        private static readonly Color Disabled = Hex("526572");

        private static TMP_FontAsset font;
        private static Sprite uiSprite;

        [MenuItem("Escape UNIFRANZ/Presentación/Build Vertical Slice")]
        public static void BuildVerticalSlice()
        {
            GameplayAdvancedSceneBuilder.BuildAll();
        }

        public static void ApplyAll()
        {
            font = LoadOrCreateFont();
            for (int index = 0; index < ZonePaths.Length; index++)
            {
                ApplyZone(ZonePaths[index]);
            }

            ApplyBootstrap();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);
            Debug.Log("Vertical slice presentation pass completed.");
        }

        private static void ApplyZone(string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ZoneContext zone = UnityEngine.Object.FindFirstObjectByType<ZoneContext>(
                FindObjectsInactive.Include);
            if (zone == null)
            {
                throw new InvalidOperationException("Zone scene has no ZoneContext: " + path);
            }

            Transform environment = FindRecursive(zone.transform, "Environment");
            Transform collision = FindRecursive(zone.transform, "WorldCollision");
            if (environment == null || collision == null)
            {
                throw new InvalidOperationException("Zone roots are incomplete: " + path);
            }

            NormalizeWorldPalette(zone, environment, collision);
            BuildWorldDecor(environment, zone.ZoneId);
            RemoveOrConvertWorldLabels(zone.transform, environment);
            BuildCheckpointBeacons(zone.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void NormalizeWorldPalette(
            ZoneContext zone,
            Transform environment,
            Transform collision)
        {
            Transform background = FindRecursive(environment, "GrayboxBackground");
            SpriteRenderer backgroundRenderer = background != null
                ? background.GetComponent<SpriteRenderer>()
                : null;
            if (backgroundRenderer != null)
            {
                backgroundRenderer.color = zone.ZoneId == "nucleo_ia"
                    ? new Color(0.055f, 0.025f, 0.095f, 1f)
                    : Background;
            }

            SpriteRenderer[] walls = collision.GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < walls.Length; index++)
            {
                walls[index].color = Raised;
            }

            ZoneExitInteractable[] exits = zone.GetComponentsInChildren<ZoneExitInteractable>(true);
            for (int index = 0; index < exits.Length; index++)
            {
                SpriteRenderer renderer = exits[index].GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.82f);
                }
            }

            RespawnPoint2D[] checkpoints = zone.GetComponentsInChildren<RespawnPoint2D>(true);
            for (int index = 0; index < checkpoints.Length; index++)
            {
                SpriteRenderer renderer = checkpoints[index].GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = new Color(Success.r, Success.g, Success.b, 0.2f);
                }
            }
        }

        private static void BuildWorldDecor(Transform environment, string zoneId)
        {
            Transform root = FindOrCreateChild(environment, "PresentationDecor");
            DestroyChildren(root);

            Color grid = new Color(Cyan.r, Cyan.g, Cyan.b, 0.055f);
            for (int index = -3; index <= 3; index++)
            {
                CreateWorldRect(root, "GridV_" + index, new Vector2(index * 2f, 0f),
                    new Vector2(0.025f, 8.2f), grid, -19);
            }
            for (int index = -2; index <= 2; index++)
            {
                CreateWorldRect(root, "GridH_" + index, new Vector2(0f, index * 1.7f),
                    new Vector2(15.2f, 0.025f), grid, -19);
            }

            Color accent = zoneId == "nucleo_ia" ? Danger : Cyan;
            CreateWorldRect(root, "TopSignalBand", new Vector2(0f, 3.62f),
                new Vector2(13.8f, 0.1f), new Color(accent.r, accent.g, accent.b, 0.28f), -17);
            CreateWorldRect(root, "BottomSignalBand", new Vector2(0f, -3.62f),
                new Vector2(10.8f, 0.06f), new Color(accent.r, accent.g, accent.b, 0.14f), -17);
            CreateWorldRect(root, "LeftPanel", new Vector2(-5.9f, 2.5f),
                new Vector2(2.6f, 1.15f), new Color(Raised.r, Raised.g, Raised.b, 0.72f), -18);
            CreateWorldRect(root, "RightPanel", new Vector2(5.7f, -2.45f),
                new Vector2(3f, 1.25f), new Color(Raised.r, Raised.g, Raised.b, 0.72f), -18);
            CreateWorldRect(root, "LeftPanelAccent", new Vector2(-5.9f, 3.05f),
                new Vector2(2.6f, 0.06f), new Color(accent.r, accent.g, accent.b, 0.4f), -16);
            CreateWorldRect(root, "RightPanelAccent", new Vector2(5.7f, -1.85f),
                new Vector2(3f, 0.06f), new Color(accent.r, accent.g, accent.b, 0.4f), -16);
        }

        private static void RemoveOrConvertWorldLabels(Transform zoneRoot, Transform environment)
        {
            TextMesh[] labels = zoneRoot.GetComponentsInChildren<TextMesh>(true);
            for (int index = labels.Length - 1; index >= 0; index--)
            {
                TextMesh legacy = labels[index];
                if (legacy == null)
                {
                    continue;
                }

                if (legacy.transform.IsChildOf(environment) ||
                    legacy.name.IndexOf("CheckpointLabel", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    legacy.gameObject.SetActive(false);
                    UnityEngine.Object.DestroyImmediate(legacy);
                    continue;
                }

                string copy = legacy.text;
                Color color = legacy.color;
                GameObject gameObject = legacy.gameObject;
                UnityEngine.Object.DestroyImmediate(legacy);
                TextMeshPro replacement = GetOrAdd<TextMeshPro>(gameObject);
                replacement.font = font;
                replacement.text = copy;
                replacement.fontSize = 1.45f;
                replacement.alignment = TextAlignmentOptions.Center;
                replacement.textWrappingMode = TextWrappingModes.NoWrap;
                replacement.color = color;
                replacement.rectTransform.sizeDelta = new Vector2(4.2f, 1.1f);
                MeshRenderer renderer = replacement.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 40);
                }
            }

            TextMeshPro[] tmpLabels = zoneRoot.GetComponentsInChildren<TextMeshPro>(true);
            for (int index = 0; index < tmpLabels.Length; index++)
            {
                TextMeshPro label = tmpLabels[index];
                if (label != null && label.transform.IsChildOf(environment))
                {
                    label.gameObject.SetActive(false);
                }
            }
        }

        private static void BuildCheckpointBeacons(Transform zoneRoot)
        {
            RespawnPoint2D[] checkpoints = zoneRoot.GetComponentsInChildren<RespawnPoint2D>(true);
            for (int index = 0; index < checkpoints.Length; index++)
            {
                Transform checkpoint = checkpoints[index].transform;
                Transform label = checkpoint.Find("CheckpointLabel");
                if (label != null)
                {
                    label.gameObject.SetActive(false);
                }

                SpriteRenderer source = checkpoint.GetComponent<SpriteRenderer>();
                SpriteRenderer halo = CreateBeaconPart(checkpoint, "BeaconHalo", source,
                    new Vector2(0.9f, 0.9f), new Color(Success.r, Success.g, Success.b, 0.22f), 1);
                SpriteRenderer core = CreateBeaconPart(checkpoint, "BeaconCore", source,
                    new Vector2(0.28f, 0.28f), new Color(Success.r, Success.g, Success.b, 0.75f), 2);
                halo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                core.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
        }

        private static SpriteRenderer CreateBeaconPart(
            Transform parent,
            string name,
            SpriteRenderer source,
            Vector2 size,
            Color color,
            int orderOffset)
        {
            Transform child = FindOrCreateChild(parent, name);
            child.localPosition = new Vector3(0f, 0f, -0.05f * orderOffset);
            child.localScale = Vector3.one;
            SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(child.gameObject);
            renderer.sprite = source != null ? source.sprite : GetUiSprite();
            renderer.drawMode = SpriteDrawMode.Simple;
            Vector2 sourceSize = renderer.sprite != null ? renderer.sprite.bounds.size : Vector2.one;
            child.localScale = new Vector3(size.x / sourceSize.x, size.y / sourceSize.y, 1f);
            renderer.color = color;
            renderer.sortingOrder = (source != null ? source.sortingOrder : 1) + orderOffset;
            return renderer;
        }

        private static void ApplyBootstrap()
        {
            Scene scene = EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);
            GameRuntimeRoot runtime = UnityEngine.Object.FindFirstObjectByType<GameRuntimeRoot>(
                FindObjectsInactive.Include);
            if (runtime == null)
            {
                throw new InvalidOperationException("Bootstrap has no GameRuntimeRoot.");
            }

            Transform uiRoot = FindRecursive(runtime.transform, "UIRoot");
            if (uiRoot == null)
            {
                throw new InvalidOperationException("Bootstrap has no UIRoot.");
            }

            ConfigureCanvas(uiRoot);
            EnsureEventSystem(runtime.transform);
            ConvertRemainingLegacyUiText(uiRoot);

            RectTransform hudRoot = EnsureRect(uiRoot, "GameplayHudRoot");
            Stretch(hudRoot);
            BuildObjectiveHud(uiRoot);
            BuildPromptHud(uiRoot);
            MoveGameplayHud(uiRoot, hudRoot);
            StyleExistingHud(hudRoot);

            FlowUi flowUi = BuildFlowUi(uiRoot, hudRoot.gameObject);
            WireFlow(runtime, uiRoot, flowUi);

            Transform fade = FindRecursive(uiRoot, "FadeOverlay");
            hudRoot.SetAsFirstSibling();
            flowUi.Root.SetAsLastSibling();
            if (fade != null)
            {
                fade.SetAsLastSibling();
            }

            Image[] graphics = uiRoot.GetComponentsInChildren<Image>(true);
            for (int index = 0; index < graphics.Length; index++)
            {
                if (graphics[index].GetComponent<Button>() == null &&
                    (fade == null || graphics[index].transform != fade))
                {
                    graphics[index].raycastTarget = false;
                }
            }

            Button[] buttons = flowUi.Root.GetComponentsInChildren<Button>(true);
            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index].targetGraphic != null)
                {
                    buttons[index].targetGraphic.raycastTarget = true;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureCanvas(Transform uiRoot)
        {
            Canvas canvas = GetOrAdd<Canvas>(uiRoot.gameObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            GetOrAdd<GraphicRaycaster>(uiRoot.gameObject);
            CanvasScaler scaler = GetOrAdd<CanvasScaler>(uiRoot.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static void EnsureEventSystem(Transform runtime)
        {
            EventSystem existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>(
                FindObjectsInactive.Include);
            GameObject eventObject;
            if (existing != null)
            {
                eventObject = existing.gameObject;
            }
            else
            {
                eventObject = new GameObject("EventSystem", typeof(EventSystem));
            }

            eventObject.transform.SetParent(runtime, false);
            InputSystemUIInputModule module = GetOrAdd<InputSystemUIInputModule>(eventObject);
            module.AssignDefaultActions();
        }

        private static void BuildObjectiveHud(Transform uiRoot)
        {
            ObjectiveView view = uiRoot.GetComponentInChildren<ObjectiveView>(true);
            if (view == null)
            {
                throw new InvalidOperationException("Bootstrap has no ObjectiveView.");
            }

            RectTransform panel = EnsurePanel(view.transform, view.name, new Color(
                Surface.r, Surface.g, Surface.b, 0.94f));
            SetRect(panel, Vector2.up, Vector2.up, new Vector2(28f, -28f),
                new Vector2(520f, 94f), new Vector2(0f, 1f));
            AddBorder(panel, Cyan, new Vector2(5f, 94f), new Vector2(-257.5f, 0f));
            TMP_Text kicker = CreateText(panel, "ObjectiveKicker", "OBJETIVO ACTUAL", 13f,
                TextAlignmentOptions.TopLeft, Cyan);
            SetRect(kicker.rectTransform, Vector2.up, Vector2.up, new Vector2(24f, -13f),
                new Vector2(465f, 22f), new Vector2(0f, 1f));
            TMP_Text body = CreateText(panel, "ObjectiveBody", string.Empty, 20f,
                TextAlignmentOptions.TopLeft, TextPrimary);
            SetRect(body.rectTransform, Vector2.up, Vector2.up, new Vector2(24f, -37f),
                new Vector2(465f, 48f), new Vector2(0f, 1f));
            body.textWrappingMode = TextWrappingModes.Normal;
            SetObject(view, "root", panel.gameObject);
            SetObject(view, "label", body);
            Transform oldBody = FindRecursive(panel, "ObjectiveText");
            if (oldBody != null && oldBody != body.transform) oldBody.gameObject.SetActive(false);
        }

        private static void BuildPromptHud(Transform uiRoot)
        {
            InteractionPromptView view = uiRoot.GetComponentInChildren<InteractionPromptView>(true);
            if (view == null)
            {
                throw new InvalidOperationException("Bootstrap has no InteractionPromptView.");
            }

            // The legacy view component lives on UIRoot. Using view.transform as the
            // prompt panel makes InteractionPromptView.Hide disable the entire Canvas.
            // Keep the controller on UIRoot and bind it to a dedicated child instead.
            Image strayRootImage = uiRoot.GetComponent<Image>();
            if (strayRootImage != null)
            {
                UnityEngine.Object.DestroyImmediate(strayRootImage);
            }

            Transform strayAccent = FindDirectChild(uiRoot, "AccentBorder");
            if (strayAccent != null)
            {
                UnityEngine.Object.DestroyImmediate(strayAccent.gameObject);
            }

            Transform strayBody = FindDirectChild(uiRoot, "PromptBody");
            if (strayBody != null)
            {
                UnityEngine.Object.DestroyImmediate(strayBody.gameObject);
            }

            Transform promptTransform = FindRecursive(uiRoot, "InteractionPrompt") ??
                FindOrCreateChild(uiRoot, "InteractionPrompt");
            RectTransform panel = EnsurePanel(promptTransform, "InteractionPrompt", new Color(
                Surface.r, Surface.g, Surface.b, 0.96f));
            SetRect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 36f), new Vector2(540f, 58f), new Vector2(0.5f, 0f));
            AddBorder(panel, Cyan, new Vector2(540f, 3f), new Vector2(0f, 27.5f));
            TMP_Text body = CreateText(panel, "PromptBody", string.Empty, 20f,
                TextAlignmentOptions.Center, TextPrimary);
            Stretch(body.rectTransform, 14f);
            SetObject(view, "promptRoot", panel.gameObject);
            SetObject(view, "promptLabel", body);
            Transform oldBody = FindRecursive(panel, "PromptText");
            if (oldBody != null && oldBody != body.transform) oldBody.gameObject.SetActive(false);
        }

        private static void MoveGameplayHud(Transform uiRoot, RectTransform hudRoot)
        {
            string[] names =
            {
                "InteractionPrompt", "Objective", "MessageToast", "BossPhase",
                "CampusMap", "ZoneTitle", "ScannerStatus", "Minimap", "ObjectiveMarker"
            };

            for (int index = 0; index < names.Length; index++)
            {
                Transform item = FindRecursive(uiRoot, names[index]);
                if (item != null && item != hudRoot)
                {
                    item.SetParent(hudRoot, false);
                }
            }
        }

        private static void StyleExistingHud(Transform hudRoot)
        {
            StylePanel(hudRoot, "MessageToast", new Color(Surface.r, Surface.g, Surface.b, 0.96f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f),
                new Vector2(560f, 58f), new Vector2(0.5f, 1f), 20f);
            StylePanel(hudRoot, "ZoneTitle", new Color(Surface.r, Surface.g, Surface.b, 0.92f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f),
                new Vector2(520f, 64f), new Vector2(0.5f, 1f), 27f);
            StylePanel(hudRoot, "BossPhase", new Color(Danger.r * 0.32f, 0.025f, 0.055f, 0.96f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f),
                new Vector2(620f, 68f), new Vector2(0.5f, 1f), 22f);
            StylePanel(hudRoot, "ScannerStatus", new Color(Surface.r, Surface.g, Surface.b, 0.94f),
                Vector2.one, Vector2.one, new Vector2(-28f, -260f),
                new Vector2(304f, 38f), Vector2.one, 15f);

            Transform minimap = FindRecursive(hudRoot, "Minimap");
            if (minimap != null)
            {
                RectTransform panel = (RectTransform)minimap;
                Image image = GetOrAdd<Image>(panel.gameObject);
                image.color = new Color(Surface.r, Surface.g, Surface.b, 0.96f);
                SetRect(panel, Vector2.one, Vector2.one, new Vector2(-28f, -28f),
                    new Vector2(304f, 224f), Vector2.one);
                TMP_Text title = FindRecursive(panel, "MinimapTitle")?.GetComponent<TMP_Text>();
                if (title != null)
                {
                    title.text = "MAPA LOCAL";
                    title.fontSize = 15f;
                    title.color = Cyan;
                    SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -18f), new Vector2(272f, 24f), new Vector2(0.5f, 1f));
                }
                TMP_Text zone = FindRecursive(panel, "MinimapZone")?.GetComponent<TMP_Text>();
                if (zone != null)
                {
                    zone.fontSize = 12f;
                    zone.color = TextMuted;
                    SetRect(zone.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -40f), new Vector2(272f, 18f), new Vector2(0.5f, 1f));
                }

                RectTransform content = FindRecursive(panel, "MinimapContent") as RectTransform;
                if (content != null)
                {
                    GetOrAdd<RectMask2D>(content.gameObject);
                    Image contentImage = GetOrAdd<Image>(content.gameObject);
                    contentImage.color = new Color(0.025f, 0.065f, 0.095f, 0.98f);
                    SetRect(content, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 26f), new Vector2(272f, 153f), new Vector2(0.5f, 0f));
                    BuildMinimapGrid(content);
                    ConfigureMinimapMarkers(content);
                }

                BuildMinimapLegend(panel);
                AddBorder(panel, Cyan, new Vector2(304f, 2f), new Vector2(0f, 111f));
            }

            Transform objectiveMarker = FindRecursive(hudRoot, "ObjectiveMarker");
            if (objectiveMarker != null)
            {
                ((RectTransform)objectiveMarker).sizeDelta = new Vector2(34f, 34f);
                Image image = objectiveMarker.GetComponent<Image>();
                if (image != null) image.color = new Color(Warning.r, Warning.g, Warning.b, 0.9f);

                RectTransform objectiveIcon = FindRecursive(objectiveMarker, "ObjectiveIcon") as RectTransform;
                if (objectiveIcon != null)
                {
                    Image iconImage = GetOrAdd<Image>(objectiveIcon.gameObject);
                    iconImage.color = Background;
                    objectiveIcon.sizeDelta = new Vector2(14f, 14f);
                    objectiveIcon.localRotation = Quaternion.Euler(0f, 0f, 45f);
                }

                RectTransform arrow = FindRecursive(objectiveMarker, "ObjectiveArrow") as RectTransform;
                if (arrow != null)
                {
                    Image arrowImage = GetOrAdd<Image>(arrow.gameObject);
                    arrowImage.color = Warning;
                    SetRect(arrow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(31f, 0f), new Vector2(22f, 4f), new Vector2(0.5f, 0.5f));
                    RectTransform arrowHead = CreateImage(arrow, "ArrowHead", Warning);
                    SetRect(arrowHead, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-2f, 0f), new Vector2(9f, 4f), new Vector2(0.5f, 0.5f));
                    arrowHead.localRotation = Quaternion.Euler(0f, 0f, 45f);
                }

                ObjectiveMarkerView markerView = objectiveMarker.GetComponent<ObjectiveMarkerView>();
                if (markerView != null && objectiveIcon != null && arrow != null)
                {
                    SetObject(markerView, "icon", objectiveIcon.GetComponent<Image>());
                    SetObject(markerView, "arrow", arrow);
                }
            }

            TMP_Text[] labels = hudRoot.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < labels.Length; index++)
            {
                labels[index].font = font;
                labels[index].raycastTarget = false;
            }
        }

        private static void BuildMinimapGrid(RectTransform content)
        {
            RectTransform gridRect = EnsureRect(content, "Grid");
            Transform grid = gridRect;
            Stretch(gridRect);
            DestroyChildren(grid);
            for (int index = 1; index < 4; index++)
            {
                RectTransform vertical = CreateImage(grid, "V" + index,
                    new Color(Cyan.r, Cyan.g, Cyan.b, 0.06f));
                SetRect(vertical, new Vector2(index / 4f, 0.5f), new Vector2(index / 4f, 0.5f),
                    Vector2.zero, new Vector2(1f, 153f), new Vector2(0.5f, 0.5f));
            }
            for (int index = 1; index < 3; index++)
            {
                RectTransform horizontal = CreateImage(grid, "H" + index,
                    new Color(Cyan.r, Cyan.g, Cyan.b, 0.06f));
                SetRect(horizontal, new Vector2(0.5f, index / 3f), new Vector2(0.5f, index / 3f),
                    Vector2.zero, new Vector2(272f, 1f), new Vector2(0.5f, 0.5f));
            }
            grid.SetAsFirstSibling();
        }

        private static void BuildMinimapLegend(RectTransform panel)
        {
            Transform previous = FindDirectChild(panel, "MinimapLegend");
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }

            RectTransform legend = EnsureRect(panel, "MinimapLegend");
            SetRect(legend, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 3f), new Vector2(272f, 20f), new Vector2(0.5f, 0f));

            string[] names = { "Objective", "Checkpoint", "Exit", "Threat" };
            string[] labels = { "OBJ", "CP", "SALIDA", "RIESGO" };
            Color[] colors = { Warning, Success, Cyan, Danger };
            float[] positions = { -118f, -54f, 15f, 84f };
            for (int index = 0; index < names.Length; index++)
            {
                RectTransform shape = CreateImage(legend, names[index] + "Shape", colors[index]);
                SetRect(shape, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(positions[index], 0f), new Vector2(9f, 9f),
                    new Vector2(0.5f, 0.5f));

                if (index == 0 || index == 3)
                {
                    shape.localRotation = Quaternion.Euler(0f, 0f, 45f);
                }
                else if (index == 1)
                {
                    shape.sizeDelta = new Vector2(9f, 3f);
                    RectTransform vertical = CreateImage(shape, "Vertical", colors[index]);
                    SetRect(vertical, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(3f, 9f), new Vector2(0.5f, 0.5f));
                }
                else
                {
                    shape.sizeDelta = new Vector2(8f, 11f);
                    RectTransform core = CreateImage(shape, "Core", Background);
                    SetRect(core, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(4f, 7f), new Vector2(0.5f, 0.5f));
                }

                TMP_Text label = CreateText(legend, names[index] + "Label", labels[index], 9f,
                    TextAlignmentOptions.Left, TextMuted);
                SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(positions[index] + 12f, 0f), new Vector2(54f, 18f),
                    new Vector2(0f, 0.5f));
            }
        }

        private static void ConfigureMinimapMarkers(RectTransform content)
        {
            Transform template = FindRecursive(content, "MarkerTemplate");
            if (template != null)
            {
                RectTransform rect = (RectTransform)template;
                rect.sizeDelta = new Vector2(16f, 16f);
                Image baseImage = GetOrAdd<Image>(template.gameObject);
                RectTransform primary = CreateImage(template, "AccentPrimary", Background);
                RectTransform secondary = CreateImage(template, "AccentSecondary", Cyan);
                SetRect(primary, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(7f, 7f), new Vector2(0.5f, 0.5f));
                SetRect(secondary, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(3f, 3f), new Vector2(0.5f, 0.5f));
                MinimapMarkerView view = template.GetComponent<MinimapMarkerView>();
                if (view != null)
                {
                    SetObject(view, "background", baseImage);
                    SetObject(view, "accentPrimary", primary.GetComponent<Image>());
                    SetObject(view, "accentSecondary", secondary.GetComponent<Image>());
                }
            }

            Transform player = FindRecursive(content, "PlayerMarker");
            if (player != null)
            {
                RectTransform rect = (RectTransform)player;
                rect.sizeDelta = new Vector2(15f, 15f);
                Image baseImage = GetOrAdd<Image>(player.gameObject);
                baseImage.color = Cyan;
                RectTransform core = CreateImage(player, "Core", Background);
                SetRect(core, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(7f, 7f), new Vector2(0.5f, 0.5f));
                RectTransform needle = CreateImage(player, "Needle", TextPrimary);
                SetRect(needle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 8f), new Vector2(2f, 7f), new Vector2(0.5f, 0.5f));
                TMP_Text oldIcon = FindRecursive(player, "Icon")?.GetComponent<TMP_Text>();
                if (oldIcon != null) oldIcon.gameObject.SetActive(false);
            }
        }

        private static FlowUi BuildFlowUi(Transform uiRoot, GameObject hudRoot)
        {
            RectTransform root = EnsureRect(uiRoot, "GameFlowRoot");
            Stretch(root);
            DestroyChildren(root);

            FlowUi result = new FlowUi { Root = root, HudRoot = hudRoot };
            BuildMainMenu(root, result);
            BuildIntro(root, result);
            BuildLoading(root, result);
            BuildPause(root, result);
            BuildControls(root, result);
            BuildDefeat(root, result);
            BuildVictory(root, result);
            BuildCredits(root, result);
            return result;
        }

        private static void BuildMainMenu(Transform root, FlowUi ui)
        {
            RectTransform screen = CreateScreen(root, "MainMenuScreen", Background);
            ui.MainMenuRoot = screen.gameObject;
            AddTechDecor(screen, Cyan);

            TMP_Text kicker = CreateText(screen, "Kicker", "PROTOCOLO DE EMERGENCIA // UNIFRANZ", 15f,
                TextAlignmentOptions.Left, Cyan);
            SetRect(kicker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(110f, -130f), new Vector2(760f, 28f), new Vector2(0f, 1f));
            TMP_Text title = CreateText(screen, "Title", "EscapeUNIFRANZ", 68f,
                TextAlignmentOptions.Left, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(110f, -175f), new Vector2(920f, 92f), new Vector2(0f, 1f));
            TMP_Text subtitle = CreateText(screen, "Subtitle", "La universidad tomada por la IA", 22f,
                TextAlignmentOptions.Left, TextMuted);
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(114f, -273f), new Vector2(720f, 42f), new Vector2(0f, 1f));

            RectTransform rail = CreateImage(screen, "MenuRail", new Color(Cyan.r, Cyan.g, Cyan.b, 0.75f));
            SetRect(rail, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(82f, -75f), new Vector2(4f, 430f), new Vector2(0f, 0.5f));
            ui.NewGame = CreateButton(screen, "NewGame", "NUEVA PARTIDA", new Vector2(320f, -42f), Cyan);
            ui.Continue = CreateButton(screen, "Continue", "CONTINUAR   // SIN DATOS", new Vector2(320f, -108f), Disabled);
            ui.Continue.interactable = false;
            ui.MainControls = CreateButton(screen, "Controls", "CONTROLES", new Vector2(320f, -174f), Border);
            ui.MainCredits = CreateButton(screen, "Credits", "CRÉDITOS", new Vector2(320f, -240f), Border);
            ui.Quit = CreateButton(screen, "Quit", "SALIR", new Vector2(320f, -306f), Danger);

            TMP_Text footer = CreateText(screen, "Footer", "VERTICAL SLICE  •  SISTEMAS / REDES / SOFTWARE", 13f,
                TextAlignmentOptions.Left, TextMuted);
            SetRect(footer.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(110f, 48f), new Vector2(720f, 26f), Vector2.zero);
        }

        private static void BuildIntro(Transform root, FlowUi ui)
        {
            RectTransform screen = CreateScreen(root, "IntroScreen", new Color(
                Background.r, Background.g, Background.b, 0.99f));
            ui.IntroRoot = screen.gameObject;
            AddTechDecor(screen, Cyan);
            RectTransform card = CreateCard(screen, "IntroCard", new Vector2(920f, 500f), Surface);
            AddBorder(card, Cyan, new Vector2(920f, 4f), new Vector2(0f, 248f));
            ui.IntroCounter = CreateText(card, "Counter", string.Empty, 13f,
                TextAlignmentOptions.TopLeft, Cyan);
            SetRect(ui.IntroCounter.rectTransform, Vector2.up, Vector2.up,
                new Vector2(52f, -44f), new Vector2(810f, 24f), new Vector2(0f, 1f));
            ui.IntroTitle = CreateText(card, "Title", string.Empty, 36f,
                TextAlignmentOptions.TopLeft, TextPrimary);
            SetRect(ui.IntroTitle.rectTransform, Vector2.up, Vector2.up,
                new Vector2(52f, -92f), new Vector2(810f, 54f), new Vector2(0f, 1f));
            ui.IntroBody = CreateText(card, "Body", string.Empty, 24f,
                TextAlignmentOptions.TopLeft, TextMuted);
            ui.IntroBody.textWrappingMode = TextWrappingModes.Normal;
            SetRect(ui.IntroBody.rectTransform, Vector2.up, Vector2.up,
                new Vector2(52f, -164f), new Vector2(810f, 150f), new Vector2(0f, 1f));
            ui.IntroNext = CreateButton(card, "Next", "CONTINUAR", new Vector2(190f, -170f), Cyan, 300f);
            ui.IntroSkip = CreateButton(card, "Skip", "OMITIR INTRO", new Vector2(-170f, -170f), Border, 300f);
            TMP_Text hint = CreateText(card, "Hint", "ENTER / CLICK para continuar   •   ESC para omitir", 13f,
                TextAlignmentOptions.Center, TextMuted);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 14f), new Vector2(700f, 24f), new Vector2(0.5f, 0f));
        }

        private static void BuildLoading(Transform root, FlowUi ui)
        {
            RectTransform screen = CreateScreen(root, "LoadingScreen", Background);
            ui.LoadingRoot = screen.gameObject;
            RectTransform mark = CreateImage(screen, "Mark", Cyan);
            SetRect(mark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 45f), new Vector2(36f, 36f), new Vector2(0.5f, 0.5f));
            mark.localRotation = Quaternion.Euler(0f, 0f, 45f);
            RectTransform markCore = CreateImage(mark, "Core", Background);
            SetRect(markCore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));
            TMP_Text label = CreateText(screen, "Label", "INICIALIZANDO SISTEMAS...", 18f,
                TextAlignmentOptions.Center, TextMuted);
            SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -40f), new Vector2(520f, 40f), new Vector2(0.5f, 0.5f));
        }

        private static void BuildPause(Transform root, FlowUi ui)
        {
            RectTransform screen = CreateScreen(root, "PauseScreen", new Color(0.015f, 0.035f, 0.055f, 0.96f));
            ui.PauseRoot = screen.gameObject;
            RectTransform card = CreateCard(screen, "PauseCard", new Vector2(640f, 570f), Surface);
            AddScreenTitle(card, "SISTEMA EN PAUSA", "ESC para continuar", Cyan);
            ui.Resume = CreateButton(card, "Resume", "CONTINUAR", new Vector2(0f, 45f), Cyan);
            ui.PauseRestart = CreateButton(card, "Restart", "REINICIAR DESDE CHECKPOINT", new Vector2(0f, -25f), Warning);
            ui.PauseControls = CreateButton(card, "Controls", "CONTROLES", new Vector2(0f, -95f), Border);
            ui.PauseMenu = CreateButton(card, "Menu", "VOLVER AL MENÚ", new Vector2(0f, -165f), Danger);
        }

        private static void BuildControls(Transform root, FlowUi ui)
        {
            RectTransform screen = CreateScreen(root, "ControlsScreen", new Color(0.015f, 0.035f, 0.055f, 0.98f));
            ui.ControlsRoot = screen.gameObject;
            RectTransform card = CreateCard(screen, "ControlsCard", new Vector2(820f, 690f), Surface);
            AddScreenTitle(card, "CONTROLES", "Entrada de teclado", Cyan);
            string[] keys = { "WASD / FLECHAS", "E", "TAB", "M", "ESC" };
            string[] actions = { "Moverse", "Interactuar", "Escáner temporal", "Mapa del campus", "Pausa / volver" };
            for (int index = 0; index < keys.Length; index++)
            {
                float y = 105f - index * 68f;
                RectTransform row = CreateImage(card, "Row_" + index, new Color(Raised.r, Raised.g, Raised.b, 0.75f));
                SetRect(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, y), new Vector2(700f, 52f), new Vector2(0.5f, 0.5f));
                TMP_Text key = CreateText(row, "Key", keys[index], 16f, TextAlignmentOptions.Left, Cyan);
                SetRect(key.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(22f, 0f), new Vector2(250f, 34f), new Vector2(0f, 0.5f));
                TMP_Text action = CreateText(row, "Action", actions[index], 17f, TextAlignmentOptions.Right, TextPrimary);
                SetRect(action.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-22f, 0f), new Vector2(360f, 34f), new Vector2(1f, 0.5f));
            }
            ui.ControlsBack = CreateButton(card, "Back", "VOLVER", new Vector2(0f, -270f), Border, 300f);
        }

        private static void BuildDefeat(Transform root, FlowUi ui)
        {
            RectTransform screen = CreateScreen(root, "DefeatScreen", new Color(0.07f, 0.012f, 0.025f, 0.97f));
            ui.DefeatRoot = screen.gameObject;
            RectTransform card = CreateCard(screen, "DefeatCard", new Vector2(780f, 500f), Surface);
            AddBorder(card, Danger, new Vector2(780f, 5f), new Vector2(0f, 247.5f));
            TMP_Text code = CreateText(card, "Code", "ERROR // ACCESO INTERRUMPIDO", 14f,
                TextAlignmentOptions.Top, Danger);
            SetRect(code.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -55f),
                new Vector2(680f, 26f), new Vector2(0.5f, 1f));
            TMP_Text title = CreateText(card, "Title", "SISTEMA COMPROMETIDO", 36f,
                TextAlignmentOptions.Top, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -100f),
                new Vector2(680f, 52f), new Vector2(0.5f, 1f));
            ui.DefeatReason = CreateText(card, "Reason", string.Empty, 20f,
                TextAlignmentOptions.Top, TextMuted);
            ui.DefeatReason.textWrappingMode = TextWrappingModes.Normal;
            SetRect(ui.DefeatReason.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -175f),
                new Vector2(650f, 82f), new Vector2(0.5f, 1f));
            ui.Retry = CreateButton(card, "Retry", "REINTENTAR DESDE CHECKPOINT", new Vector2(0f, -105f), Danger, 430f);
            ui.DefeatMenu = CreateButton(card, "Menu", "VOLVER AL MENÚ", new Vector2(0f, -175f), Border, 430f);
        }

        private static void BuildVictory(Transform root, FlowUi ui)
        {
            RectTransform screen = CreateScreen(root, "VictoryScreen", Background);
            ui.VictoryRoot = screen.gameObject;
            AddTechDecor(screen, Success);
            RectTransform card = CreateCard(screen, "VictoryCard", new Vector2(900f, 590f), Surface);
            AddBorder(card, Success, new Vector2(900f, 5f), new Vector2(0f, 292.5f));
            TMP_Text code = CreateText(card, "Code", "PROTOCOLO COMPLETADO // 100%", 14f,
                TextAlignmentOptions.Top, Success);
            SetRect(code.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -52f),
                new Vector2(780f, 26f), new Vector2(0.5f, 1f));
            TMP_Text title = CreateText(card, "Title", "Sistema apagado\nde forma segura", 40f,
                TextAlignmentOptions.Top, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -98f),
                new Vector2(780f, 110f), new Vector2(0.5f, 1f));
            TMP_Text body = CreateText(card, "Body",
                "La IA queda aislada y los sistemas críticos vuelven a responder.\nUNIFRANZ está a salvo. La integración hizo posible la solución.",
                20f, TextAlignmentOptions.Top, TextMuted);
            body.textWrappingMode = TextWrappingModes.Normal;
            SetRect(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -235f),
                new Vector2(740f, 90f), new Vector2(0.5f, 1f));
            ui.VictoryCredits = CreateButton(card, "Credits", "VER CRÉDITOS", new Vector2(170f, -215f), Success, 300f);
            ui.VictoryMenu = CreateButton(card, "Menu", "VOLVER AL MENÚ", new Vector2(-170f, -215f), Border, 300f);
        }

        private static void BuildCredits(Transform root, FlowUi ui)
        {
            RectTransform screen = CreateScreen(root, "CreditsScreen", Background);
            ui.CreditsRoot = screen.gameObject;
            AddTechDecor(screen, Cyan);
            RectTransform card = CreateCard(screen, "CreditsCard", new Vector2(820f, 760f), Surface);
            AddScreenTitle(card, "CRÉDITOS", "EscapeUNIFRANZ", Cyan);
            TMP_Text body = CreateText(card, "Body",
                "PROYECTO\nEscapeUNIFRANZ\n\nGRUPO E INTEGRANTES\nGrupo EscapeUNIFRANZ\nIntegrantes: por definir\n\nMATERIA\nProgramación Gráfica y Multimedia\n\nINSTITUCIÓN\nUNIFRANZ\n\nROLES GENERALES\nDiseño — por definir\nProgramación — equipo del proyecto\nArte / UI / Sonido — provisional",
                18f, TextAlignmentOptions.TopLeft, TextPrimary);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 8f;
            SetRect(body.rectTransform, Vector2.up, Vector2.up, new Vector2(80f, -145f),
                new Vector2(660f, 470f), new Vector2(0f, 1f));
            ui.CreditsMenu = CreateButton(card, "Menu", "VOLVER AL MENÚ", new Vector2(0f, -320f), Border, 320f);
        }

        private static void WireFlow(GameRuntimeRoot runtime, Transform uiRoot, FlowUi ui)
        {
            GameSessionController session = runtime.GetComponentInChildren<GameSessionController>(true);
            SceneFlowController sceneFlow = runtime.GetComponentInChildren<SceneFlowController>(true);
            GameplayModeController mode = runtime.GetComponentInChildren<GameplayModeController>(true);
            CampusMapController map = uiRoot.GetComponentInChildren<CampusMapController>(true);
            FadeView fade = uiRoot.GetComponentInChildren<FadeView>(true);
            if (session == null || sceneFlow == null || mode == null || fade == null)
            {
                throw new InvalidOperationException("Presentation flow dependencies are incomplete.");
            }

            GameFlowView view = GetOrAdd<GameFlowView>(ui.Root.gameObject);
            SetObject(view, "gameplayHudRoot", ui.HudRoot);
            SetObject(view, "mainMenuRoot", ui.MainMenuRoot);
            SetObject(view, "introRoot", ui.IntroRoot);
            SetObject(view, "loadingRoot", ui.LoadingRoot);
            SetObject(view, "pauseRoot", ui.PauseRoot);
            SetObject(view, "controlsRoot", ui.ControlsRoot);
            SetObject(view, "defeatRoot", ui.DefeatRoot);
            SetObject(view, "victoryRoot", ui.VictoryRoot);
            SetObject(view, "creditsRoot", ui.CreditsRoot);
            SetObject(view, "introTitle", ui.IntroTitle);
            SetObject(view, "introBody", ui.IntroBody);
            SetObject(view, "introCounter", ui.IntroCounter);
            SetObject(view, "defeatReason", ui.DefeatReason);
            SetObject(view, "newGameButton", ui.NewGame);
            SetObject(view, "continueButton", ui.Continue);
            SetObject(view, "mainControlsButton", ui.MainControls);
            SetObject(view, "mainCreditsButton", ui.MainCredits);
            SetObject(view, "quitButton", ui.Quit);
            SetObject(view, "introNextButton", ui.IntroNext);
            SetObject(view, "introSkipButton", ui.IntroSkip);
            SetObject(view, "resumeButton", ui.Resume);
            SetObject(view, "pauseRestartButton", ui.PauseRestart);
            SetObject(view, "pauseControlsButton", ui.PauseControls);
            SetObject(view, "pauseMenuButton", ui.PauseMenu);
            SetObject(view, "controlsBackButton", ui.ControlsBack);
            SetObject(view, "retryButton", ui.Retry);
            SetObject(view, "defeatMenuButton", ui.DefeatMenu);
            SetObject(view, "victoryCreditsButton", ui.VictoryCredits);
            SetObject(view, "victoryMenuButton", ui.VictoryMenu);
            SetObject(view, "creditsMenuButton", ui.CreditsMenu);

            GameFlowController controller = GetOrAdd<GameFlowController>(runtime.gameObject);
            SetObject(controller, "session", session);
            SetObject(controller, "sceneFlow", sceneFlow);
            SetObject(controller, "gameplayMode", mode);
            SetObject(controller, "campusMap", map);
            SetObject(controller, "view", view);
            SetObject(controller, "fadeView", fade);
            SetString(controller, "initialZoneId", "entrada");
            SetString(controller, "initialSpawnId", "entrada_inicio");
            SetObject(session, "gameFlow", controller);
            SetBool(session, "startNewGameOnStart", false);

            ui.MainMenuRoot.SetActive(true);
            ui.IntroRoot.SetActive(false);
            ui.LoadingRoot.SetActive(false);
            ui.PauseRoot.SetActive(false);
            ui.ControlsRoot.SetActive(false);
            ui.DefeatRoot.SetActive(false);
            ui.VictoryRoot.SetActive(false);
            ui.CreditsRoot.SetActive(false);
            ui.HudRoot.SetActive(false);
        }

        private static RectTransform CreateScreen(Transform parent, string name, Color color)
        {
            RectTransform screen = CreateImage(parent, name, color);
            Stretch(screen);
            screen.GetComponent<Image>().raycastTarget = true;
            return screen;
        }

        private static RectTransform CreateCard(Transform parent, string name, Vector2 size, Color color)
        {
            RectTransform card = CreateImage(parent, name, new Color(color.r, color.g, color.b, 0.98f));
            SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, size, new Vector2(0.5f, 0.5f));
            Outline outline = GetOrAdd<Outline>(card.gameObject);
            outline.effectColor = new Color(Border.r, Border.g, Border.b, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            return card;
        }

        private static void AddScreenTitle(Transform card, string titleText, string subtitleText, Color accent)
        {
            TMP_Text title = CreateText(card, "Title", titleText, 34f,
                TextAlignmentOptions.Top, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -58f),
                new Vector2(560f, 50f), new Vector2(0.5f, 1f));
            TMP_Text subtitle = CreateText(card, "Subtitle", subtitleText, 14f,
                TextAlignmentOptions.Top, accent);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -108f),
                new Vector2(560f, 28f), new Vector2(0.5f, 1f));
        }

        private static void AddTechDecor(Transform screen, Color accent)
        {
            RectTransform rail = CreateImage(screen, "DecorRail", new Color(accent.r, accent.g, accent.b, 0.16f));
            SetRect(rail, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-120f, 0f), new Vector2(2f, 780f), new Vector2(1f, 0.5f));
            for (int index = 0; index < 7; index++)
            {
                RectTransform node = CreateImage(screen, "DecorNode_" + index,
                    new Color(accent.r, accent.g, accent.b, 0.2f + index * 0.035f));
                SetRect(node, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-120f - (index % 2) * 72f, 270f - index * 90f),
                    new Vector2(index % 2 == 0 ? 140f : 70f, 3f), new Vector2(1f, 0.5f));
            }
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            Color accent,
            float width = 430f)
        {
            RectTransform rect = CreateImage(parent, name, Raised);
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                position, new Vector2(width, 54f), new Vector2(0.5f, 0.5f));
            Button button = GetOrAdd<Button>(rect.gameObject);
            button.targetGraphic = rect.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = Raised;
            colors.highlightedColor = new Color(accent.r * 0.45f, accent.g * 0.45f, accent.b * 0.45f, 1f);
            colors.pressedColor = new Color(accent.r * 0.7f, accent.g * 0.7f, accent.b * 0.7f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(Disabled.r, Disabled.g, Disabled.b, 0.35f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            rect.GetComponent<Image>().raycastTarget = true;

            RectTransform edge = CreateImage(rect, "Accent", accent);
            SetRect(edge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0f), new Vector2(5f, 54f), new Vector2(0f, 0.5f));
            TMP_Text text = CreateText(rect, "Label", label, 17f,
                TextAlignmentOptions.Left, TextPrimary);
            SetRect(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(24f, 0f), new Vector2(width - 42f, 40f), new Vector2(0f, 0.5f));
            return button;
        }

        private static void AddBorder(Transform parent, Color color, Vector2 size, Vector2 position)
        {
            RectTransform border = CreateImage(parent, "AccentBorder", color);
            SetRect(border, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                position, size, new Vector2(0.5f, 0.5f));
        }

        private static void StylePanel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Vector2 pivot,
            float fontSize)
        {
            Transform item = FindRecursive(parent, name);
            if (item == null)
            {
                return;
            }

            RectTransform rect = (RectTransform)item;
            Image image = GetOrAdd<Image>(item.gameObject);
            image.color = color;
            SetRect(rect, anchorMin, anchorMax, position, size, pivot);
            TMP_Text label = item.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.font = font;
                label.fontSize = fontSize;
                label.color = TextPrimary;
            }
        }

        private static RectTransform EnsurePanel(Transform target, string name, Color color)
        {
            target.name = name;
            RectTransform rect = EnsureRect(target);
            Image image = GetOrAdd<Image>(target.gameObject);
            image.sprite = GetUiSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateImage(Transform parent, string name, Color color)
        {
            Transform existing = FindDirectChild(parent, name);
            GameObject gameObject = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = EnsureRect(gameObject.transform);
            Image image = GetOrAdd<Image>(gameObject);
            image.sprite = GetUiSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            Transform existing = FindDirectChild(parent, name);
            GameObject gameObject = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            Text legacy = gameObject.GetComponent<Text>();
            if (legacy != null)
            {
                UnityEngine.Object.DestroyImmediate(legacy);
            }

            TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(gameObject);
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void ConvertRemainingLegacyUiText(Transform uiRoot)
        {
            Text[] legacyLabels = uiRoot.GetComponentsInChildren<Text>(true);
            for (int index = legacyLabels.Length - 1; index >= 0; index--)
            {
                Text legacy = legacyLabels[index];
                string value = legacy.text;
                int size = legacy.fontSize;
                Color color = legacy.color;
                TextAlignmentOptions alignment = ConvertAlignment(legacy.alignment);
                GameObject gameObject = legacy.gameObject;
                UnityEngine.Object.DestroyImmediate(legacy);
                TextMeshProUGUI replacement = GetOrAdd<TextMeshProUGUI>(gameObject);
                replacement.font = font;
                replacement.text = value;
                replacement.fontSize = size;
                replacement.color = color;
                replacement.alignment = alignment;
                replacement.raycastTarget = false;
            }
        }

        private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        private static TMP_FontAsset LoadOrCreateFont()
        {
            if (TMP_Settings.instance == null)
            {
                string packagePath = AssetDatabase.GUIDToAssetPath(TmpEssentialResourcesGuid);
                if (string.IsNullOrWhiteSpace(packagePath))
                {
                    throw new InvalidOperationException("Unity could not locate TMP Essential Resources.");
                }
                AssetDatabase.ImportPackage(packagePath, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

            TMP_FontAsset asset = TMP_Settings.defaultFontAsset;
            if (asset == null)
            {
                throw new InvalidOperationException("TMP Essential Resources have no default font asset.");
            }
            return asset;
        }

        private static GameObject CreateWorldRect(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            int sortingOrder)
        {
            Transform child = FindOrCreateChild(parent, name);
            child.position = new Vector3(position.x, position.y, 0f);
            child.localScale = Vector3.one;
            SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(child.gameObject);
            renderer.sprite = GetUiSprite();
            renderer.drawMode = SpriteDrawMode.Simple;
            Vector2 sourceSize = renderer.sprite != null ? renderer.sprite.bounds.size : Vector2.one;
            child.localScale = new Vector3(size.x / sourceSize.x, size.y / sourceSize.y, 1f);
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return child.gameObject;
        }

        private static Sprite GetUiSprite()
        {
            if (uiSprite == null)
            {
                uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            }
            return uiSprite;
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString("#" + value, out Color color)
                ? color
                : Color.magenta;
        }

        private static void DestroyChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(parent.GetChild(index).gameObject);
            }
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            return EnsureRect(FindOrCreateChild(parent, name));
        }

        private static RectTransform EnsureRect(Transform transform)
        {
            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                return rect;
            }

            GameObject gameObject = transform.gameObject;
            Transform parent = transform.parent;
            string name = gameObject.name;
            int sibling = transform.GetSiblingIndex();
            UnityEngine.Object.DestroyImmediate(gameObject);
            GameObject replacement = new GameObject(name, typeof(RectTransform));
            replacement.transform.SetParent(parent, false);
            replacement.transform.SetSiblingIndex(sibling);
            return (RectTransform)replacement.transform;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                if (parent.GetChild(index).name == name)
                {
                    return parent.GetChild(index);
                }
            }
            return null;
        }

        private static Transform FindOrCreateChild(Transform parent, string name)
        {
            Transform child = FindDirectChild(parent, name);
            if (child != null)
            {
                return child;
            }

            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.transform;
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform result = FindRecursive(parent.GetChild(index), name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }

        private static void SetObject(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            SetSerialized(target, property, item => item.objectReferenceValue = value);
        }

        private static void SetString(UnityEngine.Object target, string property, string value)
        {
            SetSerialized(target, property, item => item.stringValue = value ?? string.Empty);
        }

        private static void SetBool(UnityEngine.Object target, string property, bool value)
        {
            SetSerialized(target, property, item => item.boolValue = value);
        }

        private static void SetSerialized(
            UnityEngine.Object target,
            string property,
            Action<SerializedProperty> apply)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty item = serialized.FindProperty(property);
            if (item == null)
            {
                throw new InvalidOperationException(
                    $"Serialized property '{property}' not found on {target.GetType().Name}.");
            }
            apply(item);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private sealed class FlowUi
        {
            public RectTransform Root;
            public GameObject HudRoot;
            public GameObject MainMenuRoot;
            public GameObject IntroRoot;
            public GameObject LoadingRoot;
            public GameObject PauseRoot;
            public GameObject ControlsRoot;
            public GameObject DefeatRoot;
            public GameObject VictoryRoot;
            public GameObject CreditsRoot;
            public TMP_Text IntroTitle;
            public TMP_Text IntroBody;
            public TMP_Text IntroCounter;
            public TMP_Text DefeatReason;
            public Button NewGame;
            public Button Continue;
            public Button MainControls;
            public Button MainCredits;
            public Button Quit;
            public Button IntroNext;
            public Button IntroSkip;
            public Button Resume;
            public Button PauseRestart;
            public Button PauseControls;
            public Button PauseMenu;
            public Button ControlsBack;
            public Button Retry;
            public Button DefeatMenu;
            public Button VictoryCredits;
            public Button VictoryMenu;
            public Button CreditsMenu;
        }
    }
}
#endif
