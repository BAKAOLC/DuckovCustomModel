using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace DuckovCustomModel.Core.Data
{
    public class BundleHashInfo
    {
        public string BundleName { get; set; } = string.Empty;
        public string BundlePath { get; set; } = string.Empty;
        public string BundleHash { get; set; } = string.Empty;
        public string ConfigHash { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }

        public static string CalculateFileHash(string filePath)
        {
            if (!File.Exists(filePath)) return string.Empty;

            try
            {
                using var sha256 = SHA256.Create();
                using var fileStream = File.OpenRead(filePath);
                var hashBytes = sha256.ComputeHash(fileStream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"Failed to calculate hash for file '{filePath}': {ex.Message}");
                return string.Empty;
            }
        }

        public static string CalculateStringHash(string content)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;

            try
            {
                using var sha256 = SHA256.Create();
                var contentBytes = Encoding.UTF8.GetBytes(content);
                var hashBytes = sha256.ComputeHash(contentBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"Failed to calculate hash for string: {ex.Message}");
                return string.Empty;
            }
        }

        public static BundleHashInfo? LoadFromFile(string filePath, JsonSerializerSettings? jsonSettings = null)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                var json = File.ReadAllText(filePath);
                var settings = jsonSettings ?? JsonSettings.Default;
                return JsonConvert.DeserializeObject<BundleHashInfo>(json, settings);
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"Failed to load BundleHashInfo from '{filePath}': {ex.Message}");
                return null;
            }
        }

        public void SaveToFile(string filePath, JsonSerializerSettings? jsonSettings = null)
        {
            try
            {
                var settings = jsonSettings ?? JsonSettings.Default;
                var json = JsonConvert.SerializeObject(this, settings);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"Failed to save BundleHashInfo to '{filePath}': {ex.Message}");
            }
        }
    }
}
