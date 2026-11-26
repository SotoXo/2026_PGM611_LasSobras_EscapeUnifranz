using System;
using System.IO;
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EscapeUNIFRANZ.EditorTools
{
    public static class UpperFloorMissionSetup
    {
        const string Art="Assets/_Project/Art/ajustes_assets/";
        const string Generated=Art+"Generated/";
        [Serializable] class Sheet{public Entry[] frames;}
        [Serializable] class Entry{public Cell frame;public int duration;}
        [Serializable] class Cell{public int x,y,w,h;}
        sealed class Sequence{public Sprite[] sprites;public float[] seconds;}
        static Material material;
        public static void Build()
        {
            AssetDatabase.Refresh();if(!AssetDatabase.IsValidFolder(Generated.TrimEnd('/')))AssetDatabase.CreateFolder(Art.TrimEnd('/'),"Generated");
            material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Levels/GroupWorldLit.mat");
            var mine=Load("mina/sprites.png","mina/sprites.json","mine",64);
            var fox=Load("zorro/sprites.png","zorro/sprites.json","fox",64);
            var rescue=Load("persona_liberandose/sprites.png","persona_liberandose/sprites.json","rescue",64);
            var camera=Load("camara/superior_sprites.png","camara/superior.json","camera",64);
            var electronics=Load("caja/sprites.png","caja/sprites.json","electronics",64);
            var boxItem=Item("electronics_box","Caja de electrónica","Componente para fabricar una mina junto con una batería.",electronics.sprites[0]);
            var mineItem=Item("crafted_mine","Mina","Fabricada con una batería y una caja de electrónica. Colócala desde el inventario; explota al pisarla un robot.",mine.sprites[0]);
            var battery=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Narrative/Items/battery.asset");
            var obj=new GameObject("Mina_Fabricada");var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=mine.sprites[0];sr.sharedMaterial=material;sr.sortingLayerName="Decals";sr.sortingOrder=35;
            var trigger=obj.AddComponent<CircleCollider2D>();trigger.isTrigger=true;trigger.radius=.24f;
            var mineController=obj.AddComponent<CraftedMine>();mineController.visual=sr;mineController.frames=mine.sprites;mineController.durations=mine.seconds;
            string prefabPath=Generated+"CraftedMine.prefab";PrefabUtility.SaveAsPrefabAsset(obj,prefabPath);Object.DestroyImmediate(obj);
            var bootstrap=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity",OpenSceneMode.Single);
            var session=bootstrap.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<GameSessionController>()).Single();
            var inv=session.GetComponent<MineInventoryController>()??session.gameObject.AddComponent<MineInventoryController>();inv.minePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var narrative=session.GetComponent<NarrativeController>();var catalog=new SerializedObject(narrative);var items=catalog.FindProperty("itemCatalog");
            foreach(var item in new[]{boxItem,mineItem})if(!Enumerable.Range(0,items.arraySize).Any(i=>items.GetArrayElementAtIndex(i).objectReferenceValue==item)){items.arraySize++;items.GetArrayElementAtIndex(items.arraySize-1).objectReferenceValue=item;}
            catalog.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(bootstrap);
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Zones/Z08_Piso3Aulas.unity",OpenSceneMode.Single);var zone=Zone(scene);var mission=Root(zone);
            var crabs=zone.GetComponentsInChildren<MechanicalCrabController>();
            foreach(var crab in crabs){var bar=crab.GetComponent<CrabHealthBar>()??crab.gameObject.AddComponent<CrabHealthBar>();bar.target=crab;EditorUtility.SetDirty(bar);}
            var npc=Actor(mission,"Persona_Rescate",new Vector2(35.5f,8.3f),rescue,UpperActorRole.Rescue);npc.rescueTargets=crabs;EditorUtility.SetDirty(npc);EditorSceneManager.SaveScene(scene);
            for(int floor=4;floor<=5;floor++)
            {
                scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Zones/Z"+(floor+5).ToString("D2")+"_Piso"+floor+"Aulas.unity",OpenSceneMode.Single);zone=Zone(scene);mission=Root(zone);
                var bins=zone.GetComponentsInChildren<TrashBinInteractable>();for(int i=2;i<bins.Length;i++)Object.DestroyImmediate(bins[i].gameObject);
                var robots=zone.GetComponentsInChildren<MiniAutoController>().OrderBy(r=>r.name).ToArray();if(robots.Length!=3)throw new InvalidOperationException("Cada piso necesita tres autos robots.");
                Vector2[] positions=floor==4?new[]{new Vector2(6.56f,10.5f),new Vector2(24.6f,13.3f),new Vector2(30,9.7f)}:new[]{new Vector2(15.6f,10),new Vector2(22,13.3f),new Vector2(30,10.5f)};
                for(int i=0;i<robots.Length;i++)SetPatrol(robots[i],positions[i],i==1?Vector2.right:Vector2.up);
                Vector2[] boxes=floor==4?new[]{new Vector2(12.5f,17.1f),new Vector2(21.1f,16.6f)}:new[]{new Vector2(8.1f,8),new Vector2(21.4f,8.1f)};
                for(int i=0;i<2;i++)Pickup(mission,zone,boxItem,"piso"+floor+"_electronics_"+i,boxes[i]);
                Pickup(mission,zone,battery,"piso"+floor+"_battery_0",new Vector2(18.8f,14.9f));Pickup(mission,zone,battery,"piso"+floor+"_battery_1",new Vector2(27.9f,8.2f));
                if(floor==4)
                {
                    var deco=zone.transform.Find("Decoracion_Autor");if(deco==null){var decoration=new GameObject("Decoracion_Autor");decoration.transform.SetParent(zone.transform,false);var image=decoration.AddComponent<SpriteRenderer>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Levels/nivel3_aulas/tercer_piso/DecoracionAutor.asset");image.sharedMaterial=material;image.sortingLayerName="Decals";image.sortingOrder=30;}
                    Camera(mission,new Vector2(9.4f,13.75f),camera,0);Camera(mission,new Vector2(21.5f,13.75f),camera,1);Camera(mission,new Vector2(29.85f,12.8f),camera,2);
                }
                else Actor(mission,"Zorrito_Unico",Free(new Vector2(4.6f,15.2f),null),fox,UpperActorRole.Fox);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();Debug.Log("UPPER_FLOOR_MISSION_OK");
        }
        static Transform Root(ZoneContext zone){var old=zone.transform.Find("UpperFloorMission");if(old!=null)Object.DestroyImmediate(old.gameObject);var root=new GameObject("UpperFloorMission").transform;root.SetParent(zone.transform,false);return root;}
        static ZoneContext Zone(Scene scene)=>scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();
        static SpriteRenderer Render(Transform parent,string name,Vector2 position,Sprite sprite)
        {var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.position=position;var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingLayerName="World";sr.sortingOrder=WorldYSort2D.Order(position.y);return sr;}
        static UpperFloorActor Actor(Transform parent,string name,Vector2 position,Sequence sequence,UpperActorRole role)
        {var sr=Render(parent,name,position,sequence.sprites[0]);var actor=sr.gameObject.AddComponent<UpperFloorActor>();actor.role=role;actor.visual=sr;actor.frames=sequence.sprites;actor.durations=sequence.seconds;return actor;}
        static void Pickup(Transform parent,ZoneContext zone,ItemData item,string id,Vector2 desired)
        {var sr=Render(parent,id,Free(desired,null),item.icon);var pickup=sr.gameObject.AddComponent<CraftSupplyPickup>();pickup.item=item;pickup.pickupId=id;var box=sr.gameObject.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(.8f,.8f);}
        static void Camera(Transform parent,Vector2 position,Sequence frames,int index)
        {
            var actor=Actor(parent,"Camara_Seguridad_"+index,position,frames,UpperActorRole.Camera);var camera=actor.gameObject.AddComponent<SecurityCamera2D>();actor.cameraUnit=camera;camera.notifyNearestRobot=true;camera.disabledFlag=GameFlagId.None;camera.baseAngle=180;camera.sweepDegrees=32;camera.sweepSpeed=.5f;camera.phase=index;
            var sensorObj=new GameObject("Sensor");sensorObj.transform.SetParent(actor.transform,false);sensorObj.transform.localPosition=Vector3.down*.3f;
            var circle=sensorObj.AddComponent<CircleCollider2D>();circle.isTrigger=true;circle.radius=4;
            var sensor=sensorObj.AddComponent<DetectionSensor2D>();var so=new SerializedObject(sensor);so.FindProperty("directional").boolValue=true;so.FindProperty("fieldOfView").floatValue=70;so.ApplyModifiedPropertiesWithoutUndo();camera.sensor=sensor;
            var field=new GameObject("Perimetro_Luz").AddComponent<Light2D>();field.transform.SetParent(actor.transform,false);field.transform.localPosition=Vector3.down*.3f;field.transform.localRotation=Quaternion.Euler(0,0,180);field.lightType=Light2D.LightType.Point;field.pointLightOuterRadius=4;field.pointLightInnerRadius=.25f;field.pointLightOuterAngle=70;field.pointLightInnerAngle=40;field.intensity=.22f;field.color=new Color(1,.72f,.12f);field.shadowsEnabled=true;field.targetSortingLayers=SortingLayer.layers.Select(l=>l.id).ToArray();camera.fieldLight=field;
            var led=new GameObject("Led_Alerta").AddComponent<Light2D>();led.transform.SetParent(actor.transform,false);led.lightType=Light2D.LightType.Point;led.pointLightOuterRadius=.45f;led.intensity=.25f;led.color=Color.green;led.targetSortingLayers=field.targetSortingLayers;camera.ledLight=led;
        }
        static bool Clear(Vector2 point,Vector2 size,Transform ignore)
        {return !Physics2D.OverlapBoxAll(point+Vector2.up*.12f,size,0).Concat(Physics2D.OverlapCircleAll(point,.18f)).Any(c=>!c.isTrigger&&(ignore==null||!c.transform.IsChildOf(ignore)));}
        static Vector2 Free(Vector2 desired,Transform ignore)
        {
            Physics2D.SyncTransforms();Vector2 best=default;float score=float.MaxValue;
            for(float y=desired.y-1;y<=desired.y+1;y+=.125f)for(float x=desired.x-1;x<=desired.x+1;x+=.125f){var p=new Vector2(x,y);if(!Clear(p,new Vector2(.38f,.28f),ignore))continue;float d=(p-desired).sqrMagnitude;if(d<score){score=d;best=p;}}
            if(score==float.MaxValue)throw new InvalidOperationException("Sin posición libre cerca de "+desired);return best;
        }
        static void SetPatrol(MiniAutoController robot,Vector2 position,Vector2 axis)
        {
            position=Free(position,robot.transform);robot.transform.position=position;Physics2D.SyncTransforms();
            var so=new SerializedObject(robot);var mover=(PatrolMover2D)so.FindProperty("mover").objectReferenceValue;var path=(PatrolPath2D)new SerializedObject(mover).FindProperty("path").objectReferenceValue;var data=new SerializedObject(path);var points=data.FindProperty("points");
            for(int i=0;i<2;i++)
            {
                Vector2 end=position;var direction=axis*(i==0?-1:1);
                for(float d=.125f;d<=1.5f;d+=.125f){var p=position+direction*d;if(!Clear(p,new Vector2(.4f,.28f),robot.transform))break;end=p;}
                if(Vector2.Distance(end,position)<.25f)throw new InvalidOperationException("Patrulla demasiado corta: "+robot.name);
                ((Transform)points.GetArrayElementAtIndex(i).objectReferenceValue).position=end;
            }
            EditorUtility.SetDirty(path);
        }
        static ItemData Item(string id,string title,string description,Sprite sprite)
        {string path=Generated+id+".asset";var item=AssetDatabase.LoadAssetAtPath<ItemData>(path);if(item==null){item=ScriptableObject.CreateInstance<ItemData>();AssetDatabase.CreateAsset(item,path);}item.id=id;item.displayName=title;item.description=item.examineText=description;item.icon=sprite;item.stackable=true;item.consumable=true;item.category=ItemCategory.Consumable;EditorUtility.SetDirty(item);return item;}
        static Sequence Load(string png,string json,string key,float ppu)
        {
            string path=Art+png;var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var data=JsonUtility.FromJson<Sheet>(File.ReadAllText(Art+json));var result=new Sequence{sprites=new Sprite[data.frames.Length],seconds=new float[data.frames.Length]};
            for(int i=0;i<data.frames.Length;i++){var f=data.frames[i];var sprite=Sprite.Create(texture,new Rect(f.frame.x,texture.height-f.frame.y-f.frame.h,f.frame.w,f.frame.h),new Vector2(.5f,.2f),ppu);string asset=Generated+key+"_"+i+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(asset);if(old==null)AssetDatabase.CreateAsset(sprite,asset);else{EditorUtility.CopySerialized(sprite,old);Object.DestroyImmediate(sprite);sprite=old;}result.sprites[i]=sprite;result.seconds[i]=Mathf.Max(.02f,f.duration/1000f);}
            return result;
        }
    }
}
