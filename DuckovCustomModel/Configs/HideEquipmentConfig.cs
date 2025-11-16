using System;
using System.Collections.Generic;
using System.IO;
using DuckovCustomModel.Core;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DuckovCustomModel.Configs
{
    public class HideEquipmentConfig : ConfigBase
    {
        public Dictionary<ModelTarget, bool> HideEquipment { get; set; } = [];
        public Dictionary<string, bool> HideAICharacterEquipment { get; set; } = [];

        public override void LoadDefault()
        {
            HideEquipment = [];
            foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
            {
                if (target == ModelTarget.AICharacter) continue;
                HideEquipment[target] = false;
            }
        }

        public override bool Validate()
        {
            var modified = false;
            foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
            {
                if (target == ModelTarget.AICharacter) continue;
                if (!HideEquipment.TryAdd(target, false)) continue;
                modified = true;
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (HideAICharacterEquipment != null) return modified;
            HideAICharacterEquipment = [];
            modified = true;

            return modified;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not HideEquipmentConfig otherConfig) return;
            HideEquipment = new(otherConfig.HideEquipment);
            HideAICharacterEquipment = new(otherConfig.HideAICharacterEquipment);
        }

        public override void LoadFromFile(string filePath, bool autoSaveOnLoad = true)
        {
            try
            {
                ConfigManager.CreateDirectoryIfNotExists();

                if (!File.Exists(filePath))
                {
                    ModLogger.LogWarning($"Config file '{filePath}' does not exist. Loading default config.");
                    LoadDefault();
                    MigrateFromUIConfig();
                    if (autoSaveOnLoad) SaveToFile(filePath);
                    return;
                }

                var json = File.ReadAllText(filePath);
                JsonConvert.PopulateObject(json, this, JsonSettings.Default);

                MigrateFromUIConfig();

                if (Validate() && autoSaveOnLoad) SaveToFile(filePath);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to load config from file '{filePath}': {ex.Message}");
                LoadDefault();
                MigrateFromUIConfig();
                if (autoSaveOnLoad) SaveToFile(filePath);
            }
        }

        private void MigrateFromUIConfig()
        {
            try
            {
                var uiConfigPath = Path.Combine(ConfigManager.ConfigBaseDirectory, "UIConfig.json");
                if (!File.Exists(uiConfigPath)) return;

                var json = File.ReadAllText(uiConfigPath);
                var jsonObject = JObject.Parse(json);

                var migrated = false;

                var hideCharacterEquipmentToken = jsonObject["HideCharacterEquipment"];
                if (hideCharacterEquipmentToken is { Type: JTokenType.Boolean })
                {
                    var value = hideCharacterEquipmentToken.ToObject<bool>();
                    if (!HideEquipment.ContainsKey(ModelTarget.Character) ||
                        HideEquipment[ModelTarget.Character] != value)
                    {
                        HideEquipment[ModelTarget.Character] = value;
                        migrated = true;
                        ModLogger.Log($"Migrated HideCharacterEquipment ({value}) to HideEquipmentConfig");
                    }
                }

                var hidePetEquipmentToken = jsonObject["HidePetEquipment"];
                if (hidePetEquipmentToken is { Type: JTokenType.Boolean })
                {
                    var value = hidePetEquipmentToken.ToObject<bool>();
                    if (!HideEquipment.ContainsKey(ModelTarget.Pet) || HideEquipment[ModelTarget.Pet] != value)
                    {
                        HideEquipment[ModelTarget.Pet] = value;
                        migrated = true;
                        ModLogger.Log($"Migrated HidePetEquipment ({value}) to HideEquipmentConfig");
                    }
                }

                if (migrated) ModLogger.Log("Migration from UIConfig to HideEquipmentConfig completed");
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to migrate from UIConfig: {ex.Message}");
            }
        }

        public bool GetHideEquipment(ModelTarget target)
        {
            return HideEquipment.TryGetValue(target, out var value) && value;
        }

        public void SetHideEquipment(ModelTarget target, bool value)
        {
            HideEquipment[target] = value;
        }

        public bool GetHideAICharacterEquipment(string nameKey)
        {
            if (!string.IsNullOrEmpty(nameKey) && HideAICharacterEquipment.TryGetValue(nameKey, out var value))
                return value;
            return HideAICharacterEquipment.GetValueOrDefault(AICharacters.AllAICharactersKey, false);
        }

        public void SetHideAICharacterEquipment(string nameKey, bool value)
        {
            HideAICharacterEquipment[nameKey] = value;
        }
    }
}
