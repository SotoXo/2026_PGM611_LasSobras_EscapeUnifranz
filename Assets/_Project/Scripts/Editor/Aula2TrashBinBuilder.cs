#if UNITY_EDITOR
using System;
using System.Linq;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class Aula2TrashBinBuilder
    {
        private const string Art="Assets/_Project/Art/basurero/6_latas/";
        public static void PlaceInAula2()
        {
            var frames=new Sprite[7];
            for(int i=0;i<7;i++)
            {
                string path=Art+"basurero_"+i+"_latas.png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=64;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.isReadable=true;
                importer.alphaIsTransparency=true;importer.SaveAndReimport();
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
                int feet=texture.height-1;
                for(int p=0;p<pixels.Length;p++)if(pixels[p].a>128)feet=Math.Min(feet,p/texture.width);
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,feet/(float)texture.height);
                importer.SetTextureSettings(settings);importer.SaveAndReimport();
                frames[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            var scene=SceneManager.GetSceneByPath("Assets/_Project/Scenes/Zones/Z07_Aula2.unity");
            var zone=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ZoneContext>()).Single();
            var old=zone.transform.Find("Basureros_LatasVacias");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("Basureros_LatasVacias").transform;root.SetParent(zone.transform,false);
            Vector2[] desired={new Vector2(8,10.7f),new Vector2(18,10.7f),new Vector2(26,10.7f)};
            var placed=new System.Collections.Generic.List<Vector2>();
            foreach(var machine in zone.GetComponentsInChildren<VendingMachineInteractable>())placed.Add(machine.transform.position);
            for(int i=0;i<3;i++)
            {
                Vector2 point=Aula2MapBuilder.FindBinPosition(desired[i],placed);
                if(placed.Any(p=>Vector2.Distance(p,point)<1f))throw new InvalidOperationException("Basureros y expendedoras demasiado cerca.");
                placed.Add(point);
                var obj=new GameObject("Basurero_"+(i+1)+"_6Latas");obj.transform.SetParent(root,false);obj.transform.position=point;
                var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=frames[6];
                renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Levels/GroupWorldLit.mat");
                renderer.sortingLayerName="World";renderer.sortingOrder=WorldYSort2D.Order(point.y);obj.AddComponent<WorldYSort2D>();
                var solid=new GameObject("Base");solid.transform.SetParent(obj.transform,false);
                var box=solid.AddComponent<BoxCollider2D>();box.size=new Vector2(.44f,.22f);box.offset=new Vector2(0,.11f);
                var trigger=obj.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(1.2f,.9f);
                var bin=obj.AddComponent<TrashBinInteractable>();var data=new SerializedObject(bin);
                data.FindProperty("binId").stringValue="aula2_basurero_"+(i+1);
                data.FindProperty("body").objectReferenceValue=renderer;var sprites=data.FindProperty("remainingFrames");sprites.arraySize=7;
                for(int f=0;f<7;f++)sprites.GetArrayElementAtIndex(f).objectReferenceValue=frames[f];
                data.ApplyModifiedPropertiesWithoutUndo();
                var marker=obj.AddComponent<MinimapTrackable2D>();marker.displayName="Basurero · latas vacías";
                var markerData=new SerializedObject(marker);markerData.FindProperty("markerId").stringValue="basurero_"+(i+1);markerData.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("AULA2_TRASH_BINS_OK: three bins, six empty cans each, accessible wall locations.");
        }
    }
}
#endif
