using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.Puzzles;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class CampaignReleaseAudit
    {
        private const string CatalogPath = "Assets/_Project/Data/Zones/ZoneCatalog.asset";

        [MenuItem("Escape UNIFRANZ/Campaña/Auditar recorrido")]
        public static void Run() => Run(true);

        public static void Run(bool includeFinalDecoration)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var lines = new List<string>();
            var errors = new List<string>();
            var definitions = AssetDatabase.LoadAssetAtPath<ZoneCatalog>(CatalogPath).Zones;
            var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            Require(enabled.SequenceEqual(new[] { CampaignReleaseSetup.MainMenuPath, CampaignReleaseSetup.BootstrapPath }
                .Concat(definitions.Select(zone => zone.ScenePath))), "Escenas de build incorrectas", errors);

            var spawns = new Dictionary<string, HashSet<string>>();
            var links = new List<(string zone, string exit, string destination, string spawn, bool extraction)>();
            try
            {
                var menu = Open(CampaignReleaseSetup.MainMenuPath, errors);
                var menuController = Components<MainMenuController>(menu).Single();
                CheckFields(menuController, errors, "newGameButton", "continueButton", "optionsButton",
                    "optionsPanel", "optionsBackButton", "controlsButton", "creditsButton", "quitButton");
                ValidateMenuComposition(menuController, errors);
                var audioMenu = Components<ProjectAudioHooks>(menu).Single();
                Require(audioMenu.bindings.Length == 3 && audioMenu.bindings.All(b => b.clip != null) && audioMenu.mixer != null &&
                    Components<AudioListener>(menu).Length == 1, "Audio de menú incompleto", errors);

                var bootstrap = Open(CampaignReleaseSetup.BootstrapPath, errors);
                var player = Components<PlayerMovement2D>(bootstrap).Single();
                var body = player.GetComponent<CapsuleCollider2D>();
                var bodySize = body.size;
                var bodyOffset = body.offset;
                var layer = player.gameObject.layer;
                Require(Components<ThrowableCanController>(bootstrap).Length == 1 &&
                    Components<EntranceTutorialController>(bootstrap).Length == 1 &&
                    Components<GameFlowController>(bootstrap).Length == 1 &&
                    Components<Canvas>(bootstrap).Length == 1, "Jugador, tutorial o flujo persistente incompleto", errors);
                var vision = Components<PlayerVisionController>(bootstrap).Single();
                CheckFields(vision, errors, "visualController", "immediateLight", "forwardLight");
                var audio = Components<ProjectAudioHooks>(bootstrap).Single();
                Require(audio.bindings.Select(b => b.cue).Distinct().Count() == Enum.GetValues(typeof(AudioCue)).Length &&
                    audio.bindings.All(b => b.clip != null) && Components<AudioListener>(bootstrap).Length == 1 &&
                    audio.effects != null && audio.music != null && audio.ambience != null && audio.mixer != null &&
                    new[] { audio.musicBus, audio.ambienceBus, audio.sfxBus, audio.uiBus, audio.voicesBus }.All(group => group != null),
                    "Audio de partida incompleto", errors);
                var items = new SerializedObject(Components<NarrativeController>(bootstrap).Single()).FindProperty("itemCatalog");
                var itemIds = Enumerable.Range(0, items.arraySize).Select(i =>
                    ((ItemData)items.GetArrayElementAtIndex(i).objectReferenceValue)?.id).ToArray();
                Require(itemIds.Length >= 8 && itemIds.Contains("soda") && itemIds.Contains("flashlight") &&
                    itemIds.All(id => !string.IsNullOrEmpty(id)), "Inventario sin objetos requeridos", errors);
                var initialSession = Components<GameSessionController>(bootstrap).Single();
                CheckFields(initialSession, errors, "sceneFlow", "objectiveController", "gameFlow");
                lines.Add($"Bootstrap: {itemIds.Length} objetos, {audio.bindings.Length} cues, un jugador y un tutorial.");

                foreach (var definition in definitions)
                {
                    var scene = Open(definition.ScenePath, errors);
                    if (definition.ZoneId == EntranceTutorialController.PracticeZoneId)
                    {
                        Require(Components<ZoneContext>(scene).Length == 1 && Components<SpawnPoint>(scene).Length == 1 &&
                            Components<PracticeStation>(scene).Length == 2 && Components<MiniAutoController>(scene).Length == 1 &&
                            Components<HazardContact2D>(scene).Length == 0 && Components<PlayerMovement2D>(scene).Length == 0,
                            "Sala de práctica incompleta o insegura", errors);
                        continue;
                    }
                    var zone = Components<ZoneContext>(scene).Single();
                    Require(zone.ZoneId == definition.ZoneId, $"ZoneId incorrecto en {scene.name}", errors);
                    Require(Components<PlayerMovement2D>(scene).Length == 0, $"Jugador duplicado en {scene.name}", errors);
                    var points = Components<SpawnPoint>(scene);
                    var ids = points.Select(p => p.SpawnId).ToHashSet();
                    spawns.Add(zone.ZoneId, ids);
                    Require(ids.Count == points.Length && ids.Contains(zone.DefaultCheckpointSpawnId),
                        $"{scene.name}: checkpoint ausente o IDs de spawn duplicados", errors);
                    var exits = Components<ZoneExitInteractable>(scene).Where(Active).ToArray();
                    foreach (var exit in exits)
                    {
                        links.Add((zone.ZoneId, exit.name, exit.TargetZoneId, exit.TargetSpawnId, exit.EscapesAfterShutdown));
                        Require(exit.statusLight != null && exit.statusIndicator != null,
                            $"{scene.name}/{exit.name}: falta indicador", errors);
                    }

                    foreach (var actor in Components<MiniAutoController>(scene).Where(Active))
                        CheckFields(actor, errors, "mover", "sensor", "carVisual", "movementArt", "verticalArt", "impactArt");
                    var activePuzzles = Components<PuzzleInteractable>(scene).Where(Active).ToArray();
                    Require(activePuzzles.Length > 0, $"{scene.name}: no hay puzles activos", errors);
                    var pickups = Components<ItemPickupInteractable>(scene).Where(Active).ToArray();
                    if (zone.ZoneId == "entrada")
                    {
                        Require(new[] { "battery", "flashlight" }.All(id => pickups.Count(p => p.ItemId == id) == 1),
                            "Entrada: faltan la batería o la linterna únicas", errors);
                        Require(Components<SecurityTerminalInteractable>(scene).Where(Active)
                                .Any(t => t.feeds.Any(feed => feed.clue != null && feed.clue.id == "cctv_codigo")),
                            "Entrada: el código no está disponible en CCTV", errors);
                    }
                    if (zone.ZoneId == "hall")
                        Require(pickups.Count(p => p.ItemId == "key") == 1, "Hall: falta la llave de ARCA", errors);
                    if (zone.ZoneId is "entrada" or "hall" or "arca")
                        Require(Components<CompositeCollider2D>(scene).Any(c => Active(c) && c.pathCount > 0),
                            $"{scene.name}: colisión compuesta vacía o ausente", errors);
                    if (zone.ZoneId == "piso2")
                        Require(Components<ShelterInteractable>(scene).Count(Active) == 1,
                            "Simulación: falta el punto de cobertura", errors);
                    if (zone.ZoneId == "nucleo_ia")
                    {
                        var francis = Components<FrancisBossController>(scene).Single();
                        CheckFields(francis, errors, "interferenceHazard", "encounter");
                    }
                    var clueSources = Components<ClueInteractable>(scene).Where(Active).Select(component => component.clue)
                        .Concat(Components<SecurityTerminalInteractable>(scene).Where(Active).SelectMany(terminal => terminal.feeds)
                            .Where(feed => !feed.noSignal).Select(feed => feed.clue))
                        .Concat(activePuzzles.Select(component => component.puzzle?.environmentalClue))
                        .Where(clue => clue != null).Select(clue => clue.id).ToHashSet();
                    foreach (var panel in activePuzzles)
                        CheckPuzzle(scene.name, panel, clueSources, errors);

                    var rooms = Components<RoomLightingController>(scene).Where(Active).ToArray();
                    foreach (var room in rooms)
                    {
                        Require(room.fillLights.Length > 0 && room.fillLights.All(light => light != null && light.shadowsEnabled),
                            $"{scene.name}/{room.roomId}: relleno de habitación sin sombras", errors);
                    }
                    Require(rooms.Length > 0, $"{scene.name}: faltan luces de habitación activas", errors);

                    var solids = Components<Collider2D>(scene)
                        .Where(c => Active(c) && IsMapCollider(c) && !Physics2D.GetIgnoreLayerCollision(layer, c.gameObject.layer)
                            && (includeFinalDecoration || c.transform.parent == null || c.transform.parent.name != "Ambientación · Abandono")
                            && c.GetComponentInParent<PatrolMover2D>() == null &&
                            c.GetComponentInParent<DoorPanel2D>() == null &&
                            c.GetComponentInParent<KeyedPassage>() == null).ToArray();
                    var map = new WalkableGrid(zone.MinimapBounds, solids, bodySize, bodyOffset);
                    var origin = points.FirstOrDefault(p => p.SpawnId == zone.DefaultCheckpointSpawnId);
                    int reachable = origin != null ? map.Fill(origin.Position) : 0;
                    if (origin != null && reachable == 0) errors.Add($"{scene.name}/{origin.SpawnId}: checkpoint en colisión {origin.Position:F2}");
                    foreach (var point in points)
                    {
                        if (!map.IsReachable(point.Position, 0.23f))
                            errors.Add($"{scene.name}/{point.SpawnId}: spawn aislado {point.Position:F2}");
                    }
                    foreach (var target in Components<InteractableBehaviour>(scene)
                                 .Where(c => Active(c) && (c is ZoneExitInteractable || c is ClueInteractable ||
                                     c is ItemPickupInteractable || c is PuzzleInteractable || c is VendingMachineInteractable ||
                                     c is SecurityTerminalInteractable || c is EnvironmentalControlInteractable || c is ShelterInteractable ||
                                     c is ExolegOverloadInteractable || c is RobotArmsPanelInteractable || c is FrancisSystemNodeInteractable)))
                    {
                        if (!map.IsReachable(target.transform.position, 0.92f))
                            errors.Add($"{scene.name}/{target.name}: interacción aislada {target.transform.position:F2}");
                    }
                    lines.Add($"{scene.name}: {points.Length} spawns, {exits.Length} salidas, {activePuzzles.Length} puzles, " +
                        $"{rooms.Length} grupos de luz, {solids.Length} colisionadores de mapa, {reachable} celdas conectadas.");
                }

                Require(spawns.TryGetValue("entrada", out var entry) && entry.Contains("entrada_inicio"),
                    "El menú no tiene un punto válido de arranque", errors);
                Require(spawns.TryGetValue("nucleo_ia", out var core) &&
                    new[] { "francis_inicio", "francis_fase2", "francis_fase3" }.All(core.Contains),
                    "Faltan checkpoints de Francis", errors);
                Require(links.Count(link => link.extraction && link.zone == "nucleo_ia") == 1,
                    "La vía de extracción del núcleo debe ser única", errors);
                foreach (var link in links)
                {
                    Require(spawns.TryGetValue(link.destination, out var available) && available.Contains(link.spawn),
                        $"{link.zone}/{link.exit}: destino {link.destination}/{link.spawn} inválido", errors);
                    Require(links.Any(reverse => reverse.zone == link.destination && reverse.destination == link.zone),
                        $"{link.zone}/{link.exit}: acceso sin retorno", errors);
                    lines.Add($"{link.zone}/{link.exit} -> {link.destination}/{link.spawn}");
                }
            }
            finally
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllLines("Logs/campaign-audit.txt", lines.Concat(errors.Select(error => "ERROR " + error)));
                if (setup.Any(scene => scene.isLoaded && scene.isActive && !string.IsNullOrEmpty(scene.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            Debug.Log(string.Join("\n", lines));
        }

        private static Scene Open(string path, List<string> errors)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var transform in Components<Transform>(scene))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                    $"{scene.name}/{transform.name}: componente sin script", errors);
            return scene;
        }

        private static bool Active(Behaviour component) => component.isActiveAndEnabled;

        private static bool Active(Collider2D collider) => collider.enabled && collider.gameObject.activeInHierarchy;

        internal static bool IsMapCollider(Collider2D collider)
        {
            var composite = collider.GetComponentInParent<CompositeCollider2D>();
            return collider.enabled && !collider.isTrigger &&
                (collider is BoxCollider2D || collider is PolygonCollider2D) &&
                (composite == null || !composite.isTrigger);
        }

        private static void ValidateMenuComposition(MainMenuController menu, List<string> errors)
        {
            using var fields = new SerializedObject(menu);
            var names = new[] { "newGameButton", "continueButton", "optionsButton", "controlsButton", "creditsButton", "quitButton" };
            var buttonList = names.Select(name => (UnityEngine.UI.Button)fields.FindProperty(name).objectReferenceValue).ToArray();
            if (buttonList.Any(button => button == null)) return;
            var rects = buttonList.Select(button => (RectTransform)button.transform).ToArray();
            Require(buttonList.Distinct().Count() == 6 && buttonList.All(button => button.gameObject.activeSelf &&
                    (button.targetGraphic as UnityEngine.UI.Image)?.sprite != null),
                "El menú requiere seis botones visibles con sprite", errors);
            for (int i = 1; i < rects.Length; i++)
                Require(Mathf.Approximately(rects[i].anchoredPosition.x, rects[0].anchoredPosition.x) &&
                        Mathf.Approximately(rects[i - 1].anchoredPosition.y - rects[i].anchoredPosition.y, 102f),
                    $"Posición de menú fuera de la columna: {names[i]}", errors);
            Require(!buttonList[1].interactable, "Continuar debe estar deshabilitado antes de leer el guardado", errors);
            foreach (int i in new[] { 1, 2, 4 })
                Require(buttonList[i].GetComponent<MenuButtonFeedback>()?.label?.font?.name == "PirataOne SDF",
                    $"{names[i]} no usa PirataOne", errors);

            var options = (GameObject)fields.FindProperty("optionsPanel").objectReferenceValue;
            var view = options.GetComponent<GameOptionsView>();
            Require(view != null && view.volumeSliders.Length == 6 && view.volumeSliders.All(slider => slider != null) &&
                    view.flashes != null && view.vsync != null && view.frameLimit != null,
                "Faltan controles de sonido o gráficos", errors);
        }

        private static void CheckPuzzle(string sceneName, PuzzleInteractable panel, HashSet<string> sources, List<string> errors)
        {
            var data = panel.puzzle;
            if (!PuzzleModel.Validate(data, out string error))
            {
                errors.Add($"{sceneName}/{panel.name}: {error}");
                return;
            }

            foreach (string clue in (data.prerequisiteClues ?? Array.Empty<string>()).Append(data.requiredClueId))
                Require(string.IsNullOrWhiteSpace(clue) || sources.Contains(clue), $"{sceneName}/{panel.name}: no hay fuente para {clue}", errors);

            var model = new PuzzleModel(data);
            var solution = data.IsConnectionPuzzle
                ? data.connections.Select(connection => model.Connect(connection.from, connection.to))
                : data.commandOrder.Select(model.Command);
            Require(solution.All(result => result != PuzzleSubmission.Rejected) && model.IsComplete,
                $"{sceneName}/{panel.name}: la solución de escena no completa el puzle", errors);
        }

        private static void CheckFields(UnityEngine.Object target, List<string> errors, params string[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach (string name in fields)
                Require(serialized.FindProperty(name)?.objectReferenceValue != null, $"{target.name}: falta {name}", errors);
        }

        private static T[] Components<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static void Require(bool success, string message, List<string> errors)
        {
            if (!success) errors.Add(message);
        }

        internal sealed class WalkableGrid
        {
            private const float Step = 0.12f;
            private readonly Vector2 bottomLeft;
            private readonly bool[,] occupied;
            private readonly bool[,] reachable;
            private static readonly Vector2Int[] Neighbors =
                { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            public WalkableGrid(Rect bounds, Collider2D[] obstacles, Vector2 footprint, Vector2 offset)
            {
                int width = Mathf.Max(1, Mathf.FloorToInt(bounds.width / Step));
                int height = Mathf.Max(1, Mathf.FloorToInt(bounds.height / Step));
                bottomLeft = bounds.min + Vector2.one * (Step * 0.5f);
                occupied = new bool[width, height];
                reachable = new bool[width, height];

                foreach (var obstacle in obstacles.SelectMany(ObstacleBounds))
                {
                    Vector2 center = obstacle.center;
                    Vector2 clearance = (Vector2)obstacle.extents + footprint * 0.5f;
                    Vector2 min = (center - offset - clearance - bottomLeft) / Step;
                    Vector2 max = (center - offset + clearance - bottomLeft) / Step;
                    for (int x = Mathf.Max(0, Mathf.CeilToInt(min.x)); x <= Mathf.Min(width - 1, Mathf.FloorToInt(max.x)); x++)
                        for (int y = Mathf.Max(0, Mathf.CeilToInt(min.y)); y <= Mathf.Min(height - 1, Mathf.FloorToInt(max.y)); y++)
                            occupied[x, y] = true;
                }
            }

            private static IEnumerable<Bounds> ObstacleBounds(Collider2D collider)
            {
                if (collider is PolygonCollider2D polygon)
                {
                    // The authored map stores each merged pixel rectangle as one polygon path.
                    for (int path = 0; path < polygon.pathCount; path++)
                    {
                        var points = polygon.GetPath(path);
                        if (points.Length == 0) continue;
                        var bounds = new Bounds(polygon.transform.TransformPoint(points[0] + polygon.offset), Vector3.zero);
                        foreach (var point in points)
                            bounds.Encapsulate(polygon.transform.TransformPoint(point + polygon.offset));
                        yield return bounds;
                    }
                    yield break;
                }

                var box = (BoxCollider2D)collider;
                Vector2 scale = box.transform.lossyScale;
                Vector2 half = Vector2.Scale(box.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y))) * 0.5f;
                float radians = box.transform.eulerAngles.z * Mathf.Deg2Rad;
                float sin = Mathf.Abs(Mathf.Sin(radians)), cos = Mathf.Abs(Mathf.Cos(radians));
                half = new Vector2(half.x * cos + half.y * sin, half.x * sin + half.y * cos);
                yield return new Bounds(box.transform.TransformPoint(box.offset), half * 2f);
            }

            public int Fill(Vector2 origin)
            {
                var cell = Cell(origin);
                if (!Inside(cell) || occupied[cell.x, cell.y]) return 0;
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(cell);
                reachable[cell.x, cell.y] = true;
                int count = 0;
                while (queue.Count > 0)
                {
                    var previous = queue.Dequeue();
                    count++;
                    foreach (var direction in Neighbors)
                    {
                        var next = previous + direction;
                        if (!Inside(next) || occupied[next.x, next.y] || reachable[next.x, next.y]) continue;
                        reachable[next.x, next.y] = true;
                        queue.Enqueue(next);
                    }
                }
                return count;
            }

            public bool IsReachable(Vector2 point, float tolerance)
            {
                var cell = Cell(point);
                int range = Mathf.CeilToInt(tolerance / Step);
                for (int x = -range; x <= range; x++)
                    for (int y = -range; y <= range; y++)
                    {
                        var neighbor = cell + new Vector2Int(x, y);
                        if (Inside(neighbor) && reachable[neighbor.x, neighbor.y] &&
                            (bottomLeft + (Vector2)neighbor * Step - point).sqrMagnitude <= tolerance * tolerance)
                            return true;
                    }
                return false;
            }

            public Vector2 Nearest(Vector2 desired, IEnumerable<Vector2> taken = null, float separation = 0f)
            {
                Vector2 best = desired;
                float distance = float.PositiveInfinity;
                Vector2[] reserved = taken?.ToArray() ?? Array.Empty<Vector2>();
                for (int x = 0; x < reachable.GetLength(0); x++)
                for (int y = 0; y < reachable.GetLength(1); y++)
                {
                    if (!reachable[x, y]) continue;
                    Vector2 candidate = bottomLeft + new Vector2(x, y) * Step;
                    float next = (candidate - desired).sqrMagnitude;
                    if (next >= distance || reserved.Any(point => (candidate - point).sqrMagnitude < separation * separation)) continue;
                    distance = next;
                    best = candidate;
                }
                if (float.IsPositiveInfinity(distance)) throw new InvalidOperationException("No hay suelo accesible cerca de " + desired);
                return best;
            }

            private Vector2Int Cell(Vector2 point) => Vector2Int.RoundToInt((point - bottomLeft) / Step);

            private bool Inside(Vector2Int point) =>
                point.x >= 0 && point.y >= 0 && point.x < occupied.GetLength(0) && point.y < occupied.GetLength(1);
        }
    }
}
