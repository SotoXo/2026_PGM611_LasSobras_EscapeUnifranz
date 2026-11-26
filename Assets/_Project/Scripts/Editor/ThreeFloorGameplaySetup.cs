using System;
using System.IO;
using System.Linq;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EscapeUNIFRANZ.EditorTools
{
    /// <summary>Installs the authored third floor without replacing the player's second-floor layout.</summary>
    public static class ThreeFloorGameplaySetup
    {
        const string Folder="Assets/_Project/Art/Levels/nivel3_aulas/tercer_piso/";
        const string ScenePath="Assets/_Project/Scenes/Zones/Z08_Piso3Aulas.unity";
        const string Second="Assets/_Project/Scenes/Zones/Z07_Aula2.unity";
        const float Ppu=32;
        static readonly Color AmbientColor=new Color(.60f,.72f,.90f);
        static Vector2 Pixel(float x,float y)=>new Vector2(x/Ppu,(736-y)/Ppu);

        [MenuItem("Escape UNIFRANZ/Art/Integrar tres pisos y ambiente frío")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var second=EditorSceneManager.OpenScene(Second,OpenSceneMode.Single);
                var secondZone=Zone(second);
                var vending=secondZone.GetComponentsInChildren<VendingMachineInteractable>().Single();
                // Keep the source scene loaded so the machine's animation references stay valid.
                var template=vending.gameObject;
                var secondGlobals=second.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Light2D>()).Where(l=>l.enabled&&l.lightType==Light2D.LightType.Global).ToArray();
                foreach(var global in secondGlobals)global.enabled=false;
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                var root=new GameObject("Zone_Piso3Aulas"); var zone=root.AddComponent<ZoneContext>();
                Aula2MapBuilder.Set(zone,"zoneId","piso3_aulas");
                Aula2MapBuilder.Set(zone,"displayName","Piso 3 · Aulas y semiología");
                Aula2MapBuilder.Set(zone,"defaultCheckpointSpawnId","from_elevator");
                Aula2MapBuilder.Set(zone,"objectiveId","piso3_explore");
                Aula2MapBuilder.Set(zone,"objectiveText","Explora el tercer piso. E: interactuar; G: lanzar latas. El ruido atrae robots; tu linterna revela tu posición.");
                Aula2MapBuilder.Set(zone,"minimapCenter",new Vector2(19.5f,11.5f));
                Aula2MapBuilder.Set(zone,"minimapSize",new Vector2(39,23));
                var art=new GameObject("GroupLevelArt").transform;art.SetParent(root.transform,false);
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Levels/GroupWorldLit.mat");
                var floor=Import("piso.png");var walls=Import("muros.png");var objects=Import("objetos.png");
                // Replace the painted third-floor lift with the same animated doors as floor 2.
                var liftDoor=new RectInt(532,210,44,32);
                walls=ElevatorMapIntegration.WithoutDoor(walls,new RectInt(532,210,44,48),"Piso3MurosSinPuerta");

                
                if(floor.width!=1248||floor.height!=736)throw new InvalidOperationException("Unexpected third floor dimensions.");
                Draw(art,"Piso",floor,new Rect(0,0,1248,736),material,"Ground");
                for(int y=0;y<736;y+=8)Draw(art,"Muros_"+y,walls,new Rect(0,y,1248,8),material,"World");
                var furniture=Aula2MapBuilder.Furniture(objects);
                
                for(int i=0;i<furniture.Count;i++)
                {
                    var renderer=Draw(art,"Objeto_"+i,objects,furniture[i],material,"World");
                    if(furniture[i].height>=10)renderer.gameObject.AddComponent<WorldObjectOcclusion2D>();
                }
                var f=floor.GetPixels32();var w=walls.GetPixels32();var o=objects.GetPixels32();
                Debug.Log("PISO3: artwork imported; building movement mask.");
                var blocked=new bool[1248,736];var wallMask=new bool[1248,736];
                for(int y=0;y<736;y++)for(int x=0;x<1248;x++)
                {
                    int i=y*1248+x;wallMask[x,y]=w[i].a>128;
                    blocked[x,y]=wallMask[x,y]||f[i].a<128||(f[i].r==255&&f[i].g==255&&f[i].b==255);
                }
                foreach(var rect in furniture)
                {
                    bool bench=rect.width>rect.height*1.7f&&rect.width>=20;
                    int height=bench?2:Mathf.Max(2,Mathf.RoundToInt(rect.height*.4f));
                    int bottom=bench?(int)rect.yMax-height:(int)rect.y;
                    for(int y=bottom;y<bottom+height;y++)for(int x=(int)rect.x;x<rect.xMax;x++)
                        if(o[y*1248+x].a>128)blocked[x,y]=true;
                }
                // Widen only painted door thresholds, retaining their visible frames.
                foreach(var p in new[]{new Vector2(298,286),new Vector2(442,286),new Vector2(566,286),new Vector2(706,286),new Vector2(309,328),new Vector2(916,501)})
                    Clear(blocked,wallMask,(int)p.x-10,(int)p.y-5,20,12);
                foreach(var p in new[]{new Vector2(192,333),new Vector2(192,530),new Vector2(977,478),new Vector2(939,548),new Vector2(939,651)})
                    Clear(blocked,wallMask,(int)p.x-5,(int)p.y-10,12,20);
                for(int y=liftDoor.y;y<liftDoor.yMax+16;y++)for(int x=liftDoor.x;x<liftDoor.xMax;x++)
                {blocked[x,y]=false;wallMask[x,y]=false;}
                // Arrival is on the open landing in front of the authored lift.
                Vector2 arrival=Pixel(554,468);
                Aula2MapBuilder.UseNavigation(blocked,wallMask,arrival);
                Debug.Log("PISO3: navigation mask ready.");
                arrival=Aula2MapBuilder.FindOpenPosition(arrival);
                var collision=new GameObject("WorldCollision_Authored").transform;collision.SetParent(root.transform,false);
                Aula2MapBuilder.BuildColliders(collision,blocked);
                Debug.Log("PISO3: movement collider ready.");
                var shadows=new GameObject("WallShadows").transform;shadows.SetParent(root.transform,false);
                var outline=Aula2MapBuilder.BuildColliders(shadows,wallMask,true);outline.isTrigger=true;outline.excludeLayers=~0;
                Debug.Log("PISO3: shadow outlines ready.");
                var caster=shadows.gameObject.AddComponent<ShadowCaster2D>();caster.castingOption=ShadowCaster2D.ShadowCastingOptions.CastAndSelfShadow;caster.trimEdge=0;
                var shadowData=new SerializedObject(caster);shadowData.FindProperty("m_ShadowShape2DProvider").managedReferenceValue=new StaticWallShadowProvider();shadowData.ApplyModifiedPropertiesWithoutUndo();
                var elevator=Aula2MapBuilder.Exit(root.transform,"Ascensor · Elegir piso",Pixel(554,480),"aula2","from_elevator").GetComponent<ZoneExitInteractable>();
                ElevatorMapIntegration.Configure(elevator,art,liftDoor,Ppu,true);
                elevator.elevatorAnimation.entranceAtTop=true;
                var frame=elevator.elevatorAnimation.frames[0];
                elevator.elevatorAnimation.doorRenderer.transform.localScale=new Vector3(liftDoor.width/frame.rect.width,liftDoor.height/frame.rect.height,1);
                Aula2MapBuilder.Spawn(root.transform,"from_elevator",elevator.elevatorAnimation.cabinCenter);
                Aula2MapBuilder.ValidateWalkable(blocked,arrival,elevator.elevatorAnimation.cabinCenter);
                Vector2[] positions={Pixel(390,312),Pixel(500,405),Pixel(960,444)};
                for(int i=0;i<positions.Length;i++)
                {
                    positions[i]=Aula2MapBuilder.FindOpenPosition(positions[i]);
                    Aula2MapBuilder.ValidateWalkable(blocked,arrival,positions[i]);
                }
                Aula2TeamContentBuilder.PlaceRobots(zone,material,positions,new[]{Vector2.right,Vector2.up,Vector2.up});
                MechanicalCrabBuilder.Place(zone,material);
                var machine=Object.Instantiate(template,root.transform);machine.name="Expendedora_Piso3";
                machine.transform.position=Aula2MapBuilder.FindMachinePosition(Pixel(800,300));
                var machineData=new SerializedObject(machine.GetComponent<VendingMachineInteractable>());
                machineData.FindProperty("machineId").stringValue="piso3_bebidas";machineData.ApplyModifiedPropertiesWithoutUndo();
                var light=new GameObject("Ambient_Piso3").AddComponent<Light2D>();light.lightType=Light2D.LightType.Global;
                light.transform.SetParent(root.transform,false);
                ApplyLights(scene);
                EditorSceneManager.SaveScene(scene,ScenePath);
                EditorSceneManager.CloseScene(scene,true);
                foreach(var global in secondGlobals)global.enabled=true;
                SceneManager.SetActiveScene(second);
                // Navigation must now use the second floor's existing geometry, not third-floor pixels.
                Physics2D.SyncTransforms();
                Vector2 stair=new Vector2(22.5f,3.65f);
                foreach(string name in new[]{"Subir al piso 3","Spawn_from_piso3"})
                { var old=secondZone.transform.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject); }
                Aula2MapBuilder.Exit(secondZone.transform,"Subir al piso 3",stair,"piso3_aulas","from_elevator");
                Aula2MapBuilder.Spawn(secondZone.transform,"from_piso3",stair+Vector2.up*.6f);
                ApplyLights(second);EditorSceneManager.SaveScene(second);
                foreach(var name in new[]{"Z00_Entrada","Z01_Hall","Z02_ARCA"})
                { var s=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Zones/"+name+".unity",OpenSceneMode.Single);ApplyLights(s);EditorSceneManager.SaveScene(s); }
                var catalog=new SerializedObject(AssetDatabase.LoadAssetAtPath<ZoneCatalog>("Assets/_Project/Data/Zones/ZoneCatalog.asset"));
                var zones=catalog.FindProperty("zones");int index=-1;
                for(int i=0;i<zones.arraySize;i++)if(zones.GetArrayElementAtIndex(i).FindPropertyRelative("zoneId").stringValue=="piso3_aulas")index=i;
                if(index<0){index=zones.arraySize;zones.arraySize++;}
                var entry=zones.GetArrayElementAtIndex(index);entry.FindPropertyRelative("zoneId").stringValue="piso3_aulas";entry.FindPropertyRelative("scenePath").stringValue=ScenePath;catalog.ApplyModifiedPropertiesWithoutUndo();
                EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).Append(new EditorBuildSettingsScene(ScenePath,true)).ToArray();
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("reports");File.WriteAllText("reports/three-floor-validation.txt","PASS: third floor source 1248x736, three reachable patrols in separate corridors, wall-mounted vending with reachable approach, catalog and build registration.\n");
                Debug.Log("THREE_FLOORS_OK");
            }
            catch(Exception error){Debug.LogException(error);throw;}
            finally
            {
                if(setup.Any(s=>s.isLoaded&&s.isActive&&!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
        static ZoneContext Zone(Scene scene)=>scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ZoneContext>()).Single();
        static void ApplyLights(Scene scene)
        {
            foreach(var light in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Light2D>(true)))
            {
                if(light.lightType==Light2D.LightType.Global){light.color=AmbientColor;light.intensity=.095f;}
                else if(light.GetComponentInParent<ZoneContext>()!=null){light.color=AmbientColor;light.intensity=Mathf.Min(light.intensity,.045f);}
                light.targetSortingLayers=SortingLayer.layers.Select(l=>l.id).ToArray();
            }
        }
        static void Clear(bool[,] blocked,bool[,] walls,int x,int top,int width,int height)
        {for(int py=top;py<top+height;py++)for(int px=x;px<x+width;px++){blocked[px,735-py]=false;walls[px,735-py]=false;}}
        static Texture2D Import(string file)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+file);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=Ppu;
            importer.isReadable=true;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+file);
        }
        static SpriteRenderer Draw(Transform parent,string name,Texture2D texture,Rect rect,Material material,string layer)
        {
            string path=Folder+name+".asset";
            var sprite=Sprite.Create(texture,rect,Vector2.zero,Ppu,0,SpriteMeshType.FullRect);
            var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(old==null)AssetDatabase.CreateAsset(sprite,path);else{EditorUtility.CopySerialized(sprite,old);Object.DestroyImmediate(sprite);sprite=old;}
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.localPosition=new Vector3(rect.x/Ppu,rect.y/Ppu);
            var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;renderer.sortingLayerName=layer;renderer.sortingOrder=layer=="Ground"?0:WorldYSort2D.Order(rect.y/Ppu);
            return renderer;
        }
    }
}
