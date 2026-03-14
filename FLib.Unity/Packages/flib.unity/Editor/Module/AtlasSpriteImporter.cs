using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FLib.Unity.Editor
{
    public class AtlasSpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var importer = assetImporter as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.Sprite)
                return;
            var dir = Path.GetDirectoryName(importer.assetPath)!;
            if (!dir.EndsWith("AtlasTiles") && !Path.GetDirectoryName(dir)!.EndsWith("Atlas"))
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
#if UNITY_PHYSICS_2D
            importer.spriteGenerateFallbackPhysicsShape = false;
#endif
            var settings = importer.GetDefaultPlatformTextureSettings();
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.maxTextureSize = 400;
            importer.SetPlatformTextureSettings(settings);
        }
    }
}
