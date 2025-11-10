using System;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Tabs
{
    public class SettingsTab : Base.UIPanel
    {
        private Text? _animatorParamsLabel;
        private Toggle? _animatorParamsToggle;
        private Dropdown? _dcmButtonAnchorDropdown;
        private InputField? _dcmButtonOffsetXInput;
        private InputField? _dcmButtonOffsetYInput;
        private Text? _keyButtonText;
        private Text? _keyLabel;

        private int _settingRowIndex;
        private Text? _showDCMButtonLabel;
        private Toggle? _showDCMButtonToggle;

        public bool IsWaitingForKeyInput { get; private set; }

        private static UIConfig? UIConfig => ModBehaviour.Instance?.UIConfig;

        private void Update()
        {
            if (IsWaitingForKeyInput) HandleKeyInputCapture();
        }

        protected override void OnDestroy()
        {
            Localization.OnLanguageChangedEvent -= OnLanguageChanged;
            base.OnDestroy();
        }

        public event Action<bool>? OnAnimatorParamsToggleChanged;

        protected override void CreatePanel()
        {
            PanelRoot = UIFactory.CreateImage("SettingsPanel", transform, new(0.08f, 0.1f, 0.12f, 0.95f));
            UIFactory.SetupRectTransform(PanelRoot, new(0, 0), new(1, 1), Vector2.zero);

            var outline = PanelRoot.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(2, -2);
        }

        protected override void BuildContent()
        {
            if (PanelRoot == null) return;

            _settingRowIndex = 0;
            BuildSettingsContent();
            Localization.OnLanguageChangedEvent += OnLanguageChanged;
        }

        private void OnLanguageChanged(SystemLanguage language)
        {
            if (_keyButtonText != null && !IsWaitingForKeyInput)
                _keyButtonText.text = GetKeyCodeDisplayName(UIConfig?.ToggleKey ?? KeyCode.Backslash);

            if (_keyLabel != null)
                _keyLabel.text = Localization.Hotkey;

            if (_animatorParamsLabel != null)
                _animatorParamsLabel.text = Localization.ShowAnimatorParameters;

            if (_showDCMButtonLabel != null)
                _showDCMButtonLabel.text = Localization.ShowDCMButton;

            if (_dcmButtonAnchorDropdown != null)
            {
                var currentValue = _dcmButtonAnchorDropdown.value;
                RefreshAnchorDropdownOptions(_dcmButtonAnchorDropdown);
                _dcmButtonAnchorDropdown.value = currentValue;
            }

            RefreshDCMButtonPositionDisplay();
        }

        private void BuildSettingsContent()
        {
            if (PanelRoot == null) return;

            var scrollView = UIFactory.CreateScrollView("SettingsScrollView", PanelRoot.transform, out var contentArea);
            UIFactory.SetupRectTransform(scrollView.gameObject, new(0, 0), new(1, 1), Vector2.zero);
            scrollView.GetComponent<RectTransform>().offsetMin = new(10, 10);
            scrollView.GetComponent<RectTransform>().offsetMax = new(-10, -10);

            contentArea.AddComponent<VerticalLayoutGroup>();
            contentArea.AddComponent<ContentSizeFitter>();

            var contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.sizeDelta = new(800, 0);

            var layoutGroup = contentArea.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new(10, 10, 10, 10);
            layoutGroup.spacing = 10;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            var sizeFitter = contentArea.GetComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildKeySetting(contentArea);
            BuildAnimatorParamsToggle(contentArea);
            BuildShowDCMButtonToggle(contentArea);
            BuildDCMButtonPositionSettings(contentArea);
        }

        private void BuildKeySetting(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var keyLabel = UIFactory.CreateText("KeyLabel", row.transform,
                Localization.Hotkey, 18, Color.white);
            _keyLabel = keyLabel.GetComponent<Text>();
            var keyLabelRect = keyLabel.GetComponent<RectTransform>();
            keyLabelRect.anchorMin = new(0, 0.5f);
            keyLabelRect.anchorMax = new(0, 0.5f);
            keyLabelRect.pivot = new(0, 0.5f);
            keyLabelRect.sizeDelta = new(0, 30);
            keyLabelRect.anchoredPosition = new(20, 0);
            var keyLabelSizeFitter = keyLabel.AddComponent<ContentSizeFitter>();
            keyLabelSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            keyLabelSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var keyButton = UIFactory.CreateButton("KeyButton", row.transform, OnKeyButtonClicked,
                new(0.2f, 0.2f, 0.2f, 1));
            var keyButtonRect = keyButton.GetComponent<RectTransform>();
            keyButtonRect.anchorMin = new(1, 0.5f);
            keyButtonRect.anchorMax = new(1, 0.5f);
            keyButtonRect.pivot = new(1, 0.5f);
            keyButtonRect.sizeDelta = new(100, 30);
            keyButtonRect.anchoredPosition = new(-20, 0);

            var keyButtonText = UIFactory.CreateText("Text", keyButton.transform,
                GetKeyCodeDisplayName(UIConfig?.ToggleKey ?? KeyCode.Backslash), 18, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupRectTransform(keyButtonText, Vector2.zero, Vector2.one, Vector2.zero);
            _keyButtonText = keyButtonText.GetComponent<Text>();
        }

        private void BuildAnimatorParamsToggle(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var label = UIFactory.CreateText("Label", row.transform,
                Localization.ShowAnimatorParameters, 18, Color.white);
            _animatorParamsLabel = label.GetComponent<Text>();
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new(0, 0.5f);
            labelRect.anchorMax = new(0, 0.5f);
            labelRect.pivot = new(0, 0.5f);
            labelRect.sizeDelta = new(0, 30);
            labelRect.anchoredPosition = new(20, 0);
            var labelSizeFitter = label.AddComponent<ContentSizeFitter>();
            labelSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            labelSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var configWindow = GetComponentInParent<ConfigWindow>();
            var toggle = UIFactory.CreateToggle("AnimatorParamsToggle", row.transform,
                configWindow?.IsAnimatorParamsWindowVisible() ?? false, SetAnimatorParamsWindowVisible);
            var toggleRect = toggle.GetComponent<RectTransform>();
            toggleRect.anchorMin = new(1, 0.5f);
            toggleRect.anchorMax = new(1, 0.5f);
            toggleRect.pivot = new(1, 0.5f);
            toggleRect.sizeDelta = new(20, 20);
            toggleRect.anchoredPosition = new(-20, 0);

            _animatorParamsToggle = toggle;
        }

        private void BuildShowDCMButtonToggle(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var label = UIFactory.CreateText("Label", row.transform,
                Localization.ShowDCMButton, 18, Color.white);
            _showDCMButtonLabel = label.GetComponent<Text>();
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new(0, 0.5f);
            labelRect.anchorMax = new(0, 0.5f);
            labelRect.pivot = new(0, 0.5f);
            labelRect.sizeDelta = new(0, 30);
            labelRect.anchoredPosition = new(20, 0);
            var labelSizeFitter = label.AddComponent<ContentSizeFitter>();
            labelSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            labelSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var toggle = UIFactory.CreateToggle("ShowDCMButtonToggle", row.transform,
                UIConfig?.ShowDCMButton ?? true, OnShowDCMButtonToggleChanged);
            var toggleRect = toggle.GetComponent<RectTransform>();
            toggleRect.anchorMin = new(1, 0.5f);
            toggleRect.anchorMax = new(1, 0.5f);
            toggleRect.pivot = new(1, 0.5f);
            toggleRect.sizeDelta = new(20, 20);
            toggleRect.anchoredPosition = new(-20, 0);

            _showDCMButtonToggle = toggle;
        }

        private static void OnShowDCMButtonToggleChanged(bool value)
        {
            if (UIConfig == null) return;
            UIConfig.ShowDCMButton = value;
            ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
        }

        private void BuildDCMButtonPositionSettings(GameObject parent)
        {
            var anchorRow = CreateSettingRow(parent);
            var anchorLabel = UIFactory.CreateText("AnchorLabel", anchorRow.transform,
                Localization.DCMButtonAnchor, 18, Color.white);
            var anchorLabelRect = anchorLabel.GetComponent<RectTransform>();
            anchorLabelRect.anchorMin = new(0, 0.5f);
            anchorLabelRect.anchorMax = new(0, 0.5f);
            anchorLabelRect.pivot = new(0, 0.5f);
            anchorLabelRect.sizeDelta = new(0, 30);
            anchorLabelRect.anchoredPosition = new(20, 0);
            var anchorLabelSizeFitter = anchorLabel.AddComponent<ContentSizeFitter>();
            anchorLabelSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            anchorLabelSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var dropdown = UIFactory.CreateDropdown("AnchorDropdown", anchorRow.transform, OnAnchorDropdownChanged);
            var dropdownRect = dropdown.GetComponent<RectTransform>();
            dropdownRect.anchorMin = new(0.3f, 0.5f);
            dropdownRect.anchorMax = new(0.7f, 0.5f);
            dropdownRect.pivot = new(0.5f, 0.5f);
            dropdownRect.sizeDelta = new(0, 30);
            dropdownRect.anchoredPosition = new(0, 0);

            RefreshAnchorDropdownOptions(dropdown);

            _dcmButtonAnchorDropdown = dropdown;

            var offsetRow = CreateSettingRow(parent);
            var offsetLabel = UIFactory.CreateText("OffsetLabel", offsetRow.transform,
                Localization.DCMButtonOffset, 18, Color.white);
            var offsetLabelRect = offsetLabel.GetComponent<RectTransform>();
            offsetLabelRect.anchorMin = new(0, 0.5f);
            offsetLabelRect.anchorMax = new(0, 0.5f);
            offsetLabelRect.pivot = new(0, 0.5f);
            offsetLabelRect.sizeDelta = new(0, 30);
            offsetLabelRect.anchoredPosition = new(20, 0);
            var offsetLabelSizeFitter = offsetLabel.AddComponent<ContentSizeFitter>();
            offsetLabelSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            offsetLabelSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var offsetXLabel = UIFactory.CreateText("OffsetXLabel", offsetRow.transform,
                Localization.OffsetX, 16, new Color(0.9f, 0.9f, 0.9f, 1));
            var offsetXLabelRect = offsetXLabel.GetComponent<RectTransform>();
            offsetXLabelRect.anchorMin = new(0.3f, 0.5f);
            offsetXLabelRect.anchorMax = new(0.3f, 0.5f);
            offsetXLabelRect.pivot = new(0, 0.5f);
            offsetXLabelRect.sizeDelta = new(0, 25);
            offsetXLabelRect.anchoredPosition = new(20, 0);

            var offsetXInput = UIFactory.CreateInputField("OffsetXInput", offsetRow.transform);
            var offsetXInputRect = offsetXInput.GetComponent<RectTransform>();
            offsetXInputRect.anchorMin = new(0.4f, 0.5f);
            offsetXInputRect.anchorMax = new(0.55f, 0.5f);
            offsetXInputRect.pivot = new(0.5f, 0.5f);
            offsetXInputRect.sizeDelta = new(0, 25);
            offsetXInputRect.anchoredPosition = new(0, 0);
            offsetXInput.contentType = InputField.ContentType.DecimalNumber;
            offsetXInput.onValueChanged.AddListener(OnOffsetXValueChanged);
            offsetXInput.onEndEdit.AddListener(OnOffsetXEndEdit);
            _dcmButtonOffsetXInput = offsetXInput;

            var offsetYLabel = UIFactory.CreateText("OffsetYLabel", offsetRow.transform,
                Localization.OffsetY, 16, new Color(0.9f, 0.9f, 0.9f, 1));
            var offsetYLabelRect = offsetYLabel.GetComponent<RectTransform>();
            offsetYLabelRect.anchorMin = new(0.6f, 0.5f);
            offsetYLabelRect.anchorMax = new(0.6f, 0.5f);
            offsetYLabelRect.pivot = new(0, 0.5f);
            offsetYLabelRect.sizeDelta = new(0, 25);
            offsetYLabelRect.anchoredPosition = new(20, 0);

            var offsetYInput = UIFactory.CreateInputField("OffsetYInput", offsetRow.transform);
            var offsetYInputRect = offsetYInput.GetComponent<RectTransform>();
            offsetYInputRect.anchorMin = new(0.7f, 0.5f);
            offsetYInputRect.anchorMax = new(0.85f, 0.5f);
            offsetYInputRect.pivot = new(0.5f, 0.5f);
            offsetYInputRect.sizeDelta = new(0, 25);
            offsetYInputRect.anchoredPosition = new(0, 0);
            offsetYInput.contentType = InputField.ContentType.DecimalNumber;
            offsetYInput.onValueChanged.AddListener(OnOffsetYValueChanged);
            offsetYInput.onEndEdit.AddListener(OnOffsetYEndEdit);
            _dcmButtonOffsetYInput = offsetYInput;

            RefreshDCMButtonPositionDisplay();
        }

        private static void RefreshAnchorDropdownOptions(Dropdown dropdown)
        {
            dropdown.options.Clear();
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.TopLeft)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.TopCenter)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.TopRight)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.MiddleLeft)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.MiddleCenter)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.MiddleRight)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.BottomLeft)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.BottomCenter)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.BottomRight)));
        }

        private void RefreshDCMButtonPositionDisplay()
        {
            if (UIConfig == null) return;

            if (_dcmButtonAnchorDropdown != null)
            {
                var anchorValues = Enum.GetValues(typeof(AnchorPosition));
                var index = Array.IndexOf(anchorValues, UIConfig.DCMButtonAnchor);
                if (index >= 0 && index < _dcmButtonAnchorDropdown.options.Count)
                    _dcmButtonAnchorDropdown.value = index;
            }

            if (_dcmButtonOffsetXInput != null)
                _dcmButtonOffsetXInput.text = UIConfig.DCMButtonOffsetX.ToString("F1");

            if (_dcmButtonOffsetYInput != null)
                _dcmButtonOffsetYInput.text = UIConfig.DCMButtonOffsetY.ToString("F1");
        }

        private static string GetAnchorPositionText(AnchorPosition position)
        {
            return position switch
            {
                AnchorPosition.TopLeft => Localization.TopLeft,
                AnchorPosition.TopCenter => Localization.TopCenter,
                AnchorPosition.TopRight => Localization.TopRight,
                AnchorPosition.MiddleLeft => Localization.MiddleLeft,
                AnchorPosition.MiddleCenter => Localization.MiddleCenter,
                AnchorPosition.MiddleRight => Localization.MiddleRight,
                AnchorPosition.BottomLeft => Localization.BottomLeft,
                AnchorPosition.BottomCenter => Localization.BottomCenter,
                AnchorPosition.BottomRight => Localization.BottomRight,
                _ => Localization.TopLeft,
            };
        }

        private void OnAnchorDropdownChanged(int index)
        {
            if (UIConfig == null) return;
            var anchorValues = Enum.GetValues(typeof(AnchorPosition));
            if (index >= 0 && index < anchorValues.Length)
            {
                UIConfig.DCMButtonAnchor = (AnchorPosition)anchorValues.GetValue(index);
                ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                RefreshSettingsButton();
            }
        }

        private void OnOffsetXValueChanged(string value)
        {
            if (UIConfig == null) return;
            if (float.TryParse(value, out var offsetX))
            {
                UIConfig.DCMButtonOffsetX = offsetX;
                RefreshSettingsButton();
            }
        }

        private void OnOffsetXEndEdit(string value)
        {
            if (UIConfig == null) return;
            if (float.TryParse(value, out var offsetX))
            {
                UIConfig.DCMButtonOffsetX = offsetX;
                ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                RefreshSettingsButton();
            }
            else
            {
                RefreshDCMButtonPositionDisplay();
            }
        }

        private void OnOffsetYValueChanged(string value)
        {
            if (UIConfig == null) return;
            if (float.TryParse(value, out var offsetY))
            {
                UIConfig.DCMButtonOffsetY = offsetY;
                RefreshSettingsButton();
            }
        }

        private void OnOffsetYEndEdit(string value)
        {
            if (UIConfig == null) return;
            if (float.TryParse(value, out var offsetY))
            {
                UIConfig.DCMButtonOffsetY = offsetY;
                ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                RefreshSettingsButton();
            }
            else
            {
                RefreshDCMButtonPositionDisplay();
            }
        }

        private void RefreshSettingsButton()
        {
            var configWindow = GetComponentInParent<ConfigWindow>();
            configWindow?.RefreshSettingsButton();
        }

        private GameObject CreateSettingRow(GameObject parent)
        {
            var row = new GameObject("SettingRow", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent.transform, false);

            var rowImage = row.GetComponent<Image>();
            var isEven = _settingRowIndex % 2 == 0;
            rowImage.color = isEven ? new(0.12f, 0.14f, 0.16f, 0.8f) : new(0.1f, 0.12f, 0.14f, 0.8f);
            _settingRowIndex++;

            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new(0, 1);
            rowRect.anchorMax = new(1, 1);
            rowRect.pivot = new(0.5f, 1);
            rowRect.sizeDelta = new(800, 50);
            rowRect.anchoredPosition = Vector2.zero;

            var rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 50;
            rowLayoutElement.minWidth = 800;
            rowLayoutElement.preferredWidth = 800;
            rowLayoutElement.flexibleWidth = 0;
            rowLayoutElement.flexibleHeight = 0;

            return row;
        }

        private void OnKeyButtonClicked()
        {
            if (UIConfig == null) return;
            IsWaitingForKeyInput = true;
            if (_keyButtonText != null)
                _keyButtonText.text = Localization.PressAnyKey;
        }

        private void HandleKeyInputCapture()
        {
            if (!IsWaitingForKeyInput || UIConfig == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                IsWaitingForKeyInput = false;
                if (_keyButtonText != null)
                    _keyButtonText.text = GetKeyCodeDisplayName(UIConfig.ToggleKey);
                return;
            }

            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                if (Input.GetKeyDown(keyCode))
                {
                    if (keyCode == KeyCode.Mouse0 || keyCode == KeyCode.Mouse1 || keyCode == KeyCode.Mouse2 ||
                        keyCode == KeyCode.Mouse3 || keyCode == KeyCode.Mouse4 || keyCode == KeyCode.Mouse5 ||
                        keyCode == KeyCode.Mouse6)
                        continue;

                    UIConfig.ToggleKey = keyCode;
                    ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                    IsWaitingForKeyInput = false;
                    if (_keyButtonText != null)
                        _keyButtonText.text = GetKeyCodeDisplayName(keyCode);
                    return;
                }
        }

        private static string GetKeyCodeDisplayName(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.Alpha0 => "0",
                KeyCode.Alpha1 => "1",
                KeyCode.Alpha2 => "2",
                KeyCode.Alpha3 => "3",
                KeyCode.Alpha4 => "4",
                KeyCode.Alpha5 => "5",
                KeyCode.Alpha6 => "6",
                KeyCode.Alpha7 => "7",
                KeyCode.Alpha8 => "8",
                KeyCode.Alpha9 => "9",
                KeyCode.Backslash => "\\",
                KeyCode.Slash => "/",
                KeyCode.LeftBracket => "[",
                KeyCode.RightBracket => "]",
                KeyCode.Semicolon => ";",
                KeyCode.Quote => "'",
                KeyCode.Comma => ",",
                KeyCode.Period => ".",
                KeyCode.Equals => "=",
                KeyCode.Minus => "-",
                KeyCode.Plus => "+",
                KeyCode.LeftShift => "Left Shift",
                KeyCode.RightShift => "Right Shift",
                KeyCode.LeftControl => "Left Ctrl",
                KeyCode.RightControl => "Right Ctrl",
                KeyCode.LeftAlt => "Left Alt",
                KeyCode.RightAlt => "Right Alt",
                KeyCode.LeftCommand => "Left Cmd",
                KeyCode.RightCommand => "Right Cmd",
                KeyCode.LeftWindows => "Left Win",
                KeyCode.RightWindows => "Right Win",
                _ => keyCode.ToString(),
            };
        }

        public void SetAnimatorParamsWindowVisible(bool visible)
        {
            OnAnimatorParamsToggleChanged?.Invoke(visible);
        }
    }
}