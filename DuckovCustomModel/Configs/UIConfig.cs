using UnityEngine;

namespace DuckovCustomModel.Configs
{
    public enum AnchorPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    public class UIConfig : ConfigBase
    {
        public KeyCode ToggleKey { get; set; } = KeyCode.Backslash;
        public KeyCode AnimatorParamsToggleKey { get; set; } = KeyCode.None;
        public bool ShowDCMButton { get; set; } = true;
        public AnchorPosition DCMButtonAnchor { get; set; } = AnchorPosition.TopLeft;
        public float DCMButtonOffsetX { get; set; } = 10f;
        public float DCMButtonOffsetY { get; set; } = -10f;

        public override void LoadDefault()
        {
            ToggleKey = KeyCode.Backslash;
            AnimatorParamsToggleKey = KeyCode.None;
            ShowDCMButton = true;
            DCMButtonAnchor = AnchorPosition.TopLeft;
            DCMButtonOffsetX = 10f;
            DCMButtonOffsetY = -10f;
        }

        public override bool Validate()
        {
            return false;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not UIConfig otherSetting) return;
            ToggleKey = otherSetting.ToggleKey;
            AnimatorParamsToggleKey = otherSetting.AnimatorParamsToggleKey;
            ShowDCMButton = otherSetting.ShowDCMButton;
            DCMButtonAnchor = otherSetting.DCMButtonAnchor;
            DCMButtonOffsetX = otherSetting.DCMButtonOffsetX;
            DCMButtonOffsetY = otherSetting.DCMButtonOffsetY;
        }
    }
}
