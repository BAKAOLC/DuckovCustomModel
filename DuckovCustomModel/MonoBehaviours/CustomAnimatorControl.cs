using System.Reflection;
using DuckovCustomModel.Utils;
using HarmonyLib;
using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    public class CustomAnimatorControl : MonoBehaviour
    {
        private static readonly FieldInfo HasAnimationIfDashCanControlField =
            AccessTools.Field(typeof(CharacterAnimationControl), "hasAnimationIfDashCanControl");

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
                if (_modelHandler.OriginalAnimationControl != null)
                    return (bool)HasAnimationIfDashCanControlField.GetValue(_modelHandler.OriginalAnimationControl)!;
                return false;
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
            if (_customAnimator == null || _characterMainControl == null)
                return;

            UpdateDeadState();
            UpdateMovement();
            UpdateState();
            UpdateHandState();
            UpdateGunState();
            UpdateEquipmentState();
            UpdateAttackLayerWeight();
        }

        private void OnDisable()
        {
            if (_characterModel == null) return;
            var equipmentSockets = new[]
            {
                CharacterModelSocketUtils.GetHelmetSocket(_characterModel),
                CharacterModelSocketUtils.GetFaceSocket(_characterModel),
                CharacterModelSocketUtils.GetArmorSocket(_characterModel),
                CharacterModelSocketUtils.GetBackpackSocket(_characterModel),
            };

            foreach (var socket in equipmentSockets)
            {
                if (socket == null) continue;
                foreach (Transform child in socket) child.gameObject.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (_characterModel != null) _characterModel.OnAttackOrShootEvent -= OnAttack;
        }

        public void Initialize(ModelHandler modelHandler)
        {
            _modelHandler = modelHandler;
            if (_modelHandler == null) return;

            _characterMainControl = modelHandler.CharacterMainControl;
            _characterModel = modelHandler.OriginalCharacterModel;
            if (_characterMainControl == null || _characterModel == null)
                return;

            _characterModel.OnAttackOrShootEvent += OnAttack;

            _customAnimator = modelHandler.CustomAnimator;
            FindMeleeAttackLayerIndex();

            _initialized = true;
        }

        public void UpdateDeadState()
        {
            if (!_initialized) return;
            if (_customAnimator == null || _characterMainControl == null)
                return;

            if (_characterMainControl.Health == null)
                return;

            var isDead = _characterMainControl.Health.IsDead;
            _customAnimator.SetBool(AnimatorDieHash, isDead);
        }

        private void UpdateMovement()
        {
            if (!_initialized) return;
            if (_customAnimator == null || _characterMainControl == null)
                return;

            _customAnimator.SetFloat(AnimatorMoveSpeedHash, _characterMainControl.AnimationMoveSpeedValue);

            var moveDirectionValue = _characterMainControl.AnimationLocalMoveDirectionValue;
            _customAnimator.SetFloat(AnimatorMoveDirXHash, moveDirectionValue.x);
            _customAnimator.SetFloat(AnimatorMoveDirYHash, moveDirectionValue.y);

            _customAnimator.SetBool(AnimatorGroundedHash, _characterMainControl.IsOnGround);

            var movementControl = _characterMainControl.movementControl;
            _customAnimator.SetBool(AnimatorIsMovingHash, movementControl.Moving);
            _customAnimator.SetBool(AnimatorIsRunningHash, movementControl.Running);

            var dashing = _characterMainControl.Dashing;
            if (dashing && !HasAnimationIfDashCanControl && _characterMainControl.DashCanControl)
                dashing = false;
            _customAnimator.SetBool(AnimatorDashingHash, dashing);
        }

        private void UpdateState()
        {
            if (!_initialized) return;
            if (_customAnimator == null || _characterMainControl == null)
                return;

            if (_characterMainControl.Health != null)
            {
                var currentHealth = _characterMainControl.Health.CurrentHealth;
                var maxHealth = _characterMainControl.Health.MaxHealth;
                var healthRate = maxHealth > 0 ? currentHealth / maxHealth : 0.0f;
                _customAnimator.SetFloat(AnimatorHealthRateHash, healthRate);
            }
            else
            {
                _customAnimator.SetFloat(AnimatorHealthRateHash, 1.0f);
            }

            var currentWater = _characterMainControl.CurrentWater;
            var maxWater = _characterMainControl.MaxWater;
            if (maxWater > 0)
            {
                var waterRate = currentWater / maxWater;
                _customAnimator.SetFloat(AnimatorWaterRateHash, waterRate);
            }
            else
            {
                _customAnimator.SetFloat(AnimatorWaterRateHash, 1.0f);
            }

            var totalWeight = _characterMainControl.CharacterItem.TotalWeight;
            if (_characterMainControl.carryAction.Running)
                totalWeight += _characterMainControl.carryAction.GetWeight();

            var weightRate = totalWeight / _characterMainControl.MaxWeight;
            _customAnimator.SetFloat(AnimatorWeightRateHash, weightRate);

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
            _customAnimator.SetInteger(AnimatorWeightStateHash, weightState);
        }

        private void UpdateEquipmentState()
        {
            if (!_initialized) return;
            if (_customAnimator == null || _characterMainControl == null || _characterModel == null)
                return;

            var hideOriginalEquipment = ModBehaviour.Instance?.UIConfig?.HideOriginalEquipment ?? false;
            _customAnimator.SetBool(AnimatorHideOriginalEquipmentHash, hideOriginalEquipment);

            var leftHandSocket = CharacterModelSocketUtils.GetLeftHandSocket(_characterModel);
            var rightHandSocket = CharacterModelSocketUtils.GetRightHandSocket(_characterModel);
            var armorSocket = CharacterModelSocketUtils.GetArmorSocket(_characterModel);
            var helmetSocket = CharacterModelSocketUtils.GetHelmetSocket(_characterModel);
            var faceSocket = CharacterModelSocketUtils.GetFaceSocket(_characterModel);
            var backpackSocket = CharacterModelSocketUtils.GetBackpackSocket(_characterModel);
            var meleeWeaponSocket = CharacterModelSocketUtils.GetMeleeWeaponSocket(_characterModel);
            var popTextSocket = CharacterModelSocketUtils.GetPopTextSocket(_characterModel);

            _customAnimator.SetBool(AnimatorLeftHandEquipHash, leftHandSocket != null && leftHandSocket.childCount > 0);
            _customAnimator.SetBool(AnimatorRightHandEquipHash,
                rightHandSocket != null && rightHandSocket.childCount > 0);
            _customAnimator.SetBool(AnimatorArmorEquipHash, armorSocket != null && armorSocket.childCount > 0);
            _customAnimator.SetBool(AnimatorHelmetEquipHash, helmetSocket != null && helmetSocket.childCount > 0);
            _customAnimator.SetBool(AnimatorFaceEquipHash, faceSocket != null && faceSocket.childCount > 0);
            _customAnimator.SetBool(AnimatorBackpackEquipHash, backpackSocket != null && backpackSocket.childCount > 0);
            _customAnimator.SetBool(AnimatorMeleeWeaponEquipHash,
                meleeWeaponSocket != null && meleeWeaponSocket.childCount > 0);
            _customAnimator.SetBool(AnimatorHavePopTextHash, popTextSocket != null && popTextSocket.childCount > 0);

            var equipmentSockets = new[]
            {
                helmetSocket,
                faceSocket,
                armorSocket,
                backpackSocket,
            };
            var visible = !hideOriginalEquipment;
            foreach (var socket in equipmentSockets)
            {
                if (socket == null) continue;
                foreach (Transform child in socket) child.gameObject.SetActive(visible);
            }
        }

        private void UpdateHandState()
        {
            if (!_initialized) return;
            if (_customAnimator == null || _characterMainControl == null)
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

            _customAnimator.SetInteger(AnimatorHandStateHash, handState);
            _customAnimator.SetBool(AnimatorRightHandOutHash, rightHandOut);
        }

        private void UpdateGunState()
        {
            if (!_initialized) return;
            if (_customAnimator == null || _characterMainControl == null)
                return;

            if (_holdAgent != null && _gunAgent == null)
                _gunAgent = _holdAgent as ItemAgent_Gun;

            var isGunReady = false;
            var isReloading = false;
            if (_gunAgent != null)
            {
                isReloading = _gunAgent.IsReloading();
                isGunReady = _gunAgent.BulletCount > 0 && !isReloading;
            }

            _customAnimator.SetBool(AnimatorReloadingHash, isReloading);
            _customAnimator.SetBool(AnimatorGunReadyHash, isGunReady);
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
            _attackWeight = AttackLayerWeightCurve?.Evaluate(_attackTimer / attackTime) ?? 0.0f;
            if (_attackTimer >= attackTime)
            {
                _attacking = false;
                _attackWeight = 0.0f;
            }

            SetMeleeAttackLayerWeight(_attackWeight);
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
            if (_customAnimator != null)
                _customAnimator.SetTrigger(AnimatorAttackHash);
        }

        #region Animator Parameter Hashes

        private static readonly int AnimatorGroundedHash = Animator.StringToHash("Grounded");
        private static readonly int AnimatorDieHash = Animator.StringToHash("Die");
        private static readonly int AnimatorMoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int AnimatorMoveDirXHash = Animator.StringToHash("MoveDirX");
        private static readonly int AnimatorMoveDirYHash = Animator.StringToHash("MoveDirY");
        private static readonly int AnimatorIsMovingHash = Animator.StringToHash("Moving");
        private static readonly int AnimatorIsRunningHash = Animator.StringToHash("Running");
        private static readonly int AnimatorDashingHash = Animator.StringToHash("Dashing");
        private static readonly int AnimatorAttackHash = Animator.StringToHash("Attack");
        private static readonly int AnimatorHandStateHash = Animator.StringToHash("HandState");
        private static readonly int AnimatorGunReadyHash = Animator.StringToHash("GunReady");
        private static readonly int AnimatorReloadingHash = Animator.StringToHash("Reloading");
        private static readonly int AnimatorRightHandOutHash = Animator.StringToHash("RightHandOut");

        private static readonly int AnimatorHealthRateHash = Animator.StringToHash("HealthRate");
        private static readonly int AnimatorWaterRateHash = Animator.StringToHash("WaterRate");
        private static readonly int AnimatorWeightStateHash = Animator.StringToHash("WeightState");
        private static readonly int AnimatorWeightRateHash = Animator.StringToHash("WeightRate");

        private static readonly int AnimatorHideOriginalEquipmentHash = Animator.StringToHash("HideOriginalEquipment");
        private static readonly int AnimatorLeftHandEquipHash = Animator.StringToHash("LeftHandEquip");
        private static readonly int AnimatorRightHandEquipHash = Animator.StringToHash("RightHandEquip");
        private static readonly int AnimatorArmorEquipHash = Animator.StringToHash("ArmorEquip");
        private static readonly int AnimatorHelmetEquipHash = Animator.StringToHash("HelmetEquip");
        private static readonly int AnimatorFaceEquipHash = Animator.StringToHash("FaceEquip");
        private static readonly int AnimatorBackpackEquipHash = Animator.StringToHash("BackpackEquip");
        private static readonly int AnimatorMeleeWeaponEquipHash = Animator.StringToHash("MeleeWeaponEquip");
        private static readonly int AnimatorHavePopTextHash = Animator.StringToHash("HavePopText");

        #endregion
    }
}