using System.Reflection;
using Duckov;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;
using FMOD.Studio;
using HarmonyLib;
using UnityEngine;

namespace DuckovCustomModel.HarmonyPatches
{
    [HarmonyPatch]
    internal static class AudioManagerPatches
    {
        [HarmonyPatch]
        internal static class AudioManagerPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(AudioManager), "PostQuak");
            }

            private static bool Prefix(
                string soundKey,
                AudioManager.VoiceType voiceType,
                GameObject gameObject,
                // ReSharper disable once InconsistentNaming
                ref EventInstance? __result)
            {
                if (gameObject == null) return true;

                var characterMainControl = gameObject.GetComponent<CharacterMainControl>();
                if (characterMainControl == null) return true;

                var modelHandler = characterMainControl.GetComponent<ModelHandler>();
                if (modelHandler == null || !modelHandler.IsInitialized) return true;

                if (!modelHandler.HasAnySounds()) return true;
                if (!modelHandler.IsModelAudioEnabled) return true;

                var normalizedSoundKey = string.IsNullOrWhiteSpace(soundKey)
                    ? SoundTags.Normal
                    : soundKey.ToLowerInvariant().Trim();

                var soundPath = modelHandler.GetRandomSoundByTag(normalizedSoundKey);
                if (string.IsNullOrEmpty(soundPath)) return true;

                AudioManager.PostCustomSFX(soundPath);
                __result = null;
                return false;
            }
        }

        [HarmonyPatch]
        internal static class CharacterMainControlPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(CharacterMainControl), nameof(CharacterMainControl.Quack));
            }

            // ReSharper disable once InconsistentNaming
            private static bool Prefix(CharacterMainControl __instance)
            {
                var modelHandler = __instance.GetComponent<ModelHandler>();
                if (modelHandler == null || !modelHandler.IsInitialized) return true;

                if (!modelHandler.HasAnySounds()) return true;
                if (!modelHandler.IsModelAudioEnabled) return true;

                var soundPath = modelHandler.GetRandomSoundByTag(SoundTags.Normal);
                if (string.IsNullOrEmpty(soundPath)) return true;

                AudioManager.PostCustomSFX(soundPath);
                AIMainBrain.MakeSound(new()
                {
                    fromCharacter = __instance,
                    fromObject = __instance.gameObject,
                    pos = __instance.transform.position,
                    fromTeam = __instance.Team,
                    soundType = SoundTypes.unknowNoise,
                    radius = 15f,
                });
                return false;
            }
        }
    }
}
