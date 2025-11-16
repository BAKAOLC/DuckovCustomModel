using System.Collections.Generic;
using HarmonyLib;
using ItemStatsSystem;
using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    [HarmonyPatch]
    internal static class DeathLootBoxPatches
    {
        private static readonly Dictionary<Item, CharacterMainControl> DeathLootBoxOwners = [];

        [HarmonyPatch(typeof(CharacterMainControl), "OnDead")]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static void CharacterMainControl_OnDead_Prefix(CharacterMainControl __instance)
        {
            if (__instance == null)
                return;

            var characterItem = __instance.CharacterItem;
            if (characterItem == null)
                return;

            DeathLootBoxOwners[characterItem] = __instance;
        }

        [HarmonyPatch(typeof(CharacterMainControl), "OnDead")]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        private static void CharacterMainControl_OnDead_Postfix(CharacterMainControl __instance)
        {
            if (__instance == null)
                return;

            var characterItem = __instance.CharacterItem;
            if (characterItem == null)
                return;

            DeathLootBoxOwners.Remove(characterItem);
        }

        [HarmonyPatch(typeof(InteractableLootbox), nameof(InteractableLootbox.CreateFromItem))]
        [HarmonyPostfix]
        // ReSharper disable InconsistentNaming
        private static void InteractableLootBox_CreateFromItem_Postfix(InteractableLootbox __result, Item item)
        {
            if (__result == null || item == null)
                return;

            if (!DeathLootBoxOwners.TryGetValue(item, out var owner))
                return;

            if (owner == null)
            {
                DeathLootBoxOwners.Remove(item);
                return;
            }

            var modelRoot = __result.transform.Find("Model");
            if (modelRoot == null)
                return;

            var modelHandler = owner.GetComponent<ModelHandler>();
            if (modelHandler == null)
                return;

            if (!modelHandler.HaveCustomDeathLootBox())
                return;

            // Instantiate custom model
            var customModel = modelHandler.CreateCustomDeathLootBoxInstance();
            if (customModel == null)
                return;

            // Disable default model
            foreach (Transform child in modelRoot) child.gameObject.SetActive(false);

            customModel.transform.SetParent(modelRoot, false);
            customModel.transform.localPosition = Vector3.zero;
            customModel.transform.localRotation = Quaternion.identity;
            customModel.transform.localScale = Vector3.one;

            var destroyAdapter = customModel.GetComponent<OnDestroyAdapter>();
            if (destroyAdapter == null)
                destroyAdapter = customModel.AddComponent<OnDestroyAdapter>();

            destroyAdapter.OnDestroyEvent += _ =>
            {
                if (modelRoot == null)
                    return;

                // Re-enable default model
                foreach (Transform child in modelRoot)
                    if (child != null)
                        child.gameObject.SetActive(true);
            };
        }
    }
}
