using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Localizations;

namespace DuckovCustomModel.UI.Data
{
    public class TargetInfo
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public ModelTarget TargetType { get; set; }
        public string? AICharacterNameKey { get; set; }
        public bool IsSelected { get; set; }
        public bool HasModel { get; set; }
        public int ModelCount { get; set; }

        public static TargetInfo CreateCharacterTarget()
        {
            return new()
            {
                Id = "Character",
                DisplayName = Localization.TargetCharacter,
                TargetType = ModelTarget.Character,
            };
        }

        public static TargetInfo CreatePetTarget()
        {
            return new()
            {
                Id = "Pet",
                DisplayName = Localization.TargetPet,
                TargetType = ModelTarget.Pet,
            };
        }

        public static TargetInfo CreateAICharacterTarget(string nameKey, string displayName)
        {
            return new()
            {
                Id = $"AI_{nameKey}",
                DisplayName = displayName,
                TargetType = ModelTarget.AICharacter,
                AICharacterNameKey = nameKey,
            };
        }

        public static TargetInfo CreateAllAICharactersTarget()
        {
            return new()
            {
                Id = "AI_*",
                DisplayName = Localization.TargetAllAICharacters,
                TargetType = ModelTarget.AICharacter,
                AICharacterNameKey = AICharacters.AllAICharactersKey,
            };
        }
    }
}
