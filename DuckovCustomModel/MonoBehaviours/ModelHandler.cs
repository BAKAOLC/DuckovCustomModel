using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DuckovCustomModel.Data;
using DuckovCustomModel.Managers;
using HarmonyLib;
using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    public class ModelHandler : MonoBehaviour
    {
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
            if (OriginalCharacterModel == null) return;
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
            if (OriginalCharacterModel == null) return;

            var customFaceInstance = GetOriginalCustomFaceInstance();
            if (customFaceInstance != null) customFaceInstance.gameObject.SetActive(false);

            if (CustomModelInstance != null)
            {
                CustomModelInstance.SetActive(true);
                foreach (var kvp in _customModelSockets) ReplaceModelSocket(kvp.Key, kvp.Value);
            }

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
                var anchorTransform = SearchAnchorTransform(CustomModelInstance!, kvp.Value);
                if (anchorTransform != null) _customModelSockets[kvp.Key] = anchorTransform;
            }
        }

        private void ChangeToCustomModelSockets()
        {
            if (OriginalCharacterModel == null) return;

            foreach (var kvp in _customModelSockets) ReplaceModelSocket(kvp.Key, kvp.Value);
        }

        private Transform? GetOriginalCustomFaceInstance()
        {
            return OriginalCharacterModel == null ? null : OriginalCharacterModel.transform.Find("CustomFaceInstance");
        }

        private void ReplaceModelSocket(FieldInfo socketField, Transform? newSocket)
        {
            if (OriginalCharacterModel == null || newSocket == null) return;
            socketField.SetValue(OriginalCharacterModel, newSocket);
        }

        private static Transform? SearchAnchorTransform(GameObject modelInstance, string anchorName)
        {
            var transforms = modelInstance.GetComponentsInChildren<Transform>(true);
            return transforms.FirstOrDefault(t => t.name == anchorName);
        }

        #region Original Sockets FieldInfos

        private static readonly FieldInfo LefthandSocket = AccessTools.Field(typeof(CharacterModel), "lefthandSocket");

        private static readonly FieldInfo
            RightHandSocket = AccessTools.Field(typeof(CharacterModel), "rightHandSocket");

        private static readonly FieldInfo ArmorSocket = AccessTools.Field(typeof(CharacterModel), "armorSocket");
        private static readonly FieldInfo HelmatSocket = AccessTools.Field(typeof(CharacterModel), "helmatSocket");
        private static readonly FieldInfo FaceSocket = AccessTools.Field(typeof(CharacterModel), "faceSocket");
        private static readonly FieldInfo BackpackSocket = AccessTools.Field(typeof(CharacterModel), "backpackSocket");

        private static readonly FieldInfo MeleeWeaponSocket =
            AccessTools.Field(typeof(CharacterModel), "meleeWeaponSocket");

        private static readonly FieldInfo PopTextSocket = AccessTools.Field(typeof(CharacterModel), "popTextSocket");

        private static readonly FieldInfo[] OriginalModelSocketFieldInfos =
        [
            LefthandSocket,
            RightHandSocket,
            ArmorSocket,
            HelmatSocket,
            FaceSocket,
            BackpackSocket,
            MeleeWeaponSocket,
            PopTextSocket,
        ];

        #endregion

        #region Custom Sockets

        private const string LeftHandAnchorName = "LeftHandAnchor";
        private const string RightHandAnchorName = "RightHandAnchor";
        private const string ArmorAnchorName = "ArmorAnchor";
        private const string HelmatAnchorName = "HelmatAnchor";
        private const string FaceAnchorName = "FaceAnchor";
        private const string BackpackAnchorName = "BackpackAnchor";
        private const string MeleeWeaponAnchorName = "MeleeWeaponAnchor";
        private const string PopTextAnchorName = "PopTextAnchor";

        private static readonly Dictionary<FieldInfo, string> CustomModelSocketNames = new()
        {
            { LefthandSocket, LeftHandAnchorName },
            { RightHandSocket, RightHandAnchorName },
            { ArmorSocket, ArmorAnchorName },
            { HelmatSocket, HelmatAnchorName },
            { FaceSocket, FaceAnchorName },
            { BackpackSocket, BackpackAnchorName },
            { MeleeWeaponSocket, MeleeWeaponAnchorName },
            { PopTextSocket, PopTextAnchorName },
        };

        #endregion
    }
}