using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Data;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class ModelListPanel : MonoBehaviour
    {
        private readonly List<ModelBundleInfo> _filteredModelBundles = [];
        private GameObject? _content;
        private UniTaskCompletionSource? _currentRefreshTask;
        private TargetInfo? _currentTarget;
        private CancellationTokenSource? _refreshCancellationTokenSource;
        private ScrollRect? _scrollRect;
        private string _searchText = string.Empty;

        private void OnDestroy()
        {
            _refreshCancellationTokenSource?.Cancel();
            _refreshCancellationTokenSource?.Dispose();
            _currentRefreshTask?.TrySetCanceled();
        }

        public event Action? OnModelSelected;

        public void Initialize(Transform parent)
        {
            var scrollView = UIFactory.CreateScrollView("ModelListScrollView", parent, out var content);
            UIFactory.SetupRectTransform(scrollView.gameObject, Vector2.zero, Vector2.one, Vector2.zero);

            _scrollRect = scrollView;

            _content = content;
            _content.AddComponent<VerticalLayoutGroup>();
            _content.AddComponent<ContentSizeFitter>();

            UIFactory.SetupVerticalLayoutGroup(_content, 10f, new(10, 10, 10, 10), TextAnchor.UpperLeft,
                true, false, true);

            var sizeFitter = _content.GetComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        public void SetTarget(TargetInfo? targetInfo)
        {
            _currentTarget = targetInfo;
            Refresh();
        }

        public void SetSearchText(string searchText)
        {
            _searchText = searchText;
            Refresh();
        }

        public async void Refresh()
        {
            if (_content == null) return;

            if (_currentRefreshTask != null)
            {
                try
                {
                    await _currentRefreshTask.Task;
                }
                catch (OperationCanceledException)
                {
                }
                catch
                {
                    // ignored
                }

                if (_currentRefreshTask != null)
                    return;
            }

            _refreshCancellationTokenSource?.Cancel();
            _refreshCancellationTokenSource?.Dispose();

            _refreshCancellationTokenSource = new();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _refreshCancellationTokenSource.Token,
                this.GetCancellationTokenOnDestroy()
            );

            _currentRefreshTask = new();
            RefreshModelListAsync(linkedCts.Token, linkedCts).Forget();
        }

        private async UniTaskVoid RefreshModelListAsync(CancellationToken cancellationToken,
            CancellationTokenSource? linkedCts)
        {
            if (_content == null)
            {
                linkedCts?.Dispose();
                _currentRefreshTask?.TrySetResult();
                _currentRefreshTask = null;
                return;
            }

            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1f;

            try
            {
                foreach (Transform child in _content.transform) Destroy(child.gameObject);

                _filteredModelBundles.Clear();

                if (_currentTarget == null)
                {
                    linkedCts?.Dispose();
                    _currentRefreshTask?.TrySetResult();
                    _currentRefreshTask = null;
                    return;
                }

                if (_currentTarget.TargetType == ModelTarget.AICharacter)
                {
                    if (string.IsNullOrEmpty(_currentTarget.AICharacterNameKey))
                    {
                        linkedCts?.Dispose();
                        _currentRefreshTask?.TrySetResult();
                        _currentRefreshTask = null;
                        return;
                    }

                    await BuildAICharacterModelListAsync(_currentTarget.AICharacterNameKey, cancellationToken);
                }
                else
                {
                    var searchLower = _searchText.ToLowerInvariant();
                    foreach (var bundle in ModelManager.ModelBundles
                                 .Where(bundle => string.IsNullOrEmpty(searchLower)
                                                  || bundle.BundleName.ToLowerInvariant().Contains(searchLower)
                                                  || bundle.Models.Any(m => m.Name.ToLowerInvariant()
                                                                                .Contains(searchLower)
                                                                            || m.ModelID.ToLowerInvariant()
                                                                                .Contains(searchLower))))
                    {
                        var compatibleModels = bundle.Models
                            .Where(m => m.CompatibleWithType(_currentTarget.TargetType)).ToArray();
                        if (compatibleModels.Length <= 0) continue;
                        var filteredBundle = bundle.CreateFilteredCopy(compatibleModels);
                        _filteredModelBundles.Add(filteredBundle);
                    }

                    BuildNoneModelButton();

                    var count = 0;
                    var bundlesCopy = _filteredModelBundles.ToList();
                    foreach (var bundle in bundlesCopy)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var modelsCopy = bundle.Models.ToList();
                        foreach (var model in modelsCopy)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await BuildModelButtonAsync(bundle, model, cancellationToken);
                            count++;

                            if (count % 5 != 0) continue;
                            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                        }
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                if (_scrollRect != null)
                    _scrollRect.verticalNormalizedPosition = 1f;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                linkedCts?.Dispose();
                _currentRefreshTask?.TrySetResult();
                _currentRefreshTask = null;
            }
        }

        private async UniTask BuildAICharacterModelListAsync(string nameKey, CancellationToken cancellationToken)
        {
            if (_content == null) return;

            BuildNoneModelButton();

            var searchLower = _searchText.ToLowerInvariant();
            foreach (var bundle in ModelManager.ModelBundles
                         .Where(bundle => string.IsNullOrEmpty(searchLower)
                                          || bundle.BundleName.ToLowerInvariant().Contains(searchLower)
                                          || bundle.Models.Any(m => m.Name.ToLowerInvariant()
                                                                        .Contains(searchLower)
                                                                    || m.ModelID.ToLowerInvariant()
                                                                        .Contains(searchLower))))
            {
                ModelInfo[] compatibleModels;
                if (nameKey == AICharacters.AllAICharactersKey)
                    compatibleModels = bundle.Models
                        .Where(m => m.CompatibleWithType(ModelTarget.AICharacter)).ToArray();
                else
                    compatibleModels = bundle.Models
                        .Where(m => m.CompatibleWithAICharacter(nameKey)).ToArray();

                if (compatibleModels.Length <= 0) continue;
                var filteredBundle = bundle.CreateFilteredCopy(compatibleModels);
                _filteredModelBundles.Add(filteredBundle);
            }

            var count = 0;
            var bundlesCopy = _filteredModelBundles.ToList();
            foreach (var bundle in bundlesCopy)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var modelsCopy = bundle.Models.ToList();
                foreach (var model in modelsCopy)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await BuildModelButtonAsync(bundle, model, cancellationToken);
                    count++;

                    if (count % 5 != 0) continue;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
        }

        private void BuildNoneModelButton()
        {
            if (_content == null) return;

            var buttonObj = UIFactory.CreateButton("NoneModelButton", _content.transform, OnNoneModelSelected,
                new(0.2f, 0.15f, 0.15f, 0.8f)).gameObject;
            buttonObj.transform.SetAsFirstSibling();

            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new(0, 0);
            buttonRect.anchorMax = new(1, 0);
            buttonRect.pivot = new(0.5f, 0.5f);
            buttonRect.sizeDelta = new(0, 150);

            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 150;
            layoutElement.preferredHeight = 150;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;

            var outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = new(0.4f, 0.3f, 0.3f, 0.6f);
            outline.effectDistance = new(1, -1);

            var text = UIFactory.CreateText("Text", buttonObj.transform, Localization.NoModel, 16, Color.white,
                TextAnchor.MiddleCenter);
            var textComponent = text.GetComponent<Text>();
            textComponent.fontStyle = FontStyle.Bold;
            UIFactory.SetupButtonText(text);

            var button = buttonObj.GetComponent<Button>();
            UIFactory.SetupButtonColors(button, new(1, 1, 1, 1), new(0.7f, 0.5f, 0.5f, 1),
                new(0.6f, 0.4f, 0.4f, 1), new(0.7f, 0.5f, 0.5f, 1));
        }

        private async UniTask BuildModelButtonAsync(ModelBundleInfo bundle, ModelInfo model,
            CancellationToken cancellationToken)
        {
            if (_content == null) return;

            var (isValid, errorMessage) =
                await AssetBundleManager.CheckBundleStatusAsync(bundle, model, cancellationToken);
            var hasError = !isValid;

            var usingModel = ModEntry.UsingModel;
            var isInUse = false;
            if (_currentTarget != null && usingModel != null)
            {
                if (_currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null)
                    isInUse = usingModel.GetAICharacterModelID(_currentTarget.AICharacterNameKey) == model.ModelID;
                else
                    isInUse = usingModel.GetModelID(_currentTarget.TargetType) == model.ModelID;
            }

            var buttonObj = new GameObject($"ModelButton_{model.ModelID}", typeof(Image), typeof(Button),
                typeof(LayoutElement));
            buttonObj.transform.SetParent(_content.transform, false);

            var buttonImage = buttonObj.GetComponent<Image>();
            Color baseColor = hasError ? new(0.22f, 0.15f, 0.15f, 0.8f) : new(0.15f, 0.18f, 0.22f, 0.8f);
            if (isInUse && !hasError) baseColor = new(0.15f, 0.22f, 0.18f, 0.8f);
            buttonImage.color = baseColor;

            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new(0, 0);
            buttonRect.anchorMax = new(1, 0);
            buttonRect.pivot = new(0.5f, 0.5f);
            buttonRect.sizeDelta = new(0, 150);

            var layoutElement = buttonObj.GetComponent<LayoutElement>();
            layoutElement.minHeight = 150;
            layoutElement.preferredHeight = 150;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;

            var outline = buttonObj.AddComponent<Outline>();
            if (isInUse && !hasError)
                outline.effectColor = new(0.3f, 0.6f, 0.4f, 0.8f);
            else
                outline.effectColor = hasError
                    ? new(0.6f, 0.3f, 0.3f, 0.8f)
                    : new(0.3f, 0.35f, 0.4f, 0.6f);
            outline.effectDistance = new(1, -1);

            var thumbnailImage = new GameObject("Thumbnail", typeof(Image), typeof(LayoutElement));
            thumbnailImage.transform.SetParent(buttonObj.transform, false);
            var thumbnailImageComponent = thumbnailImage.GetComponent<Image>();
            var thumbnailRect = thumbnailImage.GetComponent<RectTransform>();
            thumbnailRect.anchorMin = new(0, 0.5f);
            thumbnailRect.anchorMax = new(0, 0.5f);
            thumbnailRect.pivot = new(0, 0.5f);
            thumbnailRect.anchoredPosition = new(10, 0);
            thumbnailRect.sizeDelta = new(130, 130);

            var thumbnailLayoutElement = thumbnailImage.GetComponent<LayoutElement>();
            thumbnailLayoutElement.minWidth = 130;
            thumbnailLayoutElement.minHeight = 130;
            thumbnailLayoutElement.preferredWidth = 130;
            thumbnailLayoutElement.preferredHeight = 130;
            thumbnailLayoutElement.flexibleWidth = 0;
            thumbnailLayoutElement.flexibleHeight = 0;

            var texture = await AssetBundleManager.LoadThumbnailTextureAsync(bundle, model, cancellationToken);
            if (texture != null)
            {
                var sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height),
                    new(0.5f, 0.5f));
                thumbnailImageComponent.sprite = sprite;
                thumbnailImageComponent.preserveAspect = true;
                thumbnailImageComponent.color = Color.white;
            }
            else
            {
                thumbnailImageComponent.color = new(0.15f, 0.15f, 0.15f, 1);
                var thumbnailOutline = thumbnailImage.AddComponent<Outline>();
                thumbnailOutline.effectColor = new(0.3f, 0.3f, 0.3f, 0.5f);
                thumbnailOutline.effectDistance = new(1, -1);

                var placeholderText = UIFactory.CreateText("PlaceholderText", thumbnailImage.transform,
                    Localization.NoPreview, 15, new(0.6f, 0.6f, 0.6f, 1), TextAnchor.MiddleCenter);
                UIFactory.SetupRectTransform(placeholderText, Vector2.zero, Vector2.one, Vector2.zero);
            }

            var contentArea = new GameObject("ContentArea", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentArea.transform.SetParent(buttonObj.transform, false);
            UIFactory.SetupRectTransform(contentArea, new(0, 0), new(1, 1), offsetMin: new(150, 10),
                offsetMax: new(-10, -10));
            UIFactory.SetupVerticalLayoutGroup(contentArea, 2f, new(0, 0, 0, 0), TextAnchor.UpperLeft,
                true, false, true);

            var nameText = UIFactory.CreateText("Name", contentArea.transform,
                string.IsNullOrEmpty(model.Name) ? model.ModelID : model.Name, 20,
                hasError ? new(1f, 0.6f, 0.6f, 1) : Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.SetupRectTransform(nameText, new(0, 1), new(1, 1), new(0, 24), pivot: new(0, 1),
                anchoredPosition: Vector2.zero);
            var nameTextComponent = nameText.GetComponent<Text>();
            nameTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameTextComponent.verticalOverflow = VerticalWrapMode.Overflow;

            var infoText = UIFactory.CreateText("Info", contentArea.transform,
                Localization.GetModelInfo(model.ModelID, model.Author, model.Version, model.BundleName),
                16,
                hasError ? new(1f, 0.7f, 0.7f, 1) : new(0.8f, 0.8f, 0.8f, 1), TextAnchor.UpperLeft);
            UIFactory.SetupRectTransform(infoText, Vector2.zero, Vector2.one, new(0, 18));
            var infoTextComponent = infoText.GetComponent<Text>();
            infoTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            infoTextComponent.verticalOverflow = VerticalWrapMode.Truncate;

            if (hasError)
            {
                var errorText = new GameObject("Error", typeof(Text), typeof(ContentSizeFitter));
                errorText.transform.SetParent(contentArea.transform, false);
                var errorTextComponent = errorText.GetComponent<Text>();
                errorTextComponent.text = $"⚠ {(!string.IsNullOrEmpty(errorMessage) ? errorMessage : "Unknown error")}";
                errorTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                errorTextComponent.fontSize = 15;
                errorTextComponent.color = new(1f, 0.4f, 0.4f, 1);
                errorTextComponent.alignment = TextAnchor.UpperLeft;
                errorTextComponent.fontStyle = FontStyle.Bold;
                errorTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                errorTextComponent.verticalOverflow = VerticalWrapMode.Overflow;
                var errorRect = errorText.GetComponent<RectTransform>();
                errorRect.sizeDelta = new(0, 20);
                var errorLayoutElement = errorText.AddComponent<LayoutElement>();
                errorLayoutElement.minHeight = 20;
                errorLayoutElement.flexibleHeight = 1;
                var contentSizeFitter = errorText.GetComponent<ContentSizeFitter>();
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            if (!string.IsNullOrEmpty(model.Description))
            {
                var descText = new GameObject("Description", typeof(Text), typeof(ContentSizeFitter));
                descText.transform.SetParent(contentArea.transform, false);
                var descTextComponent = descText.GetComponent<Text>();
                descTextComponent.text = model.Description;
                descTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                descTextComponent.fontSize = 15;
                descTextComponent.color = new(0.7f, 0.7f, 0.7f, 1);
                descTextComponent.alignment = TextAnchor.UpperLeft;
                descTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                descTextComponent.verticalOverflow = VerticalWrapMode.Overflow;
                var descRect = descText.GetComponent<RectTransform>();
                descRect.sizeDelta = new(0, 20);
                var descLayoutElement = descText.AddComponent<LayoutElement>();
                descLayoutElement.minHeight = 20;
                descLayoutElement.flexibleHeight = 1;
                var contentSizeFitter = descText.GetComponent<ContentSizeFitter>();
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            var button = buttonObj.GetComponent<Button>();
            if (isInUse && !hasError)
                UIFactory.SetupButtonColors(button, new(1, 1, 1, 1), new(0.5f, 0.8f, 0.6f, 1),
                    new(0.4f, 0.7f, 0.5f, 1), new(0.5f, 0.8f, 0.6f, 1));
            else
                UIFactory.SetupButtonColors(button, new(1, 1, 1, 1),
                    hasError ? new(0.7f, 0.5f, 0.5f, 1) : new(0.5f, 0.7f, 0.9f, 1),
                    hasError ? new(0.6f, 0.4f, 0.4f, 1) : new(0.4f, 0.6f, 0.8f, 1),
                    hasError ? new(0.7f, 0.5f, 0.5f, 1) : new(0.5f, 0.7f, 0.9f, 1));

            button.interactable = !hasError;
            button.onClick.AddListener(() => SelectModel(model));
        }

        private void SelectModel(ModelInfo model)
        {
            if (_currentTarget == null) return;

            var usingModel = ModEntry.UsingModel;
            if (usingModel == null) return;

            if (_currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null)
            {
                usingModel.SetAICharacterModelID(_currentTarget.AICharacterNameKey, model.ModelID);
                ConfigManager.SaveConfigToFile(usingModel, "UsingModel.json");
                if (_currentTarget.AICharacterNameKey == AICharacters.AllAICharactersKey)
                    ModelListManager.ApplyAllAICharacterModelsFromConfig(true);
                else
                    ModelListManager.ApplyModelToAICharacter(_currentTarget.AICharacterNameKey, model.ModelID, true);
            }
            else
            {
                usingModel.SetModelID(_currentTarget.TargetType, model.ModelID);
                ConfigManager.SaveConfigToFile(usingModel, "UsingModel.json");
                ModelListManager.ApplyModelToTarget(_currentTarget.TargetType, model.ModelID, true);
            }

            OnModelSelected?.Invoke();
            Refresh();
        }

        private void OnNoneModelSelected()
        {
            if (_currentTarget == null) return;

            var usingModel = ModEntry.UsingModel;
            if (usingModel == null) return;

            if (_currentTarget.TargetType == ModelTarget.AICharacter && _currentTarget.AICharacterNameKey != null)
            {
                if (_currentTarget.AICharacterNameKey == AICharacters.AllAICharactersKey)
                {
                    usingModel.SetAICharacterModelID(AICharacters.AllAICharactersKey, string.Empty);
                    ConfigManager.SaveConfigToFile(usingModel, "UsingModel.json");
                    foreach (var nameKey in AICharacters.SupportedAICharacters)
                    {
                        var modelID = usingModel.GetAICharacterModelID(nameKey);
                        if (string.IsNullOrEmpty(modelID))
                        {
                            var handlers = ModelManager.GetAICharacterModelHandlers(nameKey);
                            foreach (var handler in handlers)
                                handler.RestoreOriginalModel();
                        }
                    }
                }
                else
                {
                    usingModel.SetAICharacterModelID(_currentTarget.AICharacterNameKey, string.Empty);
                    ConfigManager.SaveConfigToFile(usingModel, "UsingModel.json");
                    var modelID = usingModel.GetAICharacterModelIDWithFallback(_currentTarget.AICharacterNameKey);
                    if (string.IsNullOrEmpty(modelID))
                    {
                        var handlers = ModelManager.GetAICharacterModelHandlers(_currentTarget.AICharacterNameKey);
                        foreach (var handler in handlers)
                            handler.RestoreOriginalModel();
                    }
                    else
                    {
                        ModelListManager.ApplyModelToAICharacter(_currentTarget.AICharacterNameKey, modelID, true);
                    }
                }
            }
            else
            {
                usingModel.SetModelID(_currentTarget.TargetType, string.Empty);
                ConfigManager.SaveConfigToFile(usingModel, "UsingModel.json");
                ModelListManager.RestoreOriginalModelForTarget(_currentTarget.TargetType);
            }

            OnModelSelected?.Invoke();
            Refresh();
        }
    }
}
