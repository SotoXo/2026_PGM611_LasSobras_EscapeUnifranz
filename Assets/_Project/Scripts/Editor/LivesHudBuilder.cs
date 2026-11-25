#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using EscapeUNIFRANZ.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class LivesHudBuilder
    {
        private const string Folder="Assets/_Project/Art/perdida_5_vidas/";
        [Serializable] private class Sheet { public Frame[] frames; }
        [Serializable] private class Frame { public int duration; }
        [MenuItem("Escape UNIFRANZ/UI/Instalar cinco vidas")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Instalar en Edit Mode.");
            const string path="Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            if(scene.isDirty)throw new InvalidOperationException("Guardar Bootstrap antes de instalar el HUD.");
            try
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"sprites.png");
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;
                importer.alphaIsTransparency=true;importer.maxTextureSize=2048;importer.SaveAndReimport();
                var ui=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)).Single(c=>c.name=="UIRoot").transform;
                var old=ui.Find("Vidas_InferiorDerecha");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                var obj=new GameObject("Vidas_InferiorDerecha",typeof(RectTransform),typeof(Image),typeof(CanvasGroup),typeof(LivesHudView));
                obj.transform.SetParent(ui,false);
                var rect=(RectTransform)obj.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);
                rect.sizeDelta=new Vector2(320,144);rect.anchoredPosition=new Vector2(-24,24);
                obj.GetComponent<Image>().raycastTarget=false;
                var hud=new SerializedObject(obj.GetComponent<LivesHudView>());
                hud.FindProperty("atlas").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"sprites.png");
                var durations=JsonUtility.FromJson<Sheet>(File.ReadAllText(Folder+"sprites.json")).frames;
                var values=hud.FindProperty("milliseconds");values.arraySize=durations.Length;
                for(int i=0;i<durations.Length;i++)values.GetArrayElementAtIndex(i).intValue=durations[i].duration;
                hud.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Debug.Log("LIVES_HUD_OK: 32 frames, bottom-right, five lives.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
#endif
