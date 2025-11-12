using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI;
using HarmonyLib;
using UnityEngine;

namespace DuckovCustomModel
{
    // ReSharper disable MemberCanBeMadeStatic.Local
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static ModBehaviour? Instance;
        private ConfigWindow? _configWindow;
        private Harmony? _harmony;
        public HideEquipmentConfig? HideEquipmentConfig;
        public IdleAudioConfig? IdleAudioConfig;
        public ModelAudioConfig? ModelAudioConfig;
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

            InitializeConfigWindow();
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

            Localization.Cleanup();

            if (_configWindow == null) return;
            Destroy(_configWindow.gameObject);
            _configWindow = null;
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
            _harmony = new(Constant.HarmonyId);

            return PatchAllInternalMethodA() || PatchAllInternalMethodB();
        }

        private bool UnpatchAll()
        {
            if (_harmony == null) return true;

            return UnpatchAllInternalMethodA() || UnpatchAllInternalMethodB();
        }

        private bool PatchAllInternalMethodA()
        {
            try
            {
                if (_harmony == null) return false;
                var patchClassProcessors = AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())
                    .Where(type => type.HasHarmonyAttribute())
                    .Select(_harmony.CreateClassProcessor);

                var successCount = 0;
                var failCount = 0;
                patchClassProcessors.DoIf(processor => string.IsNullOrEmpty(processor.Category),
                    delegate(PatchClassProcessor processor)
                    {
                        try
                        {
                            processor.Patch();
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            ModLogger.LogError(ex.ToString());
                            failCount++;
                        }
                    });

                if (successCount > 0)
                    ModLogger.Log($"Applied {successCount} Harmony patch(es) successfully by method A");
                if (failCount > 0)
                    ModLogger.LogError($"Failed to apply {failCount} Harmony patch(es) by method A");

                return successCount > 0;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to apply Harmony patches by method A: {ex}");
                throw;
            }
        }

        private bool UnpatchAllInternalMethodA()
        {
            try
            {
                if (_harmony == null) return true;
                var patchedMethods = _harmony.GetPatchedMethods().ToArray();
                var successCount = 0;
                var failCount = 0;
                foreach (var method in patchedMethods)
                    try
                    {
                        _harmony.Unpatch(method, HarmonyPatchType.All, Constant.HarmonyId);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        ModLogger.LogError(ex.ToString());
                        failCount++;
                    }

                if (successCount > 0)
                    ModLogger.Log($"Removed {successCount} Harmony patch(es) successfully by method A");
                if (failCount > 0)
                    ModLogger.LogError($"Failed to remove {failCount} Harmony patch(es) by method A");

                return failCount == 0;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to remove Harmony patches by method A: {ex}");
                throw;
            }
        }

        private bool PatchAllInternalMethodB()
        {
            try
            {
                if (_harmony == null) return false;
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                ModLogger.Log("Applied Harmony patches successfully by method B");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to apply Harmony patches by method B: {ex}");
                throw;
            }
        }

        private bool UnpatchAllInternalMethodB()
        {
            try
            {
                if (_harmony == null) return true;
                _harmony.UnpatchAll(Constant.HarmonyId);
                ModLogger.Log("Removed Harmony patches successfully by method B");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to remove Harmony patches by method B: {ex}");
                throw;
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

            IdleAudioConfig = ConfigManager.LoadConfigFromFile<IdleAudioConfig>("IdleAudioConfig.json");
            if (IdleAudioConfig.Validate())
                ConfigManager.SaveConfigToFile(IdleAudioConfig, "IdleAudioConfig.json");

            ModelAudioConfig = ConfigManager.LoadConfigFromFile<ModelAudioConfig>("ModelAudioConfig.json");
            if (ModelAudioConfig.Validate())
                ConfigManager.SaveConfigToFile(ModelAudioConfig, "ModelAudioConfig.json");

            ModLogger.Log("Configuration files loaded successfully");
        }

        private void UnloadConfig()
        {
            UIConfig = null;
            UsingModel = null;
            HideEquipmentConfig = null;
            IdleAudioConfig = null;
            ModelAudioConfig = null;
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

                var aiModelIDs = new HashSet<string>();
                foreach (var nameKey in AICharacters.SupportedAICharacters)
                {
                    var modelID = UsingModel.GetAICharacterModelID(nameKey);
                    if (!string.IsNullOrEmpty(modelID))
                        aiModelIDs.Add(modelID);
                }

                var defaultModelID = UsingModel.GetAICharacterModelID(AICharacters.AllAICharactersKey);
                if (!string.IsNullOrEmpty(defaultModelID))
                    aiModelIDs.Add(defaultModelID);

                priorityModelIDs.AddRange(aiModelIDs);
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

        private void InitializeConfigWindow()
        {
            if (_configWindow != null) return;

            var uiObject = new GameObject("ConfigWindow");
            _configWindow = uiObject.AddComponent<ConfigWindow>();
            DontDestroyOnLoad(uiObject);
            ModLogger.Log("ConfigWindow initialized.");
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