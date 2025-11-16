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
        private GameObject? _animatorParamsKeyButton;
        private Toggle? _animatorParamsToggle;
        private Dropdown? _dcmButtonAnchorDropdown;
        private InputField? _dcmButtonOffsetXInput;
        private InputField? _dcmButtonOffsetYInput;
        private bool _isWaitingForAnimatorParamsKeyInput;

        private bool _isWaitingForUIKeyInput;
        private GameObject? _keyButton;

        private int _settingRowIndex;
        private Toggle? _showDCMButtonToggle;

        private static UIConfig? UIConfig => ModEntry.UIConfig;

        public bool IsWaitingForKeyInput => _isWaitingForUIKeyInput || _isWaitingForAnimatorParamsKeyInput;

        private void Update()
        {
            if (_isWaitingForUIKeyInput || _isWaitingForAnimatorParamsKeyInput) HandleKeyInputCapture();
        }

        public void RefreshAnimatorParamsToggleState(bool visible)
        {
            if (_animatorParamsToggle == null) return;
            if (_animatorParamsToggle.isOn != visible) _animatorParamsToggle.isOn = visible;
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
            UIFactory.SetupVerticalLayoutGroup(contentArea, 10f, new(10, 10, 10, 10));

            var sizeFitter = contentArea.GetComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildKeySetting(contentArea);
            BuildAnimatorParamsKeySetting(contentArea);
            BuildAnimatorParamsToggle(contentArea);
            BuildShowDCMButtonToggle(contentArea);
            BuildDCMButtonPositionSettings(contentArea);
        }

        private void BuildKeySetting(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var keyLabel = UIFactory.CreateText("KeyLabel", row.transform,
                Localization.Hotkey, 18, Color.white);
            UIFactory.SetupLeftLabel(keyLabel);
            UIFactory.SetupContentSizeFitter(keyLabel);
            UIFactory.SetLocalizedText(keyLabel, () => Localization.Hotkey);

            var keyButton = UIFactory.CreateButton("KeyButton", row.transform, OnKeyButtonClicked,
                new(0.2f, 0.2f, 0.2f, 1));
            _keyButton = keyButton;
            UIFactory.SetupRightControl(keyButton, new(100, 30));

            var keyButtonText = UIFactory.CreateText("Text", keyButton.transform,
                GetKeyCodeDisplayName(UIConfig?.ToggleKey ?? KeyCode.Backslash), 18, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(keyButtonText);
            UIFactory.SetLocalizedText(keyButtonText, () =>
                _isWaitingForUIKeyInput
                    ? Localization.PressAnyKey
                    : GetKeyCodeDisplayName(UIConfig?.ToggleKey ?? KeyCode.Backslash));
        }

        private void BuildAnimatorParamsKeySetting(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var keyLabel = UIFactory.CreateText("KeyLabel", row.transform,
                Localization.AnimatorParamsHotkey, 18, Color.white);
            UIFactory.SetupLeftLabel(keyLabel);
            UIFactory.SetupContentSizeFitter(keyLabel);
            UIFactory.SetLocalizedText(keyLabel, () => Localization.AnimatorParamsHotkey);

            var keyButton = UIFactory.CreateButton("KeyButton", row.transform, OnAnimatorParamsKeyButtonClicked,
                new(0.2f, 0.2f, 0.2f, 1));
            _animatorParamsKeyButton = keyButton;
            UIFactory.SetupRightControl(keyButton, new(100, 30));

            var clearButton = UIFactory.CreateButton("ClearButton", row.transform, OnAnimatorParamsClearButtonClicked,
                new(0.3f, 0.2f, 0.2f, 1));
            UIFactory.SetupRightControl(clearButton, new(60, 30), -20f - 100f - 8f);

            var clearButtonText = UIFactory.CreateText("Text", clearButton.transform,
                Localization.Clear, 16, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(clearButtonText);
            UIFactory.SetLocalizedText(clearButtonText, () => Localization.Clear);

            var keyButtonText = UIFactory.CreateText("Text", keyButton.transform,
                GetKeyCodeDisplayName(UIConfig?.AnimatorParamsToggleKey ?? KeyCode.None), 18, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(keyButtonText);
            UIFactory.SetLocalizedText(keyButtonText, () =>
                _isWaitingForAnimatorParamsKeyInput
                    ? Localization.PressAnyKey
                    : GetKeyCodeDisplayName(UIConfig?.AnimatorParamsToggleKey ?? KeyCode.None));
        }

        private void BuildAnimatorParamsToggle(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var label = UIFactory.CreateText("Label", row.transform,
                Localization.ShowAnimatorParameters, 18, Color.white);
            UIFactory.SetupLeftLabel(label);
            UIFactory.SetupContentSizeFitter(label);
            UIFactory.SetLocalizedText(label, () => Localization.ShowAnimatorParameters);

            var configWindow = GetComponentInParent<ConfigWindow>();
            var toggle = UIFactory.CreateToggle("AnimatorParamsToggle", row.transform,
                configWindow?.IsAnimatorParamsWindowVisible() ?? false, SetAnimatorParamsWindowVisible);
            UIFactory.SetupRightControl(toggle.gameObject, new(20, 20));

            _animatorParamsToggle = toggle;
        }

        private void BuildShowDCMButtonToggle(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var label = UIFactory.CreateText("Label", row.transform,
                Localization.ShowDCMButton, 18, Color.white);
            UIFactory.SetupLeftLabel(label);
            UIFactory.SetupContentSizeFitter(label);
            UIFactory.SetLocalizedText(label, () => Localization.ShowDCMButton);

            var toggle = UIFactory.CreateToggle("ShowDCMButtonToggle", row.transform,
                UIConfig?.ShowDCMButton ?? true, OnShowDCMButtonToggleChanged);
            UIFactory.SetupRightControl(toggle.gameObject, new(20, 20));

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
            UIFactory.SetupLeftLabel(anchorLabel);
            UIFactory.SetupContentSizeFitter(anchorLabel);
            UIFactory.SetLocalizedText(anchorLabel, () => Localization.DCMButtonAnchor);

            var dropdown = UIFactory.CreateDropdown("AnchorDropdown", anchorRow.transform, OnAnchorDropdownChanged);
            UIFactory.SetupRightControl(dropdown.gameObject, new(200, 30));

            RefreshAnchorDropdownOptions(dropdown);

            _dcmButtonAnchorDropdown = dropdown;

            var dropdownLocalizedText = dropdown.gameObject.AddComponent<LocalizedDropdown>();
            dropdownLocalizedText.SetDropdown(dropdown);
            dropdownLocalizedText.SetRefreshAction(RefreshAnchorDropdownOnLanguageChange);

            var offsetRow = CreateSettingRow(parent);
            var offsetLabel = UIFactory.CreateText("OffsetLabel", offsetRow.transform,
                Localization.DCMButtonOffset, 18, Color.white);
            UIFactory.SetupLeftLabel(offsetLabel);
            UIFactory.SetupContentSizeFitter(offsetLabel);
            UIFactory.SetLocalizedText(offsetLabel, () => Localization.DCMButtonOffset);

            var offsetYInput = UIFactory.CreateInputField("OffsetYInput", offsetRow.transform);
            UIFactory.SetupRightControl(offsetYInput.gameObject, new(80, 25));
            offsetYInput.contentType = InputField.ContentType.DecimalNumber;
            offsetYInput.onValueChanged.AddListener(OnOffsetYValueChanged);
            offsetYInput.onEndEdit.AddListener(OnOffsetYEndEdit);
            _dcmButtonOffsetYInput = offsetYInput;

            var offsetYLabel = UIFactory.CreateText("OffsetYLabel", offsetRow.transform,
                Localization.OffsetY, 16, new Color(0.9f, 0.9f, 0.9f, 1));
            UIFactory.SetupRightLabel(offsetYLabel);
            UIFactory.SetupContentSizeFitter(offsetYLabel);
            UIFactory.SetLocalizedText(offsetYLabel, () => Localization.OffsetY);

            var offsetXInput = UIFactory.CreateInputField("OffsetXInput", offsetRow.transform);
            UIFactory.SetupRightControl(offsetXInput.gameObject, new(80, 25), -220f);
            offsetXInput.contentType = InputField.ContentType.DecimalNumber;
            offsetXInput.onValueChanged.AddListener(OnOffsetXValueChanged);
            offsetXInput.onEndEdit.AddListener(OnOffsetXEndEdit);
            _dcmButtonOffsetXInput = offsetXInput;

            var offsetXLabel = UIFactory.CreateText("OffsetXLabel", offsetRow.transform,
                Localization.OffsetX, 16, new Color(0.9f, 0.9f, 0.9f, 1));
            UIFactory.SetupRightLabel(offsetXLabel, 25f, -320f);
            UIFactory.SetupContentSizeFitter(offsetXLabel);
            UIFactory.SetLocalizedText(offsetXLabel, () => Localization.OffsetX);

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

        private void RefreshAnchorDropdownOnLanguageChange()
        {
            if (_dcmButtonAnchorDropdown == null) return;
            var currentValue = _dcmButtonAnchorDropdown.value;
            RefreshAnchorDropdownOptions(_dcmButtonAnchorDropdown);
            if (currentValue >= 0 && currentValue < _dcmButtonAnchorDropdown.options.Count)
            {
                _dcmButtonAnchorDropdown.value = -1;
                _dcmButtonAnchorDropdown.value = currentValue;
            }
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

            UIFactory.SetupAnchor(row, new(0, 1), new(1, 1), new(0.5f, 1), new(800, 50), Vector2.zero);

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
            _isWaitingForUIKeyInput = true;
            _isWaitingForAnimatorParamsKeyInput = false;
            if (_keyButton == null) return;
            var textObj = _keyButton.transform.Find("Text");
            if (textObj == null) return;
            var localizedText = textObj.GetComponent<LocalizedText>();
            if (localizedText != null)
                localizedText.RefreshText();
            if (_animatorParamsKeyButton == null) return;
            var animatorParamsTextObj = _animatorParamsKeyButton.transform.Find("Text");
            if (animatorParamsTextObj == null) return;
            var animatorParamsLocalizedText = animatorParamsTextObj.GetComponent<LocalizedText>();
            animatorParamsLocalizedText?.RefreshText();
        }

        private void OnAnimatorParamsKeyButtonClicked()
        {
            if (UIConfig == null) return;
            _isWaitingForAnimatorParamsKeyInput = true;
            _isWaitingForUIKeyInput = false;
            if (_animatorParamsKeyButton == null) return;
            var textObj = _animatorParamsKeyButton.transform.Find("Text");
            if (textObj == null) return;
            var localizedText = textObj.GetComponent<LocalizedText>();
            if (localizedText != null)
                localizedText.RefreshText();
            if (_keyButton == null) return;
            var keyTextObj = _keyButton.transform.Find("Text");
            if (keyTextObj == null) return;
            var keyLocalizedText = keyTextObj.GetComponent<LocalizedText>();
            keyLocalizedText?.RefreshText();
        }

        private void OnAnimatorParamsClearButtonClicked()
        {
            if (UIConfig == null) return;
            UIConfig.AnimatorParamsToggleKey = KeyCode.None;
            ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
            if (_animatorParamsKeyButton == null) return;
            var textObj = _animatorParamsKeyButton.transform.Find("Text");
            if (textObj == null) return;
            var localizedText = textObj.GetComponent<LocalizedText>();
            localizedText?.RefreshText();
        }

        private void HandleKeyInputCapture()
        {
            if ((!_isWaitingForUIKeyInput && !_isWaitingForAnimatorParamsKeyInput) || UIConfig == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _isWaitingForUIKeyInput = false;
                _isWaitingForAnimatorParamsKeyInput = false;
                if (_keyButton != null)
                {
                    var textObj = _keyButton.transform.Find("Text");
                    if (textObj != null)
                    {
                        var localizedText = textObj.GetComponent<LocalizedText>();
                        localizedText?.RefreshText();
                    }
                }

                if (_animatorParamsKeyButton == null) return;
                {
                    var textObj = _animatorParamsKeyButton.transform.Find("Text");
                    if (textObj == null) return;
                    var localizedText = textObj.GetComponent<LocalizedText>();
                    localizedText?.RefreshText();
                }
                return;
            }

            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                if (Input.GetKeyDown(keyCode))
                {
                    if (keyCode is KeyCode.Mouse0 or KeyCode.Mouse1 or KeyCode.Mouse2 or KeyCode.Mouse3
                        or KeyCode.Mouse4 or KeyCode.Mouse5 or KeyCode.Mouse6)
                        continue;

                    if (_isWaitingForUIKeyInput)
                    {
                        UIConfig.ToggleKey = keyCode;
                        ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                        _isWaitingForUIKeyInput = false;
                        if (_keyButton == null) return;
                        var textObj = _keyButton.transform.Find("Text");
                        if (textObj == null) return;
                        var localizedText = textObj.GetComponent<LocalizedText>();
                        localizedText?.RefreshText();
                    }
                    else if (_isWaitingForAnimatorParamsKeyInput)
                    {
                        UIConfig.AnimatorParamsToggleKey = keyCode;
                        ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                        _isWaitingForAnimatorParamsKeyInput = false;
                        if (_animatorParamsKeyButton == null) return;
                        var textObj = _animatorParamsKeyButton.transform.Find("Text");
                        if (textObj == null) return;
                        var localizedText = textObj.GetComponent<LocalizedText>();
                        localizedText?.RefreshText();
                    }

                    return;
                }
        }

        private static string GetKeyCodeDisplayName(KeyCode keyCode)
        {
            if (keyCode == KeyCode.None) return Localization.None;
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
