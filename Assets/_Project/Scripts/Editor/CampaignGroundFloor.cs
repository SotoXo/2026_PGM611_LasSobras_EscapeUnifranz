using System;
using System.Collections.Generic;
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Puzzles;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class CampaignGroundFloor
    {
        private const string MapPath = "Assets/_Project/Art/Levels/nivel1_planta_baja/nivel1_planta_baja.prefab";
        private const string MaterialPath = "Assets/_Project/Art/Levels/GroupWorldLit.mat";

        [MenuItem("Escape UNIFRANZ/Campaña/Reconectar planta baja")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                MergeCollision();
                foreach (string name in new[] { "Z00_Entrada", "Z01_Hall", "Z02_ARCA" })
                {
                    var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Zones/" + name + ".unity");
                    var zone = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ZoneContext>(true)).Single();
                    Configure(zone, zone.transform.Find("GroupLevelArt").gameObject);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException($"No se guardó {name}");
                }
            }
            finally
            {
                if (setup.Any(scene => scene.isLoaded && scene.isActive && !string.IsNullOrEmpty(scene.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        internal static void MergeCollision(GameObject prefab)
        {
            var group = prefab.transform.Find("WorldCollision_Authored");
            if (group == null) throw new InvalidOperationException("Falta la colisión del mapa completo.");
            var body = Ensure<Rigidbody2D>(group.gameObject);
            body.bodyType = RigidbodyType2D.Static;
            var composite = Ensure<CompositeCollider2D>(group.gameObject);
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Manual;
            bool hasWallShadows = prefab.transform.Find("WallShadows") != null;
            composite.vertexDistance = hasWallShadows ? 0.001f : 0.006f;
            foreach (var box in group.GetComponentsInChildren<BoxCollider2D>())
                box.compositeOperation = Collider2D.CompositeOperation.Merge;
            composite.GenerateGeometry();
            if (composite.pathCount == 0) throw new InvalidOperationException("Colisión compuesta vacía.");
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;

            if (!hasWallShadows)
            {
                // Older maps use the movement collider as their shadow source.
                var shadow = Ensure<ShadowCaster2D>(group.gameObject);
                shadow.castsShadows = true;
                shadow.selfShadows = false;
            }
            Debug.Log($"Planta baja: {composite.pathCount} contornos físicos compuestos.");
        }

        private static void MergeCollision()
        {
            var prefab = PrefabUtility.LoadPrefabContents(MapPath);
            try
            {
                MergeCollision(prefab);
                PrefabUtility.SaveAsPrefabAsset(prefab, MapPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        internal static void Configure(ZoneContext zone, GameObject art)
        {
            var narrative = zone.transform.Find("NarrativeContent");
            var director = narrative.GetComponent<WorldHorrorDirector>();
            var content = zone.GetComponentsInChildren<InteractableBehaviour>(true);
            Vector2 spawn = zone.GetComponentsInChildren<SpawnPoint>(true).Single(p => p.SpawnId == zone.DefaultCheckpointSpawnId).Position;
            var solids = art.GetComponentsInChildren<Collider2D>(true)
                .Where(CampaignReleaseAudit.IsMapCollider).ToArray();
            var walkable = new CampaignReleaseAudit.WalkableGrid(zone.MinimapBounds, solids, new Vector2(.38f, .22f), new Vector2(0, .13f));
            if (walkable.Fill(spawn) == 0) throw new InvalidOperationException("No se puede salir del checkpoint de " + zone.ZoneId);

            bool entrance = zone.ZoneId == "entrada";
            bool hall = zone.ZoneId == "hall";
            bool entranceKey = entrance && zone.GetComponentInChildren<KeyedPassage>(true) != null;
            foreach (var lamp in art.GetComponentsInChildren<Light2D>(true))
            {
                if (lamp.lightType == Light2D.LightType.Global)
                    lamp.enabled = false;
                else
                    lamp.intensity = .065f;
            }
            var global = zone.transform.Find("MapaCompleto_GlobalLight").GetComponent<Light2D>();
            global.enabled = true;
            global.color = Color.Lerp(Color.white, HorrorPalette.Cyan, .22f);
            global.intensity = entrance ? .12f : .17f;
            foreach (var pickup in art.GetComponentsInChildren<ItemPickupInteractable>(true))
            {
                bool keyInHall = hall && pickup.ItemId == "key";
                pickup.gameObject.SetActive(keyInHall || (entranceKey && pickup.ItemId == "key"));
                if (keyInHall) pickup.transform.position = walkable.Nearest(new Vector2(17.55f, 10.18f));
            }

            string[] named = entrance
                ? new[] { "PC_Recepcion", "Clue_entrada_manual", "Clue_power_confirm", "Panel_power_entrada",
                    "SecurityTerminal_entrada", "Panel_Inspeccion", "Prueba_Inspeccion", "Panel_entrance_code",
                    "Pickup_Linterna_Entrada" }
                : hall
                    ? new[] { "Clue_hall_admision", "Clue_hall_mantenimiento", "Panel_maintenance_hall",
                        "Monitor_Comprometido", "SecurityTerminal_hall" }
                    : new[] { "Clue_arca_protocolo", "Clue_arca_confirm", "TerminalAccesoARCA" };
            var restored = new List<Transform>();
            foreach (string name in named)
            {
                var target = content.First(c => c.name == name && c.enabled);
                Vector2 wish = name == "PC_Recepcion" ? new Vector2(3.25f, 8.8f) : (Vector2)target.transform.position;
                target.transform.position = walkable.Nearest(wish, restored.Select(t => (Vector2)t.position), .55f);
                restored.Add(target.transform);
                EnableInteraction(target);
            }

            if (entrance)
            {
                InstallBattery(narrative, walkable.Nearest(new Vector2(7.82f, 3.5f)));
                var robot = director.firstRobot;
                foreach (var renderer in robot.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = true;
                foreach (var collider in robot.GetComponentsInChildren<Collider2D>(true)) collider.enabled = true;
                var path = robot.GetComponentInParent<PatrolPath2D>();
                if (path != null)
                {
                    foreach (Transform child in path.transform)
                        if (child.name.StartsWith("Route_", StringComparison.Ordinal))
                            child.position = walkable.Nearest(child.position);
                }
            }
            if (hall)
            {
                var exit = zone.GetComponentsInChildren<ZoneExitInteractable>().Single(c => c.TargetZoneId == "piso2");
                if (exit.elevatorPanel == null)
                {
                    exit.transform.position = walkable.Nearest(new Vector2(12.93f, 5.58f));
                    zone.GetComponentsInChildren<SpawnPoint>().Single(p => p.SpawnId == "from_piso2").transform.position =
                        walkable.Nearest(new Vector2(13.55f, 6.42f));
                }
                director.alarmDoors = new[] { exit };
            }

            foreach (var exit in zone.GetComponentsInChildren<ZoneExitInteractable>())
                EnableInteraction(exit);
            InstallInteriorDoor(narrative);
            foreach (var camera in narrative.GetComponentsInChildren<SecurityCamera2D>(true))
            {
                foreach (var renderer in camera.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = true;
                foreach (var light in camera.GetComponentsInChildren<Light2D>(true)) light.enabled = true;
            }

            var rooms = narrative.GetComponentsInChildren<RoomLightingController>(true);
            foreach (var room in rooms) RestoreRoom(room, walkable);
            director.alarmRooms = rooms.Where(room => room.roomId is "recepcion" or "hall" or "arca").ToArray();
            director.revealRooms = entrance ? rooms.Where(room => room.roomId is "recepcion" or "dental").ToArray() :
                Array.Empty<RoomLightingController>();
            foreach (var room in director.revealRooms) room.restoredByDirector = true;
        }

        private static T Ensure<T>(GameObject owner) where T : Component
        {
            var component = owner.GetComponent<T>();
            return component != null ? component : owner.AddComponent<T>();
        }

        private static void EnableInteraction(InteractableBehaviour target)
        {
            target.gameObject.SetActive(true);
            foreach (var renderer in target.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = true;
            foreach (var light in target.GetComponentsInChildren<Light2D>(true)) light.enabled = true;
            foreach (var trigger in target.GetComponentsInChildren<Collider2D>(true))
                if (trigger.isTrigger) trigger.enabled = true;
        }

        private static void InstallInteriorDoor(Transform parent)
        {
            if (parent.Find("Puerta_SalaReuniones") != null) return;
            var owner = new GameObject("Puerta_SalaReuniones");
            owner.transform.SetParent(parent, false);
            owner.transform.position = new Vector2(13.23f, 7.91f);
            var panel = owner.AddComponent<DoorPanel2D>();
            owner.AddComponent<InteriorDoorInteractable>();
            owner.AddComponent<InteractableFeedback2D>();
            var trigger = owner.AddComponent<CircleCollider2D>();
            trigger.radius = .55f;
            trigger.isTrigger = true;

            var leaf = new GameObject("Hoja");
            leaf.transform.SetParent(owner.transform, false);
            var sprite = leaf.AddComponent<SpriteRenderer>();
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/Environment/Access/puerta_corrediza.png");
            sprite.sortingLayerName = "World";
            sprite.sortingOrder = WorldYSort2D.Order(owner.transform.position.y);
            sprite.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            leaf.transform.localScale = Vector3.one * .86f;
            var blocker = leaf.AddComponent<BoxCollider2D>();
            blocker.size = new Vector2(.68f, .13f);
            var shadow = leaf.AddComponent<ShadowCaster2D>();
            shadow.selfShadows = false;

            using var data = new SerializedObject(panel);
            data.FindProperty("leaf").objectReferenceValue = sprite;
            data.FindProperty("blocker").objectReferenceValue = blocker;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InstallBattery(Transform parent, Vector2 position)
        {
            var node = parent.Find("Pickup_Bateria_Entrada");
            if (node == null)
            {
                node = new GameObject("Pickup_Bateria_Entrada").transform;
                node.SetParent(parent, false);
            }
            node.position = position;
            var item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Narrative/Items/battery.asset");
            var pickup = Ensure<ItemPickupInteractable>(node.gameObject);
            pickup.item = item;
            pickup.itemId = item.id;
            pickup.pickupId = "entrada_bateria_respaldo";
            var sprite = Ensure<SpriteRenderer>(node.gameObject);
            sprite.sprite = item.icon;
            sprite.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            sprite.sortingLayerName = "World";
            sprite.sortingOrder = WorldYSort2D.Order(position.y);
            var collider = Ensure<CircleCollider2D>(node.gameObject);
            collider.isTrigger = true;
            collider.radius = .42f;
            Ensure<InteractableFeedback2D>(node.gameObject);
            EnableInteraction(pickup);
        }

        private static void RestoreRoom(RoomLightingController room, CampaignReleaseAudit.WalkableGrid walkable)
        {
            Rect bounds = room.roomId switch
            {
                "recepcion" => new Rect(1.45f, 6.95f, 8.7f, 2.35f),
                "dental" => new Rect(3.6f, 1.35f, 7.8f, 4f),
                "admision" or "hall" => new Rect(12.3f, 5.7f, 8.4f, 2.7f),
                "arca" => new Rect(3f, 8.6f, 6.25f, 4.55f),
                _ => room.roomBounds
            };
            room.gameObject.SetActive(true);
            room.roomBounds = bounds;
            room.normalIntensity = .52f;
            room.dimIntensity = .095f;
            var trigger = room.GetComponent<BoxCollider2D>();
            trigger.enabled = true;
            trigger.offset = room.transform.InverseTransformPoint(bounds.center);
            trigger.size = bounds.size;

            for (int i = 0; i < room.mainLights.Length; i++)
            {
                var light = room.mainLights[i];
                var anchor = bounds.center + Vector2.right * bounds.width * (i == 0 ? -.23f : .23f);
                light.transform.position = walkable.Nearest(anchor);
                light.enabled = true;
                light.intensity = room.dimIntensity;
                if (light.TryGetComponent<SpriteRenderer>(out var sprite))
                {
                    sprite.enabled = true;
                    sprite.sortingOrder = WorldYSort2D.Order(light.transform.position.y);
                }
            }

            foreach (var fill in room.fillLights)
            {
                fill.transform.position = bounds.center;
                fill.enabled = true;
                fill.intensity = room.dimIntensity * .6f;
                float x = bounds.width / 2 - .34f, y = bounds.height / 2 - .34f;
                const float corner = .24f;
                fill.SetShapePath(new[]
                {
                    new Vector3(-x + corner, -y), new Vector3(x - corner, -y),
                    new Vector3(x, -y + corner), new Vector3(x, y - corner),
                    new Vector3(x - corner, y), new Vector3(-x + corner, y),
                    new Vector3(-x, y - corner), new Vector3(-x, -y + corner)
                });
                fill.shapeLightFalloffSize = .3f;
            }

            foreach (var guide in room.auxiliaryLights)
            {
                guide.transform.position = walkable.Nearest(bounds.center);
                guide.enabled = true;
            }
        }
    }
}
