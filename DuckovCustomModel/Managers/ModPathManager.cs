using System;
using System.IO;
using UnityEngine;

namespace DuckovCustomModel.Managers
{
    public static class ModPathManager
    {
        private static string? _modConfigsRootPath;
        private static bool _isInitialized;

        public static string ModConfigsRootPath
        {
            get
            {
                if (_isInitialized) return _modConfigsRootPath!;
                InitializePath();
                _isInitialized = true;

                return _modConfigsRootPath!;
            }
        }

        private static void InitializePath()
        {
            var installPath = Path.Combine(Application.dataPath, "..", "ModConfigs");
            installPath = Path.GetFullPath(installPath);

            if (IsDirectoryWritable(installPath))
            {
                _modConfigsRootPath = installPath;
                ModLogger.Log($"Using install directory for ModConfigs: {_modConfigsRootPath}");
                return;
            }

            ModLogger.LogWarning($"Install directory is read-only, using persistent data path instead: {installPath}");
            _modConfigsRootPath = GetPersistentDataPath();
            ModLogger.Log($"Using persistent data path for ModConfigs: {_modConfigsRootPath}");
        }

        private static bool IsDirectoryWritable(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

                var testFile = Path.Combine(directoryPath, ".writetest");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.LogWarning($"Directory is not writable: {directoryPath}, Error: {ex.Message}");
                return false;
            }
        }

        private static string GetPersistentDataPath()
        {
            var persistentPath = Application.persistentDataPath;
            var modConfigsPath = Path.Combine(persistentPath, "ModConfigs");

            if (Directory.Exists(modConfigsPath)) return modConfigsPath;
            try
            {
                Directory.CreateDirectory(modConfigsPath);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to create ModConfigs directory at persistent path: {ex.Message}");
                throw;
            }

            return modConfigsPath;
        }

        public static string GetModConfigDirectory(string modId)
        {
            return Path.Combine(ModConfigsRootPath, modId);
        }
    }
}
