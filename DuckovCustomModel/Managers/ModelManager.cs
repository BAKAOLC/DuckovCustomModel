using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers
{
    public static class ModelManager
    {
        public static readonly List<ModelBundleInfo> ModelBundles = [];

        public static string ModelsDirectory => Path.Combine(ConfigManager.ConfigBaseDirectory, "Models");

        public static void UpdateModelBundles()
        {
            AssetBundleManager.UnloadAllAssetBundles(true);
            ModelBundles.Clear();

            if (!Directory.Exists(ModelsDirectory))
                Directory.CreateDirectory(ModelsDirectory);

            var modelBundleDirectories = Directory.GetDirectories(ModelsDirectory);
            foreach (var modelBundleDir in modelBundleDirectories)
            {
                var modelBundleInfo = ModelBundleInfo.LoadFromDirectory(modelBundleDir);
                if (modelBundleInfo != null)
                    ModelBundles.Add(modelBundleInfo);
            }

            CheckDuplicateModelIDs();
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

        public static ModelHandler? InitializeModelHandler(CharacterMainControl characterMainControl)
        {
            if (characterMainControl == null)
            {
                ModLogger.LogError("CharacterMainControl is null.");
                return null;
            }

            var modelHandler = characterMainControl.GetComponent<ModelHandler>();
            if (modelHandler == null)
                modelHandler = characterMainControl.gameObject.AddComponent<ModelHandler>();

            modelHandler.Initialize(characterMainControl);

            return modelHandler;
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