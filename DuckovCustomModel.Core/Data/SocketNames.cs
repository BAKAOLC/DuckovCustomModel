using System.Collections.Generic;

namespace DuckovCustomModel.Core.Data
{
    public static class SocketNames
    {
        #region External Locators

        public const string PaperBox = "PaperBoxLocator";
        public const string Carriable = "CarriableLocator";

        public static readonly List<string> ExternalSocketNames =
        [
            PaperBox,
            Carriable,
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

        public static readonly List<string> InternalSocketNames =
        [
            LeftHand,
            RightHand,
            Armor,
            Helmet,
            Face,
            Backpack,
            MeleeWeapon,
            PopText,
        ];

        #endregion
    }
}
