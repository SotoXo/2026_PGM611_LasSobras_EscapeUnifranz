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
            var root=new GameObject("Robots_Limber_Patrulla").transform;root.SetParent(zone.transform,false);
            var encounter=root.gameObject.AddComponent<EscapeUNIFRANZ.Encounters.EncounterContext>();
            var encounterData=new SerializedObject(encounter);
            encounterData.FindProperty("checkpointSpawnId").stringValue="from_elevator";
            encounterData.ApplyModifiedPropertiesWithoutUndo();
            string[] styles={"garra","rampa","sierra"};
            Vector2[] designated={new Vector2(5.17f,6.065f),new Vector2(18.40625f,9.34375f),new Vector2(24.04f,8.015f)};
            for(int i=0;i<styles.Length;i++)
            {
                Vector2 center=Aula2MapBuilder.FindOpenPosition(designated[i]);
                var route=new GameObject("Ruta_"+styles[i]);route.transform.SetParent(root,false);
                var path=route.AddComponent<PatrolPath2D>();var pathData=new SerializedObject(path);var points=pathData.FindProperty("points");points.arraySize=2;
                Vector2 axis=i==1?Vector2.right:Vector2.up;
                Vector2[] positions={Aula2MapBuilder.PatrolEndAlong(center,-axis),Aula2MapBuilder.PatrolEndAlong(center,axis)};
                for(int p=0;p<2;p++)
                {
                    var waypoint=new GameObject("Punto_"+p).transform;waypoint.SetParent(route.transform,false);waypoint.position=positions[p];
                    points.GetArrayElementAtIndex(p).objectReferenceValue=waypoint;
                }
                pathData.FindProperty("pingPong").boolValue=true;pathData.ApplyModifiedPropertiesWithoutUndo();
                var obj=new GameObject("Robot_Limber_"+styles[i]);obj.SetActive(false);obj.transform.SetParent(root,false);obj.transform.position=center;
                var body=obj.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;
                var solid=obj.AddComponent<BoxCollider2D>();solid.size=new Vector2(.32f,.22f);solid.offset=new Vector2(0,.12f);
                var attack=new GameObject("ContactoAtaque");attack.transform.SetParent(obj.transform,false);
                var hitbox=attack.AddComponent<BoxCollider2D>();hitbox.isTrigger=true;hitbox.size=new Vector2(.44f,.34f);hitbox.offset=new Vector2(0,.12f);
                var contact=attack.AddComponent<HazardContact2D>();var damage=new SerializedObject(contact);
                damage.FindProperty("encounter").objectReferenceValue=encounter;
                damage.FindProperty("defeatReason").stringValue="Te alcanzó el robot "+styles[i]+". Lanza latas con G para aturdirlo; tres impactos lo desactivan.";
                damage.ApplyModifiedPropertiesWithoutUndo();
                var mover=obj.AddComponent<PatrolMover2D>();var movement=new SerializedObject(mover);
                movement.FindProperty("path").objectReferenceValue=path;movement.FindProperty("patrolSpeed").floatValue=.65f+i*.1f;movement.ApplyModifiedPropertiesWithoutUndo();
                var detection=new GameObject("Sensor");detection.transform.SetParent(obj.transform,false);
                var trigger=detection.AddComponent<CircleCollider2D>();trigger.isTrigger=true;trigger.radius=8f;
                var sensor=detection.AddComponent<DetectionSensor2D>();
                var sensing=new SerializedObject(sensor);sensing.FindProperty("directional").boolValue=true;sensing.FindProperty("fieldOfView").floatValue=100;
                sensing.FindProperty("reactToPlayerGaze").boolValue=true;sensing.ApplyModifiedPropertiesWithoutUndo();
                var robot=obj.AddComponent<MiniAutoController>();var data=new SerializedObject(robot);
                data.FindProperty("mover").objectReferenceValue=mover;data.FindProperty("sensor").objectReferenceValue=sensor;data.ApplyModifiedPropertiesWithoutUndo();
                data.Update();data.FindProperty("maxHits").intValue=3;data.FindProperty("chaseSpeed").floatValue=1.6f+i*.15f;data.ApplyModifiedPropertiesWithoutUndo();
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
                var status=new GameObject("Estado");status.transform.SetParent(obj.transform,false);status.transform.localPosition=Vector3.up*1.15f;
                var statusText=status.AddComponent<TextMeshPro>();statusText.fontSize=2f;statusText.alignment=TextAlignmentOptions.Center;statusText.rectTransform.sizeDelta=new Vector2(1f,.4f);
                statusText.GetComponent<MeshRenderer>().sortingLayerName="Effects";
                var feedback=obj.AddComponent<ThreatFeedback2D>();feedback.marker=statusText;feedback.bodyRenderers=new[]{visual};robot.feedback=feedback;
                var artData=new SerializedObject(robot);artData.FindProperty("wear").enumValueIndex=0;artData.ApplyModifiedPropertiesWithoutUndo();
                obj.SetActive(true);
            }
        }
    }
}
#endif
