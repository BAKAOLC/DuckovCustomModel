using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Duckov;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Data;
using DuckovCustomModel.Managers;
using DuckovCustomModel.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DuckovCustomModel.MonoBehaviours
{
    public class ModelHandler : MonoBehaviour
    {
        private static readonly FieldInfo[] OriginalModelSocketFieldInfos =
            CharacterModelSocketUtils.AllSocketFields;

        private readonly Dictionary<string, Transform> _customModelLocatorCache = [];

        private readonly Dictionary<FieldInfo, Transform> _customModelSockets = [];

        private readonly HashSet<GameObject> _modifiedDeathLootBoxes = [];
        private readonly Dictionary<FieldInfo, Transform> _originalModelSockets = [];

        private readonly Dictionary<string, List<string>> _soundsByTag = [];
        private readonly HashSet<GameObject> _usingCustomSocketObjects = [];
        private GameObject? _deathLootBoxPrefab;

        private float _nextIdleAudioTime;

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

                if (Target != ModelTarget.AICharacter)
                    return ModBehaviour.Instance.HideEquipmentConfig.GetHideEquipment(Target);
                var nameKey = CharacterMainControl?.characterPreset?.nameKey;
                return !string.IsNullOrEmpty(nameKey)
                    ? ModBehaviour.Instance.HideEquipmentConfig.GetHideAICharacterEquipment(nameKey)
                    : ModBehaviour.Instance.HideEquipmentConfig.GetHideEquipment(Target);
            }
        }

        public ModelTarget Target { get; private set; }
        public string? NameKey => CharacterMainControl?.characterPreset?.nameKey;

        public bool IsInitialized { get; private set; }

        public GameObject? CustomModelInstance { get; private set; }
        public Animator? CustomAnimator { get; private set; }
        public CustomAnimatorControl? CustomAnimatorControl { get; private set; }

        private void Update()
        {
            if (!IsInitialized || CharacterMainControl == null) return;
            if (!HasIdleSounds()) return;
            if (CharacterMainControl.Health != null && CharacterMainControl.Health.IsDead) return;

            if (ModBehaviour.Instance?.IdleAudioConfig != null)
            {
                if (Target == ModelTarget.AICharacter)
                {
                    var nameKey = NameKey;
                    if (string.IsNullOrEmpty(nameKey) ||
                        !ModBehaviour.Instance.IdleAudioConfig.IsAICharacterIdleAudioEnabled(nameKey))
                        return;
                }
                else
                {
                    if (!ModBehaviour.Instance.IdleAudioConfig.IsIdleAudioEnabled(Target))
                        return;
                }
            }

            if (!(Time.time >= _nextIdleAudioTime)) return;
            PlayIdleAudio();
            ScheduleNextIdleAudio();
        }

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
                        if (child != null && child.gameObject.activeSelf)
                        {
                            var dontHide = child.GetComponent<DontHideAsEquipment>();
                            if (dontHide == null)
                                child.gameObject.SetActive(false);
                        }
                }
            else
                foreach (var socket in equipmentSockets)
                {
                    if (socket == null) continue;
                    foreach (Transform child in socket)
                        if (child != null && !child.gameObject.activeSelf)
                            child.gameObject.SetActive(true);
                }
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

            var customAnimatorControl = CharacterMainControl.GetComponent<CustomAnimatorControl>();
            if (customAnimatorControl == null)
                customAnimatorControl = CharacterMainControl.gameObject.AddComponent<CustomAnimatorControl>();

            CustomAnimatorControl = customAnimatorControl;
            customAnimatorControl.Initialize(this);

            RecordOriginalModelSockets();
            ModLogger.Log("ModelHandler initialized successfully.");
            IsInitialized = true;
        }

        public void SetTarget(ModelTarget target)
        {
            Target = target;
        }

        public bool HaveCustomDeathLootBox()
        {
            return _deathLootBoxPrefab != null;
        }

        public GameObject? CreateCustomDeathLootBoxInstance()
        {
            if (_deathLootBoxPrefab == null) return null;
            var instance = Instantiate(_deathLootBoxPrefab);
            instance.name = "DeathLootBox_CustomModel";
            ReplaceShaderAndLayer(instance);
            return instance;
        }

        public void RegisterCustomSocketObject(GameObject customSocketObject)
        {
            if (customSocketObject == null) return;
            _usingCustomSocketObjects.Add(customSocketObject);
            UpdateToCustomSocket(customSocketObject);
        }

        public void UnregisterCustomSocketObject(GameObject customSocketObject)
        {
            if (customSocketObject == null) return;
            _usingCustomSocketObjects.Remove(customSocketObject);

            var customSocketMarker = customSocketObject.GetComponent<CustomSocketMarker>();
            if (customSocketMarker == null || customSocketMarker.OriginParent == null) return;

            customSocketObject.transform.SetParent(customSocketMarker.OriginParent, false);
            customSocketObject.transform.localPosition = customSocketMarker.SocketOffset ?? Vector3.zero;
            customSocketObject.transform.localRotation = customSocketMarker.SocketRotation ?? Quaternion.identity;
            customSocketObject.transform.localScale = customSocketMarker.SocketScale ?? Vector3.one;
        }

        public void RegisterModifiedDeathLootBox(GameObject deathLootBox)
        {
            if (deathLootBox == null) return;
            _modifiedDeathLootBoxes.Add(deathLootBox);
        }

        public void UnregisterModifiedDeathLootBox(GameObject deathLootBox)
        {
            if (deathLootBox == null) return;
            _modifiedDeathLootBoxes.Remove(deathLootBox);
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
            RestoreCustomSocketObjects();

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
                RestoreCustomSocketObjects();
            }

            if (CustomModelInstance != null)
            {
                CustomAnimator = null;

                DestroyImmediate(CustomModelInstance);
                CustomModelInstance = null;
            }

            if (CustomAnimatorControl != null)
                CustomAnimatorControl.SetCustomAnimator(null);

            if (_deathLootBoxPrefab != null)
            {
                DestroyImmediate(_deathLootBoxPrefab);
                _deathLootBoxPrefab = null;
            }

            foreach (var destroyAdapter in _modifiedDeathLootBoxes
                         .Select(deathLootBox => deathLootBox.GetComponent<OnDestroyAdapter>())
                         .Where(destroyAdapter => destroyAdapter != null))
            {
                destroyAdapter.ForceInvoke();
                destroyAdapter.ClearListeners();

                DestroyImmediate(destroyAdapter.gameObject);
            }

            _customModelSockets.Clear();
            _customModelLocatorCache.Clear();
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
            UpdateCustomSocketObjects();

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

            if (CustomModelInstance != null) CleanupCustomModel();
            InitSoundFilePath(modelBundleInfo, modelInfo);
            InitializeDeathLootBoxPrefab(modelBundleInfo, modelInfo);
            InitializeCustomModelInternal(prefab, modelInfo);

            if (!HasIdleSounds()) return;
            if (Target == ModelTarget.AICharacter)
            {
                var nameKey = NameKey;
                if (ModBehaviour.Instance?.IdleAudioConfig == null ||
                    string.IsNullOrEmpty(nameKey) ||
                    ModBehaviour.Instance.IdleAudioConfig.IsAICharacterIdleAudioEnabled(nameKey))
                    ScheduleNextIdleAudio();
            }
            else
            {
                if (ModBehaviour.Instance?.IdleAudioConfig == null ||
                    ModBehaviour.Instance.IdleAudioConfig.IsIdleAudioEnabled(Target))
                    ScheduleNextIdleAudio();
            }
        }

        private void InitializeDeathLootBoxPrefab(ModelBundleInfo modelBundleInfo, ModelInfo modelInfo)
        {
            _deathLootBoxPrefab = null;

            if (string.IsNullOrWhiteSpace(modelInfo.DeathLootBoxPrefabPath)) return;

            var prefab = AssetBundleManager.LoadDeathLootBoxPrefab(modelBundleInfo, modelInfo);
            if (prefab == null) return;

            _deathLootBoxPrefab = prefab;
            ModLogger.Log($"Death loot box prefab initialized: {prefab.name}");
        }

        private void InitializeCustomModelInternal(GameObject customModelPrefab, ModelInfo modelInfo)
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

            if (modelInfo.Features is not { Length: > 0 } || !Array.Exists(modelInfo.Features,
                    feature => feature == ModelFeatures.NoAutoShaderReplace))
                ReplaceShaderAndLayer(CustomModelInstance);

            // Get the Animator component from the custom model
            CustomAnimator = CustomModelInstance.GetComponent<Animator>();
            if (CustomAnimatorControl != null)
                if (CustomAnimator != null)
                {
                    CustomAnimatorControl.SetCustomAnimator(CustomAnimator);
                }
                else
                {
                    ModLogger.LogError("No Animator component found on custom model instance.");
                    CustomAnimatorControl.SetCustomAnimator(null);
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
            foreach (var kvp in SocketNames.InternalSocketMap)
            {
                var locatorTransform = SearchLocatorTransform(CustomModelInstance!, kvp.Value);
                if (locatorTransform != null) _customModelSockets[kvp.Key] = locatorTransform;
            }

            foreach (var locatorName in SocketNames.ExternalSocketNames)
            {
                var locatorTransform = SearchLocatorTransform(CustomModelInstance!, locatorName);
                if (locatorTransform == null) continue;
                _customModelLocatorCache[locatorName] = locatorTransform;
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

        private void UpdateToCustomSocket(GameObject targetGameObject)
        {
            if (OriginalCharacterModel == null || targetGameObject == null)
                return;

            var customSocketMarker = targetGameObject.GetComponent<CustomSocketMarker>();
            if (customSocketMarker == null) return;

            if (!_customModelLocatorCache.TryGetValue(customSocketMarker.CustomSocketName, out var customSocket)
                || customSocket == null)
                return;

            targetGameObject.transform.SetParent(customSocket, false);
            targetGameObject.transform.localPosition = Vector3.zero;
            targetGameObject.transform.localRotation = Quaternion.identity;
            targetGameObject.transform.localScale = Vector3.one;
        }

        private void UpdateCustomSocketObjects()
        {
            if (OriginalCharacterModel == null || CustomModelInstance == null) return;

            foreach (var customSocketObject in _usingCustomSocketObjects)
                UpdateToCustomSocket(customSocketObject);
        }

        private void RestoreCustomSocketObjects()
        {
            if (OriginalCharacterModel == null) return;

            foreach (var kvp in _customModelLocatorCache)
            {
                var locatorTransform = kvp.Value;
                if (locatorTransform == null) continue;

                var childrenToRestore = locatorTransform.OfType<Transform>().ToArray();
                foreach (var child in childrenToRestore)
                {
                    var customSocketMarker = child.GetComponent<CustomSocketMarker>();
                    if (customSocketMarker == null || customSocketMarker.OriginParent == null) continue;

                    child.SetParent(customSocketMarker.OriginParent, false);
                    child.localPosition = customSocketMarker.SocketOffset ?? Vector3.zero;
                    child.localRotation = customSocketMarker.SocketRotation ?? Quaternion.identity;
                    child.localScale = customSocketMarker.SocketScale ?? Vector3.one;
                }
            }
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

        private static void ReplaceShaderAndLayer(GameObject targetGameObject, string layerName = "Character")
        {
            var shader = GameDefaultShader;
            if (shader == null)
            {
                ModLogger.LogError("Game default shader not found.");
                return;
            }

            var layer = LayerMask.NameToLayer(layerName);
            if (layer == -1)
            {
                ModLogger.LogError($"Layer '{layerName}' not found.");
                return;
            }

            var renderers = targetGameObject.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var gameObject = renderer.gameObject;
                if (gameObject.layer != layer)
                    gameObject.layer = layer;
                foreach (var material in renderer.materials)
                {
                    if (material == null) continue;
                    material.shader = shader;
                    if (material.HasProperty(EmissionColor))
                        material.SetColor(EmissionColor, Color.black);
                }
            }
        }

        #region Shader Constants

        // ReSharper disable once ShaderLabShaderReferenceNotResolved
        private static Shader GameDefaultShader => Shader.Find("SodaCraft/SodaCharacter");

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        #endregion

        #region Sound System

        private void InitSoundFilePath(ModelBundleInfo modelBundleInfo, ModelInfo modelInfo)
        {
            _soundsByTag.Clear();

            var bundleDirectory = modelBundleInfo.DirectoryPath;

            if (modelInfo.CustomSounds is not { Length: > 0 }) return;

            var validSoundCount = 0;

            foreach (var soundInfo in modelInfo.CustomSounds)
            {
                if (string.IsNullOrWhiteSpace(soundInfo.Path)) continue;
                var fullPath = Path.Combine(bundleDirectory, soundInfo.Path);
                if (!File.Exists(fullPath)) continue;

                if (soundInfo.Tags is not { Length: > 0 })
                    soundInfo.Tags = [SoundTags.Normal];

                foreach (var soundTag in soundInfo.Tags)
                {
                    if (!_soundsByTag.ContainsKey(soundTag))
                        _soundsByTag[soundTag] = [];
                    _soundsByTag[soundTag].Add(fullPath);
                }

                validSoundCount++;
            }

            if (validSoundCount == 0) return;
            ModLogger.Log(
                $"Initialized {validSoundCount} custom sounds for model '{modelInfo.Name}' ({modelInfo.ModelID}).");
        }

        public bool HasAnySounds()
        {
            return _soundsByTag.Count > 0 && _soundsByTag.Values.Any(sounds => sounds.Count > 0);
        }

        public string? GetRandomSoundByTag(string soundTag)
        {
            if (string.IsNullOrWhiteSpace(soundTag)) soundTag = SoundTags.Normal;
            soundTag = soundTag.ToLowerInvariant().Trim();

            if (!_soundsByTag.TryGetValue(soundTag, out var sounds) || sounds.Count == 0) return null;

            var index = Random.Range(0, sounds.Count);
            return sounds[index];
        }

        private bool HasIdleSounds()
        {
            return _soundsByTag.ContainsKey(SoundTags.Idle) &&
                   _soundsByTag[SoundTags.Idle].Count > 0;
        }

        private void PlayIdleAudio()
        {
            var soundPath = GetRandomSoundByTag(SoundTags.Idle);
            if (string.IsNullOrEmpty(soundPath)) return;

            AudioManager.PostCustomSFX(soundPath);
        }

        private void ScheduleNextIdleAudio()
        {
            if (ModBehaviour.Instance?.IdleAudioConfig == null)
            {
                _nextIdleAudioTime = Time.time + Random.Range(30f, 45f);
                return;
            }

            IdleAudioInterval interval;
            if (Target == ModelTarget.AICharacter)
            {
                var nameKey = NameKey;
                interval = !string.IsNullOrEmpty(nameKey)
                    ? ModBehaviour.Instance.IdleAudioConfig.GetAICharacterIdleAudioInterval(nameKey)
                    : ModBehaviour.Instance.IdleAudioConfig.GetIdleAudioInterval(Target);
            }
            else
            {
                interval = ModBehaviour.Instance.IdleAudioConfig.GetIdleAudioInterval(Target);
            }

            var randomInterval = Random.Range(interval.Min, interval.Max);
            _nextIdleAudioTime = Time.time + randomInterval;
        }

        #endregion
    }
}