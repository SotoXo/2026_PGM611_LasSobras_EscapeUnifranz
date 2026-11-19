#if UNITY_EDITOR
using System;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class Aula2TeamContentBuilder
    {
        public static void PlaceRobots(ZoneContext zone, Material material)
        {
            var root=new GameObject("Robots_Limber_Prueba").transform;root.SetParent(zone.transform,false);
            string[] styles={"garra","rampa","sierra"};
            for(int i=0;i<styles.Length;i++)
            {
                Vector2 center=Aula2MapBuilder.FindOpenPosition(new Vector2((480+i*120)/32f,(576-276)/32f),true);
                var route=new GameObject("Ruta_"+styles[i]);route.transform.SetParent(root,false);
                var path=route.AddComponent<PatrolPath2D>();var pathData=new SerializedObject(path);var points=pathData.FindProperty("points");points.arraySize=4;
                Vector2[] offsets={new Vector2(-.3f,-.08f),new Vector2(.3f,-.08f),new Vector2(.3f,.08f),new Vector2(-.3f,.08f)};
                for(int p=0;p<4;p++)
                {
                    var waypoint=new GameObject("Punto_"+p).transform;waypoint.SetParent(route.transform,false);waypoint.position=center+offsets[p];
                    points.GetArrayElementAtIndex(p).objectReferenceValue=waypoint;
                }
                pathData.FindProperty("pingPong").boolValue=false;pathData.ApplyModifiedPropertiesWithoutUndo();
                var obj=new GameObject("Robot_Limber_"+styles[i]);obj.SetActive(false);obj.transform.SetParent(root,false);obj.transform.position=center;
                var body=obj.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;
                var mover=obj.AddComponent<PatrolMover2D>();var movement=new SerializedObject(mover);
                movement.FindProperty("path").objectReferenceValue=path;movement.FindProperty("patrolSpeed").floatValue=.55f;movement.ApplyModifiedPropertiesWithoutUndo();
                var detection=new GameObject("Sensor");detection.transform.SetParent(obj.transform,false);
                var trigger=detection.AddComponent<CircleCollider2D>();trigger.isTrigger=true;trigger.radius=.01f;
                var sensor=detection.AddComponent<DetectionSensor2D>();sensor.enabled=false;
                var robot=obj.AddComponent<MiniAutoController>();var data=new SerializedObject(robot);
                data.FindProperty("mover").objectReferenceValue=mover;data.FindProperty("sensor").objectReferenceValue=sensor;data.ApplyModifiedPropertiesWithoutUndo();
                CampaignReleaseSetup.ConfigureMiniAuto(robot,styles[i],i);
                var visual=obj.transform.Find("CarPixelArt").GetComponent<SpriteRenderer>();
                string asset="Assets/_Project/Art/Levels/nivel4_aulas/NuevoMapa/Generated/Robot_"+styles[i]+".asset";
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Characters/Enemies/MiniAutos/"+styles[i]+"_movimiento.png");
                var sprite=Sprite.Create(texture,new Rect(0,64,64,64),new Vector2(.5f,0),64,0,SpriteMeshType.FullRect);
                var existing=AssetDatabase.LoadAssetAtPath<Sprite>(asset);
                if(existing==null)AssetDatabase.CreateAsset(sprite,asset);
                else{EditorUtility.CopySerialized(sprite,existing);UnityEngine.Object.DestroyImmediate(sprite);sprite=existing;}
                visual.sprite=sprite;
                var marker=obj.AddComponent<MinimapTrackable2D>();marker.displayName="Robot de Limber · "+styles[i];
                var label=new GameObject("Nombre");label.transform.SetParent(obj.transform,false);label.transform.localPosition=Vector3.up*.8f;
                var text=label.AddComponent<TextMeshPro>();text.text="Limber · "+styles[i];text.fontSize=1.5f;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(2f,.4f);
                text.GetComponent<MeshRenderer>().sortingLayerName="Effects";
                obj.SetActive(true);
            }
        }
    }
}
#endif
