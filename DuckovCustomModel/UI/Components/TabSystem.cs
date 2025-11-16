using System;
using System.Collections.Generic;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class TabSystem : MonoBehaviour
    {
        private readonly Dictionary<int, string> _tabLocalizationKeys = [];
        private readonly List<TabInfo> _tabs = [];
        private int _currentTab;
        private GameObject? _tabContainer;

        public event Action<int>? OnTabChanged;

        public void Initialize(Transform parent)
        {
            _tabContainer = new("TabContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            _tabContainer.transform.SetParent(parent, false);
            UIFactory.SetupRectTransform(_tabContainer, new(0, 1), new(1, 1), new(0, 60), pivot: new(0.5f, 1),
                anchoredPosition: new(0, -40));
            UIFactory.SetupHorizontalLayoutGroup(_tabContainer, 8f, new(10, 8, 10, 8));
        }

        public void AddTab(string tabName, GameObject panel, bool isActive = false, string? localizationKey = null)
        {
            if (_tabContainer == null) return;

            var tabIndex = _tabs.Count;
            var button = UIFactory.CreateButton($"Tab_{tabIndex}", _tabContainer.transform,
                () => SwitchToTab(tabIndex), new(0.2f, 0.2f, 0.2f, 1)).GetComponent<Button>();
            UIFactory.SetupRectTransform(button.gameObject, Vector2.zero, Vector2.zero, new(140, 60));

            var text = UIFactory.CreateText("Text", button.transform, tabName, 16, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(text);

            var textComponent = text.GetComponent<Text>();
            var tabInfo = new TabInfo
            {
                Name = tabName,
                Button = button,
                Panel = panel,
                IsActive = isActive,
                Text = textComponent,
            };

            _tabs.Add(tabInfo);

            if (!string.IsNullOrEmpty(localizationKey))
            {
                _tabLocalizationKeys[tabIndex] = localizationKey;
                if (textComponent != null)
                    UIFactory.SetLocalizedText(textComponent.gameObject, () => GetLocalizedText(localizationKey));
            }

            if (isActive || _tabs.Count == 1) SwitchToTab(tabIndex);
        }

        public void RefreshLocalization()
        {
            foreach (var (tabIndex, localizationKey) in _tabLocalizationKeys)
            {
                if (tabIndex < 0 || tabIndex >= _tabs.Count) continue;

                var tab = _tabs[tabIndex];
                var localizedText = GetLocalizedText(localizationKey);
                tab.Name = localizedText;

                if (tab.Text == null) continue;
                var localizedTextComponent = tab.Text.GetComponent<LocalizedText>();
                if (localizedTextComponent != null)
                    localizedTextComponent.RefreshText();
            }
        }

        private static string GetLocalizedText(string key)
        {
            return key switch
            {
                "ModelSelection" => Localization.ModelSelection,
                "Settings" => Localization.Settings,
                _ => key,
            };
        }

        public void SwitchToTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _tabs.Count) return;

            _currentTab = tabIndex;

            for (var i = 0; i < _tabs.Count; i++)
            {
                var tab = _tabs[i];
                var isActive = i == tabIndex;

                tab.Panel.SetActive(isActive);
                tab.IsActive = isActive;

                var colors = tab.Button.colors;
                colors.normalColor = isActive ? new(0.3f, 0.4f, 0.5f, 1) : new(0.2f, 0.2f, 0.2f, 1);
                tab.Button.colors = colors;
            }

            OnTabChanged?.Invoke(tabIndex);
        }

        public int GetCurrentTab()
        {
            return _currentTab;
        }

        private class TabInfo
        {
            public string Name { get; set; } = string.Empty;
            public Button Button { get; set; } = null!;
            public GameObject Panel { get; set; } = null!;
            public bool IsActive { get; set; }
            public Text? Text { get; set; }
        }
    }
}
