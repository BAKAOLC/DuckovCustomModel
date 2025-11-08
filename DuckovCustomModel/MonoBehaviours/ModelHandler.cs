using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Duckov;
using DuckovCustomModel.Data;
using DuckovCustomModel.Managers;
using DuckovCustomModel.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DuckovCustomModel.MonoBehaviours
{
    public class ModelHandler : MonoBehaviour
    {
        private static readonly FieldInfo[] OriginalModelSocketFieldInfos =
            CharacterModelSocketUtils.AllSocketFields;

        private static readonly Dictionary<ModelHandler, InputAction> ActiveQuackActions = [];

        private readonly Dictionary<FieldInfo, Transform> _customModelSockets = [];
        private readonly Dictionary<FieldInfo, Transform> _originalModelSockets = [];

        private readonly List<string> _soundPaths = [];
        private readonly Dictionary<string, List<string>> _soundsByTag = [];
        private InputAction? _newAction;
        private bool _quackEnabled;

        public CharacterMainControl? CharacterMainControl { get; private set; }
        public CharacterModel? OriginalCharacterModel { get; private set; }
        public CharacterAnimationControl? OriginalAnimationControl { get; private set; }
        public CharacterAnimationControl_MagicBlend? OriginalMagicBlendAnimationControl { get; private set; }
        public Movement? OriginalMovement { get; private set; }
        public bool IsHiddenOriginalModel { get; private set; }

        public bool IsHiddenOriginalEquipment
        {
            get
            {
                if (OriginalCharacterModel == null) return false;
                if (ModBehaviour.Instance == null) return false;
                if (ModBehaviour.Instance.HideEquipmentConfig == null) return false;
                if (!IsHiddenOriginalModel || CustomModelInstance == null) return false;

                return ModBehaviour.Instance.HideEquipmentConfig.GetHideEquipment(Target);
            }
        }

        public ModelTarget Target { get; private set; }

        public bool IsInitialized { get; private set; }

        public GameObject? CustomModelInstance { get; private set; }
        public Animator? CustomAnimator { get; private set; }
        public CustomAnimatorControl? CustomAnimatorControl { get; private set; }

        private void LateUpdate()
        {
            if (OriginalCharacterModel == null) return;

            var equipmentSockets = new[]
            {
                CharacterModelSocketUtils.GetHelmetSocket(OriginalCharacterModel),
                CharacterModelSocketUtils.GetFaceSocket(OriginalCharacterModel),
                CharacterModelSocketUtils.GetArmorSocket(OriginalCharacterModel),
                CharacterModelSocketUtils.GetBackpackSocket(OriginalCharacterModel),
            };


            if (IsHiddenOriginalEquipment)
                foreach (var socket in equipmentSockets)
                {
                    if (socket == null) continue;
                    foreach (Transform child in socket)
                        if (child.gameObject.activeSelf)
                            child.gameObject.SetActive(false);
                }
            else
                foreach (var socket in equipmentSockets)
                {
                    if (socket == null) continue;
                    foreach (Transform child in socket)
                        if (!child.gameObject.activeSelf)
                            child.gameObject.SetActive(true);
                }
        }

        private void OnDestroy()
        {
            DisableQuack();
        }

        public void Initialize(CharacterMainControl characterMainControl, ModelTarget target = ModelTarget.Character)
        {
            if (IsInitialized) return;
            CharacterMainControl = characterMainControl;
            Target = target;
            if (CharacterMainControl == null)
            {
                ModLogger.LogError("CharacterMainControl component not found.");
                return;
            }

            OriginalCharacterModel = CharacterMainControl.characterModel;
            if (OriginalCharacterModel == null)
            {
                ModLogger.LogError("No CharacterModel found on CharacterMainControl.");
                return;
            }

            OriginalMovement = CharacterMainControl.movementControl;
            if (OriginalMovement == null)
            {
                ModLogger.LogError("No Movement component found on CharacterMainControl.");
                return;
            }

            OriginalAnimationControl = OriginalCharacterModel.GetComponent<CharacterAnimationControl>();
            OriginalMagicBlendAnimationControl =
                OriginalCharacterModel.GetComponent<CharacterAnimationControl_MagicBlend>();
            if (OriginalAnimationControl == null && OriginalMagicBlendAnimationControl == null)
                ModLogger.LogError("No CharacterAnimationControl component found on CharacterModel.");

            RecordOriginalModelSockets();
            ModLogger.Log("ModelHandler initialized successfully.");
            IsInitialized = true;
        }

        public void SetTarget(ModelTarget target)
        {
            Target = target;
        }

        public void RestoreOriginalModel()
        {
            if (OriginalCharacterModel == null)
            {
                ModLogger.LogError("OriginalCharacterModel is not set.");
                return;
            }

            if (!IsHiddenOriginalModel) return;

            var customFaceInstance = GetOriginalCustomFaceInstance();
            if (customFaceInstance != null) customFaceInstance.gameObject.SetActive(true);

            RestoreToOriginalModelSockets();

            if (CustomModelInstance != null) CustomModelInstance.SetActive(false);

            if (IsHiddenOriginalModel)
                ModLogger.Log("Restored to original model.");
            IsHiddenOriginalModel = false;
        }

        public void CleanupCustomModel()
        {
            if (OriginalCharacterModel == null)
            {
                ModLogger.LogError("OriginalCharacterModel is not set.");
                return;
            }

            var wasHidden = IsHiddenOriginalModel;

            if (wasHidden)
            {
                var customFaceInstance = GetOriginalCustomFaceInstance();
                if (customFaceInstance != null) customFaceInstance.gameObject.SetActive(true);

                RestoreToOriginalModelSockets();
            }

            DisableQuack();

            if (CustomModelInstance != null)
            {
                if (CustomAnimatorControl != null)
                {
                    DestroyImmediate(CustomAnimatorControl);
                    CustomAnimatorControl = null;
                }

                CustomAnimator = null;

                DestroyImmediate(CustomModelInstance);
                CustomModelInstance = null;
            }

            _customModelSockets.Clear();
            _soundPaths.Clear();
            _soundsByTag.Clear();

            if (wasHidden)
                ModLogger.Log("Cleaned up custom model.");
            IsHiddenOriginalModel = false;
        }

        public void ChangeToCustomModel()
        {
            if (OriginalCharacterModel == null)
            {
                ModLogger.LogError("OriginalCharacterModel is not set.");
                return;
            }

            if (CustomModelInstance == null)
            {
                ModLogger.LogError("Custom model instance is not initialized.");
                return;
            }

            var customFaceInstance = GetOriginalCustomFaceInstance();
            if (customFaceInstance != null) customFaceInstance.gameObject.SetActive(false);

            CustomModelInstance.SetActive(true);
            ChangeToCustomModelSockets();

            if (!IsHiddenOriginalModel)
                ModLogger.Log("Changed to custom model.");
            IsHiddenOriginalModel = true;
        }

        public void InitializeCustomModel(ModelBundleInfo modelBundleInfo, ModelInfo modelInfo)
        {
            var prefab = AssetBundleManager.LoadModelPrefab(modelBundleInfo, modelInfo);
            if (prefab == null)
            {
                ModLogger.LogError("Failed to load custom model prefab.");
                return;
            }

            InitSoundFilePath(modelBundleInfo, modelInfo);
            InitializeCustomModel(prefab);
            if (Target == ModelTarget.Character)
                InitQuackKey();
        }

        public void InitializeCustomModel(GameObject customModelPrefab)
        {
            if (CharacterMainControl == null)
            {
                ModLogger.LogError("CharacterMainControl is not set.");
                return;
            }

            if (OriginalCharacterModel == null)
            {
                ModLogger.LogError("OriginalCharacterModel is not set.");
                return;
            }

            if (CustomModelInstance != null) CleanupCustomModel();

            // Instantiate the custom model prefab
            CustomModelInstance = Instantiate(customModelPrefab, OriginalCharacterModel.transform);
            CustomModelInstance.name = "CustomModelInstance";
            CustomModelInstance.layer = LayerMask.NameToLayer("Default");

            ReplaceShader(CustomModelInstance);

            // Get the Animator component from the custom model
            CustomAnimator = CustomModelInstance.GetComponent<Animator>();
            if (CustomAnimator != null)
            {
                CustomAnimatorControl = CustomModelInstance.AddComponent<CustomAnimatorControl>();
                CustomAnimatorControl.Initialize(this);
            }
            else
            {
                ModLogger.LogError("Custom model prefab does not have an Animator component.");
            }

            RecordCustomModelSockets();

            ModLogger.Log($"Custom model initialized: {customModelPrefab.name}");

            if (IsHiddenOriginalModel)
                ChangeToCustomModel();
        }

        private void RecordOriginalModelSockets()
        {
            if (OriginalCharacterModel == null) return;

            _originalModelSockets.Clear();
            foreach (var socketField in OriginalModelSocketFieldInfos)
            {
                var socketTransform = socketField.GetValue(OriginalCharacterModel) as Transform;
                if (socketTransform != null) _originalModelSockets[socketField] = socketTransform;
            }
        }

        private void RestoreToOriginalModelSockets()
        {
            if (OriginalCharacterModel == null) return;

            foreach (var kvp in _originalModelSockets) ReplaceModelSocket(kvp.Key, kvp.Value);
        }

        private void RecordCustomModelSockets()
        {
            if (OriginalCharacterModel == null) return;

            _customModelSockets.Clear();
            foreach (var kvp in CustomModelSocketNames)
            {
                var locatorTransform = SearchLocatorTransform(CustomModelInstance!, kvp.Value);
                if (locatorTransform != null) _customModelSockets[kvp.Key] = locatorTransform;
            }
        }

        private void ChangeToCustomModelSockets()
        {
            if (OriginalCharacterModel == null) return;

            RestoreToOriginalModelSockets();
            foreach (var kvp in _customModelSockets) ReplaceModelSocket(kvp.Key, kvp.Value);
        }

        private Transform? GetOriginalCustomFaceInstance()
        {
            if (OriginalCharacterModel == null) return null;
            var targetTransformName = Target == ModelTarget.Pet ? "Dog" : "CustomFaceInstance";
            return OriginalCharacterModel.transform.Find(targetTransformName);
        }

        private void ReplaceModelSocket(FieldInfo socketField, Transform? newSocket)
        {
            if (OriginalCharacterModel == null || newSocket == null) return;
            var originalSocket = socketField.GetValue(OriginalCharacterModel) as Transform;
            if (originalSocket == newSocket) return;
            var originalChildren = new List<Transform>();
            if (originalSocket != null)
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (Transform child in originalSocket)
                    if (child != null)
                        originalChildren.Add(child);
            socketField.SetValue(OriginalCharacterModel, newSocket);
            foreach (var child in originalChildren)
            {
                child.SetParent(newSocket, false);
                child.localRotation = Quaternion.identity;
                child.localPosition = Vector3.zero;
            }
        }

        private static Transform? SearchLocatorTransform(GameObject modelInstance, string locatorName)
        {
            var transforms = modelInstance.GetComponentsInChildren<Transform>(true);
            return transforms.FirstOrDefault(t => t.name == locatorName);
        }

        private static void ReplaceShader(GameObject targetGameObject)
        {
            var shader = GameDefaultShader;
            if (shader == null)
            {
                ModLogger.LogError("Game default shader not found.");
                return;
            }

            var renderers = targetGameObject.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            foreach (var material in renderer.materials)
            {
                if (material == null) continue;
                material.shader = shader;
                if (material.HasProperty(EmissionColor))
                    material.SetColor(EmissionColor, Color.black);
            }
        }

        #region Shader Constants

        // ReSharper disable once ShaderLabShaderReferenceNotResolved
        private static Shader GameDefaultShader => Shader.Find("SodaCraft/SodaCharacter");

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        #endregion

        #region Custom Sockets

        private const string LeftHandLocatorName = "LeftHandLocator";
        private const string RightHandLocatorName = "RightHandLocator";
        private const string ArmorLocatorName = "ArmorLocator";
        private const string HelmetLocatorName = "HelmetLocator";
        private const string FaceLocatorName = "FaceLocator";
        private const string BackpackLocatorName = "BackpackLocator";
        private const string MeleeWeaponLocatorName = "MeleeWeaponLocator";
        private const string PopTextLocatorName = "PopTextLocator";

        private static readonly Dictionary<FieldInfo, string> CustomModelSocketNames = new()
        {
            { CharacterModelSocketUtils.LeftHandSocket, LeftHandLocatorName },
            { CharacterModelSocketUtils.RightHandSocket, RightHandLocatorName },
            { CharacterModelSocketUtils.ArmorSocket, ArmorLocatorName },
            { CharacterModelSocketUtils.HelmetSocket, HelmetLocatorName },
            { CharacterModelSocketUtils.FaceSocket, FaceLocatorName },
            { CharacterModelSocketUtils.BackpackSocket, BackpackLocatorName },
            { CharacterModelSocketUtils.MeleeWeaponSocket, MeleeWeaponLocatorName },
            { CharacterModelSocketUtils.PopTextSocket, PopTextLocatorName },
        };

        #endregion

        #region Sound System

        private void InitSoundFilePath(ModelBundleInfo modelBundleInfo, ModelInfo modelInfo)
        {
            _soundPaths.Clear();
            _soundsByTag.Clear();

            var bundleDirectory = modelBundleInfo.DirectoryPath;

            if (modelInfo.SoundInfos is not { Length: > 0 }) return;
            foreach (var soundInfo in modelInfo.SoundInfos)
            {
                if (string.IsNullOrWhiteSpace(soundInfo.Path)) continue;
                var fullPath = Path.Combine(bundleDirectory, soundInfo.Path);
                if (!File.Exists(fullPath)) continue;

                foreach (var soundTag in soundInfo.TagSet)
                {
                    if (!_soundsByTag.ContainsKey(soundTag))
                        _soundsByTag[soundTag] = [];
                    _soundsByTag[soundTag].Add(fullPath);
                }
            }
        }

        public string? GetRandomSoundByTag(string soundTag)
        {
            if (string.IsNullOrWhiteSpace(soundTag)) soundTag = "normal";
            soundTag = soundTag.ToLowerInvariant().Trim();

            if (!_soundsByTag.TryGetValue(soundTag, out var sounds) || sounds.Count == 0)
            {
                if (soundTag != "normal" && _soundsByTag.TryGetValue("normal", out var normalSounds) &&
                    normalSounds.Count > 0)
                    sounds = normalSounds;
                else
                    return null;
            }

            var index = Random.Range(0, sounds.Count);
            return sounds[index];
        }

        private void InitQuackKey()
        {
            if (_soundsByTag.Count == 0 && _soundPaths.Count < 1) return;

            if (GameManager.MainPlayerInput == null)
            {
                ModLogger.LogWarning("ModelHandler: MainPlayerInput is null.");
                return;
            }

            var quackAction = GameManager.MainPlayerInput.actions.FindAction("Quack");
            if (quackAction == null)
            {
                ModLogger.LogWarning("ModelHandler: Quack action not found.");
                return;
            }

            quackAction.Disable();

            _newAction = new();
            if (quackAction.controls.Count > 0)
                foreach (var binding in quackAction.bindings)
                    _newAction.AddBinding(binding);
            _newAction.performed += PlaySound;
            _newAction.Enable();

            ActiveQuackActions[this] = _newAction;
            _quackEnabled = true;
        }

        private void DisableQuack()
        {
            if (!_quackEnabled && _newAction == null) return;

            _quackEnabled = false;

            if (_newAction != null)
            {
                _newAction.performed -= PlaySound;
                _newAction.Disable();
                _newAction.Dispose();
                _newAction = null;
            }

            ActiveQuackActions.Remove(this);

            if (GameManager.MainPlayerInput == null) return;
            var quackAction = GameManager.MainPlayerInput.actions.FindAction("Quack");
            quackAction?.Enable();
        }

        public static void DisableAllQuackActions()
        {
            var handlers = ActiveQuackActions.Keys.ToArray();
            foreach (var handler in handlers.Where(h => h != null))
                handler.DisableQuack();

            ActiveQuackActions.Clear();
        }

        public void PlaySound(InputAction.CallbackContext context)
        {
            PlaySound(context, "normal");
        }

        public void PlaySound(InputAction.CallbackContext context, string soundTag)
        {
            if (CharacterMainControl == null) return;
            if (OriginalCharacterModel == null) return;

            var soundPath = GetRandomSoundByTag(soundTag);
            if (string.IsNullOrEmpty(soundPath))
            {
                if (_soundPaths.Count == 0) return;
                var index = Random.Range(0, _soundPaths.Count);
                soundPath = _soundPaths[index];
            }

            AudioManager.PostCustomSFX(soundPath);
            AIMainBrain.MakeSound(new()
            {
                fromCharacter = CharacterMainControl.Main,
                fromObject = gameObject,
                pos = OriginalCharacterModel.transform.position,
                fromTeam = Teams.player,
                soundType = SoundTypes.unknowNoise,
                radius = 15f,
            });
        }

        #endregion
    }
}