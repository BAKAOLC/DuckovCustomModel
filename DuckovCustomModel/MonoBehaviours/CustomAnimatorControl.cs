using System.Collections.Generic;
using Duckov;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Utils;
using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    public class CustomAnimatorControl : MonoBehaviour
    {
        private readonly Dictionary<int, bool> _boolParams = new();

        private readonly Dictionary<int, float> _floatParams = new();
        private readonly Dictionary<int, int> _intParams = new();

        private bool _attacking;
        private int _attackLayerIndex = -1;
        private float _attackTimer;
        private float _attackWeight;
        private CharacterMainControl? _characterMainControl;
        private CharacterModel? _characterModel;
        private Animator? _customAnimator;
        private ItemAgent_Gun? _gunAgent;
        private DuckovItemAgent? _holdAgent;
        private bool _initialized;
        private ModelHandler? _modelHandler;

        private bool HasAnimationIfDashCanControl
        {
            get
            {
                if (_modelHandler == null)
                    return false;
                return _modelHandler.OriginalAnimationControl != null &&
                       _modelHandler.OriginalAnimationControl.hasAnimationIfDashCanControl;
            }
        }

        private AnimationCurve? AttackLayerWeightCurve
        {
            get
            {
                if (_modelHandler == null)
                    return null;
                if (_modelHandler.OriginalAnimationControl != null)
                    return _modelHandler.OriginalAnimationControl.attackLayerWeightCurve;
                return _modelHandler.OriginalMagicBlendAnimationControl != null
                    ? _modelHandler.OriginalMagicBlendAnimationControl.attackLayerWeightCurve
                    : null;
            }
        }

        private float AttackTime
        {
            get
            {
                if (_modelHandler == null)
                    return 0.3f;
                if (_modelHandler.OriginalAnimationControl != null)
                    return _modelHandler.OriginalAnimationControl.attackTime;
                return _modelHandler.OriginalMagicBlendAnimationControl != null
                    ? _modelHandler.OriginalMagicBlendAnimationControl.attackTime
                    : 0.3f;
            }
        }

        private void Update()
        {
            if (!_initialized) return;
            if (_characterMainControl == null)
                return;

            UpdateDeadState();
            UpdateMovement();
            UpdateVelocityAndAim();
            UpdateCharacterStatus();
            UpdateCharacterType();
            UpdateHandState();
            UpdateGunState();
            UpdateEquipmentState();
            UpdateEquipmentTypeID();
            UpdateAttackLayerWeight();
            UpdateActionState();
            UpdateTimeAndWeather();
        }

        private void OnDestroy()
        {
            if (_characterModel != null) _characterModel.OnAttackOrShootEvent -= OnAttack;
            if (_characterMainControl != null) _characterMainControl.OnHoldAgentChanged -= OnHoldAgentChanged;
            UnsubscribeGunEvents();
        }

        public void Initialize(ModelHandler modelHandler)
        {
            if (modelHandler == null) return;

            switch (_initialized)
            {
                case true when _modelHandler == modelHandler:
                    return;
                case true:
                {
                    if (_characterModel != null) _characterModel.OnAttackOrShootEvent -= OnAttack;
                    if (_characterMainControl != null) _characterMainControl.OnHoldAgentChanged -= OnHoldAgentChanged;
                    UnsubscribeGunEvents();
                    break;
                }
            }

            _modelHandler = modelHandler;
            _characterMainControl = modelHandler.CharacterMainControl;
            _characterModel = modelHandler.OriginalCharacterModel;
            if (_characterMainControl == null || _characterModel == null)
                return;

            _characterModel.OnAttackOrShootEvent += OnAttack;
            _characterMainControl.OnHoldAgentChanged += OnHoldAgentChanged;

            _customAnimator = modelHandler.CustomAnimator;
            FindMeleeAttackLayerIndex();

            _initialized = true;

            OnHoldAgentChanged(_characterMainControl.CurrentHoldItemAgent);
        }

        public void SetCustomAnimator(Animator? animator)
        {
            _customAnimator = animator;
            _attackLayerIndex = -1;
            if (_customAnimator != null) FindMeleeAttackLayerIndex();
        }

        private void SetAnimatorFloat(int hash, float value)
        {
            _floatParams[hash] = value;
            if (_customAnimator != null)
                _customAnimator.SetFloat(hash, value);
        }

        private void SetAnimatorInteger(int hash, int value)
        {
            _intParams[hash] = value;
            if (_customAnimator != null)
                _customAnimator.SetInteger(hash, value);
        }

        private void SetAnimatorBool(int hash, bool value)
        {
            _boolParams[hash] = value;
            if (_customAnimator != null)
                _customAnimator.SetBool(hash, value);
        }

        private void SetAnimatorTrigger(int hash)
        {
            if (_customAnimator != null)
                _customAnimator.SetTrigger(hash);
        }

        public float GetParameterFloat(int hash)
        {
            return _floatParams.GetValueOrDefault(hash, 0f);
        }

        public int GetParameterInteger(int hash)
        {
            return _intParams.GetValueOrDefault(hash, 0);
        }

        public bool GetParameterBool(int hash)
        {
            return _boolParams.TryGetValue(hash, out var value) && value;
        }

        public void UpdateDeadState()
        {
            if (!_initialized) return;
            if (_characterMainControl == null)
                return;

            if (_characterMainControl.Health == null)
                return;

            var isDead = _characterMainControl.Health.IsDead;
            SetAnimatorBool(CustomAnimatorHash.Die, isDead);
        }

        private void UpdateMovement()
        {
            if (!_initialized) return;
            if (_characterMainControl == null)
                return;

            SetAnimatorFloat(CustomAnimatorHash.MoveSpeed, _characterMainControl.AnimationMoveSpeedValue);

            var moveDirectionValue = _characterMainControl.AnimationLocalMoveDirectionValue;
            SetAnimatorFloat(CustomAnimatorHash.MoveDirX, moveDirectionValue.x);
            SetAnimatorFloat(CustomAnimatorHash.MoveDirY, moveDirectionValue.y);

            SetAnimatorBool(CustomAnimatorHash.Grounded, _characterMainControl.IsOnGround);

            var movementControl = _characterMainControl.movementControl;
            SetAnimatorBool(CustomAnimatorHash.IsMoving, movementControl.Moving);
            SetAnimatorBool(CustomAnimatorHash.IsRunning, movementControl.Running);

            var dashing = _characterMainControl.Dashing;
            if (dashing && !HasAnimationIfDashCanControl && _characterMainControl.DashCanControl)
                dashing = false;
            SetAnimatorBool(CustomAnimatorHash.Dashing, dashing);
        }

        private void UpdateCharacterType()
        {
            if (!_initialized) return;
            if (_modelHandler == null)
                return;

            var characterType = (int)_modelHandler.Target;
            SetAnimatorInteger(CustomAnimatorHash.CurrentCharacterType, characterType);
        }

        private void UpdateVelocityAndAim()
        {
            if (!_initialized) return;
            if (_characterMainControl == null)
                return;

            var velocity = _characterMainControl.Velocity;
            SetAnimatorFloat(CustomAnimatorHash.VelocityMagnitude, velocity.magnitude);
            SetAnimatorFloat(CustomAnimatorHash.VelocityX, velocity.x);
            SetAnimatorFloat(CustomAnimatorHash.VelocityY, velocity.y);
            SetAnimatorFloat(CustomAnimatorHash.VelocityZ, velocity.z);

            var aimDir = _characterMainControl.CurrentAimDirection;
            SetAnimatorFloat(CustomAnimatorHash.AimDirX, aimDir.x);
            SetAnimatorFloat(CustomAnimatorHash.AimDirY, aimDir.y);
            SetAnimatorFloat(CustomAnimatorHash.AimDirZ, aimDir.z);

            var inAds = _characterMainControl.IsInAdsInput;
            SetAnimatorBool(CustomAnimatorHash.InAds, inAds);

            var adsValue = _characterMainControl.AdsValue;
            SetAnimatorFloat(CustomAnimatorHash.AdsValue, adsValue);

            var aimType = (int)_characterMainControl.AimType;
            SetAnimatorInteger(CustomAnimatorHash.AimType, aimType);
        }

        private void UpdateCharacterStatus()
        {
            if (!_initialized) return;
            if (_characterMainControl == null)
                return;

            var hidden = _characterMainControl.Hidden;
            SetAnimatorBool(CustomAnimatorHash.Hidden, hidden);

            if (_characterMainControl.Health != null)
            {
                var currentHealth = _characterMainControl.Health.CurrentHealth;
                var maxHealth = _characterMainControl.Health.MaxHealth;
                var healthRate = maxHealth > 0 ? currentHealth / maxHealth : 0.0f;
                SetAnimatorFloat(CustomAnimatorHash.HealthRate, healthRate);
            }
            else
            {
                SetAnimatorFloat(CustomAnimatorHash.HealthRate, 1.0f);
            }

            var currentWater = _characterMainControl.CurrentWater;
            var maxWater = _characterMainControl.MaxWater;
            if (maxWater > 0)
            {
                var waterRate = currentWater / maxWater;
                SetAnimatorFloat(CustomAnimatorHash.WaterRate, waterRate);
            }
            else
            {
                SetAnimatorFloat(CustomAnimatorHash.WaterRate, 1.0f);
            }

            var totalWeight = _characterMainControl.CharacterItem.TotalWeight;
            if (_characterMainControl.carryAction.Running)
                totalWeight += _characterMainControl.carryAction.GetWeight();

            var weightRate = totalWeight / _characterMainControl.MaxWeight;
            SetAnimatorFloat(CustomAnimatorHash.WeightRate, weightRate);

            int weightState;
            if (!LevelManager.Instance.IsRaidMap)
                weightState = (int)CharacterMainControl.WeightStates.normal;
            else
                weightState = totalWeight switch
                {
                    > 1 => (int)CharacterMainControl.WeightStates.overWeight,
                    > 0.75f => (int)CharacterMainControl.WeightStates.superHeavy,
                    > 0.25f => (int)CharacterMainControl.WeightStates.normal,
                    _ => (int)CharacterMainControl.WeightStates.light,
                };
            SetAnimatorInteger(CustomAnimatorHash.WeightState, weightState);
        }

        private void UpdateEquipmentState()
        {
            if (!_initialized) return;
            if (_characterMainControl == null || _characterModel == null)
                return;

            var thermalOn = _characterMainControl.ThermalOn;
            SetAnimatorBool(CustomAnimatorHash.ThermalOn, thermalOn);

            var hideOriginalEquipment = false;
            if (ModEntry.HideEquipmentConfig != null && _modelHandler != null)
            {
                if (_modelHandler.Target == ModelTarget.AICharacter)
                {
                    var nameKey = _characterMainControl?.characterPreset?.nameKey;
                    if (!string.IsNullOrEmpty(nameKey))
                        hideOriginalEquipment = ModEntry.HideEquipmentConfig
                            .GetHideAICharacterEquipment(nameKey);
                }
                else
                {
                    hideOriginalEquipment =
                        ModEntry.HideEquipmentConfig.GetHideEquipment(_modelHandler.Target);
                }
            }

            SetAnimatorBool(CustomAnimatorHash.HideOriginalEquipment, hideOriginalEquipment);

            var popTextSocket = CharacterModelSocketUtils.GetPopTextSocket(_characterModel);
            var havePopText = popTextSocket != null && popTextSocket.childCount > 0;
            SetAnimatorBool(CustomAnimatorHash.HavePopText, havePopText);
        }

        private void UpdateEquipmentTypeID()
        {
            if (!_initialized) return;
            if (_characterMainControl == null || _characterModel == null)
                return;

            #region Armor/Helmet/Face/Backpack/Headset

            var characterItemSlots = _characterMainControl.CharacterItem.Slots;
            var armorSlot = characterItemSlots.GetSlot(CharacterEquipmentController.armorHash);
            var helmetSlot = characterItemSlots.GetSlot(CharacterEquipmentController.helmatHash);
            var faceSlot = characterItemSlots.GetSlot(CharacterEquipmentController.faceMaskHash);
            var backpackSlot = characterItemSlots.GetSlot(CharacterEquipmentController.backpackHash);
            var headsetSlot = characterItemSlots.GetSlot(CharacterEquipmentController.headsetHash);

            var armorTypeID = armorSlot?.Content != null ? armorSlot.Content.TypeID : 0;
            var helmetTypeID = helmetSlot?.Content != null ? helmetSlot.Content.TypeID : 0;
            var faceTypeID = faceSlot?.Content != null ? faceSlot.Content.TypeID : 0;
            var backpackTypeID = backpackSlot?.Content != null ? backpackSlot.Content.TypeID : 0;
            var headsetTypeID = headsetSlot?.Content != null ? headsetSlot.Content.TypeID : 0;

            SetAnimatorInteger(CustomAnimatorHash.ArmorTypeID, armorTypeID);
            SetAnimatorBool(CustomAnimatorHash.ArmorEquip, armorTypeID > 0);

            SetAnimatorInteger(CustomAnimatorHash.HelmetTypeID, helmetTypeID);
            SetAnimatorBool(CustomAnimatorHash.HelmetEquip, helmetTypeID > 0);

            SetAnimatorInteger(CustomAnimatorHash.FaceTypeID, faceTypeID);
            SetAnimatorBool(CustomAnimatorHash.FaceEquip, faceTypeID > 0);

            SetAnimatorInteger(CustomAnimatorHash.BackpackTypeID, backpackTypeID);
            SetAnimatorBool(CustomAnimatorHash.BackpackEquip, backpackTypeID > 0);

            SetAnimatorInteger(CustomAnimatorHash.HeadsetTypeID, headsetTypeID);
            SetAnimatorBool(CustomAnimatorHash.HeadsetEquip, headsetTypeID > 0);

            #endregion
        }

        private void UpdateHandState()
        {
            if (!_initialized) return;
            if (_characterMainControl == null)
                return;

            var handState = 0;
            var rightHandOut = true;

            if (_holdAgent == null || !_holdAgent.isActiveAndEnabled)
                _holdAgent = _characterMainControl.CurrentHoldItemAgent;
            else
                handState = (int)_holdAgent.handAnimationType;
            if (_characterMainControl.carryAction.Running)
                handState = -1;

            if (_holdAgent == null || !_holdAgent.gameObject.activeSelf || _characterMainControl.reloadAction.Running)
                rightHandOut = false;

            SetAnimatorInteger(CustomAnimatorHash.HandState, handState);
            SetAnimatorBool(CustomAnimatorHash.RightHandOut, rightHandOut);
        }

        private void UpdateGunState()
        {
            if (!_initialized) return;
            if (_characterMainControl == null)
                return;

            if (_holdAgent != null && _gunAgent == null)
                _gunAgent = _holdAgent as ItemAgent_Gun;

            var isGunReady = false;
            var isReloading = false;
            var ammoRate = 0.0f;
            var shootMode = -1;
            var gunState = -1;
            if (_gunAgent != null)
            {
                isReloading = _gunAgent.IsReloading();
                isGunReady = _gunAgent.BulletCount > 0 && !isReloading;
                shootMode = (int)_gunAgent.GunItemSetting.triggerMode;
                gunState = (int)_gunAgent.GunState;
                var maxAmmo = _gunAgent.Capacity;
                if (maxAmmo > 0)
                    ammoRate = (float)_gunAgent.BulletCount / maxAmmo;
            }

            SetAnimatorInteger(CustomAnimatorHash.GunState, gunState);
            SetAnimatorInteger(CustomAnimatorHash.ShootMode, shootMode);
            SetAnimatorFloat(CustomAnimatorHash.AmmoRate, ammoRate);
            SetAnimatorBool(CustomAnimatorHash.Reloading, isReloading);
            SetAnimatorBool(CustomAnimatorHash.GunReady, isGunReady);
        }

        private void UpdateAttackLayerWeight()
        {
            if (!_attacking)
            {
                if (_attackWeight <= 0) return;
                _attackWeight = 0;
                SetMeleeAttackLayerWeight(_attackWeight);
                return;
            }

            _attackTimer += Time.deltaTime;
            var attackTime = AttackTime;
            var attackProgress = attackTime > 0 ? Mathf.Clamp01(_attackTimer / attackTime) : 0.0f;
            _attackWeight = AttackLayerWeightCurve?.Evaluate(attackProgress) ?? 0.0f;
            if (_attackTimer >= attackTime)
            {
                _attacking = false;
                _attackWeight = 0.0f;
            }

            SetMeleeAttackLayerWeight(_attackWeight);
        }

        private void UpdateActionState()
        {
            if (!_initialized) return;
            if (_characterMainControl == null)
                return;

            var currentAction = _characterMainControl.CurrentAction;
            var isActionRunning = false;
            var actionProgress = 0.0f;
            var actionPriority = 0;
            if (currentAction != null)
            {
                isActionRunning = currentAction.Running;
                if (currentAction is IProgress progressAction)
                    actionProgress = progressAction.GetProgress().progress;
                actionPriority = (int)currentAction.ActionPriority();
            }

            SetAnimatorBool(CustomAnimatorHash.ActionRunning, isActionRunning);
            SetAnimatorFloat(CustomAnimatorHash.ActionProgress, actionProgress);
            SetAnimatorInteger(CustomAnimatorHash.ActionPriority, actionPriority);
        }

        private void UpdateTimeAndWeather()
        {
            if (!_initialized) return;

            var timeOfDayController = TimeOfDayController.Instance;
            if (timeOfDayController == null)
            {
                SetAnimatorFloat(CustomAnimatorHash.Time, -1f);
                SetAnimatorInteger(CustomAnimatorHash.Weather, -1);
                SetAnimatorInteger(CustomAnimatorHash.TimePhase, -1);
                return;
            }

            var time = timeOfDayController.Time;
            SetAnimatorFloat(CustomAnimatorHash.Time, time);

            var currentWeather = timeOfDayController.CurrentWeather;
            var weatherValue = (int)currentWeather;
            SetAnimatorInteger(CustomAnimatorHash.Weather, weatherValue);

            var currentPhase = timeOfDayController.CurrentPhase.timePhaseTag;
            var timePhaseValue = (int)currentPhase;
            SetAnimatorInteger(CustomAnimatorHash.TimePhase, timePhaseValue);
        }

        private void FindMeleeAttackLayerIndex()
        {
            if (_customAnimator == null) return;
            if (_attackLayerIndex < 0)
                _attackLayerIndex = _customAnimator.GetLayerIndex("MeleeAttack");
            if (_attackLayerIndex >= 0)
                _customAnimator.SetLayerWeight(_attackLayerIndex, 0);
        }

        private void SetMeleeAttackLayerWeight(float weight)
        {
            if (_attackLayerIndex < 0 || _customAnimator == null)
                return;
            _customAnimator.SetLayerWeight(_attackLayerIndex, weight);
        }

        private void OnAttack()
        {
            _attacking = true;
            _attackTimer = 0.0f;
            FindMeleeAttackLayerIndex();
            SetAnimatorTrigger(CustomAnimatorHash.Attack);
        }

        private void OnHoldAgentChanged(DuckovItemAgent? agent)
        {
            UnsubscribeGunEvents();

            _holdAgent = agent;
            _gunAgent = agent as ItemAgent_Gun;

            UpdateHandParams();

            if (_gunAgent == null) return;
            _gunAgent.OnShootEvent += OnShoot;
            _gunAgent.OnLoadedEvent += OnLoaded;
        }

        private void UpdateHandParams()
        {
            if (!_initialized) return;
            if (_characterMainControl == null || _characterModel == null)
                return;

            var leftHandTypeID = 0;
            var rightHandTypeID = 0;
            var meleeWeaponTypeID = 0;
            var weaponInLocator = 0;

            var currentHoldItemAgent = _characterMainControl?.CurrentHoldItemAgent;
            if (currentHoldItemAgent != null)
                switch (currentHoldItemAgent.handheldSocket)
                {
                    case HandheldSocketTypes.leftHandSocket:
                        var leftHandSocket = CharacterModelSocketUtils.GetLeftHandSocket(_characterModel);
                        if (leftHandSocket != null)
                        {
                            leftHandTypeID = currentHoldItemAgent.Item.TypeID;
                            weaponInLocator = (int)HandheldSocketTypes.leftHandSocket;
                        }
                        else
                        {
                            rightHandTypeID = currentHoldItemAgent.Item.TypeID;
                            weaponInLocator = (int)HandheldSocketTypes.normalHandheld;
                        }

                        break;
                    case HandheldSocketTypes.meleeWeapon:
                        meleeWeaponTypeID = currentHoldItemAgent.Item.TypeID;
                        weaponInLocator = (int)HandheldSocketTypes.meleeWeapon;
                        break;
                    case HandheldSocketTypes.normalHandheld:
                    default:
                        rightHandTypeID = currentHoldItemAgent.Item.TypeID;
                        weaponInLocator = (int)HandheldSocketTypes.normalHandheld;
                        break;
                }

            SetAnimatorInteger(CustomAnimatorHash.WeaponInLocator, weaponInLocator);
            SetAnimatorInteger(CustomAnimatorHash.LeftHandTypeID, leftHandTypeID);
            SetAnimatorBool(CustomAnimatorHash.LeftHandEquip, leftHandTypeID > 0);
            SetAnimatorInteger(CustomAnimatorHash.RightHandTypeID, rightHandTypeID);
            SetAnimatorBool(CustomAnimatorHash.RightHandEquip, rightHandTypeID > 0);
            SetAnimatorInteger(CustomAnimatorHash.MeleeWeaponTypeID, meleeWeaponTypeID);
            SetAnimatorBool(CustomAnimatorHash.MeleeWeaponEquip, meleeWeaponTypeID > 0);
        }

        private void UnsubscribeGunEvents()
        {
            if (_gunAgent == null) return;
            _gunAgent.OnShootEvent -= OnShoot;
            _gunAgent.OnLoadedEvent -= OnLoaded;
        }

        private void OnShoot()
        {
            SetAnimatorTrigger(CustomAnimatorHash.Shoot);
            if (_gunAgent != null && _gunAgent.BulletCount > 0)
                return;
            SetAnimatorBool(CustomAnimatorHash.Loaded, false);
        }

        private void OnLoaded()
        {
            SetAnimatorBool(CustomAnimatorHash.Loaded, true);
        }
    }
}
