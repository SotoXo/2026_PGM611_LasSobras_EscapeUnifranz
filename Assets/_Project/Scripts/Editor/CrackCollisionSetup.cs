using System.Linq;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EscapeUNIFRANZ.EditorTools
{
    public static class CrackCollisionSetup
    {
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Zones/Z08_Piso3Aulas.unity",OpenSceneMode.Single);
            var effect=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<CrabRoomRupture>()).Single();
            var old=effect.transform.Find("Huecos_Grandes_Colision");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("Huecos_Grandes_Colision").transform;root.SetParent(effect.transform,false);
            effect.middleHoles=Make(root,"Intermedia",new[]{new Rect(59,61,22,14),new Rect(130,135,18,9)});
            effect.finalHoles=Make(root,"Final",new[]{new Rect(54,61,30,15),new Rect(126,124,37,23),new Rect(70,206,31,15),new Rect(162,215,34,23)});
            EditorUtility.SetDirty(effect);EditorSceneManager.SaveScene(scene);Debug.Log("CRACK_COLLISIONS_OK: 15/30/45 hits, two intermediate holes and four final holes.");
        }
        static BoxCollider2D[] Make(Transform root,string name,Rect[] shapes)
        {
            var colliders=new BoxCollider2D[shapes.Length];
            for(int i=0;i<shapes.Length;i++)
            {
                var obj=new GameObject(name+"_"+i);obj.transform.SetParent(root,false);var rect=shapes[i];
                obj.transform.localPosition=new Vector3(rect.center.x/32,(288-rect.center.y)/32);
                var box=obj.AddComponent<BoxCollider2D>();box.size=rect.size/32;box.enabled=false;colliders[i]=box;
            }
            return colliders;
        }
    }
}
