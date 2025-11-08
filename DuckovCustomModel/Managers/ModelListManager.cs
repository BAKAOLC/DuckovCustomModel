using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Data;

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
            if (ModBehaviour.Instance?.UsingModel != null)
                foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
                {
                    var modelID = ModBehaviour.Instance.UsingModel.GetModelID(target);
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

                    var modelID = ModBehaviour.Instance?.UsingModel?.GetModelID(target) ?? string.Empty;
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
                            await AssetBundleManager.GetOrLoadAssetBundleAsync(priorityBundle, false, cancellationToken);
                            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                        }

                    var totalCount = ModelManager.ModelBundles.Sum(b => b.Models.Length);
                    var count = 0;

                    if (priorityModels.Count > 0)
                        foreach (var (priorityBundle, priorityModel) in priorityModels)
                        {
                            var priorityBundleInList = ModelManager.ModelBundles.FirstOrDefault(b => b == priorityBundle);
                            var priorityModelInList = priorityBundleInList?.Models.FirstOrDefault(m => m == priorityModel);
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

                        var currentModelID = ModBehaviour.Instance?.UsingModel?.GetModelID(target) ?? string.Empty;
                        if (string.IsNullOrEmpty(currentModelID)) continue;

                        var refreshStartModelID = _refreshStartModelIDs?.GetValueOrDefault(target);
                        if (currentModelID != refreshStartModelID)
                        {
                            ModLogger.Log(
                                $"Model for {target} was changed during refresh (from {refreshStartModelID} to {currentModelID}), skipping auto-restore");
                            continue;
                        }

                        if (!ModelManager.FindModelByID(currentModelID, out var bundleInfo, out var modelInfo))
                            continue;

                        if (!modelInfo.CompatibleWithType(target)) continue;

                        var handlers = ModelManager.GetAllModelHandlers(target);
                        foreach (var handler in handlers)
                        {
                            handler.InitializeCustomModel(bundleInfo, modelInfo);
                            handler.ChangeToCustomModel();
                        }

                        ModLogger.Log(
                            $"Restored {target} model after bundle update: {modelInfo.Name} ({currentModelID})");
                    }
                }
            }
            catch (OperationCanceledException)
            {
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
    }
}