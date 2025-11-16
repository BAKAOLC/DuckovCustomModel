using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Core.Data;

namespace DuckovCustomModel.Managers
{
    public static class ModelListManager
    {
        private static CancellationTokenSource? _refreshCancellationTokenSource;
        private static UniTaskCompletionSource? _refreshCompletionSource;

        private static Dictionary<ModelTarget, string>? _refreshStartModelIDs;
        private static HashSet<string>? _currentRefreshingBundles;

        public static bool IsRefreshing { get; private set; }
        public static IReadOnlyCollection<string>? CurrentRefreshingBundles => _currentRefreshingBundles;

        public static event Action? OnRefreshStarted;
        public static event Action? OnRefreshCompleted;
        public static event Action<string>? OnRefreshProgress;

        public static void RefreshModelList(IEnumerable<string>? priorityModelIDs = null)
        {
            if (IsRefreshing)
            {
                _refreshCancellationTokenSource?.Cancel();
                _refreshCancellationTokenSource?.Dispose();
            }

            _refreshCancellationTokenSource = new();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _refreshCancellationTokenSource.Token,
                CancellationToken.None
            );

            _refreshCompletionSource = new();
            RefreshModelListAsync(linkedCts.Token, linkedCts, _refreshCompletionSource, priorityModelIDs).Forget();
        }

        public static async UniTask WaitForRefreshCompletion()
        {
            if (_refreshCompletionSource != null) await _refreshCompletionSource.Task;
        }

