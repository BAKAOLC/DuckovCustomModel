using System;
using System.IO;
using UnityEngine;

namespace DuckovCustomModelRegister
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public const string TargetModId = "DuckovCustomModel";

        private static string? _modConfigsRootPath;
        private static bool _isInitialized;

        public static string ModDirectory => Path.GetDirectoryName(typeof(ModBehaviour).Assembly.Location)!;
        public static string ModelDirectory => Path.Combine(GetModConfigDirectory(TargetModId), "Models");

        private static string ModConfigsRootPath
        {
            get
            {
                if (_isInitialized) return _modConfigsRootPath!;
                InitializePath();
                _isInitialized = true;

                return _modConfigsRootPath!;
            }
        }

        private void OnEnable()
        {
            CreateModelDirectoryIfNeeded();
            CopyModels();
        }

        private static void InitializePath()
        {
            var installPath = Path.Combine(Application.dataPath, "..", "ModConfigs");
            installPath = Path.GetFullPath(installPath);

            if (IsDirectoryWritable(installPath))
            {
                _modConfigsRootPath = installPath;
                Debug.Log($"[DuckovCustomModelRegister] Using install directory for ModConfigs: {_modConfigsRootPath}");
                return;
            }

            Debug.LogWarning(
                $"[DuckovCustomModelRegister] Install directory is read-only, using persistent data path instead: {installPath}");
            _modConfigsRootPath = GetPersistentDataPath();
            Debug.Log($"[DuckovCustomModelRegister] Using persistent data path for ModConfigs: {_modConfigsRootPath}");
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
                Debug.LogWarning(
                    $"[DuckovCustomModelRegister] Directory is not writable: {directoryPath}, Error: {ex.Message}");
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
                Debug.LogError(
                    $"[DuckovCustomModelRegister] Failed to create ModConfigs directory at persistent path: {ex.Message}");
                throw;
            }

            return modConfigsPath;
        }

        private static string GetModConfigDirectory(string modId)
        {
            return Path.Combine(ModConfigsRootPath, modId);
        }

        private static void CreateModelDirectoryIfNeeded()
        {
            if (Directory.Exists(ModelDirectory)) return;
            Directory.CreateDirectory(ModelDirectory);
        }

        private static void CopyModels()
        {
            var sourceDir = Path.Combine(ModDirectory, "Models");
            CopyFolder(sourceDir, ModelDirectory);
        }

        private static void CopyFolder(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir)) return;
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

            foreach (var filePath in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(filePath);
                var destFilePath = Path.Combine(destDir, fileName);
                File.Copy(filePath, destFilePath, true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(directory);
                var destSubDir = Path.Combine(destDir, dirName);
                CopyFolder(directory, destSubDir);
            }
        }
    }
}
