using System.Linq;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.EditorTools
{
    /// <summary>Herramientas de Abril: 8 bits, sonidos y decoración. Ejecutar en orden desde el menú "Escape UNIFRANZ/Abril".</summary>
    public static class AbrilSetup
    {
        private const string Root = "Assets/_Project/";
        private const string Ui = Root + "Art/UI/PixelUI/";
        private const string Deco = Root + "Art/Environment/Decoration/";
        private const string FontAssetPath = Ui + "PixelTerror 5x7 SDF.asset";
        private static readonly string[] Zones = { "Z00_Entrada", "Z01_Hall", "Z02_ARCA", "Z03_Piso2", "Z04_Piso3", "Z05_LabSoftware", "Z06_NucleoIA", "Z07_Aula2", "Z08_Piso3Aulas" };

        public static void InstallAll()
        {
            ImportArt();
            if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath)==null)CreateFont();
            BindAudio();
            var scene=EditorSceneManager.OpenScene(Root+"Scenes/Bootstrap/Bootstrap.unity",OpenSceneMode.Single);
            foreach(var image in Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(image.name=="Minimap"){image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Ui+"minimap_frame.png");image.type=Image.Type.Sliced;foreach(var text in image.GetComponentsInChildren<TMP_Text>(true))text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);EditorUtility.SetDirty(image);}
            foreach(var marker in Object.FindObjectsByType<EscapeUNIFRANZ.Map.MinimapMarkerView>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {marker.pixelIcons=new[]{"exit","objective","checkpoint","threat","point","point","threat"}.Select(n=>AssetDatabase.LoadAssetAtPath<Sprite>(Ui+"icon_"+n+".png")).ToArray();EditorUtility.SetDirty(marker);}
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("ABRIL_INTEGRATION_OK");
        }

        public static void KeepMinimapAndAudio()
        {
            foreach(string name in Zones)
            {
                var scene=EditorSceneManager.OpenScene(Root+"Scenes/Zones/"+name+".unity",OpenSceneMode.Single);
                foreach(var zone in Object.FindObjectsByType<ZoneContext>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    var decoration=zone.transform.Find("Decoración_Capa");
                    if(decoration!=null)Object.DestroyImmediate(decoration.gameObject);
                }
                EditorSceneManager.SaveScene(scene);
            }
            BindAudio();AssetDatabase.SaveAssets();Debug.Log("ABRIL_ONLY_MINIMAP_AUDIO_OK");
        }

        [MenuItem("Escape UNIFRANZ/Abril/1 - Importar pixel art")]
        public static void ImportArt()
        {
            AssetDatabase.Refresh();
            int decoPpu = 64;
            var ref0 = AssetImporter.GetAtPath(Root + "Art/Environment/Decals/grietas.png") as TextureImporter;
            if (ref0 != null) decoPpu = (int)ref0.spritePixelsPerUnit;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Ui, Deco }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                bool ui = path.StartsWith(Ui);
                string n = System.IO.Path.GetFileNameWithoutExtension(path);
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.spritePixelsPerUnit = ui ? 16 : decoPpu;
                ti.spriteBorder = n.StartsWith("btn_") ? new Vector4(4, 4, 4, 4) : n == "panel_frame" || n == "minimap_frame" ? new Vector4(8, 8, 8, 8) : Vector4.zero;
                ti.SaveAndReimport();
            }
            Debug.Log("[Abril] Pixel art importado (filtro Point, sin compresión).");
        }

        [MenuItem("Escape UNIFRANZ/Abril/2 - Crear fuente TMP 8 bits")]
        public static void CreateFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(Ui + "PixelTerror5x7.ttf");
            if (font == null) { Debug.LogError("[Abril] No se encontró PixelTerror5x7.ttf"); return; }
            var fa = TMP_FontAsset.CreateFontAsset(font, 20, 2, GlyphRenderMode.RASTER, 512, 512, AtlasPopulationMode.Dynamic, true);
            AssetDatabase.CreateAsset(fa, FontAssetPath);
            fa.material.name = "PixelTerror Material"; AssetDatabase.AddObjectToAsset(fa.material, fa);
            foreach (var tex in fa.atlasTextures) { tex.name = "PixelTerror Atlas"; tex.filterMode = FilterMode.Point; AssetDatabase.AddObjectToAsset(tex, fa); }
            string chars = " ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚÜÑáéíóúüñ0123456789.,:;!?¡¿-+/()'\"%_=*<>·—–";
            fa.TryAddCharacters(chars, out string missing);
            EditorUtility.SetDirty(fa); AssetDatabase.SaveAssets();
            Debug.Log("[Abril] Fuente TMP creada: " + FontAssetPath + (string.IsNullOrEmpty(missing) ? "" : " | sin glifo: " + missing));
        }

        [MenuItem("Escape UNIFRANZ/Abril/3 - Aplicar estilo 8 bits al menú de inicio")]
        public static void SkinMainMenu()
        {
            var scene = EditorSceneManager.OpenScene(Root + "Scenes/UI/MainMenu.unity", OpenSceneMode.Single);
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite S(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(Ui + n + ".png");
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var img = b.targetGraphic as Image ?? b.GetComponent<Image>();
                if (img == null) continue;
                img.sprite = S("btn_normal"); img.type = Image.Type.Sliced; img.color = Color.white;
                b.transition = Selectable.Transition.SpriteSwap;
                b.spriteState = new SpriteState { highlightedSprite = S("btn_hover"), selectedSprite = S("btn_hover"), pressedSprite = S("btn_pressed"), disabledSprite = S("btn_disabled") };
                if (b.GetComponent<PixelButtonFeedback>() == null) b.gameObject.AddComponent<PixelButtonFeedback>();
                EditorUtility.SetDirty(b);
            }
            if (fa != null)
                foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { t.font = fa; EditorUtility.SetDirty(t); }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("[Abril] Menú de inicio en 8 bits aplicado. Revisa tamaños de texto en el Inspector (la fuente 5x7 se ve mejor en múltiplos de 8).");
        }

        [MenuItem("Escape UNIFRANZ/Abril/4 - Conectar sonidos nuevos (ascensor, expendedora, pisos)")]
        public static void BindAudio()
        {
            var cues = new (AudioCue cue, string file, float vol)[] {
                (AudioCue.ElevatorOpen, "elevator_open", .7f), (AudioCue.ElevatorTravel, "elevator_travel", .5f), (AudioCue.ElevatorArrive, "elevator_arrive", .6f),
                (AudioCue.VendingDispense, "vending_dispense", .7f),
                (AudioCue.AmbienceFloor0, "ambience_floor0", .35f), (AudioCue.AmbienceFloor1, "ambience_floor1", .35f), (AudioCue.AmbienceFloor2, "ambience_floor2", .35f) };
            foreach (string scenePath in new[] { Root + "Scenes/Bootstrap/Bootstrap.unity", Root + "Scenes/UI/MainMenu.unity" })
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (var hooks in Object.FindObjectsByType<ProjectAudioHooks>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var so = new SerializedObject(hooks); var arr = so.FindProperty("bindings");
                    foreach (var (cue, file, vol) in cues)
                    {
                        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/Campaign/" + file + ".wav");
                        if (clip == null) continue;
                        SerializedProperty item = null;
                        for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).FindPropertyRelative("cue").enumValueIndex == (int)cue) item = arr.GetArrayElementAtIndex(i);
                        if (item == null) { arr.arraySize++; item = arr.GetArrayElementAtIndex(arr.arraySize - 1); item.FindPropertyRelative("cue").enumValueIndex = (int)cue; }
                        item.FindPropertyRelative("clip").objectReferenceValue = clip; item.FindPropertyRelative("volume").floatValue = vol;
                    }
                    so.ApplyModifiedProperties();
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[Abril] Sonidos nuevos conectados en Bootstrap y MainMenu.");
        }

        [MenuItem("Escape UNIFRANZ/Abril/5 - Añadir capa Decoración a los pisos")]
        public static void AddDecoration()
        {
            var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { Deco }).Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g))).Where(s => s != null).ToArray();
            foreach (string zoneName in Zones)
            {
                var scene = EditorSceneManager.OpenScene(Root + "Scenes/Zones/" + zoneName + ".unity", OpenSceneMode.Single);
                var zone = Object.FindFirstObjectByType<ZoneContext>(FindObjectsInactive.Include);
                if (zone == null) { Debug.LogWarning("[Abril] Sin ZoneContext en " + zoneName); continue; }
                var holder = zone.transform.Find("Decoración_Capa")?.gameObject ?? new GameObject("Decoración_Capa");
                holder.transform.SetParent(zone.transform, false);
                var sc = holder.GetComponent<DecorationScatterer>() ?? holder.AddComponent<DecorationScatterer>();
                int floor = EscapeUNIFRANZ.Map.FloorInfo.FloorOf(zone.ZoneId);
                sc.sprites = sprites; sc.area = zone.MinimapBounds; sc.seed = 100 + System.Array.IndexOf(Zones, zoneName);
                sc.litMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Art/Levels/GroupWorldLit.mat");
                sc.count = floor == 0 ? 45 : floor == 1 ? 55 : 65;
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[Abril] Capa Decoración añadida (se genera al iniciar cada zona, sin colliders).");
        }
    }
}
