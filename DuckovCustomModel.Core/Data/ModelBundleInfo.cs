using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DuckovCustomModel.Core.Data
{
    public class ModelBundleInfo
    {
        public string BundleName { get; set; } = string.Empty;
        public string BundlePath { get; set; } = string.Empty;

        public ModelInfo[] Models { get; set; } = [];

        [JsonIgnore] public string DirectoryPath { get; internal set; } = string.Empty;

        public static ModelBundleInfo? LoadFromDirectory(string directoryPath,
            JsonSerializerSettings? jsonSettings = null)
        {
            var infoFilePath = Path.Combine(directoryPath, "bundleinfo.json");
            if (!File.Exists(infoFilePath)) return null;

            try
            {
                var json = File.ReadAllText(infoFilePath);
                var settings = jsonSettings ?? JsonSettings.Default;
                var info = JsonConvert.DeserializeObject<ModelBundleInfo>(json, settings);
                if (info == null) return info;
                info.DirectoryPath = directoryPath;
                info.Models = info.Models.Where(model => model.Validate()).ToArray();
                return info;
            }
            catch (JsonException ex)
            {
                ModLogger.LogError(
                    $"Failed to parse bundleinfo.json in '{directoryPath}': {ex.Message}");
                ModLogger.LogException(ex);
                return null;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"Failed to load bundleinfo.json from '{directoryPath}': {ex.Message}");
                ModLogger.LogException(ex);
                return null;
            }
        }

        public ModelBundleInfo CreateFilteredCopy(ModelInfo[] filteredModels)
        {
            var copy = new ModelBundleInfo
            {
                BundleName = BundleName,
                BundlePath = BundlePath,
                Models = filteredModels,
                DirectoryPath = DirectoryPath,
            };
            return copy;
        }
    }
}
