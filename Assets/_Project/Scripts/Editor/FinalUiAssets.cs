using System.IO;
using EscapeUNIFRANZ.Narrative;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace EscapeUNIFRANZ.EditorTools
{
    public static class FinalUiAssets
    {
        private const string Ui = "Assets/Resources/UI/";
        private const string EmptyCan = "Assets/_Project/Art/Items/lata_vacia.png";

        public static void Install()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
                AssetDatabase.CreateFolder("Assets/Resources", "UI");
            ImportFont();
            DrawFrame();
            DrawCan();
            var item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Narrative/Items/empty_can.asset");
            item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(EmptyCan);
            item.description = "Lánzala para dañar al robot. Se consume al acertar. Si falla, recógela del suelo.";
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
        }

        private static void ImportFont()
        {
            string path = Ui + "PixelifySans.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null) return;
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Art/UI/Fonts/PixelifySans-Regular.ttf");
            var font = TMP_FontAsset.CreateFontAsset(source, 54, 6, GlyphRenderMode.SDFAA, 512, 512,
                AtlasPopulationMode.Dynamic, false);
            font.name = "Pixelify Sans";
            const string latin = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
                "ÁÉÍÓÚÜÑáéíóúüñ¿¡·×«»—–…º°";
            font.TryAddCharacters(latin);
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, path);
            font.material.name = "Pixelify Sans SDF";
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures)
            {
                atlas.name = "Pixelify Sans atlas";
                AssetDatabase.AddObjectToAsset(atlas, font);
            }
            EditorUtility.SetDirty(font);
        }

        private static void DrawFrame()
        {
            WriteSprite(Ui + "PixelFrame.png", 24, 24, 100, Vector4.one * 6f, (x, y) =>
            {
                int nearX = Mathf.Min(x, 23 - x), nearY = Mathf.Min(y, 23 - y);
                if (nearX + nearY < 3) return Color.clear;
                if (Mathf.Min(nearX, nearY) == 0) return new Color32(49, 25, 34, 255);
                if (Mathf.Min(nearX, nearY) <= 2) return new Color32(128, 55, 66, 255);
                if (Mathf.Min(nearX, nearY) == 3) return new Color32(59, 35, 45, 255);
                return new Color32(19, 17, 25, 249);
            });
        }

        private static void DrawCan()
        {
            WriteSprite(EmptyCan, 16, 20, 64, Vector4.zero, (x, y) =>
            {
                int edge = y == 2 || y == 17 ? 4 : 3;
                if (y < 2 || y > 17 || x < edge || x >= 16 - edge) return Color.clear;
                if (x == edge || x == 15 - edge || y == 2 || y == 17) return new Color32(39, 44, 57, 255);
                if (y >= 14)
                {
                    if (y == 15 && x >= 7 && x <= 10) return new Color32(48, 58, 64, 255);
                    return new Color32(211, 222, 214, 255);
                }
                if (x + y == 16 || (x == 6 && y > 7)) return new Color32(207, 215, 203, 255);
                if (x >= 10 || y < 5) return new Color32(77, 98, 110, 255);
                return new Color32(132, 154, 158, 255);
            });
        }

        private static void WriteSprite(string path, int width, int height, int ppu, Vector4 border,
            System.Func<int, int, Color> pixel)
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null) return;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    texture.SetPixel(x, y, pixel(x, y));
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.spriteBorder = border;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
