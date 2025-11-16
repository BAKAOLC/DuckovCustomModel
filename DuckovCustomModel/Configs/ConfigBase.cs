using System;
using System.IO;
using DuckovCustomModel.Core;
using DuckovCustomModel.Managers;
using Newtonsoft.Json;

namespace DuckovCustomModel.Configs
{
    public abstract class ConfigBase : IConfigBase
    {
        // ReSharper disable once MemberCanBeProtected.Global
        public abstract void LoadDefault();

        public abstract void CopyFrom(IConfigBase other);

        public virtual void LoadFromFile(string filePath, bool autoSaveOnLoad = true)
        {
            try
            {
                ConfigManager.CreateDirectoryIfNotExists();

                if (!File.Exists(filePath))
                {
                    ModLogger.LogWarning($"Config file '{filePath}' does not exist. Loading default config.");
                    LoadDefault();
                    if (autoSaveOnLoad) SaveToFile(filePath);
                    return;
                }

                var json = File.ReadAllText(filePath);
                JsonConvert.PopulateObject(json, this, JsonSettings.Default);
                if (Validate() && autoSaveOnLoad) SaveToFile(filePath);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to load config from file '{filePath}': {ex.Message}");
                LoadDefault();
                if (autoSaveOnLoad) SaveToFile(filePath);
            }
        }

        public virtual void SaveToFile(string filePath, bool withBackup = true)
        {
            try
            {
                ConfigManager.CreateDirectoryIfNotExists();

                if (withBackup && File.Exists(filePath)) ConfigManager.CreateBackupFile(filePath);

                var json = JsonConvert.SerializeObject(this, JsonSettings.Default);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to save config to file '{filePath}': {ex.Message}");
            }
        }

        public virtual object Clone()
        {
            var clone = (ConfigBase)Activator.CreateInstance(GetType());
            clone.CopyFrom(this);
            return clone;
        }

        public abstract bool Validate();
    }
}
