using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GruelTerraSplines
{
    sealed class NoiseTextureAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var changedTextures = new List<Texture2D>();

            CollectTextures(importedAssets, changedTextures);
            CollectTextures(movedAssets, changedTextures);

            bool invalidated = OperationApplierBase.InvalidateNoiseTextures(changedTextures, notifyListeners: false);

            if (deletedAssets != null)
            {
                for (int i = 0; i < deletedAssets.Length; i++)
                {
                    if (IsTexturePath(deletedAssets[i]))
                    {
                        invalidated |= OperationApplierBase.InvalidateAllNoiseTextures(notifyListeners: false);
                        break;
                    }
                }
            }

            if (!invalidated)
                return;

            EditorApplication.delayCall += NotifyNoiseTextureImportFinished;
        }

        static void CollectTextures(string[] assetPaths, List<Texture2D> textures)
        {
            if (assetPaths == null)
                return;

            for (int i = 0; i < assetPaths.Length; i++)
            {
                string assetPath = assetPaths[i];
                if (!IsTexturePath(assetPath))
                    continue;

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture != null)
                {
                    textures.Add(texture);
                }
            }
        }

        static bool IsTexturePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            var importer = AssetImporter.GetAtPath(assetPath);
            return importer is TextureImporter;
        }

        static void NotifyNoiseTextureImportFinished()
        {
            OperationApplierBase.NotifyNoiseTextureCacheInvalidated();
        }
    }
}