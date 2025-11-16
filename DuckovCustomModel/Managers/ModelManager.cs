using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using DuckovCustomModel.Core;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Managers
{
    public static class ModelManager
    {
        public static readonly List<ModelBundleInfo> ModelBundles = [];

        private static readonly Dictionary<string, BundleHashInfo> BundleHashCache = [];

        public static string ModelsDirectory => Path.Combine(ConfigManager.ConfigBaseDirectory, "Models");

        public static HashSet<string> UpdateModelBundles()
        {
            if (!Directory.Exists(ModelsDirectory))
                Directory.CreateDirectory(ModelsDirectory);

            var modelBundleDirectories = Directory.GetDirectories(ModelsDirectory);
            var bundlesToUnload = new HashSet<string>();
            var bundlesToReload = new HashSet<string>();

            foreach (var modelBundleDir in modelBundleDirectories)
                try
                {
                    var infoFilePath = Path.Combine(modelBundleDir, "bundleinfo.json");
                    if (!File.Exists(infoFilePath)) continue;

                    var bundleInfo = ModelBundleInfo.LoadFromDirectory(modelBundleDir, JsonSettings.Default);
                    if (bundleInfo == null) continue;

                    var bundlePath = Path.Combine(bundleInfo.DirectoryPath, bundleInfo.BundlePath);
                    if (!File.Exists(bundlePath)) continue;

                    var configContent = File.ReadAllText(infoFilePath);
                    var configHash = BundleHashInfo.CalculateStringHash(configContent);
                    var bundleHash = BundleHashInfo.CalculateFileHash(bundlePath);

                    var bundleKey = Path.GetFileName(modelBundleDir);
                    if (string.IsNullOrEmpty(bundleKey))
                        bundleKey = modelBundleDir;

                    var needsReload = false;

                    if (BundleHashCache.TryGetValue(bundleKey, out var cachedHash))
                    {
                        if (cachedHash.ConfigHash != configHash || cachedHash.BundleHash != bundleHash)
                        {
                            needsReload = true;
                            ModLogger.Log($"Bundle '{bundleInfo.BundleName}' changed, will reload");
                        }
                    }
                    else
                    {
                        needsReload = true;
                        ModLogger.Log($"New bundle '{bundleInfo.BundleName}' detected, will load");
                    }

                    if (needsReload)
                    {
                        bundlesToReload.Add(bundleInfo.BundleName);
                        bundlesToUnload.Add(bundleInfo.BundleName);
                    }

                    var hashInfo = new BundleHashInfo
                    {
                        BundleName = bundleInfo.BundleName,
                        BundlePath = bundleInfo.BundlePath,
                        BundleHash = bundleHash,
                        ConfigHash = configHash,
                        LastModified = File.GetLastWriteTime(bundlePath),
                    };
                    BundleHashCache[bundleKey] = hashInfo;
                }
                catch (Exception ex)
                {
                    ModLogger.LogError($"Error processing bundle directory '{modelBundleDir}': {ex.Message}");
                    ModLogger.LogException(ex);
                }

            var existingBundles = ModelBundles.ToList();
            foreach (var existingBundle in existingBundles)
            {
                var bundleDir = existingBundle.DirectoryPath;
                if (!string.IsNullOrEmpty(bundleDir) && Directory.Exists(bundleDir)) continue;
                var bundleKey = Path.GetFileName(bundleDir);
                if (string.IsNullOrEmpty(bundleKey))
                    bundleKey = bundleDir;

                bundlesToUnload.Add(existingBundle.BundleName);
                ModelBundles.Remove(existingBundle);
                BundleHashCache.Remove(bundleKey);
                ModLogger.Log($"Bundle '{existingBundle.BundleName}' removed");
            }

            foreach (var bundleKey in bundlesToUnload)
            {
                var bundleToUnload = ModelBundles.FirstOrDefault(b => b.BundleName == bundleKey);
                if (bundleToUnload == null) continue;
                foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
                {
                    var handlers = GetAllModelHandlers(target);
                    foreach (var handler in handlers)
                    {
                        if (!handler.IsHiddenOriginalModel) continue;
                        var modelID = ModEntry.UsingModel?.GetModelID(target) ?? string.Empty;
                        if (string.IsNullOrEmpty(modelID)) continue;
                        if (!FindModelByID(modelID, out var currentBundleInfo, out _)) continue;
                        if (currentBundleInfo.BundleName == bundleKey)
                            handler.CleanupCustomModel();
                    }
                }

                var bundlePath = Path.Combine(bundleToUnload.DirectoryPath, bundleToUnload.BundlePath);
                AssetBundleManager.UnloadAssetBundle(bundlePath);
            }

            foreach (var modelBundleDir in modelBundleDirectories)
                try
                {
                    var bundleInfo = ModelBundleInfo.LoadFromDirectory(modelBundleDir, JsonSettings.Default);
                    if (bundleInfo == null) continue;

                    var bundleKey = bundleInfo.BundleName;
                    if (bundlesToReload.Contains(bundleKey))
                    {
                        var existingBundle = ModelBundles.FirstOrDefault(b => b.BundleName == bundleKey);
                        if (existingBundle != null)
                            ModelBundles.Remove(existingBundle);

                        foreach (var modelInfo in bundleInfo.Models)
                            modelInfo.BundleName = bundleKey;

                        ModelBundles.Add(bundleInfo);
                    }
                    else
                    {
                        var existingBundle = ModelBundles.FirstOrDefault(b => b.BundleName == bundleKey);
                        if (existingBundle == null)
                        {
                            foreach (var modelInfo in bundleInfo.Models)
                                modelInfo.BundleName = bundleKey;

                            ModelBundles.Add(bundleInfo);
                        }
                        else
                        {
                            foreach (var modelInfo in existingBundle.Models)
                                modelInfo.BundleName = bundleKey;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.LogError($"Error loading bundle from directory '{modelBundleDir}': {ex.Message}");
                    ModLogger.LogException(ex);
                }

            CheckDuplicateModelIDs();

            return bundlesToReload;
        }

        public static IReadOnlyDictionary<string, List<string>> GetDuplicateModelIDs()
        {
            var modelIdDictionary = new Dictionary<string, List<string>>();
            foreach (var modelBundle in ModelBundles)
            foreach (var modelInfo in modelBundle.Models)
            {
                if (!modelIdDictionary.ContainsKey(modelInfo.ModelID))
                    modelIdDictionary[modelInfo.ModelID] = [];
                modelIdDictionary[modelInfo.ModelID].Add(modelBundle.BundleName);
            }

            return modelIdDictionary.Where(kvp => kvp.Value.Count > 1)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static bool FindModelByID(string modelID,
            [NotNullWhen(true)] out ModelBundleInfo? foundModel,
            [NotNullWhen(true)] out ModelInfo? foundModelInfo)
        {
            foundModel = null;
            foundModelInfo = null;

            foreach (var modelBundle in ModelBundles)
            foreach (var modelInfo in modelBundle.Models)
                if (modelInfo.ModelID == modelID)
                {
                    foundModel = modelBundle;
                    foundModelInfo = modelInfo;
                    return true;
                }

            return false;
        }

        public static ModelHandler? InitializeModelHandler(CharacterMainControl characterMainControl,
            ModelTarget target = ModelTarget.Character)
        {
            if (characterMainControl == null)
            {
                ModLogger.LogError("CharacterMainControl is null.");
                return null;
            }

            var modelHandler = characterMainControl.GetComponent<ModelHandler>();
            if (modelHandler == null)
                modelHandler = characterMainControl.gameObject.AddComponent<ModelHandler>();

            modelHandler.Initialize(characterMainControl, target);

            return modelHandler;
        }

        public static List<ModelHandler> GetAllModelHandlers(ModelTarget target)
        {
            var handlers = new List<ModelHandler>();

            if (LevelManager.Instance == null) return handlers;

            var allHandlers = Object.FindObjectsByType<ModelHandler>(FindObjectsSortMode.None);
            handlers.AddRange(allHandlers.Where(handler => handler.Target == target && handler.IsInitialized));

            return handlers;
        }

        public static List<ModelHandler> GetAICharacterModelHandlers(string nameKey)
        {
            var handlers = new List<ModelHandler>();

            if (LevelManager.Instance == null) return handlers;
            if (string.IsNullOrEmpty(nameKey)) return handlers;

            var allHandlers = Object.FindObjectsByType<ModelHandler>(FindObjectsSortMode.None);
            foreach (var handler in allHandlers)
            {
                if (handler.Target != ModelTarget.AICharacter || !handler.IsInitialized) continue;
                if (handler.CharacterMainControl?.characterPreset?.nameKey != nameKey) continue;
                handlers.Add(handler);
            }

            return handlers;
        }


        private static void CheckDuplicateModelIDs()
        {
            var duplicates = GetDuplicateModelIDs();
            foreach (var (modelID, bundles) in duplicates)
            {
                var bundleNames = string.Join(", ", bundles);
                ModLogger.LogWarning($"Duplicate ModelID '{modelID}' found in bundles: {bundleNames}");
            }
        }
    }
}
