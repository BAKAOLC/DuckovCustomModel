using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Duckov;
using Duckov.UI;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using DuckovCustomModel.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DuckovCustomModel.MonoBehaviours
{
    public class ModelHandler : MonoBehaviour
    {
        private static readonly IReadOnlyDictionary<string, FieldInfo> OriginalModelSocketFieldInfos =
            CharacterModelSocketUtils.AllSocketFields;

        private readonly HashSet<GameObject> _currentUsingCustomSocketObjects = [];
        private readonly Dictionary<string, Transform> _customModelLocators = [];
        private readonly Dictionary<FieldInfo, Transform> _customModelSockets = [];
        private readonly HashSet<GameObject> _modifiedDeathLootBoxes = [];
        private readonly Dictionary<string, Transform> _originalModelLocators = [];
        private readonly Dictionary<FieldInfo, Transform> _originalModelSockets = [];
        private readonly Dictionary<string, List<string>> _soundsByTag = [];
        private Renderer[]? _cachedCustomModelRenderers;

        private ModelBundleInfo? _currentModelBundleInfo;
        private ModelInfo? _currentModelInfo;
        private CustomCharacterSoundMaker? _customCharacterSoundMaker;
        private CharacterSubVisuals? _customModelSubVisuals;
        private GameObject? _deathLootBoxPrefab;
        private GameObject? _headColliderObject;

        private float _nextIdleAudioTime;

        private bool ReplaceShader => _currentModelInfo is not { Features: { Length: > 0 } }
                                      || !_currentModelInfo.Features.Contains(ModelFeatures.NoAutoShaderReplace);

        public CharacterMainControl? CharacterMainControl { get; private set; }
        public CharacterModel? OriginalCharacterModel { get; private set; }
        public GameObject? OriginalModelOcclusionBody { get; private set; }
        public CharacterSoundMaker? OriginalCharacterSoundMaker { get; private set; }
        public CharacterAnimationControl? OriginalAnimationControl { get; private set; }
        public CharacterAnimationControl_MagicBlend? OriginalMagicBlendAnimationControl { get; private set; }
        public Movement? OriginalMovement { get; private set; }
        public bool IsHiddenOriginalModel { get; private set; }

        public bool IsHiddenOriginalEquipment
        {
            get
            {
                if (OriginalCharacterModel == null) return false;
                if (ModEntry.HideEquipmentConfig == null) return false;
                if (!IsHiddenOriginalModel || CustomModelInstance == null) return false;

                if (Target != ModelTarget.AICharacter)
                    return ModEntry.HideEquipmentConfig.GetHideEquipment(Target);
                var nameKey = CharacterMainControl?.characterPreset?.nameKey;
                if (string.IsNullOrEmpty(nameKey))
                    return ModEntry.HideEquipmentConfig.GetHideEquipment(Target);

                var effectiveNameKey = GetEffectiveAICharacterConfigKey(nameKey);
                return ModEntry.HideEquipmentConfig.GetHideAICharacterEquipment(effectiveNameKey);
            }
        }

        public bool IsModelAudioEnabled
        {
            get
            {
                var modelAudioConfig = ModEntry.ModelAudioConfig;
                if (modelAudioConfig == null) return true;

                if (Target != ModelTarget.AICharacter)
                    return modelAudioConfig.IsModelAudioEnabled(Target);

                var nameKey = NameKey;
                if (string.IsNullOrEmpty(nameKey)) return true;

                var effectiveNameKey = GetEffectiveAICharacterConfigKey(nameKey);
                return modelAudioConfig.IsAICharacterModelAudioEnabled(effectiveNameKey);
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

            if (ModEntry.IdleAudioConfig != null)
            {
                if (Target == ModelTarget.AICharacter)
                {
                    var nameKey = NameKey;
                    if (string.IsNullOrEmpty(nameKey)) return;
                    var effectiveNameKey = GetEffectiveAICharacterConfigKey(nameKey);
                    if (!ModEntry.IdleAudioConfig.IsAICharacterIdleAudioEnabled(effectiveNameKey))
                        return;
                }
                else
                {
                    if (!ModEntry.IdleAudioConfig.IsIdleAudioEnabled(Target))
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
            RecordOriginalModelOcclusionBody();
            RecordOriginalHeadCollider();
            RecordOriginalSoundMaker();

            if (CharacterMainControl.Health != null) CharacterMainControl.Health.OnDeadEvent.AddListener(OnDeath);

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

            var renderers = GetAllRenderers(instance);
            ReplaceRenderersLayer(renderers);

            if (ReplaceShader)
                ReplaceRenderersShader(renderers);

            return instance;
        }

        public void RegisterCustomSocketObject(GameObject customSocketObject)
        {
            if (customSocketObject == null) return;
            _currentUsingCustomSocketObjects.Add(customSocketObject);
            UpdateToCustomSocket(customSocketObject);
        }

        public void UnregisterCustomSocketObject(GameObject customSocketObject)
        {
            if (customSocketObject == null) return;
            _currentUsingCustomSocketObjects.Remove(customSocketObject);
            RestoreCustomSocketObject(customSocketObject);
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

        public Transform? GetOriginalSocketTransform(string socketName)
        {
            if (OriginalCharacterModel == null) return null;

            if (_originalModelLocators.TryGetValue(socketName, out var cachedLocator)
                && cachedLocator != null)
                return cachedLocator;

            return null;
        }

        public Transform? GetCustomSocketTransform(string socketName)
        {
            if (CustomModelInstance == null) return null;

            if (_customModelLocators.TryGetValue(socketName, out var cachedLocator)
                && cachedLocator != null)
                return cachedLocator;

            return null;
        }

        public void RestoreOriginalModel()
        {
            if (OriginalCharacterModel == null)
            {
                ModLogger.LogError("OriginalCharacterModel is not set.");
                return;
            }

            if (CharacterMainControl == null)
            {
                ModLogger.LogError("CharacterMainControl is not set.");
                return;
            }

            if (!IsHiddenOriginalModel) return;

            if (_customModelSubVisuals != null)
                CharacterMainControl.RemoveVisual(_customModelSubVisuals);

            RestoreToOriginalModelSockets();
            UpdateColliderHeight();

            if (OriginalCharacterSoundMaker != null)
                OriginalCharacterSoundMaker.enabled = true;

            var customFaceInstance = GetOriginalCustomFaceInstance();
            if (customFaceInstance != null) customFaceInstance.gameObject.SetActive(true);
            if (CustomModelInstance != null) CustomModelInstance.SetActive(false);

            ForceUpdateHealthBar();

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

            RestoreOriginalModel();

            if (CustomModelInstance != null)
            {
                CustomAnimator = null;

                DestroyImmediate(CustomModelInstance);
                CustomModelInstance = null;
            }

            _cachedCustomModelRenderers = null;

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

            _customModelLocators.Clear();
            _customModelSockets.Clear();
            _soundsByTag.Clear();
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

            if (CharacterMainControl == null)
            {
                ModLogger.LogError("CharacterMainControl is not set.");
                return;
            }

            if (_customModelSubVisuals != null)
                CharacterMainControl.AddSubVisuals(_customModelSubVisuals);

            ChangeToCustomModelSockets();
            UpdateColliderHeight();

            if (OriginalCharacterSoundMaker != null)
                OriginalCharacterSoundMaker.enabled = false;

            var customFaceInstance = GetOriginalCustomFaceInstance();
            if (customFaceInstance != null) customFaceInstance.gameObject.SetActive(false);

            CustomModelInstance.SetActive(true);

            ForceUpdateHealthBar();

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
            _currentModelBundleInfo = modelBundleInfo;
            _currentModelInfo = modelInfo;
            InitSoundFilePath(modelBundleInfo, modelInfo);
            InitializeDeathLootBoxPrefab(modelBundleInfo, modelInfo);
            InitializeCustomModelInternal(prefab, modelInfo);

            if (!HasIdleSounds()) return;
            if (Target == ModelTarget.AICharacter)
            {
                var nameKey = NameKey;
                if (ModEntry.IdleAudioConfig == null || string.IsNullOrEmpty(nameKey))
                {
                    ScheduleNextIdleAudio();
                }
                else
                {
                    var effectiveNameKey = GetEffectiveAICharacterConfigKey(nameKey);
                    if (ModEntry.IdleAudioConfig.IsAICharacterIdleAudioEnabled(effectiveNameKey))
                        ScheduleNextIdleAudio();
                }
            }
            else
            {
                if (ModEntry.IdleAudioConfig == null ||
                    ModEntry.IdleAudioConfig.IsIdleAudioEnabled(Target))
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

            _cachedCustomModelRenderers = GetAllRenderers(CustomModelInstance);
            ReplaceRenderersLayer(_cachedCustomModelRenderers);
            if (ReplaceShader)
                ReplaceRenderersShader(_cachedCustomModelRenderers);

            SetShowBackMaterial();
            InitializeCustomCharacterSubVisuals();
            InitializeCustomCharacterSoundMaker(modelInfo);

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

            var socketFields = OriginalModelSocketFieldInfos.ToArray();
            foreach (var (socketName, socketField) in socketFields)
            {
                var socketTransform = socketField.GetValue(OriginalCharacterModel) as Transform;
                if (socketTransform == null) continue;
                _originalModelSockets[socketField] = socketTransform;
                _originalModelLocators[socketName] = socketTransform;
            }
        }

        private void RestoreToOriginalModelSockets()
        {
            if (OriginalCharacterModel == null) return;

            RestoreCustomSocketObjects();
            foreach (var kvp in _originalModelSockets) ReplaceModelSocket(kvp.Key, kvp.Value);
        }

        private void RecordCustomModelSockets()
        {
            if (OriginalCharacterModel == null) return;

            _customModelSockets.Clear();

            var socketFields = OriginalModelSocketFieldInfos.ToArray();
            foreach (var locatorName in SocketNames.InternalSocketNames)
            {
                var locatorTransform = SearchLocatorTransform(CustomModelInstance!, locatorName);
                if (locatorTransform == null) continue;
                _customModelLocators[locatorName] = locatorTransform;

                var fieldInfo = socketFields.FirstOrDefault(f => f.Key == locatorName).Value;
                if (fieldInfo != null)
                    _customModelSockets[fieldInfo] = locatorTransform;
            }

            foreach (var locatorName in SocketNames.ExternalSocketNames)
            {
                var locatorTransform = SearchLocatorTransform(CustomModelInstance!, locatorName);
                if (locatorTransform == null) continue;
                _customModelLocators[locatorName] = locatorTransform;
            }
        }

        private void ChangeToCustomModelSockets()
        {
            if (OriginalCharacterModel == null) return;

            RestoreToOriginalModelSockets();
            foreach (var kvp in _customModelSockets) ReplaceModelSocket(kvp.Key, kvp.Value);
            UpdateCustomSocketObjects();
        }

        private Transform? GetOriginalCustomFaceInstance()
        {
            if (OriginalAnimationControl != null)
                return OriginalAnimationControl.animator.transform;
            return OriginalMagicBlendAnimationControl != null
                ? OriginalMagicBlendAnimationControl.animator.transform
                : null;
        }

        private void RecordOriginalModelOcclusionBody()
        {
            if (OriginalModelOcclusionBody != null) return;

            var originalCustomFaceInstance = GetOriginalCustomFaceInstance();
            if (originalCustomFaceInstance == null) return;

            var originalDuckBody = originalCustomFaceInstance.Find("DuckBody");
            if (originalDuckBody == null) return;

            OriginalModelOcclusionBody = originalDuckBody.gameObject;
        }

        private void RecordOriginalHeadCollider()
        {
            if (OriginalCharacterModel == null) return;

            var helmetTransform = GetOriginalSocketTransform(SocketNames.Helmet);
            if (helmetTransform == null) return;

            var headCollider = helmetTransform.GetComponentInChildren<HeadCollider>();
            if (headCollider == null) return;

            _headColliderObject = headCollider.gameObject;

            if (headCollider.gameObject.GetComponent<DontHideAsEquipment>() != null) return;
            headCollider.gameObject.AddComponent<DontHideAsEquipment>();
        }

        private void RecordOriginalSoundMaker()
        {
            if (OriginalCharacterSoundMaker != null) return;

            if (CharacterMainControl == null) return;

            var soundMaker = CharacterMainControl.GetComponent<CharacterSoundMaker>();
            if (soundMaker == null) return;

            OriginalCharacterSoundMaker = soundMaker;
        }

        private void UpdateColliderHeight()
        {
            if (_headColliderObject == null || CharacterMainControl == null) return;

            var mainDamageReceiver = CharacterMainControl.mainDamageReceiver;
            if (mainDamageReceiver == null) return;

            var capsuleCollider = mainDamageReceiver.GetComponent<CapsuleCollider>();
            if (capsuleCollider == null) return;

            var height = (float)(_headColliderObject.transform.localScale.y * 0.5 +
                _headColliderObject.transform.position.y - CharacterMainControl.transform.position.y + 0.5);
            capsuleCollider.height = height;
            capsuleCollider.center = Vector3.up * (height * 0.5f);
        }

        private void UpdateToCustomSocket(GameObject targetGameObject)
        {
            if (OriginalCharacterModel == null || targetGameObject == null)
                return;

            var customSocketMarker = targetGameObject.GetComponent<CustomSocketMarker>();
            if (customSocketMarker == null) return;

            var customSocketNames = customSocketMarker.CustomSocketNames;
            foreach (var socketName in customSocketNames)
            {
                var customSocket = GetSocketTransform(socketName);
                if (customSocket == null) continue;
                targetGameObject.transform.SetParent(customSocket, false);
                targetGameObject.transform.localPosition = Vector3.zero;
                targetGameObject.transform.localRotation = Quaternion.identity;
                targetGameObject.transform.localScale = Vector3.one;
                return;
            }
        }

        private Transform? GetSocketTransform(string socketName)
        {
            if (OriginalCharacterModel == null) return null;

            if (_customModelLocators.TryGetValue(socketName, out var cachedLocator)
                && cachedLocator != null)
                return cachedLocator;

            if (_originalModelLocators.TryGetValue(socketName, out var originalLocator)
                && originalLocator != null)
                return originalLocator;

            return null;
        }

        private void UpdateCustomSocketObjects()
        {
            if (OriginalCharacterModel == null || CustomModelInstance == null) return;

            var targets = _currentUsingCustomSocketObjects.ToArray();
            _currentUsingCustomSocketObjects.Clear();
            foreach (var customSocketObject in targets)
            {
                if (customSocketObject == null) continue;
                _currentUsingCustomSocketObjects.Add(customSocketObject);
                UpdateToCustomSocket(customSocketObject);
            }
        }

        private void RestoreCustomSocketObjects()
        {
            if (OriginalCharacterModel == null) return;

            foreach (var customSocketObject in _currentUsingCustomSocketObjects.ToArray())
            {
                if (customSocketObject == null)
                {
                    _currentUsingCustomSocketObjects.Remove(customSocketObject!);
                    continue;
                }

                RestoreCustomSocketObject(customSocketObject);
            }
        }

        private void RestoreCustomSocketObject(GameObject customSocketObject)
        {
            if (OriginalCharacterModel == null || customSocketObject == null) return;

            var customSocketMarker = customSocketObject.GetComponent<CustomSocketMarker>();
            if (customSocketMarker == null || customSocketMarker.OriginParent == null) return;

            customSocketObject.transform.SetParent(customSocketMarker.OriginParent, false);
            customSocketObject.transform.localPosition = customSocketMarker.SocketOffset ?? Vector3.zero;
            customSocketObject.transform.localRotation = customSocketMarker.SocketRotation ?? Quaternion.identity;
            customSocketObject.transform.localScale = customSocketMarker.SocketScale ?? Vector3.one;
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

        private void SetShowBackMaterial()
        {
            if (OriginalModelOcclusionBody == null) return;

            var originalSkinnedMeshRenderer =
                OriginalModelOcclusionBody.GetComponent<SkinnedMeshRenderer>();
            if (originalSkinnedMeshRenderer == null) return;

            var characterShowBackShader = GameCharacterShowBackShader;
            if (characterShowBackShader == null) return;

            var originalMaterial = originalSkinnedMeshRenderer.materials
                .FirstOrDefault(m => m != null && m.shader == characterShowBackShader);
            if (originalMaterial == null) return;

            var clonedMaterial = new Material(originalMaterial);
            var renderers = _cachedCustomModelRenderers?.OfType<SkinnedMeshRenderer>().ToArray();
            if (renderers == null || renderers.Length == 0) return;
            foreach (var renderer in renderers)
            {
                if (renderer.material == null) continue;
                if (renderer.materials.Any(m => m != null && m.shader == characterShowBackShader))
                    continue;
                renderer.materials = renderer.materials.Concat([clonedMaterial]).ToArray();
            }
        }

        private void ForceUpdateHealthBar()
        {
            if (CharacterMainControl == null || CharacterMainControl.Health == null) return;
            var healthBar = HealthBarManager.Instance.GetActiveHealthBar(CharacterMainControl.Health);
            if (healthBar == null) return;
            healthBar.RefreshOffset();
        }

        private void OnDeath(DamageInfo damageInfo)
        {
            if (!IsModelAudioEnabled) return;

            var soundPath = GetRandomSoundByTag(SoundTags.TriggerOnDeath);
            if (string.IsNullOrEmpty(soundPath)) return;

            AudioManager.PostCustomSFX(soundPath);
        }

        private void InitializeCustomCharacterSubVisuals()
        {
            if (CustomModelInstance == null || _customModelSubVisuals != null) return;

            var subVisuals = CustomModelInstance.GetComponent<CharacterSubVisuals>();
            if (subVisuals == null)
            {
                subVisuals = CustomModelInstance.AddComponent<CharacterSubVisuals>();
                subVisuals.renderers = [];
                subVisuals.particles = [];
                subVisuals.lights = [];
                subVisuals.sodaPointLights = [];
                subVisuals.character = CharacterMainControl;
                subVisuals.mainModel = OriginalCharacterModel;
            }

            _customModelSubVisuals = subVisuals;
            _customModelSubVisuals.SetRenderers();
        }

        private void InitializeCustomCharacterSoundMaker(ModelInfo? modelInfo = null)
        {
            if (CharacterMainControl == null || CustomModelInstance == null) return;

            var soundMaker = CustomModelInstance.GetComponent<CustomCharacterSoundMaker>();
            if (soundMaker == null)
            {
                soundMaker = CustomModelInstance.gameObject.AddComponent<CustomCharacterSoundMaker>();
                soundMaker.characterMainControl = CharacterMainControl;

                var originalSoundMaker = CharacterMainControl.GetComponent<CharacterSoundMaker>();
                if (originalSoundMaker != null)
                {
                    soundMaker.characterMainControl = originalSoundMaker.characterMainControl;
                    soundMaker.originalCharacterSoundMaker = originalSoundMaker;
                }
            }

            if (modelInfo != null)
            {
                if (modelInfo.WalkSoundFrequency.HasValue)
                    soundMaker.CustomWalkSoundFrequency = modelInfo.WalkSoundFrequency.Value;
                if (modelInfo.RunSoundFrequency.HasValue)
                    soundMaker.CustomRunSoundFrequency = modelInfo.RunSoundFrequency.Value;
            }

            _customCharacterSoundMaker = soundMaker;
        }

        private static string GetEffectiveAICharacterConfigKey(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey)) return AICharacters.AllAICharactersKey;

            var usingModel = ModEntry.UsingModel;
            if (usingModel == null) return nameKey;

            var modelID = usingModel.GetAICharacterModelID(nameKey);
            return !string.IsNullOrEmpty(modelID) ? nameKey : AICharacters.AllAICharactersKey;
        }

        private static Transform? SearchLocatorTransform(GameObject modelInstance, string locatorName)
        {
            var transforms = modelInstance.GetComponentsInChildren<Transform>(true);
            return transforms.FirstOrDefault(t => t.name == locatorName);
        }

        private static Renderer[] GetAllRenderers(GameObject targetGameObject)
        {
            return targetGameObject.GetComponentsInChildren<Renderer>(true);
        }

        private static void ReplaceRenderersLayer(Renderer[] renderers, string layerName = "Character")
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer == -1)
            {
                ModLogger.LogError($"Layer '{layerName}' not found.");
                return;
            }

            foreach (var renderer in renderers)
            {
                var gameObject = renderer.gameObject;
                if (gameObject.layer != layer)
                    gameObject.layer = layer;
            }
        }

        private static void ReplaceRenderersShader(Renderer[] renderers, string? shaderName = null)
        {
            var shader = shaderName != null ? Shader.Find(shaderName) : GameDefaultShader;
            if (shader == null)
            {
                ModLogger.LogError(shaderName != null
                    ? $"Shader '{shaderName}' not found."
                    : "Game default shader not found.");
                return;
            }

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

        // ReSharper disable once ShaderLabShaderReferenceNotResolved
        private static Shader GameCharacterShowBackShader => Shader.Find("CharacterShowBack");

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
            if (!IsModelAudioEnabled) return;

            var soundPath = GetRandomSoundByTag(SoundTags.Idle);
            if (string.IsNullOrEmpty(soundPath)) return;

            AudioManager.PostCustomSFX(soundPath);
        }

        private void ScheduleNextIdleAudio()
        {
            if (ModEntry.IdleAudioConfig == null)
            {
                _nextIdleAudioTime = Time.time + Random.Range(30f, 45f);
                return;
            }

            IdleAudioInterval interval;
            if (Target == ModelTarget.AICharacter)
            {
                var nameKey = NameKey;
                if (string.IsNullOrEmpty(nameKey))
                {
                    interval = ModEntry.IdleAudioConfig.GetIdleAudioInterval(Target);
                }
                else
                {
                    var effectiveNameKey = GetEffectiveAICharacterConfigKey(nameKey);
                    interval = ModEntry.IdleAudioConfig.GetAICharacterIdleAudioInterval(effectiveNameKey);
                }
            }
            else
            {
                interval = ModEntry.IdleAudioConfig.GetIdleAudioInterval(Target);
            }

            var randomInterval = Random.Range(interval.Min, interval.Max);
            _nextIdleAudioTime = Time.time + randomInterval;
        }

        #endregion
    }
}
