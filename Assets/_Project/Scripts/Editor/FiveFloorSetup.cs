using System;
using System.IO;
using System.Linq;
using EscapeUNIFRANZ.World;
using EscapeUNIFRANZ.Encounters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EscapeUNIFRANZ.EditorTools
{
    public static class FiveFloorSetup
    {
        const string Art="Assets/_Project/Art/Levels/nivel3_aulas/tercer_piso/";
        const string Third="Assets/_Project/Scenes/Zones/Z08_Piso3Aulas.unity";
        [Serializable] class Sheet{public Frame[] frames;}
        [Serializable] class Frame{public Cell frame;public int duration;}
        [Serializable] class Cell{public int x,y,w,h;}
        [MenuItem("Escape UNIFRANZ/Art/Integrar decoración y pisos 4 y 5")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            var second=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Zones/Z07_Aula2.unity",OpenSceneMode.Single);
            var bin=second.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<TrashBinInteractable>()).First();
            string templatePath=Art+"BasureroTemplate.prefab";PrefabUtility.SaveAsPrefabAsset(bin.gameObject,templatePath);
            var scene=EditorSceneManager.OpenScene(Third,OpenSceneMode.Single);var zone=Zone(scene);
            foreach(string name in new[]{"Decoracion_Autor","Ruptura_Aula_Cangrejo"}){var old=zone.transform.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Levels/GroupWorldLit.mat");
            var deco=Import("decoracion.png");
            Draw(zone.transform,"Decoracion_Autor",deco,new Rect(0,0,1248,736),Vector2.zero,"Decals",30,material,"DecoracionAutor.asset");
            string sheetPath=Art+"cuarto_ruptura/sprites.png";var texture=Import("cuarto_ruptura/sprites.png");
            var data=JsonUtility.FromJson<Sheet>(File.ReadAllText(Art+"cuarto_ruptura/sprites.json"));
            if(data.frames.Length!=24)throw new InvalidOperationException("La ruptura necesita 24 cuadros.");
            var root=new GameObject("Ruptura_Aula_Cangrejo");root.transform.SetParent(zone.transform,false);root.transform.position=new Vector2(968f/32,(736-220-288)/32f);
            var renderer=root.AddComponent<SpriteRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerName="Decals";renderer.sortingOrder=40;
            var effect=root.AddComponent<CrabRoomRupture>();effect.crab=zone.GetComponentInChildren<MechanicalCrabController>();effect.crabs=zone.GetComponentsInChildren<MechanicalCrabController>();effect.visual=renderer;effect.frames=new Sprite[24];effect.durations=new float[24];
            for(int i=0;i<24;i++){var f=data.frames[i];effect.frames[i]=SpriteAsset(texture,new Rect(f.frame.x,texture.height-f.frame.y-f.frame.h,f.frame.w,f.frame.h),"cuarto_ruptura/GameFrame_"+i+".asset");effect.durations[i]=Mathf.Max(.02f,f.duration/1000f);}
            renderer.sprite=effect.frames[0];Supply(zone,3,templatePath);EditorSceneManager.SaveScene(scene);
            for(int floor=4;floor<=5;floor++)
            {
                scene=EditorSceneManager.OpenScene(Third,OpenSceneMode.Single);
                string path="Assets/_Project/Scenes/Zones/Z"+(floor+5).ToString("D2")+"_Piso"+floor+"Aulas.unity";
                EditorSceneManager.SaveScene(scene,path,true);scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);zone=Zone(scene);
                zone.name="Zone_Piso"+floor+"Aulas";Aula2MapBuilder.Set(zone,"zoneId","piso"+floor+"_aulas");Aula2MapBuilder.Set(zone,"displayName","Piso "+floor+" · Aulas");Aula2MapBuilder.Set(zone,"objectiveId","piso"+floor+"_explore");
                Aula2MapBuilder.Set(zone,"objectiveText","Explora el piso "+floor+". E: recoger latas; G: lanzar. Usa el ascensor para cambiar de piso.");
                foreach(string name in new[]{"Decoracion_Autor","Ruptura_Aula_Cangrejo","Decoración_Capa","Ambientación · Abandono"}){var old=zone.transform.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
                foreach(var crab in zone.GetComponentsInChildren<MechanicalCrabController>())Object.DestroyImmediate(crab.gameObject);
                Supply(zone,floor,templatePath);EditorSceneManager.SaveScene(scene);Register("piso"+floor+"_aulas",path);
            }
            AssetDatabase.SaveAssets();Debug.Log("FIVE_FLOORS_OK: authored decoration and rupture only on floor 3; floors 4/5 registered with independent supplies.");
        }
        static ZoneContext Zone(Scene scene)=>scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();
        static void Supply(ZoneContext zone,int floor,string templatePath)
        {
            var old=zone.transform.Find("Basureros_Piso");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("Basureros_Piso").transform;root.SetParent(zone.transform,false);
            Vector2[] preferred={new Vector2(8.5f,14.8f),new Vector2(19.5f,14.7f),new Vector2(33.7f,15)};
            for(int i=0;i<3;i++)
            {
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(templatePath),zone.gameObject.scene);obj.transform.SetParent(root,false);obj.transform.position=Free(preferred[i],zone);
                var so=new SerializedObject(obj.GetComponent<TrashBinInteractable>());so.FindProperty("binId").stringValue="piso"+floor+"_basurero_"+i;so.ApplyModifiedPropertiesWithoutUndo();
                var marker=obj.GetComponent<EscapeUNIFRANZ.Map.MinimapTrackable2D>();if(marker!=null){var markerData=new SerializedObject(marker);markerData.FindProperty("markerId").stringValue="piso"+floor+"_basurero_"+i;markerData.ApplyModifiedPropertiesWithoutUndo();}
            }
            foreach(var machine in zone.GetComponentsInChildren<VendingMachineInteractable>()){var so=new SerializedObject(machine);so.FindProperty("machineId").stringValue="piso"+floor+"_bebidas";so.ApplyModifiedPropertiesWithoutUndo();}
        }
        static Vector2 Free(Vector2 desired,ZoneContext zone)
        {
            Physics2D.SyncTransforms();var filter=ContactFilter2D.noFilter;filter.useTriggers=false;var hits=new System.Collections.Generic.List<Collider2D>();
            Vector2 best=default;float score=float.MaxValue;
            for(float y=desired.y-1.25f;y<=desired.y+1.25f;y+=.125f)for(float x=desired.x-1.25f;x<=desired.x+1.25f;x+=.125f)
            {
                var p=new Vector2(x,y);hits.Clear();Physics2D.OverlapBox(p+Vector2.up*.11f,new Vector2(.6f,.4f),0,filter,hits);
                if(hits.Count>0||zone.GetComponentsInChildren<ZoneExitInteractable>().Any(e=>Vector2.Distance(e.transform.position,p)<1.2f))continue;
                hits.Clear();Physics2D.OverlapBox(p+Vector2.down*.27f,new Vector2(.38f,.22f),0,filter,hits);if(hits.Count>0)continue;
                float d=(p-desired).sqrMagnitude;if(d<score){score=d;best=p;}
            }
            if(score==float.MaxValue)throw new InvalidOperationException("No hay sitio libre para el basurero.");return best;
        }
        static void Register(string id,string path)
        {
            var so=new SerializedObject(AssetDatabase.LoadAssetAtPath<ZoneCatalog>("Assets/_Project/Data/Zones/ZoneCatalog.asset"));var list=so.FindProperty("zones");int index=-1;
            for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).FindPropertyRelative("zoneId").stringValue==id)index=i;
            if(index<0){index=list.arraySize;list.arraySize++;}var entry=list.GetArrayElementAtIndex(index);entry.FindPropertyRelative("zoneId").stringValue=id;entry.FindPropertyRelative("scenePath").stringValue=path;so.ApplyModifiedPropertiesWithoutUndo();
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(s=>s.path!=path).Append(new EditorBuildSettingsScene(path,true)).ToArray();
        }
        static Texture2D Import(string file)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+file);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(Art+file);
        }
        static Sprite SpriteAsset(Texture2D texture,Rect rect,string file)
        {
            var sprite=Sprite.Create(texture,rect,Vector2.zero,32);var existing=AssetDatabase.LoadAssetAtPath<Sprite>(Art+file);if(existing==null)AssetDatabase.CreateAsset(sprite,Art+file);else{EditorUtility.CopySerialized(sprite,existing);Object.DestroyImmediate(sprite);sprite=existing;}return sprite;
        }
        static void Draw(Transform parent,string name,Texture2D texture,Rect rect,Vector2 position,string layer,int order,Material material,string file)
        {var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.localPosition=position;var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=SpriteAsset(texture,rect,file);sr.sharedMaterial=material;sr.sortingLayerName=layer;sr.sortingOrder=order;}
    }
}
