using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DuckovCustomModel.Core.Data
{
    public class AnimatorParamInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Hash { get; set; }
        public string Type { get; set; } = string.Empty;
        public object? InitialValue { get; set; }
    }

    public static class CustomAnimatorHash
    {
        public static readonly int CurrentCharacterType = Animator.StringToHash("CurrentCharacterType"); // int

        public static readonly int Grounded = Animator.StringToHash("Grounded"); // bool
        public static readonly int Die = Animator.StringToHash("Die"); // bool
        public static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed"); // float
        public static readonly int MoveDirX = Animator.StringToHash("MoveDirX"); // float
        public static readonly int MoveDirY = Animator.StringToHash("MoveDirY"); // float
        public static readonly int IsMoving = Animator.StringToHash("Moving"); // bool
        public static readonly int IsRunning = Animator.StringToHash("Running"); // bool
        public static readonly int Dashing = Animator.StringToHash("Dashing"); // bool
        public static readonly int Attack = Animator.StringToHash("Attack"); // trigger
        public static readonly int Shoot = Animator.StringToHash("Shoot"); // trigger
        public static readonly int HandState = Animator.StringToHash("HandState"); // int
        public static readonly int GunReady = Animator.StringToHash("GunReady"); // bool
        public static readonly int GunState = Animator.StringToHash("GunState"); // int
        public static readonly int ShootMode = Animator.StringToHash("ShootMode"); // int
        public static readonly int Loaded = Animator.StringToHash("Loaded"); // bool
        public static readonly int Reloading = Animator.StringToHash("Reloading"); // bool
        public static readonly int AmmoRate = Animator.StringToHash("AmmoRate"); // float
        public static readonly int RightHandOut = Animator.StringToHash("RightHandOut"); // bool

        public static readonly int HealthRate = Animator.StringToHash("HealthRate"); // float
        public static readonly int WaterRate = Animator.StringToHash("WaterRate"); // float
        public static readonly int WeightState = Animator.StringToHash("WeightState"); // int
        public static readonly int WeightRate = Animator.StringToHash("WeightRate"); // float

        public static readonly int HideOriginalEquipment = Animator.StringToHash("HideOriginalEquipment"); // bool
        public static readonly int WeaponInLocator = Animator.StringToHash("WeaponInLocator"); // int
        public static readonly int LeftHandEquip = Animator.StringToHash("LeftHandEquip"); // bool
        public static readonly int RightHandEquip = Animator.StringToHash("RightHandEquip"); // bool
        public static readonly int ArmorEquip = Animator.StringToHash("ArmorEquip"); // bool
        public static readonly int HelmetEquip = Animator.StringToHash("HelmetEquip"); // bool
        public static readonly int HeadsetEquip = Animator.StringToHash("HeadsetEquip"); // bool
        public static readonly int FaceEquip = Animator.StringToHash("FaceEquip"); // bool
        public static readonly int BackpackEquip = Animator.StringToHash("BackpackEquip"); // bool
        public static readonly int MeleeWeaponEquip = Animator.StringToHash("MeleeWeaponEquip"); // bool
        public static readonly int HavePopText = Animator.StringToHash("HavePopText"); // bool

        public static readonly int LeftHandTypeID = Animator.StringToHash("LeftHandTypeID"); // int
        public static readonly int RightHandTypeID = Animator.StringToHash("RightHandTypeID"); // int
        public static readonly int ArmorTypeID = Animator.StringToHash("ArmorTypeID"); // int
        public static readonly int HelmetTypeID = Animator.StringToHash("HelmetTypeID"); // int
        public static readonly int HeadsetTypeID = Animator.StringToHash("HeadsetTypeID"); // int
        public static readonly int FaceTypeID = Animator.StringToHash("FaceTypeID"); // int
        public static readonly int BackpackTypeID = Animator.StringToHash("BackpackTypeID"); // int
        public static readonly int MeleeWeaponTypeID = Animator.StringToHash("MeleeWeaponTypeID"); // int

        public static readonly int Hidden = Animator.StringToHash("Hidden"); // bool
        public static readonly int VelocityMagnitude = Animator.StringToHash("VelocityMagnitude"); // float
        public static readonly int VelocityX = Animator.StringToHash("VelocityX"); // float
        public static readonly int VelocityY = Animator.StringToHash("VelocityY"); // float
        public static readonly int VelocityZ = Animator.StringToHash("VelocityZ"); // float
        public static readonly int AimDirX = Animator.StringToHash("AimDirX"); // float
        public static readonly int AimDirY = Animator.StringToHash("AimDirY"); // float
        public static readonly int AimDirZ = Animator.StringToHash("AimDirZ"); // float
        public static readonly int ThermalOn = Animator.StringToHash("ThermalOn"); // bool
        public static readonly int InAds = Animator.StringToHash("InAds"); // bool
        public static readonly int AdsValue = Animator.StringToHash("AdsValue"); // float
        public static readonly int AimType = Animator.StringToHash("AimType"); // int
        public static readonly int ActionRunning = Animator.StringToHash("ActionRunning"); // bool
        public static readonly int ActionProgress = Animator.StringToHash("ActionProgress"); // float
        public static readonly int ActionPriority = Animator.StringToHash("ActionPriority"); // int

        public static readonly int Weather = Animator.StringToHash("Weather"); // int
        public static readonly int Time = Animator.StringToHash("Time"); // float
        public static readonly int TimePhase = Animator.StringToHash("TimePhase"); // int

        public static List<AnimatorParamInfo> GetAllParams()
        {
            return new List<AnimatorParamInfo>
            {
                new() { Name = "CurrentCharacterType", Hash = CurrentCharacterType, Type = "int", InitialValue = 0 },
                new() { Name = "Grounded", Hash = Grounded, Type = "bool", InitialValue = false },
                new() { Name = "Die", Hash = Die, Type = "bool", InitialValue = false },
                new() { Name = "MoveSpeed", Hash = MoveSpeed, Type = "float", InitialValue = 0f },
                new() { Name = "MoveDirX", Hash = MoveDirX, Type = "float", InitialValue = 0f },
                new() { Name = "MoveDirY", Hash = MoveDirY, Type = "float", InitialValue = 0f },
                new() { Name = "Moving", Hash = IsMoving, Type = "bool", InitialValue = false },
                new() { Name = "Running", Hash = IsRunning, Type = "bool", InitialValue = false },
                new() { Name = "Dashing", Hash = Dashing, Type = "bool", InitialValue = false },
                new() { Name = "Attack", Hash = Attack, Type = "trigger", InitialValue = null },
                new() { Name = "Shoot", Hash = Shoot, Type = "trigger", InitialValue = null },
                new() { Name = "HandState", Hash = HandState, Type = "int", InitialValue = 0 },
                new() { Name = "GunReady", Hash = GunReady, Type = "bool", InitialValue = false },
                new() { Name = "GunState", Hash = GunState, Type = "int", InitialValue = -1 },
                new() { Name = "ShootMode", Hash = ShootMode, Type = "int", InitialValue = -1 },
                new() { Name = "Loaded", Hash = Loaded, Type = "bool", InitialValue = false },
                new() { Name = "Reloading", Hash = Reloading, Type = "bool", InitialValue = false },
                new() { Name = "AmmoRate", Hash = AmmoRate, Type = "float", InitialValue = 0f },
                new() { Name = "RightHandOut", Hash = RightHandOut, Type = "bool", InitialValue = false },
                new() { Name = "HealthRate", Hash = HealthRate, Type = "float", InitialValue = 0f },
                new() { Name = "WaterRate", Hash = WaterRate, Type = "float", InitialValue = 0f },
                new() { Name = "WeightState", Hash = WeightState, Type = "int", InitialValue = 0 },
                new() { Name = "WeightRate", Hash = WeightRate, Type = "float", InitialValue = 0f },
                new()
                {
                    Name = "HideOriginalEquipment", Hash = HideOriginalEquipment, Type = "bool", InitialValue = false,
                },
                new() { Name = "WeaponInLocator", Hash = WeaponInLocator, Type = "int", InitialValue = 0 },
                new() { Name = "LeftHandEquip", Hash = LeftHandEquip, Type = "bool", InitialValue = false },
                new() { Name = "RightHandEquip", Hash = RightHandEquip, Type = "bool", InitialValue = false },
                new() { Name = "ArmorEquip", Hash = ArmorEquip, Type = "bool", InitialValue = false },
                new() { Name = "HelmetEquip", Hash = HelmetEquip, Type = "bool", InitialValue = false },
                new() { Name = "HeadsetEquip", Hash = HeadsetEquip, Type = "bool", InitialValue = false },
                new() { Name = "FaceEquip", Hash = FaceEquip, Type = "bool", InitialValue = false },
                new() { Name = "BackpackEquip", Hash = BackpackEquip, Type = "bool", InitialValue = false },
                new() { Name = "MeleeWeaponEquip", Hash = MeleeWeaponEquip, Type = "bool", InitialValue = false },
                new() { Name = "HavePopText", Hash = HavePopText, Type = "bool", InitialValue = false },
                new() { Name = "LeftHandTypeID", Hash = LeftHandTypeID, Type = "int", InitialValue = 0 },
                new() { Name = "RightHandTypeID", Hash = RightHandTypeID, Type = "int", InitialValue = 0 },
                new() { Name = "ArmorTypeID", Hash = ArmorTypeID, Type = "int", InitialValue = 0 },
                new() { Name = "HelmetTypeID", Hash = HelmetTypeID, Type = "int", InitialValue = 0 },
                new() { Name = "HeadsetTypeID", Hash = HeadsetTypeID, Type = "int", InitialValue = 0 },
                new() { Name = "FaceTypeID", Hash = FaceTypeID, Type = "int", InitialValue = 0 },
                new() { Name = "BackpackTypeID", Hash = BackpackTypeID, Type = "int", InitialValue = 0 },
                new() { Name = "MeleeWeaponTypeID", Hash = MeleeWeaponTypeID, Type = "int", InitialValue = 0 },
                new() { Name = "Hidden", Hash = Hidden, Type = "bool", InitialValue = false },
                new() { Name = "VelocityMagnitude", Hash = VelocityMagnitude, Type = "float", InitialValue = 0f },
                new() { Name = "VelocityX", Hash = VelocityX, Type = "float", InitialValue = 0f },
                new() { Name = "VelocityY", Hash = VelocityY, Type = "float", InitialValue = 0f },
                new() { Name = "VelocityZ", Hash = VelocityZ, Type = "float", InitialValue = 0f },
                new() { Name = "AimDirX", Hash = AimDirX, Type = "float", InitialValue = 0f },
                new() { Name = "AimDirY", Hash = AimDirY, Type = "float", InitialValue = 0f },
                new() { Name = "AimDirZ", Hash = AimDirZ, Type = "float", InitialValue = 0f },
                new() { Name = "ThermalOn", Hash = ThermalOn, Type = "bool", InitialValue = false },
                new() { Name = "InAds", Hash = InAds, Type = "bool", InitialValue = false },
                new() { Name = "AdsValue", Hash = AdsValue, Type = "float", InitialValue = 0f },
                new() { Name = "AimType", Hash = AimType, Type = "int", InitialValue = 0 },
                new() { Name = "ActionRunning", Hash = ActionRunning, Type = "bool", InitialValue = false },
                new() { Name = "ActionProgress", Hash = ActionProgress, Type = "float", InitialValue = 0f },
                new() { Name = "ActionPriority", Hash = ActionPriority, Type = "int", InitialValue = 0 },
                new() { Name = "Weather", Hash = Weather, Type = "int", InitialValue = -1 },
                new() { Name = "Time", Hash = Time, Type = "float", InitialValue = -1f },
                new() { Name = "TimePhase", Hash = TimePhase, Type = "int", InitialValue = -1 },
            }.OrderBy(p => p.Name).ToList();
        }
    }
}
