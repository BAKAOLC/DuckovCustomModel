using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DuckovCustomModel.Core.Data
{
    public class ModelInfo : IValidatable
    {
        public string ModelID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        public string PrefabPath { get; set; } = string.Empty;

        public string? DeathLootBoxPrefabPath { get; set; }

        public SoundInfo[] CustomSounds { get; set; } = [];

        [JsonIgnore] public string BundleName { get; set; } = string.Empty;

        public ModelTarget[] Target { get; set; } = [ModelTarget.Character];

        public string[] SupportedAICharacters { get; set; } = [];

        public string[] Features { get; set; } = [];

        public float? WalkSoundFrequency { get; set; }

        public float? RunSoundFrequency { get; set; }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(ModelID)) return false;
            if (string.IsNullOrWhiteSpace(Name)) return false;
            if (string.IsNullOrWhiteSpace(PrefabPath)) return false;

            if (string.IsNullOrWhiteSpace(DeathLootBoxPrefabPath)) DeathLootBoxPrefabPath = null;

            var targets = new List<ModelTarget>();
            foreach (var target in Target ?? [])
            {
                if (!Enum.IsDefined(typeof(ModelTarget), target)) continue;
                if (!targets.Contains(target)) targets.Add(target);
            }

            var soundInfos = new List<SoundInfo>();
            foreach (var soundInfo in CustomSounds ?? [])
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (soundInfo == null || string.IsNullOrWhiteSpace(soundInfo.Path)) continue;
                soundInfo.Initialize();
                soundInfos.Add(soundInfo);
            }

            var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var feature in Features ?? [])
            {
                if (string.IsNullOrWhiteSpace(feature)) continue;
                features.Add(feature.Trim());
            }

            Features = features.ToArray();

            Target = targets.ToArray();
            CustomSounds = soundInfos.ToArray();

            return true;
        }

        public bool CompatibleWithType(ModelTarget modelTarget)
        {
            return Array.Exists(Target, target => target == modelTarget);
        }

        public bool CompatibleWithAICharacter(string nameKey)
        {
            if (!CompatibleWithType(ModelTarget.AICharacter)) return false;
            if (nameKey == AICharacters.AllAICharactersKey) return true;
            if (SupportedAICharacters is not { Length: > 0 }) return false;
            return Array.Exists(SupportedAICharacters, key => key == AICharacters.AllAICharactersKey || key == nameKey);
        }
    }
}
