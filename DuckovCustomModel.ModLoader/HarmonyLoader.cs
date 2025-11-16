using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Duckov.Modding;

namespace DuckovCustomModel
{
    public static class HarmonyLoader
    {
        public static event Action? OnReadyToPatch;

        public static void Initialize()
        {
            if (!LoadHarmony())
            {
                ModLogger.LogError("Failed to load Harmony. Waiting for mod activation to retry.");
                RegisterModActivatedEvents();
                return;
            }

            ModLogger.Log("Harmony Initialized Successfully");
            ModLogger.Log("Triggering ReadyToPatch Event");
            ReadyToPatch();
        }

        public static void Uninitialize()
        {
            // Currently, no uninitialization logic is required.
        }

        private static void RegisterModActivatedEvents()
        {
            UnregisterModActivatedEvents();
            ModManager.OnModActivated += OnModActivated;
        }

        private static void UnregisterModActivatedEvents()
        {
            ModManager.OnModActivated -= OnModActivated;
        }

        private static void ReadyToPatch()
        {
            OnReadyToPatch?.Invoke();
        }

        private static void OnModActivated(ModInfo modInfo, Duckov.Modding.ModBehaviour modBehaviour)
        {
            if (modBehaviour.GetType().Assembly == typeof(HarmonyLoader).Assembly) return;
            ModLogger.Log($"Mod Activated: {modInfo.name}. Attempting to initialize Harmony again.");

            if (!LoadHarmony()) return;

            ModLogger.Log("Harmony Initialized Successfully on Mod Activation");
            UnregisterModActivatedEvents();
            ReadyToPatch();
        }

        private static bool LoadHarmony()
        {
            try
            {
                var harmonyType = Type.GetType("HarmonyLib.Harmony, 0Harmony");
                if (harmonyType != null) return true;
                if (!FindHarmonyLibLocally(out var harmonyAssembly))
                {
                    ModLogger.LogError("HarmonyLib not found. Please ensure Harmony is installed.");
                    return false;
                }

                harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony");
                if (harmonyType != null) return true;
                ModLogger.LogError("HarmonyLib.Harmony type not found in Harmony assembly.");
                return false;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error initializing Harmony: {ex}");
            }

            return false;
        }

        private static bool FindHarmonyLibLocally([NotNullWhen(true)] out Assembly? harmonyAssembly)
        {
            harmonyAssembly = null;
            try
            {
                var path = Path.GetDirectoryName(typeof(HarmonyLoader).Assembly.Location);
                if (path == null) return false;

                var targetAssemblyFile = Path.Combine(path, "0Harmony.dll");
                if (!File.Exists(targetAssemblyFile)) return false;

                try
                {
                    ModLogger.Log($"Loading Assembly from: {targetAssemblyFile}");

                    var bytes = File.ReadAllBytes(targetAssemblyFile);
                    var targetAssembly = Assembly.Load(bytes);
                    harmonyAssembly = targetAssembly;

                    ModLogger.Log("HarmonyLib Assembly Loaded Successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    ModLogger.LogError($"Error loading HarmonyLib assembly: {ex}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error finding HarmonyLib assembly: {ex}");
            }

            return false;
        }
    }
}
