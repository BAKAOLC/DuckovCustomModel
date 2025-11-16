using System;

namespace DuckovCustomModel
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
#pragma warning disable CA2211
        public static ModBehaviour? Instance;
#pragma warning restore CA2211

        private void Awake()
        {
            if (Instance != null)
            {
                ModLogger.LogWarning("Multiple instances of ModBehaviour detected. Destroying duplicate.");
                Destroy(this);
                return;
            }

            Instance = this;
            ModLogger.Log($"{Constant.ModName} Loaded");
        }

#pragma warning disable CA1822
        private void OnEnable()
#pragma warning restore CA1822
        {
            ModLoader.Initialize();
            HarmonyLoader.Initialize();
        }

        private void OnDisable()
        {
            OnModDisabled?.Invoke();

            ModLoader.Uninitialize();
            HarmonyLoader.Uninitialize();
        }

#pragma warning disable CA1822
        private void OnDestroy()
#pragma warning restore CA1822
        {
            ModLoader.Uninitialize();
            HarmonyLoader.Uninitialize();

            Instance = null;
        }

        public event Action? OnModDisabled;
    }
}
