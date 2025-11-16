using System;
using System.Collections.Generic;
using DuckovCustomModel.Core.Data;
using UnityEngine;

namespace DuckovCustomModel.Configs
{
    public class IdleAudioInterval
    {
        public float Min { get; set; } = 30f;
        public float Max { get; set; } = 45f;
    }

    public class IdleAudioConfig : ConfigBase
    {
        public Dictionary<ModelTarget, IdleAudioInterval> IdleAudioIntervals { get; set; } = [];
        public Dictionary<string, IdleAudioInterval> AICharacterIdleAudioIntervals { get; set; } = [];
        public Dictionary<ModelTarget, bool> EnableIdleAudio { get; set; } = [];
        public Dictionary<string, bool> AICharacterEnableIdleAudio { get; set; } = [];

        public override void LoadDefault()
        {
            IdleAudioIntervals = [];
            foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
            {
                if (target == ModelTarget.AICharacter) continue;
                IdleAudioIntervals[target] = new() { Min = 30f, Max = 45f };
            }

            AICharacterIdleAudioIntervals = [];

            EnableIdleAudio = [];
            foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
            {
                if (target == ModelTarget.AICharacter) continue;
                EnableIdleAudio[target] = target != ModelTarget.Character;
            }

            AICharacterEnableIdleAudio = [];
            AICharacterEnableIdleAudio[AICharacters.AllAICharactersKey] = true;
        }

        public override bool Validate()
        {
            var modified = false;

            foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
            {
                if (target == ModelTarget.AICharacter) continue;
                if (!IdleAudioIntervals.TryGetValue(target, out var interval))
                {
                    IdleAudioIntervals[target] = new() { Min = 30f, Max = 45f };
                    modified = true;
                }
                else
                {
                    var originalMin = interval.Min;
                    var originalMax = interval.Max;
                    if (interval.Min < 0.1f) interval.Min = 0.1f;
                    if (interval.Max < interval.Min) interval.Max = interval.Min;
                    if (!Mathf.Approximately(originalMin, interval.Min) ||
                        !Mathf.Approximately(originalMax, interval.Max))
                        modified = true;
                }
            }

            AICharacterIdleAudioIntervals ??= [];
            foreach (var (key, interval) in AICharacterIdleAudioIntervals)
                if (interval == null)
                {
                    AICharacterIdleAudioIntervals[key] = new() { Min = 30f, Max = 45f };
                    modified = true;
                }
                else
                {
                    var originalMin = interval.Min;
                    var originalMax = interval.Max;
                    if (interval.Min < 0.1f) interval.Min = 0.1f;
                    if (interval.Max < interval.Min) interval.Max = interval.Min;
                    if (!Mathf.Approximately(originalMin, interval.Min) ||
                        !Mathf.Approximately(originalMax, interval.Max))
                        modified = true;
                }

            EnableIdleAudio ??= [];
            foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
            {
                if (target == ModelTarget.AICharacter) continue;
                if (EnableIdleAudio.ContainsKey(target)) continue;
                EnableIdleAudio[target] = target != ModelTarget.Character;
                modified = true;
            }

            AICharacterEnableIdleAudio ??= [];
            if (!AICharacterEnableIdleAudio.TryAdd(AICharacters.AllAICharactersKey, true)) return modified;
            modified = true;

            return modified;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not IdleAudioConfig otherConfig) return;
            IdleAudioIntervals = new(otherConfig.IdleAudioIntervals);
            AICharacterIdleAudioIntervals = new(otherConfig.AICharacterIdleAudioIntervals);
            EnableIdleAudio = new(otherConfig.EnableIdleAudio);
            AICharacterEnableIdleAudio = new(otherConfig.AICharacterEnableIdleAudio);
        }

        public IdleAudioInterval GetIdleAudioInterval(ModelTarget target)
        {
            return IdleAudioIntervals.TryGetValue(target, out var value) && value != null
                ? value
                : new() { Min = 30f, Max = 45f };
        }

        public void SetIdleAudioInterval(ModelTarget target, float min, float max)
        {
            if (min < 0.1f) min = 0.1f;
            if (max < min) max = min;
            IdleAudioIntervals[target] = new() { Min = min, Max = max };
        }

        public IdleAudioInterval GetAICharacterIdleAudioInterval(string nameKey)
        {
            if (!string.IsNullOrEmpty(nameKey) && AICharacterIdleAudioIntervals.TryGetValue(nameKey, out var value) &&
                value != null)
                return value;
            if (AICharacterIdleAudioIntervals.TryGetValue(AICharacters.AllAICharactersKey, out var defaultInterval) &&
                defaultInterval != null)
                return defaultInterval;
            return new() { Min = 30f, Max = 45f };
        }

        public void SetAICharacterIdleAudioInterval(string nameKey, float min, float max)
        {
            if (min < 0.1f) min = 0.1f;
            if (max < min) max = min;
            AICharacterIdleAudioIntervals[nameKey] = new() { Min = min, Max = max };
        }

        public bool IsIdleAudioEnabled(ModelTarget target)
        {
            return EnableIdleAudio.TryGetValue(target, out var enabled) && enabled;
        }

        public void SetIdleAudioEnabled(ModelTarget target, bool enabled)
        {
            EnableIdleAudio[target] = enabled;
        }

        public bool IsAICharacterIdleAudioEnabled(string nameKey)
        {
            if (!string.IsNullOrEmpty(nameKey) && AICharacterEnableIdleAudio.TryGetValue(nameKey, out var enabled))
                return enabled;
            return AICharacterEnableIdleAudio.GetValueOrDefault(AICharacters.AllAICharactersKey, true);
        }

        public void SetAICharacterIdleAudioEnabled(string nameKey, bool enabled)
        {
            AICharacterEnableIdleAudio[nameKey] = enabled;
        }
    }
}
