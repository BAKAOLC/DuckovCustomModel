using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel
{
    // ReSharper disable MemberCanBeMadeStatic.Local
    public static class ModEntry
    {
        private static ConfigWindow? _configWindow;
        private static Harmony? _harmony;
        public static HideEquipmentConfig? HideEquipmentConfig;
        public static IdleAudioConfig? IdleAudioConfig;
        public static ModelAudioConfig? ModelAudioConfig;
        public static UIConfig? UIConfig;
        public static UsingModel? UsingModel;

        private static bool _initialized;

        public static string? ModDirectory { get; private set; }

        public static void Initialize(string modDirectory)
        {
            if (_initialized)
            {
                ModLogger.LogWarning($"{Constant.ModName} is already initialized.");
                return;
            }

            ModDirectory = modDirectory;
            ModLogger.Log($"Initializing {Constant.ModName}...");

            _initialized = true;

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

            ModLogger.Log($"{Constant.ModName} loaded (Version {Constant.ModVersion})");
        }

        public static void Uninitialize()
        {
            if (!_initialized)
            {
                ModLogger.LogWarning($"{Constant.ModName} is not initialized.");
                return;
            }

            ModLogger.Log($"Unloading {Constant.ModName}...");

            UnloadConfig();

            var unpatched = UnpatchAll();
            if (!unpatched) ModLogger.LogError("Unable to remove Harmony patches, the mod may not unload correctly.");

            LevelManager.OnLevelBeginInitializing -= LevelManager_OnLevelBeginInitializing;
            LevelManager.OnLevelInitialized -= LevelManager_OnLevelInitialized;
            LevelManager.OnAfterLevelInitialized -= LevelManager_OnAfterLevelInitialized;

            ModelListManager.CancelRefresh();

            Localization.Cleanup();

            if (_configWindow != null)
            {
                Object.Destroy(_configWindow.gameObject);
                _configWindow = null;
            }

            _initialized = false;

            ModLogger.Log($"{Constant.ModName} unloaded.");
        }

        private static bool PatchAll()
        {
            _harmony = new(Constant.HarmonyId);

            return PatchAllInternalMethodA() || PatchAllInternalMethodB();
        }

        private static bool UnpatchAll()
        {
            if (_harmony == null) return true;

            return UnpatchAllInternalMethodA() || UnpatchAllInternalMethodB();
        }

        private static bool PatchAllInternalMethodA()
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
                return false;
            }
        }

        private static bool UnpatchAllInternalMethodA()
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
                return false;
            }
        }

        private static bool PatchAllInternalMethodB()
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
                return false;
            }
        }

        private static bool UnpatchAllInternalMethodB()
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
                return false;
            }
        }

        private static void LoadConfig()
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

        private static void UnloadConfig()
        {
            UIConfig = null;
            UsingModel = null;
            HideEquipmentConfig = null;
            IdleAudioConfig = null;
            ModelAudioConfig = null;
        }

        private static void LevelManager_OnLevelBeginInitializing()
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

        private static void LevelManager_OnLevelInitialized()
        {
            var mainCharacterControl = LevelManager.Instance.MainCharacter;
            var petCharacterControl = LevelManager.Instance.PetCharacter;
            InitializeModelHandlerToCharacter(mainCharacterControl, "MainCharacter");
            InitializeModelHandlerToCharacter(petCharacterControl, "PetCharacter", ModelTarget.Pet);
        }

        private static void LevelManager_OnAfterLevelInitialized()
        {
            ModelListManager.ApplyAllModelsFromConfig();
        }

        private static void InitializeConfigWindow()
        {
            if (_configWindow != null) return;

            var uiObject = new GameObject("ConfigWindow");
            _configWindow = uiObject.AddComponent<ConfigWindow>();
            Object.DontDestroyOnLoad(uiObject);
            ModLogger.Log("ConfigWindow initialized.");
        }

        private static void InitializeModelHandlerToCharacter(CharacterMainControl characterMainControl,
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
