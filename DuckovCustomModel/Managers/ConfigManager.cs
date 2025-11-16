using System;
using System.IO;
using DuckovCustomModel.Configs;

namespace DuckovCustomModel.Managers
{
    public static class ConfigManager
    {
        public static string ConfigBaseDirectory => ModPathManager.GetModConfigDirectory(Constant.ModID);

        public static void CreateDirectoryIfNotExists()
        {
            if (!Directory.Exists(ConfigBaseDirectory)) Directory.CreateDirectory(ConfigBaseDirectory);
        }

        public static T LoadConfigFromFile<T>(string configName, bool autoSaveOnLoad = true) where T : ConfigBase
        {
            var configFilePath = Path.Combine(ConfigBaseDirectory, configName);
            var configInstance = Activator.CreateInstance<T>();
            configInstance.LoadFromFile(configFilePath, autoSaveOnLoad);
            return configInstance;
        }

        public static void SaveConfigToFile<T>(T configInstance, string configName) where T : ConfigBase
        {
            var configFilePath = Path.Combine(ConfigBaseDirectory, configName);
            configInstance.SaveToFile(configFilePath);
        }

        public static void CreateBackupFile(string originalFilePath)
        {
            try
            {
                if (!File.Exists(originalFilePath)) return;
                var backupFilePath = $"{originalFilePath}.bak";
                File.Copy(originalFilePath, backupFilePath, true);
                ModLogger.Log($"Backup created at: {backupFilePath}");
            }
            catch (IOException e)
            {
                ModLogger.LogError($"Failed to create backup file: {e.Message}");
            }
        }
    }
}
