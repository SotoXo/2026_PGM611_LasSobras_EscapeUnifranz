using System;
using System.Linq;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EscapeUNIFRANZ.EditorTools
{
    /// <summary>
    /// Menú: Escape UNIFRANZ > Expendedoras > Instalar en piso 1 y 2.
    /// Usa los sprites de Art/Environment/expendedoras_grises_v2. Importa sprites, crea el ItemData
    /// "soda", lo registra en el catálogo, agrega el lanzamiento [G] al jugador (Bootstrap) y coloca
    /// una expendedora de bebidas (comprable) y una de frituras (decorativa) en cada piso.
    /// Es repetible: reemplaza el nodo "VendingMachines" de cada zona.
    /// </summary>
    public static class VendingMachineBuilder
    {
        private const string V2 = "Assets/_Project/Art/Environment/expendedoras_grises_v2/";
        private const string CanArt = "Assets/_Project/Art/Items/lata_naranja.png";
        private const string SodaAsset = "Assets/_Project/Data/Narrative/Items/soda.asset";
        private const string EmptyCanAsset = "Assets/_Project/Data/Narrative/Items/empty_can.asset";
        [MenuItem("Escape UNIFRANZ/Expendedoras/Configurar latas y derrame")]
        public static void ConfigureCanCombatItems()
        {
            var soda=MakeSoda();AssetDatabase.SaveAssets();PrepareBootstrap(soda);
            AssetDatabase.SaveAssets();Debug.Log("CAN_SPILL_ITEMS_OK: full and empty cans registered; maximum three per dispenser.");
        }
        private const string Bootstrap = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        private const string LitMaterial = "Assets/_Project/Art/Levels/GroupWorldLit.mat";
        private const string RootName = "VendingMachines";

        private sealed class Spot
        {
            public string name, machineId; public bool drinks; public float centerPx, feetPx;
            public Spot(string n, string id, bool d, float cx, float fy) { name = n; machineId = id; drinks = d; centerPx = cx; feetPx = fy; }
        }

        // Píxeles del nivel (origen abajo-izquierda, 64 px = 1 unidad), igual que LevelGeometry.json.
        private static readonly (string scene, Spot[] spots)[] Floors =
        {
            ("Z03_Piso2", new[] {
                new Spot("Expendedora_Bebidas_P1", "p1_bebidas", true, 672f, 640f),
                new Spot("Expendedora_Frituras_P1", "p1_frituras", false, 736f, 640f) }),
            ("Z04_Piso3", new[] {
                new Spot("Expendedora_Bebidas_P2", "p2_bebidas", true, 1216f, 232f),
                new Spot("Expendedora_Frituras_P2", "p2_frituras", false, 1280f, 232f) }),
        };

        private static string[] DrinkFrames => Enumerable.Range(0, 6).Select(i => V2 + "bebidas_compra_0" + i + "_64.png").ToArray();

        public static void InstallInAula2()
        {
            foreach(string p in DrinkFrames) ImportSprite(p,SpriteAlignment.BottomCenter);
            ImportSprite(V2+"expendedora_bebidas_reposo_64.png",SpriteAlignment.BottomCenter);
            ImportSprite(CanArt,SpriteAlignment.Center);
            var soda=MakeSoda();AssetDatabase.SaveAssets();PrepareBootstrap(soda);
            Vector2 position=new Vector2(21.26f,4.84f);
            PlaceFloor("Z07_Aula2",new[]{new Spot("Expendedora_Prueba_Abril","aula2_abril_prueba",true,position.x*64,position.y*64)},soda);
            var scene=SceneManager.GetSceneByPath("Assets/_Project/Scenes/Zones/Z07_Aula2.unity");
            var machine=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<VendingMachineInteractable>()).Single();
            var label=new GameObject("Prueba_Abril");label.transform.SetParent(machine.transform,false);label.transform.localPosition=Vector3.up*1.1f;
            var text=label.AddComponent<TMPro.TextMeshPro>();text.text="Abril · Expendedora de prueba";text.fontSize=1.5f;text.alignment=TMPro.TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(3.5f,.4f);
            text.GetComponent<MeshRenderer>().sortingLayerName="Effects";
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Escape UNIFRANZ/Expendedoras/Instalar en piso 1 y 2")]
        public static void Install()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();

            foreach (string p in DrinkFrames) ImportSprite(p, SpriteAlignment.BottomCenter);
            ImportSprite(V2 + "expendedora_bebidas_reposo_64.png", SpriteAlignment.BottomCenter);
            ImportSprite(V2 + "expendedora_frituras_64.png", SpriteAlignment.BottomCenter);
            ImportSprite(CanArt, SpriteAlignment.Center);

            var soda = MakeSoda();
            PrepareBootstrap(soda);
            foreach (var floor in Floors) PlaceFloor(floor.scene, floor.spots, soda);
            AssetDatabase.SaveAssets();
            Debug.Log("[Expendedoras] Listo en " + string.Join(", ", Floors.Select(f => f.scene)) + ". Jugar: [E] comprar/recoger, [G] lanzar.");
        }

        [MenuItem("Escape UNIFRANZ/Expendedoras/Reparar máquinas existentes")]
        public static void RepairExistingMachines()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();

            // Estos recursos ya existen en el proyecto; la reparación solo completa
            // referencias que quedaron vacías y conserva la posición de cada máquina.
            foreach (string path in DrinkFrames)
                if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
                    ImportSprite(path, SpriteAlignment.BottomCenter);

            string idlePath = V2 + "expendedora_bebidas_reposo_64.png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(idlePath) == null)
                ImportSprite(idlePath, SpriteAlignment.BottomCenter);

            string friturasPath = V2 + "expendedora_frituras_64.png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(friturasPath) == null)
                ImportSprite(friturasPath, SpriteAlignment.BottomCenter);

            string canPath = CanArt;
            if (AssetDatabase.LoadAssetAtPath<Sprite>(canPath) == null)
                ImportSprite(canPath, SpriteAlignment.Center);

            var soda = AssetDatabase.LoadAssetAtPath<ItemData>(SodaAsset);
            if (soda == null) soda = MakeSoda();
            PrepareBootstrap(soda);

            int repaired = 0;
            foreach (var floor in Floors)
            {
                string scenePath = "Assets/_Project/Scenes/Zones/" + floor.scene + ".unity";
                var scene = SceneManager.GetSceneByPath(scenePath);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                foreach (var machine in scene.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<VendingMachineInteractable>(true)))
                {
                    var so = new SerializedObject(machine);
                    var product = so.FindProperty("product");
                    var body = so.FindProperty("body");
                    var idle = so.FindProperty("idleSprite");
                    var frames = so.FindProperty("purchaseFrames");

                    if (product != null && product.objectReferenceValue == null)
                    {
                        product.objectReferenceValue = soda;
                        repaired++;
                    }

                    var renderer = machine.GetComponent<SpriteRenderer>();
                    if (body != null && body.objectReferenceValue == null) body.objectReferenceValue = renderer;
                    if (idle != null && idle.objectReferenceValue == null)
                        idle.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(idlePath);

                    if (frames != null && frames.arraySize == 0)
                    {
                        frames.arraySize = DrinkFrames.Length;
                        for (int i = 0; i < DrinkFrames.Length; i++)
                            frames.GetArrayElementAtIndex(i).objectReferenceValue =
                                AssetDatabase.LoadAssetAtPath<Sprite>(DrinkFrames[i]);
                    }

                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(machine);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Expendedoras] Reparación terminada. Referencias de producto completadas: " + repaired);
        }

        private static void ImportSprite(string path, SpriteAlignment alignment)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("No se encontró " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)alignment;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static ItemData MakeSoda()
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(SodaAsset);
            if (item == null) { item = ScriptableObject.CreateInstance<ItemData>(); AssetDatabase.CreateAsset(item, SodaAsset); }
            item.id = "soda"; item.displayName = "Lata de refresco";
            item.description = "G: lanzar. Derrama refresco y deja un charco que detiene robots. Recoge la lata vacía con E para volver a atacar.";
            item.examineText = "El líquido puede detener durante cuatro segundos a cada robot que cruce el charco. La lata vacía se recoge y sirve para golpear robots. Cada expendedora entrega como máximo tres latas.";
            item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(CanArt);
            item.fallbackIcon = "SOD"; item.consumable = true; item.stackable = true; item.placeholderIcon = false;
            item.category = ItemCategory.Consumable; item.immediateUse = ImmediateItemUse.None;
            EditorUtility.SetDirty(item);
            return item;
        }
        private static ItemData MakeEmptyCan()
        {
            var item=AssetDatabase.LoadAssetAtPath<ItemData>(EmptyCanAsset);
            if(item==null){item=ScriptableObject.CreateInstance<ItemData>();AssetDatabase.CreateAsset(item,EmptyCanAsset);}
            item.id="empty_can";item.displayName="Lata vacía";
            item.description="G: lanzar para dañar robots. Se consume al acertar; si cae al suelo puedes recogerla con E. No deja charcos.";
            item.examineText="Tres impactos de latas vacías derrotan a un robot. Cada impacto consume la lata. Las latas llenas solo inmovilizan y dejan líquido.";
            item.icon=AssetDatabase.LoadAssetAtPath<Sprite>(CanArt);item.fallbackIcon="LATA";
            item.consumable=true;item.stackable=true;item.placeholderIcon=false;item.category=ItemCategory.Consumable;
            EditorUtility.SetDirty(item);return item;
        }

        private static void PrepareBootstrap(ItemData soda)
        {
            var scene = SceneManager.GetSceneByPath(Bootstrap);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(Bootstrap, OpenSceneMode.Additive);
            soda = AssetDatabase.LoadAssetAtPath<ItemData>(SodaAsset);
            if (soda == null) throw new InvalidOperationException("No se encontró el producto soda de la expendedora.");

            var controller = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NarrativeController>(true)).FirstOrDefault();
            if (controller == null) throw new InvalidOperationException("No hay NarrativeController en Bootstrap.");
            var so = new SerializedObject(controller);
            var catalog = so.FindProperty("itemCatalog");
            bool present = false;
            for (int i = 0; i < catalog.arraySize; i++) if (catalog.GetArrayElementAtIndex(i).objectReferenceValue == soda) present = true;
            if (!present) { catalog.arraySize++; catalog.GetArrayElementAtIndex(catalog.arraySize - 1).objectReferenceValue = soda; }
            var emptyCan=MakeEmptyCan();
            bool hasEmpty=false;
            for(int i=0;i<catalog.arraySize;i++)if(catalog.GetArrayElementAtIndex(i).objectReferenceValue==emptyCan)hasEmpty=true;
            if(!hasEmpty){catalog.arraySize++;catalog.GetArrayElementAtIndex(catalog.arraySize-1).objectReferenceValue=emptyCan;}
            so.ApplyModifiedProperties();

            var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerMovement2D>(true)).FirstOrDefault();
            if (player == null) throw new InvalidOperationException("No hay jugador (PlayerMovement2D) en Bootstrap.");
            var thrower = player.GetComponent<ThrowableCanController>();
            if (thrower == null) thrower = player.gameObject.AddComponent<ThrowableCanController>();
            var throwerSo = new SerializedObject(thrower);
            throwerSo.FindProperty("canSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(CanArt);
            throwerSo.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }

        private static void PlaceFloor(string sceneName, Spot[] spots, ItemData soda)
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Zones/" + sceneName + ".unity", OpenSceneMode.Single);
            // Opening a scene can unload/reimport previously held ScriptableObjects.
            soda = AssetDatabase.LoadAssetAtPath<ItemData>(SodaAsset);
            if (soda == null) throw new InvalidOperationException("No se encontró el producto soda de la expendedora.");
            var zone = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ZoneContext>(true)).First();
            var old = zone.transform.Find(RootName); if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(RootName).transform; root.SetParent(zone.transform, false);
            var lit = AssetDatabase.LoadAssetAtPath<Material>(LitMaterial);
            var idle = AssetDatabase.LoadAssetAtPath<Sprite>(V2 + "expendedora_bebidas_reposo_64.png");

            foreach (var spot in spots)
            {
                var obj = new GameObject(spot.name); obj.transform.SetParent(root, false);
                obj.transform.localPosition = new Vector3(spot.centerPx / 64f, spot.feetPx / 64f, 0f);

                var sr = obj.AddComponent<SpriteRenderer>();
                obj.AddComponent<WorldYSort2D>();
                sr.sprite = spot.drinks ? idle : AssetDatabase.LoadAssetAtPath<Sprite>(V2 + "expendedora_frituras_64.png");
                if (lit != null) sr.sharedMaterial = lit;
                sr.sortingLayerName = "World"; sr.sortingOrder = WorldYSort2D.Order(spot.feetPx / 64f);

                var solid = new GameObject("Solid"); solid.transform.SetParent(obj.transform, false);
                var body = solid.AddComponent<BoxCollider2D>(); body.size = new Vector2(0.9f, 0.3f); body.offset = new Vector2(0f, 0.15f);

                if (!spot.drinks) continue;   // frituras: solo decoración (el arte no trae entrega)

                var trigger = obj.AddComponent<BoxCollider2D>(); trigger.isTrigger = true;
                trigger.size = new Vector2(1.6f, 1.0f); trigger.offset = new Vector2(0f, -0.1f);

                var machine = obj.AddComponent<VendingMachineInteractable>();
                var so = new SerializedObject(machine);
                so.FindProperty("machineId").stringValue = spot.machineId;
                so.FindProperty("product").objectReferenceValue = soda;
                so.FindProperty("body").objectReferenceValue = sr;
                so.FindProperty("idleSprite").objectReferenceValue = idle;
                var frames = so.FindProperty("purchaseFrames"); var paths = DrinkFrames; frames.arraySize = paths.Length;
                for (int i = 0; i < paths.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
                so.ApplyModifiedProperties();

                var scan = obj.AddComponent<Scannable2D>(); scan.enabled = true;
                var scanSo = new SerializedObject(scan);
                scanSo.FindProperty("category").intValue = (int)ScanCategory.Interactable;
                scanSo.FindProperty("shortLabel").stringValue = "EXPENDEDORA";
                scanSo.ApplyModifiedProperties();

                var marker = obj.AddComponent<MinimapTrackable2D>();
                var markerSo = new SerializedObject(marker);
                markerSo.FindProperty("markerId").stringValue = "vending_" + spot.machineId;
                markerSo.FindProperty("initiallyDiscovered").boolValue = false;
                markerSo.ApplyModifiedProperties();
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
    }
}
