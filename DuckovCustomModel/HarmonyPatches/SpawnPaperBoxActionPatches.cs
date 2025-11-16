using DuckovCustomModel.Core.Data;
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

            var modelHandler = targetCharacter.GetComponent<ModelHandler>();
            if (modelHandler == null || !modelHandler.IsInitialized) return;

            var customSocketMarker = instance.GetComponent<CustomSocketMarker>();
            if (customSocketMarker == null)
            {
                customSocketMarker = instance.gameObject.AddComponent<CustomSocketMarker>();
                customSocketMarker.AddCustomSocketName(SocketNames.PaperBox);

                var socketTransform = __instance.socket switch
                {
                    SpawnPaperBoxAction.Sockets.helmat => modelHandler.GetOriginalSocketTransform(SocketNames.Helmet),
                    SpawnPaperBoxAction.Sockets.armor => modelHandler.GetOriginalSocketTransform(SocketNames.Armor),
                    _ => __instance.MainControl.transform,
                };
                customSocketMarker.OriginParent = socketTransform;
            }

            var dontHideAsEquipment = instance.GetComponent<DontHideAsEquipment>();
            if (dontHideAsEquipment == null) instance.gameObject.AddComponent<DontHideAsEquipment>();

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
