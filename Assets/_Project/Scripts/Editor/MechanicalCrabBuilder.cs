using System;
using System.IO;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEngine;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class MechanicalCrabBuilder
    {
        const string Root="Assets/_Project/Art/Characters/Enemies/Cangrejo/cangrejo_acciones/";
        [Serializable] class Sheet{public Frame[] frames;}
        [Serializable] class Frame{public Rectangle frame;public int duration;}
        [Serializable] class Rectangle{public int x,y,w,h;}
        public static void Place(ZoneContext zone,Material material)
        {
            var obj=new GameObject("Cangrejo_Mecanico_Aula_Vacia");obj.transform.SetParent(zone.transform,false);obj.transform.position=new Vector2(33.5f,11.3f);
            var body=obj.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;
            var collider=obj.AddComponent<BoxCollider2D>();collider.size=new Vector2(.85f,.45f);collider.offset=new Vector2(0,.18f);
            var encounter=obj.AddComponent<EncounterContext>();Aula2MapBuilder.Set(encounter,"checkpointSpawnId","from_elevator");
            var renderer=obj.AddComponent<SpriteRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerName="World";
            var crab=obj.AddComponent<MechanicalCrabController>();crab.visual=renderer;crab.encounter=encounter;crab.room=new Rect(31,7.8f,5.4f,7.2f);
            crab.directions=new CrabDirectionFrames[4];string[] names={"abajo","izquierda","derecha","arriba"};
            for(int d=0;d<4;d++)
            {
                string folder=Root+names[d]+"/";string path=folder+"sprites.png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var sheet=JsonUtility.FromJson<Sheet>(File.ReadAllText(folder+"sprites.json"));
                if(sheet.frames.Length!=44)throw new InvalidOperationException("Expected 44 crab frames in "+folder);
                var frames=new CrabDirectionFrames{frames=new Sprite[44],seconds=new float[44]};
                for(int i=0;i<44;i++)
                {
                    var f=sheet.frames[i];var sprite=Sprite.Create(texture,new Rect(f.frame.x,texture.height-f.frame.y-f.frame.h,f.frame.w,f.frame.h),new Vector2(.5f,.2f),64);
                    string asset=folder+"GameFrame_"+i+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(asset);
                    if(existing==null)AssetDatabase.CreateAsset(sprite,asset);else{EditorUtility.CopySerialized(sprite,existing);UnityEngine.Object.DestroyImmediate(sprite);sprite=existing;}
                    frames.frames[i]=sprite;frames.seconds[i]=Mathf.Max(.02f,f.duration/1000f);
                }
                crab.directions[d]=frames;
            }
            renderer.sprite=crab.directions[0].frames[0];renderer.sortingOrder=WorldYSort2D.Order(obj.transform.position.y);
            Aula2MapBuilder.ValidateWalkable(null,new Vector2(17.3f,8.15f),obj.transform.position);
            EditorUtility.SetDirty(crab);
        }
    }
}
