using DuckovCustomModel.Data;
using DuckovCustomModel.MonoBehaviours;
using HarmonyLib;

namespace DuckovCustomModel.HarmonyPatches
{
    [HarmonyPatch]
    internal static class SpawnPaperBoxActionPatches
    {
        [HarmonyPatch(typeof(SpawnPaperBoxAction), "OnTriggered")]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        internal static void SpawnPaperBoxAction_OnTriggered_Postfix(SpawnPaperBoxAction __instance)
        {
            var instance = __instance.instance;
            if (instance == null) return;

            var targetCharacter = instance.character;
            if (targetCharacter == null) return;

            var customSocketMarker = instance.GetComponent<CustomSocketMarker>();
            if (customSocketMarker == null)
            {
                customSocketMarker = instance.gameObject.AddComponent<CustomSocketMarker>();
                customSocketMarker.CustomSocketName = SocketNames.PaperBox;
                customSocketMarker.OriginParent = instance.transform.parent;
            }

            var dontHideAsEquipment = instance.GetComponent<DontHideAsEquipment>();
            if (dontHideAsEquipment == null) instance.gameObject.AddComponent<DontHideAsEquipment>();

            var modelHandler = targetCharacter.GetComponent<ModelHandler>();
            if (modelHandler == null || !modelHandler.IsInitialized) return;

            modelHandler.RegisterCustomSocketObject(instance.gameObject);
        }

        [HarmonyPatch(typeof(SpawnPaperBoxAction), "OnDestroy")]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        internal static void SpawnPaperBoxAction_OnDestroy_Postfix(SpawnPaperBoxAction __instance)
        {
            var instance = __instance.instance;
            if (instance == null) return;

            var targetCharacter = instance.character;
            if (targetCharacter == null) return;

            var modelHandler = targetCharacter.GetComponent<ModelHandler>();
            if (modelHandler == null || !modelHandler.IsInitialized) return;

            modelHandler.UnregisterCustomSocketObject(instance.gameObject);
        }
    }
}