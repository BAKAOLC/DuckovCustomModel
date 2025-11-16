using System;
using System.Collections.Generic;
using DuckovCustomModel.Core.Data;

namespace DuckovCustomModel.Configs
{
    public class ModelAudioConfig : ConfigBase
    {
        public Dictionary<ModelTarget, bool> EnableModelAudio { get; set; } = [];
        public Dictionary<string, bool> AICharacterEnableModelAudio { get; set; } = [];

        public override void LoadDefault()
        {
            EnableModelAudio = [];
            foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
            {
                if (target == ModelTarget.AICharacter) continue;
                EnableModelAudio[target] = true;
            }

            AICharacterEnableModelAudio = [];
            AICharacterEnableModelAudio[AICharacters.AllAICharactersKey] = true;
        }

        public override bool Validate()
        {
            var modified = false;

            EnableModelAudio ??= [];
            foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
            {
                if (target == ModelTarget.AICharacter) continue;
                if (EnableModelAudio.ContainsKey(target)) continue;
                EnableModelAudio[target] = true;
                modified = true;
            }

            AICharacterEnableModelAudio ??= [];
            if (!AICharacterEnableModelAudio.TryAdd(AICharacters.AllAICharactersKey, true)) return modified;
            modified = true;

            return modified;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not ModelAudioConfig otherConfig) return;
            EnableModelAudio = new(otherConfig.EnableModelAudio);
            AICharacterEnableModelAudio = new(otherConfig.AICharacterEnableModelAudio);
        }

        public bool IsModelAudioEnabled(ModelTarget target)
        {
            return EnableModelAudio.TryGetValue(target, out var enabled) && enabled;
        }

        public void SetModelAudioEnabled(ModelTarget target, bool enabled)
        {
            EnableModelAudio[target] = enabled;
        }

        public bool IsAICharacterModelAudioEnabled(string nameKey)
        {
            if (!string.IsNullOrEmpty(nameKey) && AICharacterEnableModelAudio.TryGetValue(nameKey, out var enabled))
                return enabled;
            return AICharacterEnableModelAudio.GetValueOrDefault(AICharacters.AllAICharactersKey, true);
        }

        public void SetAICharacterModelAudioEnabled(string nameKey, bool enabled)
        {
            AICharacterEnableModelAudio[nameKey] = enabled;
        }
    }
}
