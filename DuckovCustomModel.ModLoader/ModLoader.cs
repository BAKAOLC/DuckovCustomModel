using System;
using System.IO;
using System.Reflection;

namespace DuckovCustomModel
{
    public static class ModLoader
    {
        private static Assembly? _loadedAssembly;
        private static string? _modDirectory;

        public static void Initialize()
        {
            Uninitialize();
            _modDirectory = Path.GetDirectoryName(typeof(ModLoader).Assembly.Location);
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            HarmonyLoader.OnReadyToPatch += OnReadyToPatch;
        }

        public static void Uninitialize()
        {
            HarmonyLoader.OnReadyToPatch -= OnReadyToPatch;
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;

            OnModDisabled();
        }

        private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            if (_modDirectory == null) return null;

            var assemblyName = new AssemblyName(args.Name);
            var assemblyFileName = $"{assemblyName.Name}.dll";
            var assemblyPath = Path.Combine(_modDirectory, assemblyFileName);

            if (!File.Exists(assemblyPath)) return null;
            try
            {
                ModLogger.Log($"Resolving assembly: {assemblyFileName} from {assemblyPath}");
                var bytes = File.ReadAllBytes(assemblyPath);
                return Assembly.Load(bytes);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to load assembly {assemblyFileName}: {ex}");
            }

            return null;
        }

        private static void OnReadyToPatch()
        {
            var path = Path.GetDirectoryName(typeof(ModLoader).Assembly.Location);
            if (path == null)
            {
                ModLogger.LogError("Failed to get assembly directory.");
                return;
            }

            var targetAssemblyFile = Path.Combine(path, Constant.TargetAssemblyName);
            if (!File.Exists(targetAssemblyFile))
            {
                ModLogger.LogError($"Target assembly not found: {targetAssemblyFile}");
                return;
            }

            try
            {
                ModLogger.Log($"Loading Assembly from: {targetAssemblyFile}");

                var bytes = File.ReadAllBytes(targetAssemblyFile);
                var targetAssembly = Assembly.Load(bytes);
                _loadedAssembly = targetAssembly;

                ModLogger.Log("Invoking ModEntry.Initialize...");

                InvokeModEntryMethodInitialize();

                if (ModBehaviour.Instance != null)
                {
                    ModBehaviour.Instance.OnModDisabled -= OnModDisabled;
                    ModBehaviour.Instance.OnModDisabled += OnModDisabled;
                }

                ModLogger.Log("ModLoader initialization complete.");
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error loading target assembly or applying patches: {ex}");
            }
        }

        private static void OnModDisabled()
        {
            if (_loadedAssembly == null) return;

            if (ModBehaviour.Instance != null) ModBehaviour.Instance.OnModDisabled -= OnModDisabled;

            ModLogger.Log("Uninitializing Mod...");

            InvokeModEntryMethodUninitialize();
            _loadedAssembly = null;

            ModLogger.Log("Mod uninitialization complete.");
        }

        private static void InvokeModEntryMethodInitialize()
        {
            InvokeModEntryMethod("Initialize", [_modDirectory]);
        }

        private static void InvokeModEntryMethodUninitialize()
        {
            InvokeModEntryMethod("Uninitialize");
        }

        private static void InvokeModEntryMethod(string methodName, object?[]? parameters = null)
        {
            if (_loadedAssembly == null)
            {
                ModLogger.LogError("Target assembly is not loaded. Cannot invoke ModEntry methods.");
                return;
            }

            var modEntryType = _loadedAssembly.GetType($"{Constant.ModId}.ModEntry");
            if (modEntryType == null)
            {
                ModLogger.LogError("ModEntry type not found in target assembly.");
                return;
            }

            MethodInfo? method;
            if (parameters != null)
            {
                var parameterTypes = new Type[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                    parameterTypes[i] = parameters[i]?.GetType() ?? typeof(object);
                method = modEntryType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null,
                    parameterTypes, null);
            }
            else
            {
                method = modEntryType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            }

            if (method == null)
            {
                ModLogger.LogError($"ModEntry.{methodName} method not found.");
                return;
            }

            try
            {
                method.Invoke(null, parameters);
                ModLogger.Log($"ModEntry.{methodName} invoked successfully.");
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error invoking ModEntry.{methodName}: {ex}");
            }
        }
    }
}
