using System.Collections.Generic;
using System.Reflection;
using DuckovCustomModel.Core.Data;
using HarmonyLib;
using UnityEngine;

namespace DuckovCustomModel.Utils
{
    public static class CharacterModelSocketUtils
    {
        public static IReadOnlyDictionary<string, FieldInfo> AllSocketFields => new Dictionary<string, FieldInfo>
        {
            { SocketNames.LeftHand, LeftHandSocket },
            { SocketNames.RightHand, RightHandSocket },
            { SocketNames.Armor, ArmorSocket },
            { SocketNames.Helmet, HelmetSocket },
            { SocketNames.Face, FaceSocket },
            { SocketNames.Backpack, BackpackSocket },
            { SocketNames.MeleeWeapon, MeleeWeaponSocket },
            { SocketNames.PopText, PopTextSocket },
        };

        // ReSharper disable once StringLiteralTypo
        public static FieldInfo LeftHandSocket { get; } = AccessTools.Field(typeof(CharacterModel), "lefthandSocket");

        public static FieldInfo RightHandSocket { get; } = AccessTools.Field(typeof(CharacterModel), "rightHandSocket");

        public static FieldInfo ArmorSocket { get; } = AccessTools.Field(typeof(CharacterModel), "armorSocket");

        // ReSharper disable once StringLiteralTypo
        public static FieldInfo HelmetSocket { get; } = AccessTools.Field(typeof(CharacterModel), "helmatSocket");

        public static FieldInfo FaceSocket { get; } = AccessTools.Field(typeof(CharacterModel), "faceSocket");

        public static FieldInfo BackpackSocket { get; } = AccessTools.Field(typeof(CharacterModel), "backpackSocket");

        public static FieldInfo MeleeWeaponSocket { get; } =
            AccessTools.Field(typeof(CharacterModel), "meleeWeaponSocket");

        public static FieldInfo PopTextSocket { get; } = AccessTools.Field(typeof(CharacterModel), "popTextSocket");

        public static Transform? GetLeftHandSocket(CharacterModel characterModel)
        {
            return LeftHandSocket.GetValue(characterModel) as Transform;
        }

        public static Transform? GetRightHandSocket(CharacterModel characterModel)
        {
            return RightHandSocket.GetValue(characterModel) as Transform;
        }

        public static Transform? GetArmorSocket(CharacterModel characterModel)
        {
            return ArmorSocket.GetValue(characterModel) as Transform;
        }

        public static Transform? GetHelmetSocket(CharacterModel characterModel)
        {
            return HelmetSocket.GetValue(characterModel) as Transform;
        }

        public static Transform? GetFaceSocket(CharacterModel characterModel)
        {
            return FaceSocket.GetValue(characterModel) as Transform;
        }

        public static Transform? GetBackpackSocket(CharacterModel characterModel)
        {
            return BackpackSocket.GetValue(characterModel) as Transform;
        }

        public static Transform? GetMeleeWeaponSocket(CharacterModel characterModel)
        {
            return MeleeWeaponSocket.GetValue(characterModel) as Transform;
        }

        public static Transform? GetPopTextSocket(CharacterModel characterModel)
        {
            return PopTextSocket.GetValue(characterModel) as Transform;
        }
    }
}
