using System.Collections.Generic;
using System.Reflection;
using DuckovCustomModel.Utils;

namespace DuckovCustomModel.Data
{
    public static class SocketNames
    {
        #region External Locators

        public const string PaperBox = "PaperBoxLocator";

        public static readonly List<string> ExternalSocketNames =
        [
            PaperBox,
        ];

        #endregion

        #region Internal Locators

        public const string LeftHand = "LeftHandLocator";
        public const string RightHand = "RightHandLocator";
        public const string Armor = "ArmorLocator";
        public const string Helmet = "HelmetLocator";
        public const string Face = "FaceLocator";
        public const string Backpack = "BackpackLocator";
        public const string MeleeWeapon = "MeleeWeaponLocator";
        public const string PopText = "PopTextLocator";

        public static readonly Dictionary<FieldInfo, string> InternalSocketMap = new()
        {
            { CharacterModelSocketUtils.LeftHandSocket, LeftHand },
            { CharacterModelSocketUtils.RightHandSocket, RightHand },
            { CharacterModelSocketUtils.ArmorSocket, Armor },
            { CharacterModelSocketUtils.HelmetSocket, Helmet },
            { CharacterModelSocketUtils.FaceSocket, Face },
            { CharacterModelSocketUtils.BackpackSocket, Backpack },
            { CharacterModelSocketUtils.MeleeWeaponSocket, MeleeWeapon },
            { CharacterModelSocketUtils.PopTextSocket, PopText },
        };

        #endregion
    }
}