using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.MonoBehaviours;
using HarmonyLib;
using UnityEngine;

namespace DuckovCustomModel
{
    // ReSharper disable MemberCanBeMadeStatic.Local
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static ModBehaviour? Instance;
        private Harmony? _harmony;
        private ModelSelectorUI? _modelSelectorUI;
        public HideEquipmentConfig? HideEquipmentConfig;
        public UIConfig? UIConfig;
        public UsingModel? UsingModel;

        private void Awake()
        {
            if (Instance != null)
            {
                ModLogger.LogError("Multiple instances of ModBehaviour detected! There should only be one instance.");
                Destroy(this);
                return;
            }

            Instance = this;
            ModLogger.Log($"{Constant.ModName} loaded (Version {Constant.ModVersion})");
        }

        private void OnEnable()
        {
            LoadConfig();

            var patched = PatchAll();
            if (!patched) ModLogger.LogError("Unable to apply Harmony patches, the mod may not function correctly.");

            LevelManager.OnLevelBeginInitializing += LevelManager_OnLevelBeginInitializing;
            LevelManager.OnLevelInitialized += LevelManager_OnLevelInitialized;
            LevelManager.OnAfterLevelInitialized += LevelManager_OnAfterLevelInitialized;

            _ = ModelManager.UpdateModelBundles();

            var priorityModelIDs = new List<string>();
            if (UsingModel != null)
                priorityModelIDs.AddRange(from ModelTarget target in Enum.GetValues(typeof(ModelTarget))
                    select UsingModel.GetModelID(target)
                    into modelID
                    where !string.IsNullOrEmpty(modelID)
                    select modelID);

            ModelListManager.RefreshModelList(priorityModelIDs);

            InitializeModelSelectorUI();
        }

        private void OnDisable()
        {
            UnloadConfig();

            var unpatched = UnpatchAll();
            if (!unpatched) ModLogger.LogError("Unable to remove Harmony patches, the mod may not unload correctly.");

            LevelManager.OnLevelBeginInitializing -= LevelManager_OnLevelBeginInitializing;
            LevelManager.OnLevelInitialized -= LevelManager_OnLevelInitialized;
            LevelManager.OnAfterLevelInitialized -= LevelManager_OnAfterLevelInitialized;

            ModelListManager.CancelRefresh();

            ModelSelectorUILocalization.Cleanup();

            if (_modelSelectorUI == null) return;
            Destroy(_modelSelectorUI);
            _modelSelectorUI = null;
        }

        private void OnDestroy()
        {
            Instance = null;
            UnloadConfig();

            var unpatched = UnpatchAll();
            if (!unpatched)
                ModLogger.LogError("Unable to remove Harmony patches, the mod may not unload correctly.");
        }

        private bool PatchAll()
        {
            try
            {
                _harmony = new(Constant.HarmonyId);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                ModLogger.Log("Successfully applied Harmony patches");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Unable to apply Harmony patches: {ex}");
                return false;
            }
        }

        private bool UnpatchAll()
        {
            try
            {
                if (_harmony == null) return true;
                _harmony.UnpatchAll(_harmony.Id);
                _harmony = null;
                ModLogger.Log("Harmony patches removed successfully");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Unable to remove Harmony patches: {ex}");
                return false;
            }
        }

        private void LoadConfig()
        {
            UIConfig = ConfigManager.LoadConfigFromFile<UIConfig>("UIConfig.json");
            if (UIConfig.Validate()) ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");

            UsingModel = ConfigManager.LoadConfigFromFile<UsingModel>("UsingModel.json");
            if (UsingModel.Validate()) ConfigManager.SaveConfigToFile(UsingModel, "UsingModel.json");

            HideEquipmentConfig = ConfigManager.LoadConfigFromFile<HideEquipmentConfig>("HideEquipmentConfig.json");
            if (HideEquipmentConfig.Validate())
                ConfigManager.SaveConfigToFile(HideEquipmentConfig, "HideEquipmentConfig.json");

            ModLogger.Log("Configuration files loaded successfully");
        }

        private void UnloadConfig()
        {
            UIConfig = null;
            UsingModel = null;
            HideEquipmentConfig = null;
        }

        private void LevelManager_OnLevelBeginInitializing()
        {
            var priorityModelIDs = new List<string>();
            if (UsingModel != null)
            {
                priorityModelIDs.AddRange(from ModelTarget target in Enum.GetValues(typeof(ModelTarget))
                    where target != ModelTarget.AICharacter
                    select UsingModel.GetModelID(target)
                    into modelID
                    where !string.IsNullOrEmpty(modelID)
                    select modelID);

                priorityModelIDs.AddRange(AICharacters.SupportedAICharacters
                    .Select(nameKey => UsingModel.GetAICharacterModelID(nameKey))
                    .Where(modelID => !string.IsNullOrEmpty(modelID)));
            }

            ModelListManager.RefreshModelList(priorityModelIDs);
        }

        private void LevelManager_OnLevelInitialized()
        {
            var mainCharacterControl = LevelManager.Instance.MainCharacter;
            var petCharacterControl = LevelManager.Instance.PetCharacter;
            InitializeModelHandlerToCharacter(mainCharacterControl, "MainCharacter");
            InitializeModelHandlerToCharacter(petCharacterControl, "PetCharacter", ModelTarget.Pet);
        }

        private void LevelManager_OnAfterLevelInitialized()
        {
            ModelListManager.ApplyAllModelsFromConfig();
        }

        private void InitializeModelSelectorUI()
        {
            if (_modelSelectorUI != null) return;

            var uiObject = new GameObject("ModelSelectorUI");
            _modelSelectorUI = uiObject.AddComponent<ModelSelectorUI>();
            DontDestroyOnLoad(uiObject);
            ModLogger.Log("ModelSelectorUI initialized.");
        }

        private void InitializeModelHandlerToCharacter(CharacterMainControl characterMainControl,
            string characterName, ModelTarget target = ModelTarget.Character)
        {
            if (characterMainControl == null)
            {
                ModLogger.LogError($"Initialize ModelHandler to {characterName} failed: CharacterMainControl is null");
                return;
            }

            var modelHandler = ModelManager.InitializeModelHandler(characterMainControl, target);
            if (modelHandler != null) return;
            ModLogger.LogError($"Initialize ModelHandler to {characterName} failed: ModelHandler is null");
        }
    }
    // ReSharper restore MemberCanBeMadeStatic.Local
}