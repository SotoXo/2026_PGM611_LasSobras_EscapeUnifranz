#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.EditorTools
{
    /// <summary>
    /// Reproducible assembly for the directional Player visual and standalone main menu.
    /// </summary>
    public static class PlayerAndMainMenuBuilder
    {
        private const string BootstrapPath = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        private const string MainMenuPath = "Assets/_Project/Scenes/UI/MainMenu.unity";
        private const string GeneratedFolder =
            "Assets/_Project/Art/Characters/Player/Generated";
        private const string ControllerPath = GeneratedFolder + "/PlayerDirectional.controller";
        private const string FrontPath =
            "Assets/_Project/Art/Characters/Player/Sprite-delante.aseprite";
        private const string BackPath =
            "Assets/_Project/Art/Characters/Player/Sprite-detras.aseprite";
        private const string LeftPath =
            "Assets/_Project/Art/Characters/Player/Izquierda.aseprite";
        private const string RightPath =
            "Assets/_Project/Art/Characters/Player/Derecha.aseprite";
        private const string BackgroundPath =
            "Assets/_Project/Art/UI/MainMenu/Backgrounds/pasillounifranzl (1).aseprite";
        private const string OutlineFontMaterialPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat";
        private const string ShadowFontMaterialPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Drop Shadow.mat";

        private static readonly Color Background = new Color(0.018f, 0.027f, 0.045f, 1f);
        private static readonly Color Surface = new Color(0.025f, 0.045f, 0.065f, 0.91f);
        private static readonly Color Raised = new Color(0.045f, 0.075f, 0.095f, 0.94f);
        private static readonly Color Cyan = new Color(0.16f, 0.92f, 0.96f, 1f);
        private static readonly Color Danger = new Color(0.98f, 0.20f, 0.25f, 1f);
        private static readonly Color TextPrimary = new Color(0.94f, 0.97f, 0.94f, 1f);
        private static readonly Color TextMuted = new Color(0.64f, 0.76f, 0.78f, 1f);

        [MenuItem("Escape UNIFRANZ/Visual/Build Player and Main Menu")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before building visual content.");
            }

            EnsureFolder(GeneratedFolder);
            Dictionary<PlayerVisualController.FacingDirection, DirectionAssets> directions =
                BuildDirectionalAnimations();
            AnimatorController controller = BuildAnimatorController(directions);

            Sprite frontSprite = directions[PlayerVisualController.FacingDirection.Front].Sprites[0];
            UpdateBootstrapPlayer(controller, frontSprite);

            Sprite background = LoadSprites(BackgroundPath).FirstOrDefault();
            if (background == null)
            {
                throw new InvalidOperationException("The main-menu Aseprite has no imported Sprite.");
            }

            BuildMainMenu(background);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Directional Player and MainMenu assembly completed.");
        }

        [MenuItem("Escape UNIFRANZ/Visual/Rebuild Main Menu Only")]
        public static void BuildMainMenuOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before rebuilding the menu.");
            }

            Sprite background = LoadSprites(BackgroundPath).FirstOrDefault();
            if (background == null)
            {
                throw new InvalidOperationException("The main-menu Aseprite has no imported Sprite.");
            }

            BuildMainMenu(background);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Atmospheric MainMenu rebuild completed.");
        }

        private static Dictionary<PlayerVisualController.FacingDirection, DirectionAssets>
            BuildDirectionalAnimations()
        {
            var result = new Dictionary<PlayerVisualController.FacingDirection, DirectionAssets>();
            AddDirection(result, PlayerVisualController.FacingDirection.Front, "Front", FrontPath);
            AddDirection(result, PlayerVisualController.FacingDirection.Back, "Back", BackPath);
            AddDirection(result, PlayerVisualController.FacingDirection.Left, "Left", LeftPath);
            AddDirection(result, PlayerVisualController.FacingDirection.Right, "Right", RightPath);
            return result;
        }

        private static void AddDirection(
            IDictionary<PlayerVisualController.FacingDirection, DirectionAssets> result,
            PlayerVisualController.FacingDirection direction,
            string suffix,
            string sourcePath)
        {
            Sprite[] sprites = LoadSprites(sourcePath);
            if (sprites.Length == 0)
            {
                throw new InvalidOperationException(sourcePath + " has no imported Sprite subassets.");
            }

            AnimationClip idle = CreateClip("Idle" + suffix, new[] { sprites[0] }, 1f, true);
            AnimationClip walk = CreateClip("Walk" + suffix, sprites, 8f, true);
            result.Add(direction, new DirectionAssets(sprites, idle, walk));
        }

        private static Sprite[] LoadSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static AnimationClip CreateClip(
            string name,
            IReadOnlyList<Sprite> sprites,
            float framesPerSecond,
            bool loop)
        {
            string path = GeneratedFolder + "/" + name + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = name };
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            clip.frameRate = framesPerSecond;
            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };

            int keyCount = sprites.Count > 1 ? sprites.Count + 1 : 1;
            var keyframes = new ObjectReferenceKeyframe[keyCount];
            for (int index = 0; index < sprites.Count; index++)
            {
                keyframes[index] = new ObjectReferenceKeyframe
                {
                    time = index / framesPerSecond,
                    value = sprites[index]
                };
            }

            if (sprites.Count > 1)
            {
                keyframes[keyframes.Length - 1] = new ObjectReferenceKeyframe
                {
                    time = sprites.Count / framesPerSecond,
                    value = sprites[0]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            SerializedObject serializedClip = new SerializedObject(clip);
            SerializedProperty loopTime = serializedClip.FindProperty(
                "m_AnimationClipSettings.m_LoopTime");
            if (loopTime != null)
            {
                loopTime.boolValue = loop;
                serializedClip.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController BuildAnimatorController(
            IReadOnlyDictionary<PlayerVisualController.FacingDirection, DirectionAssets> directions)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter(PlayerVisualController.DirectionParameter,
                AnimatorControllerParameterType.Int);
            controller.AddParameter(PlayerVisualController.IsWalkingParameter,
                AnimatorControllerParameterType.Bool);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }
            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
            {
                stateMachine.RemoveAnyStateTransition(transition);
            }

            AnimatorState defaultState = null;
            int row = 0;
            foreach (KeyValuePair<PlayerVisualController.FacingDirection, DirectionAssets> pair in
                     directions.OrderBy(item => (int)item.Key))
            {
                string suffix = pair.Key.ToString();
                AnimatorState idle = stateMachine.AddState("Idle" + suffix,
                    new Vector3(260f, 80f + row * 110f));
                idle.motion = pair.Value.Idle;
                AnimatorState walk = stateMachine.AddState("Walk" + suffix,
                    new Vector3(520f, 80f + row * 110f));
                walk.motion = pair.Value.Walk;
                AddDirectionalTransition(stateMachine, idle, pair.Key, false);
                AddDirectionalTransition(stateMachine, walk, pair.Key, true);

                if (pair.Key == PlayerVisualController.FacingDirection.Front)
                {
                    defaultState = idle;
                }
                row++;
            }

            stateMachine.defaultState = defaultState;
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(stateMachine);
            return controller;
        }

        private static void AddDirectionalTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState state,
            PlayerVisualController.FacingDirection direction,
            bool walking)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                (float)direction,
                PlayerVisualController.DirectionParameter);
            transition.AddCondition(
                walking ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                PlayerVisualController.IsWalkingParameter);
        }

        private static void UpdateBootstrapPlayer(
            RuntimeAnimatorController controller,
            Sprite initialSprite)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != BootstrapPath)
            {
                if (scene.isDirty)
                {
                    throw new InvalidOperationException(
                        "The active scene has unsaved changes. Open Bootstrap before building.");
                }
                scene = EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);
            }

            PlayerMovement2D movement = UnityEngine.Object.FindFirstObjectByType<PlayerMovement2D>();
            if (movement == null)
            {
                throw new InvalidOperationException("Bootstrap has no PlayerMovement2D.");
            }

            GameObject player = movement.gameObject;
            PlayerVisualController visualController = player.GetComponent<PlayerVisualController>();
            if (visualController == null)
            {
                visualController = player.AddComponent<PlayerVisualController>();
            }

            Transform visual = player.transform.Find("Visual");
            if (visual == null)
            {
                visual = new GameObject("Visual").transform;
                visual.SetParent(player.transform, false);
            }

            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            }
            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visual.gameObject.AddComponent<Animator>();
            }

            visual.localScale = new Vector3(5f, 5f, 1f);
            visual.localRotation = Quaternion.identity;
            renderer.sprite = initialSprite;
            renderer.color = Color.white;
            renderer.flipX = false;
            renderer.flipY = false;
            renderer.sortingOrder = 10;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            Bounds bounds = initialSprite.bounds;
            visual.localPosition = new Vector3(
                -bounds.center.x * visual.localScale.x,
                -bounds.center.y * visual.localScale.y,
                0f);

            SpriteRenderer oldRenderer = player.GetComponent<SpriteRenderer>();
            if (oldRenderer != null && oldRenderer != renderer)
            {
                UnityEngine.Object.DestroyImmediate(oldRenderer);
            }
            Animator oldAnimator = player.GetComponent<Animator>();
            if (oldAnimator != null && oldAnimator != animator)
            {
                UnityEngine.Object.DestroyImmediate(oldAnimator);
            }

            SerializedObject serializedController = new SerializedObject(visualController);
            SetObject(serializedController, "movement", movement);
            SetObject(serializedController, "spriteRenderer", renderer);
            SetObject(serializedController, "animator", animator);
            SetInt(serializedController, "initialDirection",
                (int)PlayerVisualController.FacingDirection.Front);
            SetBool(serializedController, "centerVisualOnPlayer", true);
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("Unity could not save Bootstrap.");
            }
        }

        private static void BuildMainMenu(Sprite backgroundSprite)
        {
            EnsureFolder("Assets/_Project/Scenes/UI");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject canvasObject = new GameObject(
                "MainMenuCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(MainMenuController),
                typeof(MainMenuAmbienceController));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform background = CreateImage(
                canvasObject.transform, "Background", Color.white);
            Stretch(background);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = true;
            backgroundImage.color = new Color(0.94f, 0.98f, 1f, 1f);
            background.localScale = new Vector3(1.08f, 1.08f, 1f);
            AspectRatioFitter fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;

            RectTransform overlay = CreateImage(
                canvasObject.transform, "ColdAtmosphere",
                new Color(0.015f, 0.09f, 0.12f, 0.18f));
            Stretch(overlay);

            RectTransform leftShade = CreateImage(
                canvasObject.transform, "LeftReadabilityShade",
                new Color(0.006f, 0.018f, 0.028f, 0.72f));
            SetRect(leftShade, Vector2.zero, new Vector2(0.54f, 1f),
                Vector2.zero, Vector2.zero, Vector2.zero);

            RectTransform topShade = CreateImage(
                canvasObject.transform, "TopVignette", new Color(0f, 0f, 0f, 0.30f));
            SetRect(topShade, new Vector2(0f, 0.86f), Vector2.one,
                Vector2.zero, Vector2.zero, new Vector2(0.5f, 1f));

            RectTransform bottomShade = CreateImage(
                canvasObject.transform, "BottomVignette", new Color(0f, 0f, 0f, 0.46f));
            SetRect(bottomShade, Vector2.zero, new Vector2(1f, 0.18f),
                Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f));

            RectTransform flicker = CreateImage(
                canvasObject.transform, "ElectricalFlicker", new Color(0f, 0f, 0f, 0.012f));
            Stretch(flicker);

            RectTransform scanline = CreateImage(
                canvasObject.transform, "SurveillanceScanline", new Color(0.18f, 0.92f, 0.96f, 0.055f));
            SetRect(scanline, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(0f, 3f), new Vector2(0.5f, 0.5f));

            RectTransform topLine = CreateImage(
                canvasObject.transform, "TopSignalRail", new Color(Cyan.r, Cyan.g, Cyan.b, 0.72f));
            SetRect(topLine, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -3f), new Vector2(0f, 3f), new Vector2(0.5f, 1f));

            RectTransform threatRail = CreateImage(
                canvasObject.transform, "ThreatRail", Danger);
            SetRect(threatRail, new Vector2(1f, 1f), Vector2.one,
                new Vector2(-82f, -3f), new Vector2(164f, 3f), new Vector2(0.5f, 1f));

            RectTransform alertPulse = CreateImage(
                canvasObject.transform, "AlertPulse", Danger);
            SetRect(alertPulse, new Vector2(1f, 1f), Vector2.one,
                new Vector2(-620f, -54f), new Vector2(9f, 9f), new Vector2(0.5f, 0.5f));

            TMP_Text statusReadout = CreateText(
                canvasObject.transform,
                "SurveillanceReadout",
                "CAM 07  //  PASILLO CENTRAL  //  SEÑAL INESTABLE",
                13f,
                TextAlignmentOptions.TopRight,
                Danger);
            statusReadout.fontStyle = FontStyles.Bold;
            statusReadout.characterSpacing = 2.2f;
            SetRect(statusReadout.rectTransform, new Vector2(1f, 1f), Vector2.one,
                new Vector2(-84f, -45f), new Vector2(520f, 28f), new Vector2(1f, 1f));

            TMP_Text systemReadout = CreateText(
                canvasObject.transform,
                "SystemReadout",
                "UNFRZ_AI_LOCKDOWN  //  ACCESS NODE 00",
                12f,
                TextAlignmentOptions.BottomRight,
                new Color(TextMuted.r, TextMuted.g, TextMuted.b, 0.78f));
            systemReadout.characterSpacing = 2f;
            SetRect(systemReadout.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-82f, 42f), new Vector2(470f, 26f), new Vector2(1f, 0f));

            RectTransform mainPanel = CreatePanel(canvasObject.transform, "MainPanel");
            BuildMainPanel(mainPanel, out Button newGame, out Button controls,
                out Button credits, out Button quit);

            RectTransform controlsPanel = CreatePanel(canvasObject.transform, "ControlsPanel");
            Button controlsBack = BuildControlsPanel(controlsPanel);

            RectTransform creditsPanel = CreatePanel(canvasObject.transform, "CreditsPanel");
            Button creditsBack = BuildCreditsPanel(creditsPanel);

            controlsPanel.gameObject.SetActive(false);
            creditsPanel.gameObject.SetActive(false);

            MainMenuController controller = canvasObject.GetComponent<MainMenuController>();
            SerializedObject serializedController = new SerializedObject(controller);
            SetString(serializedController, "bootstrapScenePath", BootstrapPath);
            SetObject(serializedController, "mainPanel", mainPanel.gameObject);
            SetObject(serializedController, "controlsPanel", controlsPanel.gameObject);
            SetObject(serializedController, "creditsPanel", creditsPanel.gameObject);
            SetObject(serializedController, "newGameButton", newGame);
            SetObject(serializedController, "controlsButton", controls);
            SetObject(serializedController, "creditsButton", credits);
            SetObject(serializedController, "quitButton", quit);
            SetObject(serializedController, "controlsBackButton", controlsBack);
            SetObject(serializedController, "creditsBackButton", creditsBack);
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            MainMenuAmbienceController ambience =
                canvasObject.GetComponent<MainMenuAmbienceController>();
            SerializedObject serializedAmbience = new SerializedObject(ambience);
            SetObject(serializedAmbience, "background", background);
            SetObject(serializedAmbience, "flickerOverlay", flicker.GetComponent<Image>());
            SetObject(serializedAmbience, "scanline", scanline);
            SetObject(serializedAmbience, "alertPulse", alertPulse.GetComponent<Image>());
            SetObject(serializedAmbience, "statusReadout", statusReadout);
            serializedAmbience.ApplyModifiedPropertiesWithoutUndo();

            GameObject eventSystemObject = new GameObject(
                "EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            if (!EditorSceneManager.SaveScene(scene, MainMenuPath))
            {
                throw new InvalidOperationException("Unity could not save MainMenu.");
            }
        }

        private static void BuildMainPanel(
            Transform panel,
            out Button newGame,
            out Button controls,
            out Button credits,
            out Button quit)
        {
            TMP_Text kicker = CreateText(panel, "Kicker",
                "SISTEMA DE EMERGENCIA  //  NIVEL ROJO", 15f,
                TextAlignmentOptions.Left, Danger);
            kicker.fontStyle = FontStyles.Bold;
            kicker.characterSpacing = 3.2f;
            SetRect(kicker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(120f, -102f), new Vector2(760f, 30f), new Vector2(0f, 1f));

            TMP_Text title = CreateText(panel, "TitleEscape", "ESCAPE", 104f,
                TextAlignmentOptions.Left, TextPrimary);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = -1.5f;
            title.fontSharedMaterial = LoadFontMaterial(OutlineFontMaterialPath);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(112f, -132f), new Vector2(760f, 124f), new Vector2(0f, 1f));

            TMP_Text institution = CreateText(panel, "TitleInstitution", "UNIFRANZ", 40f,
                TextAlignmentOptions.Left, Cyan);
            institution.fontStyle = FontStyles.Bold;
            institution.characterSpacing = 10f;
            institution.fontSharedMaterial = LoadFontMaterial(ShadowFontMaterialPath);
            SetRect(institution.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(120f, -242f), new Vector2(760f, 54f), new Vector2(0f, 1f));

            TMP_Text subtitle = CreateText(panel, "Subtitle",
                "LA UNIVERSIDAD TOMADA POR LA IA", 18f,
                TextAlignmentOptions.Left, TextMuted);
            subtitle.fontStyle = FontStyles.Bold;
            subtitle.characterSpacing = 3.6f;
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(121f, -302f), new Vector2(760f, 36f), new Vector2(0f, 1f));

            RectTransform rail = CreateImage(panel, "MenuRail", Cyan);
            SetRect(rail, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(88f, -82f), new Vector2(3f, 440f), new Vector2(0f, 0.5f));

            RectTransform railAlert = CreateImage(panel, "MenuRailAlert", Danger);
            SetRect(railAlert, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(88f, 118f), new Vector2(3f, 40f), new Vector2(0f, 0.5f));

            newGame = CreateButton(panel, "NewGame", "01  //  NUEVA PARTIDA",
                new Vector2(340f, -30f), Cyan);
            controls = CreateButton(panel, "Controls", "02  //  CONTROLES",
                new Vector2(340f, -104f), Cyan);
            credits = CreateButton(panel, "Credits", "03  //  CRÉDITOS",
                new Vector2(340f, -178f), Cyan);
            quit = CreateButton(panel, "Quit", "04  //  SALIR",
                new Vector2(340f, -252f), Danger);

            TMP_Text footer = CreateText(panel, "Footer",
                "ENLACE COMPROMETIDO  //  IA HOSTIL EN RED", 13f,
                TextAlignmentOptions.Left, Danger);
            footer.fontStyle = FontStyles.Bold;
            footer.characterSpacing = 2.2f;
            SetRect(footer.rectTransform, Vector2.zero, Vector2.zero,
                new Vector2(120f, 48f), new Vector2(760f, 30f), Vector2.zero);
        }

        private static Button BuildControlsPanel(Transform panel)
        {
            RectTransform card = CreateCard(panel, "ControlsCard", new Vector2(760f, 650f));
            CreateCardTitle(card, "CONTROLES", "ENTRADA // OPERADOR");
            string[] controls =
            {
                "MOVERSE", "WASD / FLECHAS",
                "INTERACTUAR", "E (MANTENER)",
                "PAUSA / VOLVER", "ESC",
                "MAPA DEL CAMPUS", "M",
                "ESCÁNER", "TAB"
            };
            for (int index = 0; index < controls.Length; index += 2)
            {
                float y = 105f - (index / 2) * 64f;
                TMP_Text action = CreateText(card, "Action" + index, controls[index], 17f,
                    TextAlignmentOptions.Left, TextMuted);
                action.fontStyle = FontStyles.Bold;
                action.characterSpacing = 1.8f;
                SetRect(action.rectTransform, new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(-260f, y),
                    new Vector2(310f, 36f), new Vector2(0f, 0.5f));
                TMP_Text key = CreateText(card, "Key" + index, controls[index + 1], 18f,
                    TextAlignmentOptions.Right, Cyan);
                key.fontStyle = FontStyles.Bold;
                key.characterSpacing = 1.4f;
                SetRect(key.rectTransform, new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(260f, y),
                    new Vector2(310f, 36f), new Vector2(1f, 0.5f));
            }
            return CreateButton(card, "Back", "VOLVER", new Vector2(0f, -245f), Cyan, 320f);
        }

        private static Button BuildCreditsPanel(Transform panel)
        {
            RectTransform card = CreateCard(panel, "CreditsCard", new Vector2(820f, 650f));
            CreateCardTitle(card, "CRÉDITOS", "ARCHIVO // PROYECTO");
            TMP_Text body = CreateText(card, "Body",
                "EscapeUNIFRANZ\n\nPROYECTO ACADÉMICO\nPROGRAMACIÓN GRÁFICA Y MULTIMEDIA\nUNIFRANZ\n\nGRUPO / INTEGRANTES\n[EDITAR AQUÍ]",
                23f, TextAlignmentOptions.Center, TextPrimary);
            body.fontStyle = FontStyles.Bold;
            body.characterSpacing = 1.3f;
            body.lineSpacing = 6f;
            body.textWrappingMode = TextWrappingModes.Normal;
            SetRect(body.rectTransform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 10f),
                new Vector2(680f, 360f), new Vector2(0.5f, 0.5f));
            return CreateButton(card, "Back", "VOLVER", new Vector2(0f, -245f), Cyan, 320f);
        }

        private static void CreateCardTitle(Transform card, string titleText, string kickerText)
        {
            RectTransform accent = CreateImage(card, "Accent", Cyan);
            SetRect(accent, new Vector2(0f, 1f), Vector2.one,
                Vector2.zero, new Vector2(0f, 5f), new Vector2(0.5f, 1f));
            TMP_Text kicker = CreateText(card, "Kicker", kickerText, 14f,
                TextAlignmentOptions.TopLeft, Cyan);
            kicker.fontStyle = FontStyles.Bold;
            kicker.characterSpacing = 2.8f;
            SetRect(kicker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, -42f), new Vector2(500f, 28f), new Vector2(0f, 1f));
            TMP_Text title = CreateText(card, "Title", titleText, 40f,
                TextAlignmentOptions.TopLeft, TextPrimary);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 2f;
            title.fontSharedMaterial = LoadFontMaterial(OutlineFontMaterialPath);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(46f, -82f), new Vector2(600f, 62f), new Vector2(0f, 1f));
        }

        private static RectTransform CreatePanel(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            Stretch(rect);
            return rect;
        }

        private static RectTransform CreateCard(Transform parent, string name, Vector2 size)
        {
            RectTransform card = CreateImage(parent, name, Surface);
            SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, size, new Vector2(0.5f, 0.5f));
            return card;
        }

        private static RectTransform CreateImage(Transform parent, string name, Color color)
        {
            GameObject gameObject = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return gameObject.GetComponent<RectTransform>();
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject gameObject = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.font = LoadFont();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            Color accent,
            float width = 440f)
        {
            RectTransform rect = CreateImage(parent, name, Raised);
            SetRect(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                position, new Vector2(width, 60f), new Vector2(0.5f, 0.5f));
            Button button = rect.gameObject.AddComponent<Button>();
            Image target = rect.GetComponent<Image>();
            target.raycastTarget = true;
            button.targetGraphic = target;
            ColorBlock colors = button.colors;
            colors.normalColor = Raised;
            colors.highlightedColor = new Color(accent.r * 0.45f,
                accent.g * 0.45f, accent.b * 0.45f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(accent.r * 0.70f,
                accent.g * 0.70f, accent.b * 0.70f, 1f);
            colors.disabledColor = new Color(0.10f, 0.12f, 0.13f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            RectTransform edge = CreateImage(rect, "Accent", accent);
            SetRect(edge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(6f, 60f), new Vector2(0f, 0.5f));
            TMP_Text text = CreateText(rect, "Label", label, 18f,
                TextAlignmentOptions.Left, TextPrimary);
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 2.2f;
            text.fontSharedMaterial = LoadFontMaterial(ShadowFontMaterialPath);
            SetRect(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(28f, 0f), new Vector2(width - 48f, 44f), new Vector2(0f, 0.5f));
            return button;
        }

        private static TMP_FontAsset LoadFont()
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            string guid = AssetDatabase.FindAssets("t:TMP_FontAsset")
                .FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException("No TMP font asset is available.");
            }
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static Material LoadFontMaterial(string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                throw new InvalidOperationException("Missing TMP material preset: " + path);
            }

            return material;
        }

        private static void Stretch(RectTransform rect)
        {
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Vector2(0.5f, 0.5f));
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
            rect.localScale = Vector3.one;
        }

        private static void UpdateBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.path != MainMenuPath)
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(MainMenuPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
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

        private static void SetObject(SerializedObject target, string property, UnityEngine.Object value)
        {
            SerializedProperty serializedProperty = target.FindProperty(property);
            if (serializedProperty == null)
            {
                throw new InvalidOperationException("Missing serialized property: " + property);
            }
            serializedProperty.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject target, string property, string value)
        {
            SerializedProperty serializedProperty = target.FindProperty(property);
            if (serializedProperty == null)
            {
                throw new InvalidOperationException("Missing serialized property: " + property);
            }
            serializedProperty.stringValue = value;
        }

        private static void SetInt(SerializedObject target, string property, int value)
        {
            SerializedProperty serializedProperty = target.FindProperty(property);
            if (serializedProperty == null)
            {
                throw new InvalidOperationException("Missing serialized property: " + property);
            }
            serializedProperty.intValue = value;
        }

        private static void SetBool(SerializedObject target, string property, bool value)
        {
            SerializedProperty serializedProperty = target.FindProperty(property);
            if (serializedProperty == null)
            {
                throw new InvalidOperationException("Missing serialized property: " + property);
            }
            serializedProperty.boolValue = value;
        }

        private sealed class DirectionAssets
        {
            public DirectionAssets(Sprite[] sprites, AnimationClip idle, AnimationClip walk)
            {
                Sprites = sprites;
                Idle = idle;
                Walk = walk;
            }

            public Sprite[] Sprites { get; }
            public AnimationClip Idle { get; }
            public AnimationClip Walk { get; }
        }
    }
}
#endif
