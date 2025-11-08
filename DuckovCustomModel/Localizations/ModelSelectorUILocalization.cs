using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using SodaCraft.Localizations;
using UnityEngine;

namespace DuckovCustomModel.Localizations
{
    public static class ModelSelectorUILocalization
    {
        private static Dictionary<string, string>? _currentLanguageDict;
        private static Dictionary<string, string>? _englishDict;
        private static bool _isInitialized;
        private static SystemLanguage CurrentLanguage => LocalizationManager.CurrentLanguage;

        private static string LocalizationDirectory
        {
            get
            {
                var dllPath = Path.GetDirectoryName(typeof(ModelSelectorUILocalization).Assembly.Location);
                return Path.Combine(dllPath ?? string.Empty, "Localizations");
            }
        }

        private static Dictionary<string, string> DefaultEnglish => new()
        {
            { "Title", "Model Selector" },
            { "SearchPlaceholder", "Search models..." },
            { "Refresh", "Refresh" },
            { "Loading", "Loading..." },
            { "LoadingModelList", "Loading model list..." },
            { "HideCharacterEquipment", "Hide Character Equipment" },
            { "HidePetEquipment", "Hide Pet Equipment" },
            { "Hotkey", "Hotkey:" },
            { "PressAnyKey", "Press any key..." },
            { "NoPreview", "No Preview" },
            { "Author", "Author" },
            { "Version", "Version" },
            { "NoModel", "No Model (Restore Original)" },
            { "TargetType", "Target Type:" },
            { "TargetCharacter", "Character" },
            { "TargetPet", "Pet" },
            { "TargetAICharacter", "AI Character" },
        };

        public static string Title => GetText("Title");
        public static string SearchPlaceholder => GetText("SearchPlaceholder");
        public static string Refresh => GetText("Refresh");
        public static string Loading => GetText("Loading");
        public static string LoadingModelList => GetText("LoadingModelList");
        public static string HideCharacterEquipment => GetText("HideCharacterEquipment");
        public static string HidePetEquipment => GetText("HidePetEquipment");
        public static string Hotkey => GetText("Hotkey");
        public static string PressAnyKey => GetText("PressAnyKey");
        public static string NoPreview => GetText("NoPreview");
        public static string Author => GetText("Author");
        public static string Version => GetText("Version");
        public static string NoModel => GetText("NoModel");
        public static string TargetType => GetText("TargetType");
        public static string TargetCharacter => GetText("TargetCharacter");
        public static string TargetPet => GetText("TargetPet");
        public static string TargetAICharacter => GetText("TargetAICharacter");

        private static void Initialize()
        {
            if (_isInitialized) return;

            LocalizationManager.OnSetLanguage += OnLanguageChanged;

            try
            {
                Directory.CreateDirectory(LocalizationDirectory);

                LoadLanguageFiles();
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to initialize localization: {ex.Message}");
                _currentLanguageDict = DefaultEnglish;
                _englishDict = DefaultEnglish;
            }

            _isInitialized = true;
        }

        private static void OnLanguageChanged(SystemLanguage language)
        {
            _isInitialized = false;
            LoadLanguageFiles();
            _isInitialized = true;
        }

        private static void LoadLanguageFiles()
        {
            try
            {
                var languageKey = GetLanguageKey();
                var languageFile = Path.Combine(LocalizationDirectory, $"{languageKey}.json");
                var englishFile = Path.Combine(LocalizationDirectory, "English.json");

                _englishDict = LoadLanguageFile(englishFile, DefaultEnglish);
                _currentLanguageDict = languageKey == "English"
                    ? _englishDict
                    : LoadLanguageFile(languageFile, _englishDict);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to load language files: {ex.Message}");
                _currentLanguageDict = DefaultEnglish;
                _englishDict = DefaultEnglish;
            }
        }

        private static Dictionary<string, string> LoadLanguageFile(string filePath, Dictionary<string, string> fallback)
        {
            if (!File.Exists(filePath))
            {
                try
                {
                    var json = JsonConvert.SerializeObject(fallback, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                    ModLogger.Log($"Created localization file: {filePath}");
                }
                catch (Exception ex)
                {
                    ModLogger.LogError($"Failed to create localization file {filePath}: {ex.Message}");
                }

                return fallback;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (dict == null)
                {
                    ModLogger.LogWarning($"Failed to parse localization file: {filePath}, using fallback.");
                    return fallback;
                }

                var result = new Dictionary<string, string>(fallback);
                foreach (var kvp in dict) result[kvp.Key] = kvp.Value;
                return result;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to load localization file {filePath}: {ex.Message}");
                return fallback;
            }
        }

        private static string GetLanguageKey()
        {
            var language = CurrentLanguage;

            if (language == SystemLanguage.Chinese || language == SystemLanguage.ChineseSimplified)
                return "Chinese";

            return language.ToString();
        }

        private static string GetText(string key)
        {
            Initialize();

            if (_currentLanguageDict != null && _currentLanguageDict.TryGetValue(key, out var text))
                return text;

            if (_englishDict != null && _englishDict.TryGetValue(key, out var englishText))
                return englishText;

            return key;
        }

        public static string GetLoadingProgress(int count, int total)
        {
            return $"{Loading} ({count}/{total})";
        }

        public static string GetModelInfo(string modelId, string? author, string? version, string? bundleName = null)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"ID: {modelId}");
            if (!string.IsNullOrEmpty(author))
                stringBuilder.Append($" | {Author}: {author}");
            if (!string.IsNullOrEmpty(version))
                stringBuilder.Append($" | {Version}: {version}");
            if (!string.IsNullOrEmpty(bundleName))
                stringBuilder.Append($" | Bundle: {bundleName}");
            return stringBuilder.ToString();
        }

        public static void Cleanup()
        {
            if (!_isInitialized) return;

            try
            {
                LocalizationManager.OnSetLanguage -= OnLanguageChanged;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to cleanup localization: {ex.Message}");
            }

            _isInitialized = false;
            _currentLanguageDict = null;
            _englishDict = null;
        }
    }
}