        private static async UniTaskVoid RefreshModelListAsync(CancellationToken cancellationToken,
            CancellationTokenSource? linkedCts, UniTaskCompletionSource? completionSource,
            IEnumerable<string>? priorityModelIDs)
        {
            IsRefreshing = true;

            _refreshStartModelIDs = new();
            if (ModEntry.UsingModel != null)
                foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
                {
                    var modelID = ModEntry.UsingModel.GetModelID(target);
                    if (!string.IsNullOrEmpty(modelID))
                        _refreshStartModelIDs[target] = modelID;
                }

            OnRefreshStarted?.Invoke();

            var priorityModels = new List<(ModelBundleInfo bundle, ModelInfo model)>();

            if (priorityModelIDs != null)
                foreach (var priorityModelID in priorityModelIDs)
                {
                    if (string.IsNullOrEmpty(priorityModelID)) continue;
                    if (ModelManager.FindModelByID(priorityModelID, out var bundleInfo, out var modelInfo))
                        priorityModels.Add((bundleInfo, modelInfo));
                }

            var needsTemporaryRestore = false;
            var targetsToRestore = new Dictionary<ModelTarget, bool>();

            try
            {
                var bundlesToReload = ModelManager.UpdateModelBundles();
                _currentRefreshingBundles = bundlesToReload;

                foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
                {
                    var handlers = ModelManager.GetAllModelHandlers(target);
                    if (handlers.Count == 0) continue;

                    var modelID = ModEntry.UsingModel?.GetModelID(target) ?? string.Empty;
                    if (string.IsNullOrEmpty(modelID)) continue;

                    if (!ModelManager.FindModelByID(modelID, out var bundleInfo, out _)) continue;

                    var bundleName = bundleInfo.BundleName;
                    if (!bundlesToReload.Contains(bundleName)) continue;

                    var needsRestore = false;
                    foreach (var handler in handlers.Where(handler => handler.IsHiddenOriginalModel))
                    {
                        needsRestore = true;
                        handler.CleanupCustomModel();
                    }

                    if (!needsRestore) continue;
                    needsTemporaryRestore = true;
                    targetsToRestore[target] = true;
                    ModLogger.Log(
                        $"Temporarily cleaned up {target} custom model for bundle update: {bundleName}");
                }

                if (bundlesToReload.Count > 0)
                {
                    if (priorityModels.Count > 0)
                        foreach (var (priorityBundle, priorityModel) in priorityModels)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            OnRefreshProgress?.Invoke($"Loading priority model: {priorityModel.Name}");
                            await AssetBundleManager.GetOrLoadAssetBundleAsync(priorityBundle, false,
                                cancellationToken);
                            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                        }

                    var totalCount = ModelManager.ModelBundles.Sum(b => b.Models.Length);
                    var count = 0;

                    if (priorityModels.Count > 0)
                        foreach (var (priorityBundle, priorityModel) in priorityModels)
                        {
                            var priorityBundleInList =
                                ModelManager.ModelBundles.FirstOrDefault(b => b == priorityBundle);
                            var priorityModelInList =
                                priorityBundleInList?.Models.FirstOrDefault(m => m == priorityModel);
                            if (priorityModelInList == null) continue;
                            cancellationToken.ThrowIfCancellationRequested();
                            if (priorityBundleInList != null)
                                await AssetBundleManager.CheckBundleStatusAsync(priorityBundleInList,
                                    priorityModelInList,
                                    cancellationToken);
                            count++;
                            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                        }

                    foreach (var bundle in ModelManager.ModelBundles)
                    foreach (var model in bundle.Models)
                    {
                        if (priorityModels.Any(pm => pm.bundle == bundle && pm.model == model))
                            continue;

                        cancellationToken.ThrowIfCancellationRequested();
                        await AssetBundleManager.CheckBundleStatusAsync(bundle, model, cancellationToken);
                        count++;

                        if (count % 10 != 0) continue;
                        OnRefreshProgress?.Invoke($"Loading... ({count}/{totalCount})");
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                if (needsTemporaryRestore)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                    foreach (var target in targetsToRestore.Keys)
                    {
                        if (!targetsToRestore[target]) continue;

                        var currentModelID = ModEntry.UsingModel?.GetModelID(target) ?? string.Empty;
                        if (string.IsNullOrEmpty(currentModelID)) continue;

                        var refreshStartModelID = _refreshStartModelIDs?.GetValueOrDefault(target);
                        if (currentModelID != refreshStartModelID)
                        {
                            ModLogger.Log(
                                $"Model for {target} was changed during refresh (from {refreshStartModelID} to {currentModelID}), skipping auto-restore");
                            continue;
                        }

                        if (!ModelManager.FindModelByID(currentModelID, out _, out var modelInfo))
                            continue;

                        if (!modelInfo.CompatibleWithType(target)) continue;

                        ApplyModelToTarget(target, currentModelID, true);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error during model list refresh: {ex.Message}");
                ModLogger.LogException(ex);
            }
            finally
            {
                IsRefreshing = false;
                _refreshStartModelIDs = null;
                _currentRefreshingBundles = null;
                completionSource?.TrySetResult();
                _refreshCompletionSource = null;
                linkedCts?.Dispose();
                OnRefreshCompleted?.Invoke();
            }
        }

        public static void CancelRefresh()
        {
            _refreshCancellationTokenSource?.Cancel();
            _refreshCancellationTokenSource?.Dispose();
        }

        public static async UniTask WaitForModelBundleReady(string modelID,
            CancellationToken cancellationToken = default)
        {
            if (!IsRefreshing) return;

            if (string.IsNullOrEmpty(modelID)) return;

            if (!ModelManager.FindModelByID(modelID, out var bundleInfo, out _)) return;

            if (_currentRefreshingBundles == null || !_currentRefreshingBundles.Contains(bundleInfo.BundleName)) return;

            await WaitForRefreshCompletion();
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        public static void ApplyModelToTarget(ModelTarget target, string modelID, bool forceReapply = false)
        {
            if (ModEntry.UsingModel == null) return;
            if (string.IsNullOrEmpty(modelID)) return;

            var handlers = ModelManager.GetAllModelHandlers(target);
            if (handlers.Count == 0) return;

            if (!ModelManager.FindModelByID(modelID, out var bundleInfo, out var modelInfo))
            {
                ModLogger.LogWarning($"Model '{modelID}' not found for {target}");
                return;
            }

            if (!modelInfo.CompatibleWithType(target))
            {
                ModLogger.LogWarning($"Model '{modelID}' is not compatible with {target}");
                return;
            }

            if (!forceReapply)
            {
                var allApplied = true;
                var needsAudioRestore = false;
                foreach (var handler in handlers)
                {
                    if (!handler.IsHiddenOriginalModel)
                    {
                        allApplied = false;
                        break;
                    }

                    if (!handler.HasAnySounds() && modelInfo.CustomSounds is { Length: > 0 }) needsAudioRestore = true;
                }

                if (allApplied && !needsAudioRestore) return;
            }

            foreach (var handler in handlers)
            {
                handler.InitializeCustomModel(bundleInfo, modelInfo);
                handler.ChangeToCustomModel();
            }

            ModLogger.Log($"Applied model '{modelInfo.Name}' ({modelID}) to {handlers.Count} {target} object(s)");
        }

        public static void RestoreOriginalModelForTarget(ModelTarget target)
        {
            var handlers = ModelManager.GetAllModelHandlers(target);
            foreach (var handler in handlers)
                handler.RestoreOriginalModel();
        }

        public static void ApplyAllModelsFromConfig(bool forceReapply = false)
        {
            if (ModEntry.UsingModel == null) return;

            foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
            {
                if (target == ModelTarget.AICharacter) continue;

                var modelID = ModEntry.UsingModel.GetModelID(target);
                if (string.IsNullOrEmpty(modelID)) continue;

                ApplyModelToTarget(target, modelID, forceReapply);
            }

            ApplyAllAICharacterModelsFromConfig(forceReapply);
        }

        public static void ApplyAllAICharacterModelsFromConfig(bool forceReapply = false)
        {
            if (ModEntry.UsingModel == null) return;

            foreach (var nameKey in AICharacters.SupportedAICharacters)
            {
                var modelID = ModEntry.UsingModel.GetAICharacterModelIDWithFallback(nameKey);
                if (string.IsNullOrEmpty(modelID)) continue;

                ApplyModelToAICharacter(nameKey, modelID, forceReapply);
            }
        }

        public static void ApplyModelToAICharacter(string nameKey, string modelID, bool forceReapply = false)
        {
            if (ModEntry.UsingModel == null) return;
            if (string.IsNullOrEmpty(modelID)) return;
            if (string.IsNullOrEmpty(nameKey)) return;

            var handlers = ModelManager.GetAICharacterModelHandlers(nameKey);
            if (handlers.Count == 0) return;

            if (!ModelManager.FindModelByID(modelID, out var bundleInfo, out var modelInfo))
            {
                ModLogger.LogWarning($"Model '{modelID}' not found for AICharacter '{nameKey}'");
                return;
            }

            if (!modelInfo.CompatibleWithAICharacter(nameKey))
            {
                ModLogger.LogWarning($"Model '{modelID}' is not compatible with AICharacter '{nameKey}'");
                return;
            }

            if (!forceReapply)
            {
                var allApplied = true;
                var needsAudioRestore = false;
                foreach (var handler in handlers)
                {
                    if (!handler.IsHiddenOriginalModel)
                    {
                        allApplied = false;
                        break;
                    }

                    if (!handler.HasAnySounds() && modelInfo.CustomSounds is { Length: > 0 }) needsAudioRestore = true;
                }

                if (allApplied && !needsAudioRestore) return;
            }

            foreach (var handler in handlers)
            {
                handler.InitializeCustomModel(bundleInfo, modelInfo);
                handler.ChangeToCustomModel();
            }

            ModLogger.Log(
                $"Applied model '{modelInfo.Name}' ({modelID}) to {handlers.Count} AICharacter '{nameKey}' object(s)");
        }

        public static void ApplyModelToTargetAfterRefresh(ModelTarget target, string modelID,
            IReadOnlyCollection<string>? bundlesToReload = null)
        {
            if (ModEntry.UsingModel == null) return;
            if (string.IsNullOrEmpty(modelID)) return;

            if (!ModelManager.FindModelByID(modelID, out var bundleInfo, out var modelInfo))
            {
                ModLogger.LogWarning($"Model '{modelID}' not found for {target}");
                return;
            }

            var needsReapply = false;
            if (bundlesToReload is { Count: > 0 })
                if (bundlesToReload.Contains(bundleInfo.BundleName))
                    needsReapply = true;

            if (!needsReapply)
            {
                var handlers = ModelManager.GetAllModelHandlers(target);
                var allApplied = handlers.All(handler => handler.IsHiddenOriginalModel);
                var needsAudioRestore = handlers.Any(handler =>
                    !handler.HasAnySounds() && modelInfo.CustomSounds is { Length: > 0 });

                if (allApplied && !needsAudioRestore) return;
            }

            ApplyModelToTarget(target, modelID, true);
        }
    }
}
