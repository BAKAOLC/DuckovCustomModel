using DuckovCustomModel.Configs;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Data;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class TargetSettingsPanel : MonoBehaviour
    {
        private GameObject? _content;
        private TargetInfo? _currentTarget;
        private Toggle? _enableIdleAudioToggle;
        private Toggle? _enableModelAudioToggle;
        private Toggle? _hideEquipmentToggle;
        private InputField? _idleAudioMaxIntervalInput;
        private InputField? _idleAudioMinIntervalInput;
        private ScrollRect? _scrollRect;
        private int _settingRowIndex;

        public void Initialize(Transform parent)
        {
            var scrollView = UIFactory.CreateScrollView("TargetSettingsScrollView", parent, out var content);
            UIFactory.SetupRectTransform(scrollView.gameObject, Vector2.zero, Vector2.one, Vector2.zero);

            _scrollRect = scrollView;
            _content = content;

            UIFactory.SetupVerticalLayoutGroup(_content, 10f, new(10, 10, 10, 10), TextAnchor.UpperCenter,
                true, false, true);
            UIFactory.SetupContentSizeFitter(_content, ContentSizeFitter.FitMode.Unconstrained);
        }

        public void SetTarget(TargetInfo? targetInfo)
        {
            _currentTarget = targetInfo;
            Refresh();
        }

        public void Refresh()
        {
            if (_content == null) return;

            foreach (Transform child in _content.transform) Destroy(child.gameObject);

            if (_currentTarget == null) return;

            _settingRowIndex = 0;
            BuildHideEquipmentSetting();
            BuildEnableModelAudioSetting();
            BuildEnableIdleAudioSetting();
            BuildIdleAudioIntervalSettings();
        }

        private void BuildHideEquipmentSetting()
        {
            if (_content == null || _currentTarget == null) return;

            var settingRow = CreateSettingRow();
            var hideEquipmentConfig = ModEntry.HideEquipmentConfig;

            var label = UIFactory.CreateText("Label", settingRow.transform,
                _currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null
                    ? string.Format(Localization.HideAICharacterEquipment, _currentTarget.DisplayName)
                    : _currentTarget.TargetType == ModelTarget.Character
                        ? Localization.HideCharacterEquipment
                        : Localization.HidePetEquipment,
                18, Color.white);
            UIFactory.SetupLeftLabel(label);
            UIFactory.SetupContentSizeFitter(label);

            var displayName = _currentTarget.DisplayName;
            var targetType = _currentTarget.TargetType;
            var aiCharacterNameKey = _currentTarget.AICharacterNameKey;
            UIFactory.SetLocalizedText(label, () =>
                targetType == ModelTarget.AICharacter && aiCharacterNameKey != null
                    ? string.Format(Localization.HideAICharacterEquipment, displayName)
                    : targetType == ModelTarget.Character
                        ? Localization.HideCharacterEquipment
                        : Localization.HidePetEquipment);

            var isOn = false;
            if (hideEquipmentConfig != null)
            {
                if (_currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null)
                    isOn = hideEquipmentConfig.GetHideAICharacterEquipment(_currentTarget.AICharacterNameKey);
                else
                    isOn = hideEquipmentConfig.GetHideEquipment(_currentTarget.TargetType);
            }

            var toggle = UIFactory.CreateToggle("HideEquipmentToggle", settingRow.transform, isOn,
                OnHideEquipmentToggleChanged);
            UIFactory.SetupRightControl(toggle.gameObject, new(20, 20));

            _hideEquipmentToggle = toggle;
        }

        private void BuildEnableModelAudioSetting()
        {
            if (_content == null || _currentTarget == null) return;

            var settingRow = CreateSettingRow();
            var modelAudioConfig = ModEntry.ModelAudioConfig;

            var label = UIFactory.CreateText("Label", settingRow.transform, Localization.EnableModelAudio, 18,
                Color.white);
            UIFactory.SetupLeftLabel(label);
            UIFactory.SetupContentSizeFitter(label);
            UIFactory.SetLocalizedText(label, () => Localization.EnableModelAudio);

            var isOn = true;
            if (modelAudioConfig != null)
            {
                if (_currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null)
                    isOn = modelAudioConfig.IsAICharacterModelAudioEnabled(_currentTarget.AICharacterNameKey);
                else
                    isOn = modelAudioConfig.IsModelAudioEnabled(_currentTarget.TargetType);
            }

            var toggle = UIFactory.CreateToggle("EnableModelAudioToggle", settingRow.transform, isOn,
                OnEnableModelAudioToggleChanged);
            UIFactory.SetupRightControl(toggle.gameObject, new(20, 20));

            _enableModelAudioToggle = toggle;
        }

        private void BuildEnableIdleAudioSetting()
        {
            if (_content == null || _currentTarget == null) return;

            var settingRow = CreateSettingRow();
            var idleAudioConfig = ModEntry.IdleAudioConfig;

            var label = UIFactory.CreateText("Label", settingRow.transform, Localization.EnableIdleAudio, 18,
                Color.white);
            UIFactory.SetupLeftLabel(label);
            UIFactory.SetupContentSizeFitter(label);
            UIFactory.SetLocalizedText(label, () => Localization.EnableIdleAudio);

            var isOn = false;
            if (idleAudioConfig != null)
            {
                if (_currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null)
                    isOn = idleAudioConfig.IsAICharacterIdleAudioEnabled(_currentTarget.AICharacterNameKey);
                else
                    isOn = idleAudioConfig.IsIdleAudioEnabled(_currentTarget.TargetType);
            }

            var toggle = UIFactory.CreateToggle("EnableIdleAudioToggle", settingRow.transform, isOn,
                OnEnableIdleAudioToggleChanged);
            UIFactory.SetupRightControl(toggle.gameObject, new(20, 20));

            _enableIdleAudioToggle = toggle;
        }

        private void BuildIdleAudioIntervalSettings()
        {
            if (_content == null || _currentTarget == null) return;

            var idleAudioConfig = ModEntry.IdleAudioConfig;
            if (idleAudioConfig == null) return;

            IdleAudioInterval interval;
            if (_currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null)
                interval = idleAudioConfig.GetAICharacterIdleAudioInterval(_currentTarget.AICharacterNameKey);
            else
                interval = idleAudioConfig.GetIdleAudioInterval(_currentTarget.TargetType);

            var minIntervalRow = CreateSettingRow();
            var minLabel = UIFactory.CreateText("Label", minIntervalRow.transform,
                $"{Localization.IdleAudioInterval} {Localization.Seconds} ({Localization.MinValue})", 18, Color.white);
            UIFactory.SetupLeftLabel(minLabel);
            UIFactory.SetupContentSizeFitter(minLabel);
            UIFactory.SetLocalizedText(minLabel, () =>
                $"{Localization.IdleAudioInterval} {Localization.Seconds} ({Localization.MinValue})");

            _idleAudioMinIntervalInput =
                UIFactory.CreateInputField("IdleAudioMinIntervalInput", minIntervalRow.transform);
            _idleAudioMinIntervalInput.text = interval.Min.ToString("F1");
            if (_idleAudioMinIntervalInput.textComponent != null)
                _idleAudioMinIntervalInput.textComponent.fontSize += 4;
            UIFactory.SetupRightControl(_idleAudioMinIntervalInput.gameObject, new(100, 30));
            _idleAudioMinIntervalInput.onEndEdit.AddListener(OnIdleAudioMinIntervalChanged);

            var maxIntervalRow = CreateSettingRow();
            var maxLabel = UIFactory.CreateText("Label", maxIntervalRow.transform,
                $"{Localization.IdleAudioInterval} {Localization.Seconds} ({Localization.MaxValue})", 18, Color.white);
            UIFactory.SetupLeftLabel(maxLabel);
            UIFactory.SetupContentSizeFitter(maxLabel);
            UIFactory.SetLocalizedText(maxLabel, () =>
                $"{Localization.IdleAudioInterval} {Localization.Seconds} ({Localization.MaxValue})");

            _idleAudioMaxIntervalInput =
                UIFactory.CreateInputField("IdleAudioMaxIntervalInput", maxIntervalRow.transform);
            _idleAudioMaxIntervalInput.text = interval.Max.ToString("F1");
            if (_idleAudioMaxIntervalInput.textComponent != null)
                _idleAudioMaxIntervalInput.textComponent.fontSize += 4;
            UIFactory.SetupRightControl(_idleAudioMaxIntervalInput.gameObject, new(100, 30));
            _idleAudioMaxIntervalInput.onEndEdit.AddListener(OnIdleAudioMaxIntervalChanged);
        }

        private GameObject CreateSettingRow()
        {
            if (_content == null) return new();

            var row = new GameObject("SettingRow", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_content.transform, false);

            var rowImage = row.GetComponent<Image>();
            var isEven = _settingRowIndex % 2 == 0;
            rowImage.color = isEven ? new(0.12f, 0.14f, 0.16f, 0.8f) : new(0.1f, 0.12f, 0.14f, 0.8f);
            _settingRowIndex++;

            UIFactory.SetupAnchor(row, new(0, 1), new(1, 1), new(0.5f, 1), new(0, 50), Vector2.zero);

            var rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 50;
            rowLayoutElement.flexibleWidth = 1;
            rowLayoutElement.flexibleHeight = 0;

            return row;
        }

        private void OnHideEquipmentToggleChanged(bool value)
        {
            if (_currentTarget == null) return;

            var hideEquipmentConfig = ModEntry.HideEquipmentConfig;
            if (hideEquipmentConfig == null) return;

            if (_currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null)
            {
                hideEquipmentConfig.SetHideAICharacterEquipment(_currentTarget.AICharacterNameKey, value);
                ConfigManager.SaveConfigToFile(hideEquipmentConfig, "HideEquipmentConfig.json");
            }
            else
            {
                hideEquipmentConfig.SetHideEquipment(_currentTarget.TargetType, value);
                ConfigManager.SaveConfigToFile(hideEquipmentConfig, "HideEquipmentConfig.json");
            }
        }

        private void OnEnableModelAudioToggleChanged(bool value)
        {
            if (_currentTarget == null) return;

            var modelAudioConfig = ModEntry.ModelAudioConfig;
            if (modelAudioConfig == null) return;

            if (_currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null)
            {
                modelAudioConfig.SetAICharacterModelAudioEnabled(_currentTarget.AICharacterNameKey, value);
                ConfigManager.SaveConfigToFile(modelAudioConfig, "ModelAudioConfig.json");
            }
            else
            {
                modelAudioConfig.SetModelAudioEnabled(_currentTarget.TargetType, value);
                ConfigManager.SaveConfigToFile(modelAudioConfig, "ModelAudioConfig.json");
            }
        }

        private void OnEnableIdleAudioToggleChanged(bool value)
        {
            if (_currentTarget == null) return;

            var idleAudioConfig = ModEntry.IdleAudioConfig;
            if (idleAudioConfig == null) return;

            if (_currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null)
            {
                idleAudioConfig.SetAICharacterIdleAudioEnabled(_currentTarget.AICharacterNameKey, value);
                ConfigManager.SaveConfigToFile(idleAudioConfig, "IdleAudioConfig.json");
            }
            else
            {
                idleAudioConfig.SetIdleAudioEnabled(_currentTarget.TargetType, value);
                ConfigManager.SaveConfigToFile(idleAudioConfig, "IdleAudioConfig.json");
            }
        }

        private void OnIdleAudioMinIntervalChanged(string value)
        {
            if (_currentTarget == null) return;

            var idleAudioConfig = ModEntry.IdleAudioConfig;
            if (idleAudioConfig == null) return;

            if (!float.TryParse(value, out var minValue)) return;
            if (minValue < 0.1f) minValue = 0.1f;

            IdleAudioInterval interval;
            if (_currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null)
            {
                interval = idleAudioConfig.GetAICharacterIdleAudioInterval(_currentTarget.AICharacterNameKey);
                idleAudioConfig.SetAICharacterIdleAudioInterval(_currentTarget.AICharacterNameKey, minValue,
                    interval.Max);
                ConfigManager.SaveConfigToFile(idleAudioConfig, "IdleAudioConfig.json");
            }
            else
            {
                interval = idleAudioConfig.GetIdleAudioInterval(_currentTarget.TargetType);
                idleAudioConfig.SetIdleAudioInterval(_currentTarget.TargetType, minValue, interval.Max);
                ConfigManager.SaveConfigToFile(idleAudioConfig, "IdleAudioConfig.json");
            }
        }

        private void OnIdleAudioMaxIntervalChanged(string value)
        {
            if (_currentTarget == null) return;

            var idleAudioConfig = ModEntry.IdleAudioConfig;
            if (idleAudioConfig == null) return;

            if (!float.TryParse(value, out var maxValue)) return;
            if (maxValue < 0.1f) maxValue = 0.1f;

            IdleAudioInterval interval;
            if (_currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null)
            {
                interval = idleAudioConfig.GetAICharacterIdleAudioInterval(_currentTarget.AICharacterNameKey);
                if (maxValue < interval.Min) maxValue = interval.Min;
                idleAudioConfig.SetAICharacterIdleAudioInterval(_currentTarget.AICharacterNameKey, interval.Min,
                    maxValue);
                ConfigManager.SaveConfigToFile(idleAudioConfig, "IdleAudioConfig.json");
            }
            else
            {
                interval = idleAudioConfig.GetIdleAudioInterval(_currentTarget.TargetType);
                if (maxValue < interval.Min) maxValue = interval.Min;
                idleAudioConfig.SetIdleAudioInterval(_currentTarget.TargetType, interval.Min, maxValue);
                ConfigManager.SaveConfigToFile(idleAudioConfig, "IdleAudioConfig.json");
            }
        }
    }
}
