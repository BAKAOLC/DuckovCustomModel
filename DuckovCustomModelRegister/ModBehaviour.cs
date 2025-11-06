using System.IO;
using UnityEngine;

namespace DuckovCustomModelRegister
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static string ModDirectory => Path.GetDirectoryName(typeof(ModBehaviour).Assembly.Location)!;
        public static string ModelDirectory => $"{Application.dataPath}/../ModConfigs/DuckovCustomModel/Models";

        private void OnEnable()
        {
            CreateModelDirectoryIfNeeded();
            CopyModels();
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