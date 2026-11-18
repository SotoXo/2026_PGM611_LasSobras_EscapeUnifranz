using UnityEditor;
using UnityEngine;

namespace EscapeUNIFRANZ.Editor
{
    /// <summary>Preserves the native pixels, transparency and atlas coordinates.</summary>
    public sealed class MobileControlTextureImporter : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/_Project/Resources/MobileControls/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder, System.StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            bool joystick = assetPath.EndsWith("joystick_base.png") ||
                assetPath.EndsWith("joystick_palanca.png");
            importer.textureType = joystick ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (joystick)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 64;
            }
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
        }

        [MenuItem("EscapeUNIFRANZ/Build/Import Mobile Control Art")]
        public static void ImportAll()
        {
            foreach (string id in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder.TrimEnd('/') }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(id), ImportAssetOptions.ForceUpdate);
        }
    }
}
