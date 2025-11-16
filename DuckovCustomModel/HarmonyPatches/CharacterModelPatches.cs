using System.Linq;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using HarmonyLib;

namespace DuckovCustomModel.HarmonyPatches
{
    [HarmonyPatch]
    internal static class CharacterModelPatches
    {
        [HarmonyPatch(typeof(AICharacterController), nameof(AICharacterController.Init))]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        internal static void AICharacterController_Init_Postfix(AICharacterController __instance)
        {
            if (__instance == null) return;
            var characterMainControl = __instance.CharacterMainControl;
            if (characterMainControl == null) return;

            var preset = characterMainControl.characterPreset;
            if (preset == null) return;
            if (string.IsNullOrEmpty(preset.nameKey)) return;
            if (!AICharacters.SupportedAICharacters.Contains(preset.nameKey)) return;

            var modelHandler = ModelManager.InitializeModelHandler(characterMainControl, ModelTarget.AICharacter);
            if (modelHandler == null) return;

            var usingModel = ModEntry.UsingModel;
            if (usingModel == null) return;

            var modelID = usingModel.GetAICharacterModelIDWithFallback(preset.nameKey);
            if (string.IsNullOrEmpty(modelID)) return;

            if (!ModelManager.FindModelByID(modelID, out var bundleInfo, out var modelInfo)) return;

            if (!modelInfo.CompatibleWithAICharacter(preset.nameKey)) return;

            modelHandler.InitializeCustomModel(bundleInfo, modelInfo);
            modelHandler.ChangeToCustomModel();
        }
    }
}
