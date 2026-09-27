#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Input;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.EditorTools
{
    /// <summary>
    /// Reproducible graybox assembly for TAREA 009. It preserves existing Hall/ARCA roots.
    /// </summary>
    public static class GameplayAdvancedSceneBuilder
    {
        private const string BootstrapPath = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        private const string ZonesFolder = "Assets/_Project/Scenes/Zones";
        private const string MapDataPath = "Assets/_Project/Data/Map/CampusMapData.asset";
        private const string ToggleMapReferencePath =
            "Assets/_Project/Data/Input/PlayerToggleMapReference.asset";
        private const string ScanReferencePath =
            "Assets/_Project/Data/Input/PlayerScanReference.asset";
        private const string GrayboxSpritePath =
            "Assets/_Project/Resources/Presentation/GrayboxFullRect.asset";
        private const string TmpEssentialResourcesGuid = "ce4ff17ca867d2b48b5c8a4181611901";

        private static readonly string[] BuildScenePaths =
        {
            BootstrapPath,
            ZonesFolder + "/Z00_Entrada.unity",
            ZonesFolder + "/Z01_Hall.unity",
            ZonesFolder + "/Z02_ARCA.unity",
            ZonesFolder + "/Z03_Piso2.unity",
            ZonesFolder + "/Z04_Piso3.unity",
            ZonesFolder + "/Z05_LabSoftware.unity",
            ZonesFolder + "/Z06_NucleoIA.unity"
        };

        private static Sprite grayboxSprite;

        [MenuItem("Escape UNIFRANZ/TAREA 009/Build Gameplay Advanced Graybox")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before building TAREA 009 content.");
            }

            EnsureFolder("Assets/_Project/Data/Map");
            CampusMapData mapData = BuildCampusMapData();
            InputActionReference toggleMapReference = BuildToggleMapReference();
            InputActionReference scanReference = BuildScanReference();

            BuildEntrada();
            BuildHall();
            BuildArca();
            BuildPiso2();
            BuildPiso3();
            BuildLabSoftware();
            BuildNucleoIA();
            UpdateZoneCatalog();
            mapData = AssetDatabase.LoadAssetAtPath<CampusMapData>(MapDataPath);
            toggleMapReference = AssetDatabase.LoadAssetAtPath<InputActionReference>(
                ToggleMapReferencePath);
            scanReference = AssetDatabase.LoadAssetAtPath<InputActionReference>(
                ScanReferencePath);
            BuildBootstrap(mapData, toggleMapReference, scanReference);
            VerticalSlicePresentationBuilder.ApplyAll();
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);
            Debug.Log("TAREA 009 graybox assembly completed.");
        }

        private static void BuildEntrada()
        {
            ZoneParts zone = OpenZone(
                "Z00_Entrada",
                "entrada",
                "entrada_ingresar",
                "Ingresa a la universidad.");
            CreateSpawn(zone.Spawns, "from_bootstrap", new Vector2(-5.5f, 0f));
            CreateSpawn(zone.Spawns, "from_hall", new Vector2(5f, 0f));
            CreateRespawnPoint(zone.Spawns, "entrada", "entrada_inicio", new Vector2(-5.5f, 0f));
            CreateExit(zone.Interactables, "AccesoHall", new Vector2(6.3f, 0f),
                "[E] Ingresar al Hall", "hall", "from_entrada");
            CreateWorldLabel(zone.Environment, "EntradaLabel", "ENTRADA UNIFRANZ", new Vector2(0f, 2.8f));
            SaveZone(zone);
        }

        private static void BuildHall()
        {
            ZoneParts zone = OpenZone(
                "Z01_Hall",
                "hall",
                "hall_explorar",
                "Explora el Hall.");
            CreateSpawn(zone.Spawns, "from_entrada", new Vector2(-5.5f, 0f));
            CreateSpawn(zone.Spawns, "from_piso2", new Vector2(0f, 2.8f));
            CreateRespawnPoint(zone.Spawns, "hall", "hall_principal", Vector2.zero);
            CreateExit(zone.Interactables, "ExitToEntrada", new Vector2(-6.3f, 0f),
                "[E] Volver a la entrada", "entrada", "from_hall");
            CreateExit(zone.Interactables, "ExitToPiso2", new Vector2(0f, 3.35f),
                "[E] Acceder al segundo piso", "piso2", "from_hall",
                GameFlagId.Piso2Unlocked, "Acceso restringido por el sistema.");
            CreateWorldLabel(zone.Environment, "HallConnectionsLabel",
                "ENTRADA  ←  HALL  →  ARCA     ↑ PISO 2", new Vector2(0f, 2.8f));
            SaveZone(zone);
        }

        private static void BuildArca()
        {
            ZoneParts zone = OpenZone(
                "Z02_ARCA",
                "arca",
                "arca_restaurar",
                "Restablece el acceso al segundo piso.");
            CreateRespawnPoint(zone.Spawns, "arca", "arca_principal", new Vector2(-4.5f, 0f));
            GameObject terminal = CreateInteractableBox(
                zone.Interactables,
                "TerminalAccesoARCA",
                new Vector2(2f, 0.5f),
                new Vector2(1.4f, 1.8f),
                new Color(0.12f, 0.75f, 0.8f),
                "[E] Restablecer acceso");
            ProgressionFlagInteractable progression = GetOrAdd<ProgressionFlagInteractable>(terminal);
            ConfigurePrompt(progression, "[E] Restablecer acceso");
            SetEnum(progression, "completionFlag", GameFlagId.ArcaAccessRestored);
            SetEnum(progression, "unlockedFlag", GameFlagId.Piso2Unlocked);
            SetString(progression, "successMessage", "Acceso al segundo piso restablecido.");
            SetString(progression, "nextObjectiveId", "hall_acceder_piso2");
            SetString(progression, "nextObjectiveText", "Regresa al Hall y accede al segundo piso.");
            CreateWorldLabel(zone.Environment, "ArcaLabel", "ARCA — TERMINAL DE ACCESO", new Vector2(0f, 2.8f));
            SaveZone(zone);
        }

        private static void BuildPiso2()
        {
            ZoneParts zone = OpenZone(
                "Z03_Piso2",
                "piso2",
                "piso2_exoleg",
                "Desactiva el exoesqueleto.");
            CreateSpawn(zone.Spawns, "from_hall", new Vector2(-5.5f, -2.2f));
            CreateSpawn(zone.Spawns, "from_piso3", new Vector2(5.2f, 2.2f));
            CreateRespawnPoint(zone.Spawns, "piso2", "piso2_entrada", new Vector2(-4.5f, -2.2f));
            CreateRespawnPoint(zone.Spawns, "piso2", "piso2_antes_exoleg", new Vector2(-0.5f, 2.7f));
            CreateExit(zone.Interactables, "ExitToHall", new Vector2(-6.3f, -2.2f),
                "[E] Volver al Hall", "hall", "from_piso2");
            CreateExit(zone.Interactables, "ExitToPiso3", new Vector2(6.3f, 2.2f),
                "[E] Acceder al tercer piso", "piso3", "from_piso2",
                GameFlagId.Piso3Unlocked, "El EXOLEG bloquea el acceso.");

            EncounterContext routeEncounter = CreateEncounterContext(
                zone.Hazards, "MiniAutoEncounter", "piso2_entrada");
            CreateMiniAuto(routeEncounter.transform, "MiniAuto", new Vector2(-3f, -1f),
                new[] { new Vector2(-3f, -1f), new Vector2(-1.2f, -1f), new Vector2(-3f, 1f) },
                routeEncounter);
            CreateExoleg(zone.Encounter);
            CreateWorldLabel(zone.Environment, "Piso2Label", "PISO 2 — HARDWARE", new Vector2(0f, 2.9f));
            SaveZone(zone);
        }

        private static void BuildPiso3()
        {
            ZoneParts zone = OpenZone(
                "Z04_Piso3",
                "piso3",
                "piso3_perro",
                "Desconecta al perro robot.");
            CreateSpawn(zone.Spawns, "from_piso2", new Vector2(-5.5f, -2.2f));
            CreateSpawn(zone.Spawns, "from_lab", new Vector2(5.2f, 2.2f));
            CreateRespawnPoint(zone.Spawns, "piso3", "piso3_entrada", new Vector2(-4.5f, -2.2f));
            CreateRespawnPoint(zone.Spawns, "piso3", "piso3_antes_robotdog", new Vector2(-2.8f, -2.5f));
            CreateExit(zone.Interactables, "ExitToPiso2", new Vector2(-6.3f, -2.2f),
                "[E] Volver al segundo piso", "piso2", "from_piso3");
            CreateExit(zone.Interactables, "ExitToLab", new Vector2(6.3f, 2.2f),
                "[E] Acceder al laboratorio", "lab_software", "from_piso3",
                GameFlagId.LabSoftwareUnlocked, "La red de vigilancia bloquea el laboratorio.");

            EncounterContext routeEncounter = CreateEncounterContext(
                zone.Hazards, "Piso3MiniAutoEncounter", "piso3_entrada");
            CreateMiniAuto(routeEncounter.transform, "MiniAutoPiso3", new Vector2(-3.2f, 1.5f),
                new[] { new Vector2(-3.2f, 1.5f), new Vector2(-1.2f, 1.5f) }, routeEncounter);
            CreateRobotDog(zone.Encounter);
            CreateWorldLabel(zone.Environment, "Piso3Label", "PISO 3 — REDES", new Vector2(0f, 2.9f));
            SaveZone(zone);
        }

        private static void BuildLabSoftware()
        {
            ZoneParts zone = OpenZone(
                "Z05_LabSoftware",
                "lab_software",
                "lab_brazos",
                "Desactiva el sistema de manipulación.");
            CreateSpawn(zone.Spawns, "from_piso3", new Vector2(-5.5f, -2.2f));
            CreateSpawn(zone.Spawns, "from_nucleo", new Vector2(5.2f, 2.2f));
            CreateRespawnPoint(zone.Spawns, "lab_software", "lab_entrada", new Vector2(-4.5f, -2.2f));
            CreateRespawnPoint(zone.Spawns, "lab_software", "lab_antes_brazos", new Vector2(-0.7f, 0f));
            CreateExit(zone.Interactables, "ExitToPiso3", new Vector2(-6.3f, -2.2f),
                "[E] Volver al tercer piso", "piso3", "from_lab");
            CreateExit(zone.Interactables, "ExitToNucleo", new Vector2(6.3f, 2.2f),
                "[E] Acceder al núcleo IA", "nucleo_ia", "from_lab",
                GameFlagId.NucleoUnlocked, "Los brazos bloquean el acceso al núcleo.");
            CreateRobotArms(zone.Encounter);
            CreateWorldLabel(zone.Environment, "LabLabel", "LAB SOFTWARE — MANIPULACIÓN", new Vector2(0f, 2.9f));
            SaveZone(zone);
        }

        private static void BuildNucleoIA()
        {
            ZoneParts zone = OpenZone(
                "Z06_NucleoIA",
                "nucleo_ia",
                "nucleo_francis",
                "Detén a Francis y apaga la IA de forma segura.",
                new Color(0.08f, 0.03f, 0.16f));
            CreateSpawn(zone.Spawns, "from_lab", new Vector2(-5.5f, -2.2f));
            CreateRespawnPoint(zone.Spawns, "nucleo_ia", "nucleo_entrada", new Vector2(-5.2f, -2.2f));
            CreateRespawnPoint(zone.Spawns, "nucleo_ia", "francis_inicio", new Vector2(-3.7f, -2.2f));
            CreateRespawnPoint(zone.Spawns, "nucleo_ia", "francis_fase2", new Vector2(-2.8f, 0f));
            CreateRespawnPoint(zone.Spawns, "nucleo_ia", "francis_fase3", new Vector2(-2.8f, 2f));
            GameObject exit = CreateExit(zone.Interactables, "ExitToLab", new Vector2(-6.3f, -2.2f),
                "[E] Volver al laboratorio", "lab_software", "from_nucleo");
            DisableByFlag lockAfterStart = GetOrAdd<DisableByFlag>(exit);
            SetEnum(lockAfterStart, "disabledFlag", GameFlagId.FrancisEncounterStarted);
            SetObjectArray(lockAfterStart, "behaviours", new UnityEngine.Object[]
            {
                exit.GetComponent<ZoneExitInteractable>()
            });
            CreateFrancis(zone.Encounter);
            CreateWorldLabel(zone.Environment, "NucleoLabel", "NÚCLEO IA — APAGADO SEGURO", new Vector2(0f, 2.9f));
            SaveZone(zone);
        }

        private static ZoneParts OpenZone(
            string sceneName,
            string zoneId,
            string objectiveId,
            string objectiveText,
            Color? backgroundColor = null)
        {
            string path = ZonesFolder + "/" + sceneName + ".unity";
            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null
                ? EditorSceneManager.OpenScene(path, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = FindRoot(scene, sceneName) ?? new GameObject(sceneName);
            SceneManager.MoveGameObjectToScene(root, scene);
            ZoneContext zoneContext = GetOrAdd<ZoneContext>(root);
            SetString(zoneContext, "zoneId", zoneId);
            SetString(zoneContext, "displayName", ResolveZoneDisplayName(zoneId));
            SetString(zoneContext, "defaultCheckpointSpawnId", ResolveDefaultCheckpoint(zoneId));
            SetString(zoneContext, "objectiveId", objectiveId);
            SetString(zoneContext, "objectiveText", objectiveText);
            SetInt(zoneContext, "objectiveTotal", ResolveObjectiveTotal(zoneId));
            SetVector2(zoneContext, "minimapCenter", Vector2.zero);
            SetVector2(zoneContext, "minimapSize", new Vector2(16f, 9f));

            Transform environment = FindOrCreateChild(root.transform, "Environment");
            Transform collision = FindOrCreateChild(root.transform, "WorldCollision");
            Transform spawns = FindOrCreateChild(root.transform, "SpawnPoints");
            Transform interactables = FindOrCreateChild(root.transform, "Interactables");
            Transform hazards = FindOrCreateChild(root.transform, "Hazards");
            Transform encounter = FindOrCreateChild(root.transform, "Encounter");
            FindOrCreateChild(root.transform, "Camera");

            if (FindRecursive(environment, "GrayboxBackground") == null)
            {
                CreateBox(environment, "GrayboxBackground", Vector2.zero, new Vector2(16f, 9f),
                    backgroundColor ?? new Color(0.12f, 0.17f, 0.24f), false, -20);
            }

            CreateWall(collision, "WallLeft", new Vector2(-7.9f, 0f), new Vector2(0.3f, 9f));
            CreateWall(collision, "WallRight", new Vector2(7.9f, 0f), new Vector2(0.3f, 9f));
            CreateWall(collision, "WallTop", new Vector2(0f, 4.4f), new Vector2(16f, 0.3f));
            CreateWall(collision, "WallBottom", new Vector2(0f, -4.4f), new Vector2(16f, 0.3f));

            if (FindRecursive(root.transform, "Global Light 2D") == null)
            {
                Type lightComponentType = Type.GetType(
                    "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
                GameObject lightObject = lightComponentType != null
                    ? new GameObject("Global Light 2D", lightComponentType)
                    : new GameObject("Global Light 2D");
                lightObject.transform.SetParent(environment, false);

                if (lightComponentType != null)
                {
                    Component light = lightObject.GetComponent(lightComponentType);
                    var lightTypeProperty = lightComponentType.GetProperty("lightType");
                    var intensityProperty = lightComponentType.GetProperty("intensity");
                    lightTypeProperty?.SetValue(
                        light,
                        Enum.Parse(lightTypeProperty.PropertyType, "Global"));
                    intensityProperty?.SetValue(light, 1f);
                }
            }

            return new ZoneParts(path, scene, root, environment, collision, spawns,
                interactables, hazards, encounter);
        }

        private static void SaveZone(ZoneParts zone)
        {
            ConfigureZonePresentation(zone);
            EditorSceneManager.MarkSceneDirty(zone.Scene);
            if (!EditorSceneManager.SaveScene(zone.Scene, zone.Path))
            {
                throw new InvalidOperationException("Could not save scene " + zone.Path);
            }
        }

        private static void ConfigureZonePresentation(ZoneParts zone)
        {
            ZoneContext context = zone.Root.GetComponent<ZoneContext>();
            string zoneId = context != null ? context.ZoneId : string.Empty;

            ZoneExitInteractable[] exits = zone.Root.GetComponentsInChildren<ZoneExitInteractable>(true);
            for (int index = 0; index < exits.Length; index++)
            {
                ZoneExitInteractable exit = exits[index];
                string objectiveId = string.Empty;
                if (zoneId == "entrada" && exit.TargetZoneId == "hall") objectiveId = "entrada_ingresar";
                if (zoneId == "hall" && exit.TargetZoneId == "arca") objectiveId = "hall_explorar";
                ConfigureTrackable(exit.gameObject, "exit_" + exit.TargetZoneId,
                    true, true, objectiveId, string.Empty, false);
                ConfigureScannable(exit.gameObject, ScanCategory.Door, "PUERTA", string.Empty);
            }

            RespawnPoint2D[] checkpoints = zone.Root.GetComponentsInChildren<RespawnPoint2D>(true);
            for (int index = 0; index < checkpoints.Length; index++)
            {
                RespawnPoint2D checkpoint = checkpoints[index];
                ConfigureTrackable(checkpoint.gameObject, "checkpoint_" + checkpoint.RespawnId,
                    true, false, string.Empty, checkpoint.RespawnId, false);
                ConfigureScannable(checkpoint.gameObject, ScanCategory.Checkpoint, "CHECKPOINT", string.Empty);
            }

            ProgressionFlagInteractable[] terminals =
                zone.Root.GetComponentsInChildren<ProgressionFlagInteractable>(true);
            for (int index = 0; index < terminals.Length; index++)
            {
                string objectiveId = zoneId == "arca" ? "arca_restaurar" : string.Empty;
                ConfigureTrackable(terminals[index].gameObject, "terminal_" + terminals[index].name,
                    true, false, objectiveId, string.Empty, false);
                ConfigureScannable(terminals[index].gameObject, ScanCategory.Terminal,
                    "TERMINAL — RED", "[ERROR]");
            }

            ExolegOverloadInteractable[] overloads =
                zone.Root.GetComponentsInChildren<ExolegOverloadInteractable>(true);
            for (int index = 0; index < overloads.Length; index++)
            {
                ConfigureTrackable(overloads[index].gameObject, "overload_" + index,
                    true, false, "piso2_exoleg", string.Empty, false);
                ConfigureScannable(overloads[index].gameObject, ScanCategory.Actuator,
                    "ACTUADOR", "[ENERGIZADO]");
            }

            RobotDogNetworkNodeInteractable[] networkNodes =
                zone.Root.GetComponentsInChildren<RobotDogNetworkNodeInteractable>(true);
            for (int index = 0; index < networkNodes.Length; index++)
            {
                ConfigureTrackable(networkNodes[index].gameObject, "network_node_" + index,
                    true, false, "piso3_perro", string.Empty, false);
                ConfigureScannable(networkNodes[index].gameObject, ScanCategory.NetworkNode,
                    "NODO — RED", string.Empty);
            }

            RobotArmsPanelInteractable[] panels =
                zone.Root.GetComponentsInChildren<RobotArmsPanelInteractable>(true);
            for (int index = 0; index < panels.Length; index++)
            {
                ConfigureTrackable(panels[index].gameObject, "arms_panel_" + index,
                    true, false, "lab_brazos", string.Empty, false);
                ConfigureScannable(panels[index].gameObject, ScanCategory.Terminal,
                    "TERMINAL — HARDWARE", "[ENERGIZADO]");
            }

            FrancisBossController[] francisControllers =
                zone.Root.GetComponentsInChildren<FrancisBossController>(true);
            for (int index = 0; index < francisControllers.Length; index++)
            {
                ConfigureTrackable(francisControllers[index].gameObject, "francis_arena",
                    true, false, "nucleo_francis", string.Empty, false);
            }

            FrancisSystemNodeInteractable[] francisNodes =
                zone.Root.GetComponentsInChildren<FrancisSystemNodeInteractable>(true);
            for (int index = 0; index < francisNodes.Length; index++)
            {
                string objectiveId = francisNodes[index].name.Contains("Francis_0_")
                    ? "francis_PhaseNetwork"
                    : francisNodes[index].name.Contains("Francis_1_")
                        ? "francis_PhaseHardware"
                        : "francis_PhaseShutdown";
                ConfigureTrackable(francisNodes[index].gameObject, "francis_node_" + index,
                    true, false, objectiveId, string.Empty, false);
                ConfigureScannable(francisNodes[index].gameObject, ScanCategory.NetworkNode,
                    "NODO — SISTEMA", "[CONECTADO]");
            }

            DetectionSensor2D[] sensors = zone.Root.GetComponentsInChildren<DetectionSensor2D>(true);
            for (int index = 0; index < sensors.Length; index++)
            {
                GameObject threat = sensors[index].transform.parent != null
                    ? sensors[index].transform.parent.gameObject
                    : sensors[index].gameObject;
                ConfigureTrackable(threat, "threat_" + threat.name,
                    false, false, string.Empty, string.Empty, true);
                ConfigureScannable(threat, ScanCategory.Sensor,
                    "SENSOR — VIGILANCIA", "[ACTIVO]");
                ThreatAlertIndicator2D alert = GetOrAdd<ThreatAlertIndicator2D>(threat);
                SetObject(alert, "sensor", sensors[index]);
            }

            TimedHazardArea2D[] hazards = zone.Root.GetComponentsInChildren<TimedHazardArea2D>(true);
            for (int index = 0; index < hazards.Length; index++)
            {
                ConfigureScannable(hazards[index].gameObject, ScanCategory.Actuator,
                    "ACTUADOR — PELIGRO", "[EN ESPERA]");
            }
        }

        private static MinimapTrackable2D ConfigureTrackable(
            GameObject gameObject,
            string markerId,
            bool initiallyDiscovered,
            bool showAsExit,
            string objectiveId,
            string checkpointId,
            bool showAsThreat)
        {
            MinimapTrackable2D trackable = GetOrAdd<MinimapTrackable2D>(gameObject);
            SetString(trackable, "markerId", markerId);
            SetBool(trackable, "initiallyDiscovered", initiallyDiscovered);
            SetBool(trackable, "secret", false);
            SetBool(trackable, "showAsExit", showAsExit);
            SetString(trackable, "objectiveId", objectiveId);
            SetString(trackable, "checkpointId", checkpointId);
            SetBool(trackable, "showAsThreat", showAsThreat);
            return trackable;
        }

        private static Scannable2D ConfigureScannable(
            GameObject gameObject,
            ScanCategory category,
            string shortLabel,
            string fallbackState)
        {
            Scannable2D scannable = GetOrAdd<Scannable2D>(gameObject);
            SetEnum(scannable, "category", category);
            SetString(scannable, "shortLabel", shortLabel);
            SetString(scannable, "fallbackState", fallbackState);
            SetBool(scannable, "secret", false);
            SetEnum(scannable, "revealFlag", GameFlagId.None);
            return scannable;
        }

        private static string ResolveZoneDisplayName(string zoneId)
        {
            switch (zoneId)
            {
                case "entrada": return "Entrada UNIFRANZ";
                case "hall": return "Hall Principal";
                case "arca": return "ARCA — Control de Acceso";
                case "piso2": return "Segundo Piso — Hardware";
                case "piso3": return "Tercer Piso — Redes";
                case "lab_software": return "Laboratorio de Software";
                case "nucleo_ia": return "Núcleo IA";
                default: return zoneId;
            }
        }

        private static string ResolveDefaultCheckpoint(string zoneId)
        {
            switch (zoneId)
            {
                case "entrada": return "entrada_inicio";
                case "hall": return "hall_principal";
                case "arca": return "arca_principal";
                case "piso2": return "piso2_entrada";
                case "piso3": return "piso3_entrada";
                case "lab_software": return "lab_entrada";
                case "nucleo_ia": return "nucleo_entrada";
                default: return string.Empty;
            }
        }

        private static int ResolveObjectiveTotal(string zoneId)
        {
            switch (zoneId)
            {
                case "piso2": return 3;
                case "piso3": return 2;
                case "nucleo_ia": return 3;
                default: return 0;
            }
        }

        private static void CreateExoleg(Transform parent)
        {
            Transform root = FindOrCreateChild(parent, "ExolegEncounter");
            EncounterContext context = GetOrAdd<EncounterContext>(root.gameObject);
            SetString(context, "checkpointSpawnId", "piso2_antes_exoleg");

            CreateBox(root, "EXOLEG_Visual", new Vector2(3.7f, 0f), new Vector2(1.6f, 2.4f),
                new Color(0.85f, 0.25f, 0.12f), true, 2);
            GameObject controllerObject = FindOrCreateChild(root, "ExolegArena").gameObject;
            controllerObject.transform.position = new Vector3(2.8f, 0f, 0f);
            BoxCollider2D arena = GetOrAdd<BoxCollider2D>(controllerObject);
            arena.isTrigger = true;
            arena.size = new Vector2(7f, 6f);
            ExolegEncounterController controller = GetOrAdd<ExolegEncounterController>(controllerObject);
            SetObject(controller, "encounter", context);

            TimedHazardArea2D[] areas =
            {
                CreateHazardArea(root, "HazardLeft", new Vector2(1.4f, -0.8f), new Vector2(1.6f, 4.6f), context),
                CreateHazardArea(root, "HazardRight", new Vector2(5.2f, -0.8f), new Vector2(1.6f, 4.6f), context),
                CreateHazardArea(root, "HazardCenter", new Vector2(3.3f, -0.8f), new Vector2(1.7f, 4.6f), context)
            };
            SetObjectArray(controller, "hazardAreas", areas);

            for (int index = 0; index < 3; index++)
            {
                GameObject node = CreateInteractableBox(root, "Sobrecarga_" + (index + 1),
                    new Vector2(1.4f + index * 1.9f, -3f), new Vector2(0.7f, 0.7f),
                    new Color(1f, 0.65f, 0.08f), "[E] Activar sobrecarga");
                ExolegOverloadInteractable interactable = GetOrAdd<ExolegOverloadInteractable>(node);
                SetObject(interactable, "controller", controller);
                ConfigurePrompt(interactable, "[E] Activar sobrecarga");
            }
        }

        private static void CreateRobotDog(Transform parent)
        {
            Transform root = FindOrCreateChild(parent, "RobotDogEncounter");
            EncounterContext context = GetOrAdd<EncounterContext>(root.gameObject);
            SetString(context, "checkpointSpawnId", "piso3_antes_robotdog");

            PatrolPath2D path = CreatePath(root, "DogPatrolPath",
                new[] { new Vector2(0.5f, -1.5f), new Vector2(4.3f, -1.5f), new Vector2(4.3f, 1.4f) });
            GameObject dog = CreateBox(root, "RobotDog", new Vector2(0.5f, -1.5f),
                new Vector2(1.2f, 0.8f), new Color(0.75f, 0.15f, 0.75f), true, 3);
            Rigidbody2D body = GetOrAdd<Rigidbody2D>(dog);
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            PatrolMover2D mover = GetOrAdd<PatrolMover2D>(dog);
            SetObject(mover, "path", path);
            SetFloat(mover, "patrolSpeed", 1.8f);

            GameObject sensorObject = FindOrCreateChild(dog.transform, "DetectionSensor").gameObject;
            CircleCollider2D circle = GetOrAdd<CircleCollider2D>(sensorObject);
            circle.isTrigger = true;
            circle.radius = 2.6f;
            DetectionSensor2D sensor = GetOrAdd<DetectionSensor2D>(sensorObject);

            GameObject contact = FindOrCreateChild(dog.transform, "ContactHazard").gameObject;
            BoxCollider2D contactCollider = GetOrAdd<BoxCollider2D>(contact);
            contactCollider.isTrigger = true;
            contactCollider.size = new Vector2(1.35f, 0.95f);
            HazardContact2D hitbox = GetOrAdd<HazardContact2D>(contact);
            SetObject(hitbox, "encounter", context);

            RobotDogEncounterController controller = GetOrAdd<RobotDogEncounterController>(dog);
            SetObject(controller, "encounter", context);
            SetObject(controller, "mover", mover);
            SetObject(controller, "sensor", sensor);

            for (int index = 0; index < 2; index++)
            {
                GameObject node = CreateInteractableBox(root, "NodoRed_" + (index == 0 ? "A" : "B"),
                    new Vector2(1.2f + index * 3.4f, 2.6f), new Vector2(0.9f, 0.9f),
                    new Color(0.1f, 0.8f, 0.95f), "[E] Desconectar nodo");
                RobotDogNetworkNodeInteractable interactable =
                    GetOrAdd<RobotDogNetworkNodeInteractable>(node);
                SetObject(interactable, "controller", controller);
                SetInt(interactable, "nodeIndex", index);
                ConfigurePrompt(interactable, "[E] Desconectar nodo");
            }
        }

        private static void CreateRobotArms(Transform parent)
        {
            Transform root = FindOrCreateChild(parent, "RobotArmsEncounter");
            EncounterContext context = GetOrAdd<EncounterContext>(root.gameObject);
            SetString(context, "checkpointSpawnId", "lab_antes_brazos");

            GameObject controllerObject = FindOrCreateChild(root, "ArmsArena").gameObject;
            controllerObject.transform.position = new Vector3(2.5f, 0f, 0f);
            BoxCollider2D arena = GetOrAdd<BoxCollider2D>(controllerObject);
            arena.isTrigger = true;
            arena.size = new Vector2(7f, 6f);
            RobotArmsEncounterController controller = GetOrAdd<RobotArmsEncounterController>(controllerObject);
            SetObject(controller, "encounter", context);

            TimedHazardArea2D[] hazards = new TimedHazardArea2D[3];
            for (int index = 0; index < hazards.Length; index++)
            {
                CreateBox(root, "BrazoVisual_" + (index + 1),
                    new Vector2(1.2f + index * 2f, 2.3f), new Vector2(0.55f, 2f),
                    new Color(0.9f, 0.45f, 0.12f), true, 2);
                hazards[index] = CreateHazardArea(root, "ArmHazard_" + (index + 1),
                    new Vector2(1.2f + index * 2f, -0.5f), new Vector2(1.2f, 4.6f), context);
            }
            SetObjectArray(controller, "armHazards", hazards);

            GameObject panel = CreateInteractableBox(root, "PanelBrazos", new Vector2(3.2f, -3f),
                new Vector2(1.4f, 0.8f), new Color(0.2f, 0.9f, 0.45f), "[E] Usar panel seguro");
            RobotArmsPanelInteractable interactable = GetOrAdd<RobotArmsPanelInteractable>(panel);
            SetObject(interactable, "controller", controller);
            ConfigurePrompt(interactable, "[E] Usar panel seguro");
        }

        private static void CreateFrancis(Transform parent)
        {
            Transform root = FindOrCreateChild(parent, "FrancisEncounter");
            EncounterContext context = GetOrAdd<EncounterContext>(root.gameObject);
            SetString(context, "checkpointSpawnId", "francis_inicio");

            GameObject controllerObject = FindOrCreateChild(root, "FrancisArenaTrigger").gameObject;
            controllerObject.transform.position = new Vector3(-1.5f, 0f, 0f);
            BoxCollider2D arena = GetOrAdd<BoxCollider2D>(controllerObject);
            arena.isTrigger = true;
            arena.size = new Vector2(2f, 8f);
            FrancisBossController controller = GetOrAdd<FrancisBossController>(controllerObject);
            SetObject(controller, "encounter", context);

            PatrolPath2D path = CreatePath(root, "FrancisPath",
                new[] { new Vector2(2f, 2f), new Vector2(5f, 0f), new Vector2(2f, -2f) });
            GameObject francis = CreateBox(root, "FrancisPlaceholder", new Vector2(2f, 2f),
                new Vector2(0.9f, 1.7f), new Color(0.95f, 0.15f, 0.2f), true, 5);
            Rigidbody2D body = GetOrAdd<Rigidbody2D>(francis);
            body.bodyType = RigidbodyType2D.Kinematic;
            PatrolMover2D mover = GetOrAdd<PatrolMover2D>(francis);
            SetObject(mover, "path", path);
            SetFloat(mover, "patrolSpeed", 1.4f);

            TimedHazardArea2D interference = CreateHazardArea(root, "FrancisInterference",
                new Vector2(3.5f, 0f), new Vector2(2f, 5f), context);
            SetObject(controller, "interferenceHazard", interference);

            string[][] labels =
            {
                new[] { "Cámaras", "Accesos", "Servidor" },
                new[] { "Alimentación", "Actuadores", "Seguridad" },
                new[] { "Conexión final", "Cortar actuadores", "Shutdown" }
            };
            string[][] prompts =
            {
                new[] { "[E] Aislar cámaras", "[E] Aislar accesos", "[E] Aislar servidor" },
                new[] { "[E] Cortar alimentación", "[E] Desenergizar actuadores", "[E] Estabilizar seguridad" },
                new[] { "[E] Aislar conexión final", "[E] Cortar actuadores", "[E] Ejecutar shutdown" }
            };
            Color[] colors =
            {
                new Color(0.1f, 0.75f, 1f),
                new Color(1f, 0.55f, 0.1f),
                new Color(0.3f, 0.95f, 0.45f)
            };

            for (int phase = 0; phase < 3; phase++)
            {
                for (int step = 0; step < 3; step++)
                {
                    float x = -0.2f + step * 2.4f;
                    float y = 2.7f - phase * 2.7f;
                    GameObject node = CreateInteractableBox(root,
                        "Francis_" + phase + "_" + step + "_" + labels[phase][step],
                        new Vector2(x, y), new Vector2(1.5f, 0.75f), colors[phase], prompts[phase][step]);
                    FrancisSystemNodeInteractable interactable =
                        GetOrAdd<FrancisSystemNodeInteractable>(node);
                    SetObject(interactable, "controller", controller);
                    SetEnum(interactable, "phase", (FrancisSystemPhase)phase);
                    SetInt(interactable, "stepIndex", step);
                    ConfigurePrompt(interactable, prompts[phase][step]);
                    CreateWorldLabel(node.transform, "Label", labels[phase][step], Vector2.zero, 0.11f);
                }
            }
        }

        private static void CreateMiniAuto(
            Transform parent,
            string name,
            Vector2 position,
            Vector2[] patrolPoints,
            EncounterContext encounter)
        {
            PatrolPath2D path = CreatePath(parent, name + "Path", patrolPoints);
            GameObject auto = CreateBox(parent, name, position, new Vector2(1.2f, 0.7f),
                new Color(0.95f, 0.75f, 0.12f), true, 3);
            Rigidbody2D body = GetOrAdd<Rigidbody2D>(auto);
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            PatrolMover2D mover = GetOrAdd<PatrolMover2D>(auto);
            SetObject(mover, "path", path);

            GameObject sensorObject = FindOrCreateChild(auto.transform, "DetectionSensor").gameObject;
            CircleCollider2D sensorCollider = GetOrAdd<CircleCollider2D>(sensorObject);
            sensorCollider.isTrigger = true;
            sensorCollider.radius = 2.2f;
            DetectionSensor2D sensor = GetOrAdd<DetectionSensor2D>(sensorObject);

            GameObject contact = FindOrCreateChild(auto.transform, "ContactHazard").gameObject;
            BoxCollider2D contactCollider = GetOrAdd<BoxCollider2D>(contact);
            contactCollider.isTrigger = true;
            contactCollider.size = new Vector2(1.35f, 0.85f);
            HazardContact2D hitbox = GetOrAdd<HazardContact2D>(contact);
            SetObject(hitbox, "encounter", encounter);

            MiniAutoController controller = GetOrAdd<MiniAutoController>(auto);
            SetObject(controller, "mover", mover);
            SetObject(controller, "sensor", sensor);
        }

        private static EncounterContext CreateEncounterContext(
            Transform parent,
            string name,
            string checkpointSpawnId)
        {
            GameObject gameObject = FindOrCreateChild(parent, name).gameObject;
            EncounterContext context = GetOrAdd<EncounterContext>(gameObject);
            SetString(context, "checkpointSpawnId", checkpointSpawnId);
            return context;
        }

        private static TimedHazardArea2D CreateHazardArea(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            EncounterContext encounter)
        {
            GameObject gameObject = CreateBox(parent, name, position, size,
                new Color(1f, 0.25f, 0.1f, 0.25f), true, 1);
            BoxCollider2D collider = GetOrAdd<BoxCollider2D>(gameObject);
            collider.isTrigger = true;
            TimedHazardArea2D area = GetOrAdd<TimedHazardArea2D>(gameObject);
            SetObject(area, "indicator", gameObject.GetComponent<SpriteRenderer>());
            HazardContact2D hitbox = GetOrAdd<HazardContact2D>(gameObject);
            SetObject(hitbox, "encounter", encounter);
            SetObject(hitbox, "activationSource", area);
            return area;
        }

        private static PatrolPath2D CreatePath(Transform parent, string name, Vector2[] points)
        {
            Transform root = FindOrCreateChild(parent, name);
            var transforms = new Transform[points.Length];
            for (int index = 0; index < points.Length; index++)
            {
                transforms[index] = FindOrCreateChild(root, "Point_" + index);
                transforms[index].position = points[index];
            }

            PatrolPath2D path = GetOrAdd<PatrolPath2D>(root.gameObject);
            SetObjectArray(path, "points", transforms);
            SetBool(path, "pingPong", true);
            return path;
        }

        private static void BuildBootstrap(
            CampusMapData mapData,
            InputActionReference toggleMapReference,
            InputActionReference scanReference)
        {
            Scene scene = EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);
            mapData = AssetDatabase.LoadAssetAtPath<CampusMapData>(MapDataPath);
            toggleMapReference = AssetDatabase.LoadAssetAtPath<InputActionReference>(
                ToggleMapReferencePath);
            scanReference = AssetDatabase.LoadAssetAtPath<InputActionReference>(
                ScanReferencePath);
            GameRuntimeRoot runtime = UnityEngine.Object.FindFirstObjectByType<GameRuntimeRoot>();
            if (runtime == null)
            {
                throw new InvalidOperationException("Bootstrap has no GameRuntimeRoot.");
            }

            PlayerInputReader input = runtime.GetComponentInChildren<PlayerInputReader>(true);
            GameplayModeController mode = runtime.GetComponentInChildren<GameplayModeController>(true);
            GameSessionController session = runtime.GetComponentInChildren<GameSessionController>(true);
            Transform uiRoot = FindRecursive(runtime.transform, "UIRoot");
            if (input == null || mode == null || session == null || uiRoot == null)
            {
                throw new InvalidOperationException("Bootstrap runtime references are incomplete.");
            }

            SetObject(input, "toggleMapAction", toggleMapReference);
            SetObject(input, "scanAction", scanReference);
            SetString(session, "initialZoneId", "entrada");
            SetString(session, "initialSpawnId", "entrada_inicio");
            Rigidbody2D playerBody = input.GetComponent<Rigidbody2D>();
            if (playerBody != null)
            {
                playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            MessageToastView toast = BuildMessageToast(uiRoot);
            NotificationController notifications = GetOrAdd<NotificationController>(uiRoot.gameObject);
            SetObject(notifications, "view", toast);
            BossPhaseView bossPhase = BuildBossPhase(uiRoot);
            ZoneTitleView zoneTitle = BuildZoneTitle(uiRoot);
            ScannerController scanner = BuildScanner(uiRoot, input, mode, input.transform);
            Camera worldCamera = runtime.GetComponentInChildren<Camera>(true);
            MinimapController minimap = BuildMinimap(uiRoot, mode, input.transform);
            ObjectiveMarkerController objectiveMarker = BuildObjectiveMarker(
                uiRoot, mode, worldCamera);
            CampusMapView mapView = BuildMapView(uiRoot, mapData, out List<CampusMapNodeView> nodeViews);
            CampusMapController mapController = GetOrAdd<CampusMapController>(uiRoot.gameObject);
            SetObject(mapController, "inputReader", input);
            SetObject(mapController, "gameplayMode", mode);
            SetObject(mapController, "session", session);
            SetObject(mapController, "mapData", mapData);
            SetObject(mapController, "view", mapView);

            SetObject(session, "messageToastView", toast);
            SetObject(session, "notificationController", notifications);
            SetObject(session, "minimapController", minimap);
            SetObject(session, "scannerController", scanner);
            SetObject(session, "objectiveMarkerController", objectiveMarker);
            SetObject(session, "zoneTitleView", zoneTitle);
            SetObject(session, "bossPhaseView", bossPhase);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static MessageToastView BuildMessageToast(Transform uiRoot)
        {
            MessageToastView view = GetOrAdd<MessageToastView>(uiRoot.gameObject);
            RectTransform panel = CreateUiPanel(uiRoot, "MessageToast", new Color(0.05f, 0.07f, 0.1f, 0.92f));
            SetRect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 155f), new Vector2(760f, 70f));
            TextMeshProUGUI text = CreateUiText(panel, "MessageToastText", string.Empty, 24, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 14f);
            SetObject(view, "root", panel.gameObject);
            SetObject(view, "label", text);
            panel.gameObject.SetActive(false);
            return view;
        }

        private static ZoneTitleView BuildZoneTitle(Transform uiRoot)
        {
            RectTransform panel = CreateUiPanel(
                uiRoot,
                "ZoneTitle",
                new Color(0.035f, 0.065f, 0.1f, 0.9f));
            SetRect(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -42f), new Vector2(620f, 54f));
            TextMeshProUGUI text = CreateUiText(panel, "ZoneTitleText", string.Empty, 26, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 8f);
            ZoneTitleView view = GetOrAdd<ZoneTitleView>(panel.gameObject);
            SetObject(view, "root", panel.gameObject);
            SetObject(view, "label", text);
            panel.gameObject.SetActive(false);
            return view;
        }

        private static ScannerController BuildScanner(
            Transform uiRoot,
            PlayerInputReader input,
            GameplayModeController mode,
            Transform player)
        {
            RectTransform panel = CreateUiPanel(
                uiRoot,
                "ScannerStatus",
                new Color(0.02f, 0.24f, 0.3f, 0.92f));
            SetRect(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -110f), new Vector2(460f, 42f));
            TextMeshProUGUI text = CreateUiText(panel, "ScannerStatusText", string.Empty, 19, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 7f);
            ScannerView view = GetOrAdd<ScannerView>(panel.gameObject);
            SetObject(view, "root", panel.gameObject);
            SetObject(view, "label", text);

            ScannerController controller = GetOrAdd<ScannerController>(uiRoot.gameObject);
            SetObject(controller, "inputReader", input);
            SetObject(controller, "gameplayMode", mode);
            SetObject(controller, "player", player);
            SetObject(controller, "view", view);
            panel.gameObject.SetActive(false);
            return controller;
        }

        private static MinimapController BuildMinimap(
            Transform uiRoot,
            GameplayModeController mode,
            Transform player)
        {
            RectTransform panel = CreateUiPanel(
                uiRoot,
                "Minimap",
                new Color(0.025f, 0.05f, 0.08f, 0.94f));
            SetRect(panel, Vector2.one, Vector2.one,
                new Vector2(-135f, -100f), new Vector2(240f, 170f));

            TextMeshProUGUI title = CreateUiText(panel, "MinimapTitle", "MINIMAPA", 18, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -15f), new Vector2(216f, 26f));
            TextMeshProUGUI zone = CreateUiText(panel, "MinimapZone", string.Empty, 12, TextAnchor.MiddleCenter);
            SetRect(zone.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -35f), new Vector2(216f, 20f));

            RectTransform content = CreateUiPanel(
                panel,
                "MinimapContent",
                new Color(0.055f, 0.09f, 0.13f, 0.95f));
            SetRect(content, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 61f), new Vector2(210f, 112f));

            RectTransform markerTemplate = CreateUiPanel(
                content,
                "MarkerTemplate",
                new Color(0.1f, 0.35f, 0.45f, 0.95f));
            SetRect(markerTemplate, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(24f, 24f));
            TextMeshProUGUI markerIcon = CreateUiText(
                markerTemplate, "Icon", string.Empty, 17, TextAnchor.MiddleCenter);
            Stretch(markerIcon.rectTransform, 0f);
            MinimapMarkerView markerView = GetOrAdd<MinimapMarkerView>(markerTemplate.gameObject);
            SetObject(markerView, "root", markerTemplate.gameObject);
            SetObject(markerView, "icon", markerIcon);
            SetObject(markerView, "background", markerTemplate.GetComponent<Image>());
            markerTemplate.gameObject.SetActive(false);

            RectTransform playerMarker = CreateUiPanel(
                content,
                "PlayerMarker",
                new Color(0.08f, 0.55f, 0.78f, 1f));
            SetRect(playerMarker, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(26f, 26f));
            TextMeshProUGUI playerIcon = CreateUiText(playerMarker, "Icon", string.Empty, 18, TextAnchor.MiddleCenter);
            Stretch(playerIcon.rectTransform, 0f);

            MinimapController controller = GetOrAdd<MinimapController>(uiRoot.gameObject);
            SetObject(controller, "gameplayMode", mode);
            SetObject(controller, "player", player);
            SetObject(controller, "root", panel.gameObject);
            SetObject(controller, "content", content);
            SetObject(controller, "playerMarker", playerMarker);
            SetObject(controller, "markerTemplate", markerView);
            SetObject(controller, "zoneLabel", zone);
            panel.gameObject.SetActive(false);
            return controller;
        }

        private static ObjectiveMarkerController BuildObjectiveMarker(
            Transform uiRoot,
            GameplayModeController mode,
            Camera worldCamera)
        {
            RectTransform marker = CreateUiPanel(
                uiRoot,
                "ObjectiveMarker",
                new Color(0.04f, 0.35f, 0.58f, 0.92f));
            SetRect(marker, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(42f, 42f));

            Transform previousIcon = marker.Find("ObjectiveIcon");
            if (previousIcon != null)
            {
                UnityEngine.Object.DestroyImmediate(previousIcon.gameObject);
            }
            Transform previousArrow = marker.Find("ObjectiveArrow");
            if (previousArrow != null)
            {
                UnityEngine.Object.DestroyImmediate(previousArrow.gameObject);
            }

            RectTransform icon = CreateUiPanel(
                marker,
                "ObjectiveIcon",
                new Color(0.025f, 0.06f, 0.09f, 1f));
            SetRect(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(14f, 14f));
            icon.localRotation = Quaternion.Euler(0f, 0f, 45f);

            RectTransform arrow = CreateUiPanel(
                marker,
                "ObjectiveArrow",
                new Color(1f, 0.72f, 0.29f, 1f));
            SetRect(arrow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(31f, 0f), new Vector2(22f, 4f));
            RectTransform arrowHead = CreateUiPanel(
                arrow,
                "ArrowHead",
                new Color(1f, 0.72f, 0.29f, 1f));
            SetRect(arrowHead, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-2f, 0f), new Vector2(9f, 4f));
            arrowHead.localRotation = Quaternion.Euler(0f, 0f, 45f);
            ObjectiveMarkerView view = GetOrAdd<ObjectiveMarkerView>(marker.gameObject);
            SetObject(view, "root", marker.gameObject);
            SetObject(view, "icon", icon.GetComponent<Image>());
            SetObject(view, "arrow", arrow);

            ObjectiveMarkerController controller = GetOrAdd<ObjectiveMarkerController>(uiRoot.gameObject);
            SetObject(controller, "gameplayMode", mode);
            SetObject(controller, "worldCamera", worldCamera);
            SetObject(controller, "canvasRect", uiRoot as RectTransform);
            SetObject(controller, "view", view);
            marker.gameObject.SetActive(false);
            return controller;
        }

        private static BossPhaseView BuildBossPhase(Transform uiRoot)
        {
            BossPhaseView view = GetOrAdd<BossPhaseView>(uiRoot.gameObject);
            RectTransform panel = CreateUiPanel(uiRoot, "BossPhase", new Color(0.28f, 0.04f, 0.08f, 0.92f));
            SetRect(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -70f), new Vector2(680f, 60f));
            TextMeshProUGUI text = CreateUiText(panel, "BossPhaseText", string.Empty, 26, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 10f);
            SetObject(view, "root", panel.gameObject);
            SetObject(view, "label", text);
            panel.gameObject.SetActive(false);
            return view;
        }

        private static CampusMapView BuildMapView(
            Transform uiRoot,
            CampusMapData data,
            out List<CampusMapNodeView> nodeViews)
        {
            CampusMapView view = GetOrAdd<CampusMapView>(uiRoot.gameObject);
            RectTransform root = CreateUiPanel(uiRoot, "CampusMap", new Color(0.025f, 0.04f, 0.075f, 0.97f));
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            TextMeshProUGUI title = CreateUiText(root, "Title", "MAPA DEL CAMPUS   [M] Cerrar", 32, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -45f), new Vector2(800f, 60f));

            string[] ids = { "entrada", "hall", "arca", "piso2", "piso3", "lab_software", "nucleo_ia" };
            string[] names = { "ENTRADA", "HALL", "ARCA", "PISO 2", "PISO 3", "LAB SOFTWARE", "NÚCLEO IA" };
            Vector2[] positions =
            {
                new Vector2(0f, 260f), new Vector2(0f, 165f), new Vector2(270f, 165f),
                new Vector2(0f, 65f), new Vector2(0f, -35f), new Vector2(0f, -135f),
                new Vector2(0f, -235f)
            };

            nodeViews = new List<CampusMapNodeView>();
            for (int index = 0; index < ids.Length; index++)
            {
                RectTransform node = CreateUiPanel(root, "MapNode_" + ids[index], new Color(0.2f, 0.2f, 0.25f, 0.95f));
                SetRect(node, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), positions[index], new Vector2(230f, 58f));
                TextMeshProUGUI label = CreateUiText(node, "Label", names[index], 22, TextAnchor.MiddleCenter);
                Stretch(label.rectTransform, 8f);
                CampusMapNodeView nodeView = GetOrAdd<CampusMapNodeView>(node.gameObject);
                SetString(nodeView, "zoneId", ids[index]);
                SetObject(nodeView, "background", node.GetComponent<Image>());
                SetObject(nodeView, "label", label);
                nodeViews.Add(nodeView);
            }

            CreateUiLine(root, "LineEntradaHall", new Vector2(0f, 213f), new Vector2(5f, 37f));
            CreateUiLine(root, "LineHallArca", new Vector2(135f, 165f), new Vector2(40f, 5f));
            CreateUiLine(root, "LineHallPiso2", new Vector2(0f, 115f), new Vector2(5f, 42f));
            CreateUiLine(root, "LinePiso2Piso3", new Vector2(0f, 15f), new Vector2(5f, 42f));
            CreateUiLine(root, "LinePiso3Lab", new Vector2(0f, -85f), new Vector2(5f, 42f));
            CreateUiLine(root, "LineLabNucleo", new Vector2(0f, -185f), new Vector2(5f, 42f));

            SetObject(view, "root", root.gameObject);
            SetObjectArray(view, "nodeViews", nodeViews.ToArray());
            root.gameObject.SetActive(false);
            return view;
        }

        private static CampusMapData BuildCampusMapData()
        {
            CampusMapData data = AssetDatabase.LoadAssetAtPath<CampusMapData>(MapDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CampusMapData>();
                AssetDatabase.CreateAsset(data, MapDataPath);
            }

            var nodeData = new[]
            {
                new object[] { "entrada", "ENTRADA", GameFlagId.EntranceVisited, GameFlagId.None, GameFlagId.None },
                new object[] { "hall", "HALL", GameFlagId.HallVisited, GameFlagId.None, GameFlagId.None },
                new object[] { "arca", "ARCA", GameFlagId.ArcaVisited, GameFlagId.None, GameFlagId.None },
                new object[] { "piso2", "PISO 2", GameFlagId.Piso2Visited, GameFlagId.Piso2Unlocked, GameFlagId.HallVisited },
                new object[] { "piso3", "PISO 3", GameFlagId.Piso3Visited, GameFlagId.Piso3Unlocked, GameFlagId.Piso2Visited },
                new object[] { "lab_software", "LAB SOFTWARE", GameFlagId.LabSoftwareVisited, GameFlagId.LabSoftwareUnlocked, GameFlagId.Piso3Visited },
                new object[] { "nucleo_ia", "NÚCLEO IA", GameFlagId.NucleoVisited, GameFlagId.NucleoUnlocked, GameFlagId.LabSoftwareVisited }
            };

            SerializedObject serialized = new SerializedObject(data);
            SerializedProperty nodes = serialized.FindProperty("nodes");
            nodes.arraySize = nodeData.Length;
            for (int index = 0; index < nodeData.Length; index++)
            {
                SerializedProperty element = nodes.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("zoneId").stringValue = (string)nodeData[index][0];
                element.FindPropertyRelative("displayName").stringValue = (string)nodeData[index][1];
                element.FindPropertyRelative("visitedFlag").enumValueIndex = (int)(GameFlagId)nodeData[index][2];
                element.FindPropertyRelative("unlockFlag").enumValueIndex = (int)(GameFlagId)nodeData[index][3];
                element.FindPropertyRelative("revealFlag").enumValueIndex = (int)(GameFlagId)nodeData[index][4];
            }

            string[,] links =
            {
                { "entrada", "hall" }, { "hall", "arca" }, { "hall", "piso2" },
                { "piso2", "piso3" }, { "piso3", "lab_software" }, { "lab_software", "nucleo_ia" }
            };
            SerializedProperty connections = serialized.FindProperty("connections");
            connections.arraySize = links.GetLength(0);
            for (int index = 0; index < links.GetLength(0); index++)
            {
                SerializedProperty element = connections.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("fromZoneId").stringValue = links[index, 0];
                element.FindPropertyRelative("toZoneId").stringValue = links[index, 1];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static InputActionReference BuildToggleMapReference()
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            InputAction action = actions?.FindAction("Player/ToggleMap", true);
            InputActionReference reference =
                AssetDatabase.LoadAssetAtPath<InputActionReference>(ToggleMapReferencePath);
            if (reference == null)
            {
                reference = ScriptableObject.CreateInstance<InputActionReference>();
                AssetDatabase.CreateAsset(reference, ToggleMapReferencePath);
            }

            reference.Set(action);
            EditorUtility.SetDirty(reference);
            return reference;
        }

        private static InputActionReference BuildScanReference()
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            InputAction action = actions?.FindAction("Player/Scan", true);
            InputActionReference reference =
                AssetDatabase.LoadAssetAtPath<InputActionReference>(ScanReferencePath);
            if (reference == null)
            {
                reference = ScriptableObject.CreateInstance<InputActionReference>();
                AssetDatabase.CreateAsset(reference, ScanReferencePath);
            }

            reference.Set(action);
            EditorUtility.SetDirty(reference);
            return reference;
        }

        private static void UpdateZoneCatalog()
        {
            ZoneCatalog catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalog>(
                "Assets/_Project/Data/Zones/ZoneCatalog.asset");
            string[,] zones =
            {
                { "entrada", BuildScenePaths[1] }, { "hall", BuildScenePaths[2] },
                { "arca", BuildScenePaths[3] }, { "piso2", BuildScenePaths[4] },
                { "piso3", BuildScenePaths[5] }, { "lab_software", BuildScenePaths[6] },
                { "nucleo_ia", BuildScenePaths[7] }
            };

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty("zones");
            list.arraySize = zones.GetLength(0);
            for (int index = 0; index < zones.GetLength(0); index++)
            {
                SerializedProperty item = list.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("zoneId").stringValue = zones[index, 0];
                item.FindPropertyRelative("scenePath").stringValue = zones[index, 1];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[BuildScenePaths.Length];
            for (int index = 0; index < BuildScenePaths.Length; index++)
            {
                scenes[index] = new EditorBuildSettingsScene(BuildScenePaths[index], true);
            }
            EditorBuildSettings.scenes = scenes;
        }

        private static GameObject CreateExit(
            Transform parent,
            string name,
            Vector2 position,
            string prompt,
            string targetZone,
            string targetSpawn,
            GameFlagId requiredFlag = GameFlagId.None,
            string lockedMessage = "Acceso restringido.")
        {
            GameObject gameObject = CreateInteractableBox(parent, name, position,
                new Vector2(1.1f, 1.5f), new Color(0.18f, 0.65f, 0.35f), prompt);
            ZoneExitInteractable exit = GetOrAdd<ZoneExitInteractable>(gameObject);
            ConfigurePrompt(exit, prompt);
            SetString(exit, "targetZoneId", targetZone);
            SetString(exit, "targetSpawnId", targetSpawn);
            SetEnum(exit, "requiredFlag", requiredFlag);
            SetString(exit, "lockedMessage", lockedMessage);
            return gameObject;
        }

        private static GameObject CreateInteractableBox(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            string prompt)
        {
            return CreateBox(parent, name, position, size, color, true, 2);
        }

        private static void ConfigurePrompt(InteractableBehaviour interactable, string prompt)
        {
            SetString(interactable, "prompt", prompt);
            SetInt(interactable, "priority", 0);
        }

        private static GameObject CreateBox(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            bool collider,
            int sortingOrder)
        {
            Transform child = FindOrCreateChild(parent, name);
            child.position = new Vector3(position.x, position.y, 0f);
            child.localScale = Vector3.one;
            SpriteRenderer renderer = child.gameObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
            }

            if (renderer == null)
            {
                throw new InvalidOperationException("Could not attach SpriteRenderer to " + child.name);
            }

            renderer.sprite = GetGrayboxSprite();
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            if (collider)
            {
                BoxCollider2D box = GetOrAdd<BoxCollider2D>(child.gameObject);
                box.size = size;
            }
            return child.gameObject;
        }

        private static void CreateWall(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject wall = CreateBox(parent, name, position, size,
                new Color(0.08f, 0.1f, 0.14f), true, -5);
            wall.GetComponent<BoxCollider2D>().isTrigger = false;
        }

        private static void CreateSpawn(Transform parent, string id, Vector2 position)
        {
            GameObject gameObject = FindOrCreateChild(parent, "Spawn_" + id).gameObject;
            gameObject.transform.position = position;
            SpawnPoint spawn = GetOrAdd<SpawnPoint>(gameObject);
            SetString(spawn, "spawnId", id);
        }

        private static void CreateRespawnPoint(
            Transform parent,
            string zoneId,
            string respawnId,
            Vector2 position)
        {
            GameObject gameObject = CreateBox(
                parent,
                "Respawn_" + respawnId,
                position,
                new Vector2(0.65f, 0.65f),
                new Color(0.1f, 1f, 0.25f, 0.28f),
                false,
                1);
            SpawnPoint spawn = GetOrAdd<SpawnPoint>(gameObject);
            SetString(spawn, "spawnId", respawnId);
            CircleCollider2D trigger = GetOrAdd<CircleCollider2D>(gameObject);
            trigger.isTrigger = true;
            trigger.radius = 0.45f;
            RespawnPoint2D respawn = GetOrAdd<RespawnPoint2D>(gameObject);
            SetString(respawn, "zoneId", zoneId);
            SetString(respawn, "respawnId", respawnId);
        }

        private static void CreateWorldLabel(
            Transform parent,
            string name,
            string text,
            Vector2 position,
            float characterSize = 0.16f)
        {
            Transform child = FindOrCreateChild(parent, name);
            child.localPosition = new Vector3(position.x, position.y, -0.1f);
            TextMesh legacy = child.GetComponent<TextMesh>();
            if (legacy != null)
            {
                UnityEngine.Object.DestroyImmediate(legacy);
            }
            TextMeshPro mesh = GetOrAdd<TextMeshPro>(child.gameObject);
            mesh.font = GetPresentationFontAsset();
            mesh.text = text;
            mesh.fontSize = Mathf.Max(1f, characterSize * 10f);
            mesh.alignment = TextAlignmentOptions.Center;
            mesh.textWrappingMode = TextWrappingModes.NoWrap;
            mesh.rectTransform.sizeDelta = new Vector2(6f, 1.2f);
            mesh.color = Color.white;
        }

        private static Sprite GetGrayboxSprite()
        {
            if (grayboxSprite != null)
            {
                return grayboxSprite;
            }

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(GrayboxSpritePath);
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is Sprite sprite)
                {
                    grayboxSprite = sprite;
                    return grayboxSprite;
                }
            }

            EnsureFolder("Assets/_Project/Resources/Presentation");
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(GrayboxSpritePath);
            if (texture == null)
            {
                texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
                {
                    name = "GrayboxFullRectTexture",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                Color[] pixels = new Color[64];
                for (int index = 0; index < pixels.Length; index++)
                {
                    pixels[index] = Color.white;
                }
                texture.SetPixels(pixels);
                texture.Apply(false, false);
                AssetDatabase.CreateAsset(texture, GrayboxSpritePath);
            }

            grayboxSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                8f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(1f, 1f, 1f, 1f));
            grayboxSprite.name = "GrayboxFullRectSprite";
            AssetDatabase.AddObjectToAsset(grayboxSprite, texture);
            EditorUtility.SetDirty(texture);
            AssetDatabase.SaveAssets();
            return grayboxSprite;
        }

        private static RectTransform CreateUiPanel(Transform parent, string name, Color color)
        {
            Transform existing = FindRecursive(parent, name);
            GameObject gameObject = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = GetOrAdd<Image>(gameObject);
            image.color = color;
            return (RectTransform)gameObject.transform;
        }

        private static TextMeshProUGUI CreateUiText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            TextAnchor alignment)
        {
            Transform existing = FindRecursive(parent, name);
            GameObject gameObject = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            Text legacyLabel = gameObject.GetComponent<Text>();
            if (legacyLabel != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyLabel);
            }

            TextMeshProUGUI label = GetOrAdd<TextMeshProUGUI>(gameObject);
            label.font = GetPresentationFontAsset();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = ConvertAlignment(alignment);
            label.color = Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private static TMP_FontAsset GetPresentationFontAsset()
        {
            EnsureTmpEssentialResources();
            TMP_FontAsset asset = TMP_Settings.defaultFontAsset;
            if (asset == null)
            {
                throw new InvalidOperationException("TMP Essential Resources have no default font asset.");
            }
            return asset;
        }

        private static void EnsureTmpEssentialResources()
        {
            if (TMP_Settings.instance != null)
            {
                return;
            }

            string packagePath = AssetDatabase.GUIDToAssetPath(TmpEssentialResourcesGuid);
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                throw new InvalidOperationException("Unity could not locate TMP Essential Resources.");
            }

            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            if (TMP_Settings.instance == null)
            {
                throw new InvalidOperationException("TMP Essential Resources could not be imported.");
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

        private static void CreateUiLine(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform line = CreateUiPanel(parent, name, new Color(0.7f, 0.8f, 0.9f, 0.9f));
            SetRect(line, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            line.SetAsFirstSibling();
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }
            return null;
        }

        private static Transform FindOrCreateChild(Transform parent, string name)
        {
            Transform child = FindRecursive(parent, name);
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
                Transform found = FindRecursive(parent.GetChild(index), name);
                if (found != null)
                {
                    return found;
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

        private static void SetString(UnityEngine.Object target, string property, string value)
        {
            SetSerialized(target, property, item => item.stringValue = value ?? string.Empty);
        }

        private static void SetFloat(UnityEngine.Object target, string property, float value)
        {
            SetSerialized(target, property, item => item.floatValue = value);
        }

        private static void SetInt(UnityEngine.Object target, string property, int value)
        {
            SetSerialized(target, property, item => item.intValue = value);
        }

        private static void SetBool(UnityEngine.Object target, string property, bool value)
        {
            SetSerialized(target, property, item => item.boolValue = value);
        }

        private static void SetVector2(UnityEngine.Object target, string property, Vector2 value)
        {
            SetSerialized(target, property, item => item.vector2Value = value);
        }

        private static void SetEnum<T>(UnityEngine.Object target, string property, T value)
            where T : Enum
        {
            SetSerialized(target, property, item => item.enumValueIndex = Convert.ToInt32(value));
        }

        private static void SetObject(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            SetSerialized(target, property, item => item.objectReferenceValue = value);
        }

        private static void SetObjectArray(
            UnityEngine.Object target,
            string property,
            IReadOnlyList<UnityEngine.Object> values)
        {
            SetSerialized(target, property, item =>
            {
                item.arraySize = values.Count;
                for (int index = 0; index < values.Count; index++)
                {
                    item.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
                }
            });
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

        private readonly struct ZoneParts
        {
            public ZoneParts(
                string path,
                Scene scene,
                GameObject root,
                Transform environment,
                Transform collision,
                Transform spawns,
                Transform interactables,
                Transform hazards,
                Transform encounter)
            {
                Path = path;
                Scene = scene;
                Root = root;
                Environment = environment;
                Collision = collision;
                Spawns = spawns;
                Interactables = interactables;
                Hazards = hazards;
                Encounter = encounter;
            }

            public string Path { get; }
            public Scene Scene { get; }
            public GameObject Root { get; }
            public Transform Environment { get; }
            public Transform Collision { get; }
            public Transform Spawns { get; }
            public Transform Interactables { get; }
            public Transform Hazards { get; }
            public Transform Encounter { get; }
        }
    }
}
#endif
