using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DuckovCustomModel.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Managers
{
    public static class AssetBundleManager
    {
        private static readonly Dictionary<string, AssetBundle> LoadedBundles = [];

        public static AssetBundle? GetOrLoadAssetBundle(ModelBundleInfo bundleInfo, bool forceReload = false)
        {
            var bundlePath = Path.Combine(bundleInfo.DirectoryPath, bundleInfo.BundlePath);
            if (string.IsNullOrEmpty(bundlePath) || !File.Exists(bundlePath))
            {
                ModLogger.LogError($"AssetBundleManager: AssetBundle file not found at path: {bundlePath}");
                return null;
            }

            if (!forceReload && LoadedBundles.TryGetValue(bundlePath, out var existingBundle)) return existingBundle;

            try
            {
                var bundleData = File.ReadAllBytes(bundlePath);
                var assetBundle = AssetBundle.LoadFromMemory(bundleData);
                if (assetBundle == null)
                {
                    ModLogger.LogError($"AssetBundleManager: Failed to load AssetBundle from path: {bundlePath}");
                    return null;
                }

                if (LoadedBundles.TryGetValue(bundlePath, out var oldBundle))
                {
                    oldBundle.Unload(true);
                    LoadedBundles.Remove(bundlePath);
                }

                LoadedBundles[bundlePath] = assetBundle;
                return assetBundle;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"AssetBundleManager: Exception while loading AssetBundle from path: {bundlePath}. Exception: {ex}");
                return null;
            }
        }

        public static void UnloadAllAssetBundles(bool unloadAllLoadedObjects = false)
        {
            foreach (var bundle in LoadedBundles.Values) bundle.Unload(unloadAllLoadedObjects);
            LoadedBundles.Clear();
        }

        public static T? LoadAssetFromBundle<T>(ModelBundleInfo bundleInfo, string assetPath) where T : Object
        {
            var bundle = GetOrLoadAssetBundle(bundleInfo);
            if (bundle == null) return null;

            try
            {
                var asset = bundle.LoadAsset<T>(assetPath);
                if (asset == null)
                    ModLogger.LogError(
                        $"AssetBundleManager: Failed to load asset '{assetPath}' from bundle '{bundleInfo.BundlePath}'");
                return asset;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"AssetBundleManager: Exception while loading asset '{assetPath}' from bundle '{bundleInfo.BundlePath}'. Exception: {ex}");
                return null;
            }
        }

        public static GameObject? LoadModelPrefab(ModelBundleInfo bundleInfo, ModelInfo modelInfo)
        {
            return LoadAssetFromBundle<GameObject>(bundleInfo, modelInfo.PrefabPath);
        }

        public static Texture2D? LoadThumbnailTexture(ModelBundleInfo bundleInfo, ModelInfo modelInfo)
        {
            if (string.IsNullOrEmpty(modelInfo.ThumbnailPath)) return null;

            try
            {
                if (Path.IsPathRooted(modelInfo.ThumbnailPath)) return LoadTextureFromFile(modelInfo.ThumbnailPath);

                var bundle = GetOrLoadAssetBundle(bundleInfo);
                if (bundle != null && CheckAssetExistsInBundle(bundle, modelInfo.ThumbnailPath))
                {
                    var thumbnailFromBundle = LoadAssetFromBundle<Texture2D>(bundleInfo, modelInfo.ThumbnailPath);
                    if (thumbnailFromBundle != null)
                        return thumbnailFromBundle;
                }

                var externalPath = Path.Combine(bundleInfo.DirectoryPath, modelInfo.ThumbnailPath);
                return File.Exists(externalPath) ? LoadTextureFromFile(externalPath) : null;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"AssetBundleManager: Exception while loading thumbnail '{modelInfo.ThumbnailPath}'. Exception: {ex}");
                return null;
            }
        }

        public static bool CheckPrefabExists(ModelBundleInfo bundleInfo, ModelInfo modelInfo)
        {
            if (string.IsNullOrEmpty(modelInfo.PrefabPath)) return false;

            var bundle = GetOrLoadAssetBundle(bundleInfo);
            return bundle != null && CheckAssetExistsInBundle(bundle, modelInfo.PrefabPath);
        }

        private static bool CheckAssetExistsInBundle(AssetBundle bundle, string assetPath)
        {
            var assetNames = bundle.GetAllAssetNames();
            return assetNames.Any(name => string.Equals(name, assetPath, StringComparison.OrdinalIgnoreCase));
        }

        private static Texture2D? LoadTextureFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                ModLogger.LogError($"AssetBundleManager: Thumbnail file not found: {filePath}");
                return null;
            }

            try
            {
                var fileData = File.ReadAllBytes(filePath);
                var texture = new Texture2D(2, 2);
                if (texture.LoadImage(fileData)) return texture;
                ModLogger.LogError($"AssetBundleManager: Failed to load image from file: {filePath}");
                Object.Destroy(texture);
                return null;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"AssetBundleManager: Exception while loading texture from file '{filePath}'. Exception: {ex}");
                return null;
            }
        }
    }
}