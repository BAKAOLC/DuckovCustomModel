using System;
using System.Collections.Generic;
using UnityEngine;

namespace DuckovCustomModel.Localizations
{
    public static class LocalizedTextManager
    {
        private static readonly HashSet<LocalizedText> RegisteredTexts = [];
        private static readonly HashSet<LocalizedDropdown> RegisteredDropdowns = [];
        private static bool _isInitialized;

        public static void Register(LocalizedText localizedText)
        {
            if (localizedText == null) return;

            Initialize();
            RegisteredTexts.Add(localizedText);
        }

        public static void Unregister(LocalizedText localizedText)
        {
            if (localizedText == null) return;

            RegisteredTexts.Remove(localizedText);
        }

        public static void RegisterDropdown(LocalizedDropdown localizedDropdown)
        {
            if (localizedDropdown == null) return;

            Initialize();
            RegisteredDropdowns.Add(localizedDropdown);
        }

        public static void UnregisterDropdown(LocalizedDropdown localizedDropdown)
        {
            if (localizedDropdown == null) return;

            RegisteredDropdowns.Remove(localizedDropdown);
        }

        public static void RefreshAll()
        {
            var textsToRefresh = new List<LocalizedText>(RegisteredTexts);
            foreach (var text in textsToRefresh)
                if (text != null)
                    text.RefreshText();

            var dropdownsToRefresh = new List<LocalizedDropdown>(RegisteredDropdowns);
            foreach (var dropdown in dropdownsToRefresh)
                if (dropdown != null)
                    dropdown.Refresh();
        }

        private static void Initialize()
        {
            if (_isInitialized) return;

            Localization.OnLanguageChangedEvent += OnLanguageChanged;
            _isInitialized = true;
        }

        private static void OnLanguageChanged(SystemLanguage language)
        {
            RefreshAll();
        }

        public static void Cleanup()
        {
            if (!_isInitialized) return;

            try
            {
                Localization.OnLanguageChangedEvent -= OnLanguageChanged;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to cleanup LocalizedTextManager: {ex.Message}");
            }

            RegisteredTexts.Clear();
            RegisteredDropdowns.Clear();
            _isInitialized = false;
        }
    }
}
