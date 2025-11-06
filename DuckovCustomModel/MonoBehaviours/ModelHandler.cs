using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DuckovCustomModel.Data;
using DuckovCustomModel.Managers;
using DuckovCustomModel.Utils;
using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    public class ModelHandler : MonoBehaviour
    {
        private static readonly FieldInfo[] OriginalModelSocketFieldInfos =
            CharacterModelSocketUtils.AllSocketFields;

        private readonly Dictionary<FieldInfo, Transform> _customModelSockets = [];
        private readonly Dictionary<FieldInfo, Transform> _originalModelSockets = [];

        public CharacterMainControl? CharacterMainControl { get; private set; }
        public CharacterModel? OriginalCharacterModel { get; private set; }
        public CharacterAnimationControl? OriginalAnimationControl { get; private set; }
        public CharacterAnimationControl_MagicBlend? OriginalMagicBlendAnimationControl { get; private set; }
        public Movement? OriginalMovement { get; private set; }
        public bool IsHiddenOriginalModel { get; private set; }

        public GameObject? CustomModelInstance { get; private set; }
        public Animator? CustomAnimator { get; private set; }
        public CustomAnimatorControl? CustomAnimatorControl { get; private set; }

        // ReSharper disable once ShaderLabShaderReferenceNotResolved
        private static Shader GameDefaultShader => Shader.Find("SodaCraft/SodaCharacter");

        public void Initialize(CharacterMainControl characterMainControl)
        {
            CharacterMainControl = characterMainControl;
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

            IsHiddenOriginalModel = false;
            ModLogger.Log("Restored to original model.");
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

            IsHiddenOriginalModel = true;
            ModLogger.Log("Changed to custom model.");
        }

        public void InitializeCustomModel(ModelBundleInfo modelBundleInfo, ModelInfo modelInfo)
        {
            var prefab = AssetBundleManager.LoadModelPrefab(modelBundleInfo, modelInfo);
            if (prefab == null)
            {
                ModLogger.LogError("Failed to load custom model prefab.");
                return;
            }

            InitializeCustomModel(prefab);
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

            if (CustomModelInstance != null)
            {
                Destroy(CustomModelInstance);
                _customModelSockets.Clear();
            }

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

            ModLogger.Log("Custom model initialized successfully.");

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
            return OriginalCharacterModel == null ? null : OriginalCharacterModel.transform.Find("CustomFaceInstance");
        }

        private void ReplaceModelSocket(FieldInfo socketField, Transform? newSocket)
        {
            if (OriginalCharacterModel == null || newSocket == null) return;
            var originalSocket = socketField.GetValue(OriginalCharacterModel) as Transform;
            if (originalSocket == newSocket) return;
            var originalChildren = new List<Transform>();
            if (originalSocket != null) originalChildren.AddRange(originalSocket.Cast<Transform>());
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
                material.DisableKeyword("_EMISSION");
            }
        }

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
    }
}