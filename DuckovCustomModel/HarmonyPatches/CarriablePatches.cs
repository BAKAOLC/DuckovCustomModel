using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;
using HarmonyLib;
using UnityEngine;

namespace DuckovCustomModel.HarmonyPatches
{
    [HarmonyPatch]
    internal static class CarriablePatches
    {
        [HarmonyPatch(typeof(Carriable), nameof(Carriable.Take))]
        [HarmonyPostfix]
        // ReSharper disable InconsistentNaming
        private static void Carriable_Take_Postfix(Rigidbody ___rb, CA_Carry _carrier)
            // ReSharper restore InconsistentNaming
        {
            if (___rb == null || _carrier == null)
                return;

            var targetCharacter = _carrier.characterController;
            if (targetCharacter == null)
                return;

            var customSocketMarker = ___rb.GetComponent<CustomSocketMarker>();
            if (customSocketMarker == null)
            {
                customSocketMarker = ___rb.gameObject.AddComponent<CustomSocketMarker>();
                customSocketMarker.AddCustomSocketName(SocketNames.Carriable);
                customSocketMarker.OriginParent = ___rb.transform.parent;
                customSocketMarker.SocketOffset = ___rb.transform.localPosition;
                customSocketMarker.SocketRotation = ___rb.transform.localRotation;
                customSocketMarker.SocketScale = ___rb.transform.localScale;
            }

            var dontHideAsEquipment = ___rb.GetComponent<DontHideAsEquipment>();
            if (dontHideAsEquipment == null)
                ___rb.gameObject.AddComponent<DontHideAsEquipment>();

            var modelHandler = targetCharacter.GetComponent<ModelHandler>();
            if (modelHandler == null || !modelHandler.IsInitialized)
                return;
            modelHandler.RegisterCustomSocketObject(___rb.gameObject);
        }

        [HarmonyPatch(typeof(Carriable), nameof(Carriable.Drop))]
        [HarmonyPostfix]
        // ReSharper disable InconsistentNaming
        private static void Carriable_Drop_Postfix(Rigidbody ___rb, CA_Carry ___carrier)
            // ReSharper restore InconsistentNaming
        {
            if (___rb == null || ___carrier == null)
                return;

            var targetCharacter = ___carrier.characterController;
            if (targetCharacter == null)
                return;

            var modelHandler = targetCharacter.GetComponent<ModelHandler>();
            if (modelHandler == null || !modelHandler.IsInitialized)
                return;

            modelHandler.UnregisterCustomSocketObject(___rb.gameObject);
        }
    }
}
