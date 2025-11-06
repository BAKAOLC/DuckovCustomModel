using UnityEngine;

namespace DuckovCustomModel.Configs
{
    public class UIConfig : ConfigBase
    {
        public KeyCode ToggleKey { get; set; } = KeyCode.Backslash;
        public bool HideOriginalEquipment { get; set; }

        public override void LoadDefault()
        {
            ToggleKey = KeyCode.Backslash;
            HideOriginalEquipment = false;
        }

        public override bool Validate()
        {
            return false;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not UIConfig otherSetting) return;
            ToggleKey = otherSetting.ToggleKey;
            HideOriginalEquipment = otherSetting.HideOriginalEquipment;
        }
    }
}