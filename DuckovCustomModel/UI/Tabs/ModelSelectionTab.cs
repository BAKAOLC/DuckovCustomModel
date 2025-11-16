using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Tabs
{
    public class ModelSelectionTab : Base.UIPanel
    {
        private CanvasGroup? _contentCanvasGroup;
        private FunctionButtonBar? _functionButtonBar;
        private ModelListPanel? _modelListPanel;
        private GameObject? _overlayPanel;
        private SearchField? _searchField;
        private TargetListPanel? _targetListPanel;
        private TargetSettingsPanel? _targetSettingsPanel;

        protected override void OnDestroy()
        {
            ModelListManager.OnRefreshStarted -= OnRefreshStarted;
            ModelListManager.OnRefreshCompleted -= OnRefreshCompleted;
            base.OnDestroy();
        }

        protected override void CreatePanel()
        {
            PanelRoot = UIFactory.CreateImage("ModelSelectionPanel", transform, new(0.08f, 0.1f, 0.12f, 0.95f));
            UIFactory.SetupRectTransform(PanelRoot, new(0, 0), new(1, 1), Vector2.zero);
        }

        protected override void BuildContent()
        {
            if (PanelRoot == null) return;

            var content = CreateContent();
            var topContainer = CreateTopContainer(content);
            var bottomContainer = CreateBottomContainer(content);
            var leftContainer = CreateLeftContainer(bottomContainer);
            var midContainer = CreateMidContainer(bottomContainer);
            var rightContainer = CreateRightContainer(bottomContainer);

            var functionButtonBarContainer = CreateFunctionButtonBarContainer(topContainer);
            var targetListContainer = CreateTargetListContainer(leftContainer);
            var searchFieldContainer = CreateSearchFieldContainer(midContainer);
            var modelListContainer = CreateModelListContainer(midContainer);
            var targetSettingsContainer = CreateTargetSettingsContainer(rightContainer);

            _functionButtonBar = functionButtonBarContainer.AddComponent<FunctionButtonBar>();
            _targetListPanel = targetListContainer.AddComponent<TargetListPanel>();
            _searchField = searchFieldContainer.AddComponent<SearchField>();
            _modelListPanel = modelListContainer.AddComponent<ModelListPanel>();
            _targetSettingsPanel = targetSettingsContainer.AddComponent<TargetSettingsPanel>();

            _functionButtonBar.Initialize(functionButtonBarContainer.transform);
            _targetListPanel.Initialize(targetListContainer.transform);
            _searchField.Initialize(searchFieldContainer.transform);
            _modelListPanel.Initialize(modelListContainer.transform);
            _targetSettingsPanel.Initialize(targetSettingsContainer.transform);

            _functionButtonBar.OnRefresh += () => { ModelListManager.RefreshModelList(); };
            _functionButtonBar.OnResetInvalidModels += () => _modelListPanel?.Refresh();
            _targetListPanel.OnTargetSelected += targetInfo =>
            {
                _modelListPanel?.SetTarget(targetInfo);
                _targetSettingsPanel?.SetTarget(targetInfo);
            };
            _searchField.OnSearchChanged += searchText => _modelListPanel?.SetSearchText(searchText);
            _modelListPanel.OnModelSelected += () =>
            {
                _targetListPanel?.Refresh();
                _targetSettingsPanel?.Refresh();
            };

            var contentRect = content.GetComponent<RectTransform>();
            if (contentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            _contentCanvasGroup = content.AddComponent<CanvasGroup>();
            CreateOverlayPanel();

            ModelListManager.OnRefreshStarted += OnRefreshStarted;
            ModelListManager.OnRefreshCompleted += OnRefreshCompleted;

            UpdateRefreshOverlay();
        }

        private void CreateOverlayPanel()
        {
            if (PanelRoot == null) return;

            _overlayPanel = UIFactory.CreateImage("OverlayPanel", PanelRoot.transform, new(0, 0, 0, 0.7f));
            UIFactory.SetupRectTransform(_overlayPanel, Vector2.zero, Vector2.one, Vector2.zero);
            _overlayPanel.transform.SetAsLastSibling();

            var overlayCanvasGroup = _overlayPanel.AddComponent<CanvasGroup>();
            overlayCanvasGroup.blocksRaycasts = true;
            overlayCanvasGroup.interactable = true;

            var overlayText = UIFactory.CreateText("OverlayText", _overlayPanel.transform, Localization.Updating, 24,
                Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupRectTransform(overlayText, Vector2.zero, Vector2.one, Vector2.zero);
            var overlayTextComponent = overlayText.GetComponent<Text>();
            overlayTextComponent.fontStyle = FontStyle.Bold;

            UIFactory.SetLocalizedText(overlayText, () => Localization.Updating);

            _overlayPanel.SetActive(false);
        }

        private void OnRefreshStarted()
        {
            UpdateRefreshOverlay();
        }

        private void OnRefreshCompleted()
        {
            UpdateRefreshOverlay();

            _targetListPanel?.Refresh();
            _modelListPanel?.Refresh();
            _targetSettingsPanel?.Refresh();
        }

        private void UpdateRefreshOverlay()
        {
            var isRefreshing = ModelListManager.IsRefreshing;

            if (_overlayPanel != null)
                _overlayPanel.SetActive(isRefreshing);

            if (_contentCanvasGroup == null) return;
            _contentCanvasGroup.interactable = !isRefreshing;
            _contentCanvasGroup.blocksRaycasts = !isRefreshing;
        }

        private GameObject CreateContent()
        {
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(PanelRoot!.transform, false);
            UIFactory.SetupRectTransform(content, Vector2.zero, Vector2.one, Vector2.zero);
            UIFactory.SetupVerticalLayoutGroup(content, 10f, new(10, 10, 10, 10), TextAnchor.UpperCenter,
                true, true, true);

            return content;
        }

        private static GameObject CreateTopContainer(GameObject parent)
        {
            var container = new GameObject("TopContainer", typeof(RectTransform), typeof(LayoutElement));
            container.transform.SetParent(parent.transform, false);
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero);

            var layoutElement = container.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 80;
            layoutElement.flexibleHeight = 0;
            layoutElement.flexibleWidth = 1;

            return container;
        }

        private static GameObject CreateBottomContainer(GameObject parent)
        {
            var container = new GameObject("BottomContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            container.transform.SetParent(parent.transform, false);
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero);

            var layoutElement = container.GetComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1;
            layoutElement.flexibleWidth = 1;

            UIFactory.SetupHorizontalLayoutGroup(container, 10f, new(0, 0, 0, 0), TextAnchor.MiddleLeft,
                true);

            return container;
        }

        private static GameObject CreateLeftContainer(GameObject parent)
        {
            var container = new GameObject("LeftContainer", typeof(RectTransform), typeof(LayoutElement));
            container.transform.SetParent(parent.transform, false);
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero);

            var layoutElement = container.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 200;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 1;

            return container;
        }

        private static GameObject CreateMidContainer(GameObject parent)
        {
            var container = UIFactory.CreateImage("MidContainer", parent.transform, new(0.1f, 0.12f, 0.14f, 0.95f));
            container.AddComponent<VerticalLayoutGroup>();
            container.AddComponent<LayoutElement>();
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero);
            UIFactory.SetupVerticalLayoutGroup(container, 10f, new(0, 0, 0, 0), TextAnchor.UpperCenter,
                true, true, true);

            var layoutElement = container.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1;
            layoutElement.flexibleHeight = 1;

            return container;
        }

        private static GameObject CreateRightContainer(GameObject parent)
        {
            var container = new GameObject("RightContainer", typeof(RectTransform), typeof(LayoutElement));
            container.transform.SetParent(parent.transform, false);
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero);

            var layoutElement = container.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 500;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 1;

            return container;
        }

        private static GameObject CreateFunctionButtonBarContainer(GameObject parent)
        {
            var container = new GameObject("FunctionButtonBarContainer", typeof(RectTransform), typeof(LayoutElement));
            container.transform.SetParent(parent.transform, false);
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero);

            var layoutElement = container.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 80;
            layoutElement.flexibleHeight = 0;
            layoutElement.flexibleWidth = 1;

            return container;
        }

        private static GameObject CreateTargetListContainer(GameObject parent)
        {
            var container = new GameObject("TargetListContainer", typeof(RectTransform));
            container.transform.SetParent(parent.transform, false);
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero);
            return container;
        }

        private static GameObject CreateSearchFieldContainer(GameObject parent)
        {
            var container = new GameObject("SearchFieldContainer", typeof(RectTransform), typeof(LayoutElement));
            container.transform.SetParent(parent.transform, false);
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero);

            var layoutElement = container.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 40;
            layoutElement.flexibleHeight = 0;
            layoutElement.flexibleWidth = 1;

            return container;
        }

        private static GameObject CreateModelListContainer(GameObject parent)
        {
            var container = new GameObject("ModelListContainer", typeof(RectTransform), typeof(LayoutElement));
            container.transform.SetParent(parent.transform, false);
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero);

            var layoutElement = container.GetComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1;
            layoutElement.flexibleWidth = 1;

            return container;
        }

        private static GameObject CreateTargetSettingsContainer(GameObject parent)
        {
            var container = new GameObject("TargetSettingsContainer", typeof(RectTransform), typeof(LayoutElement));
            container.transform.SetParent(parent.transform, false);
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero);

            var layoutElement = container.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1;
            layoutElement.flexibleHeight = 1;

            return container;
        }

        protected override void OnShow()
        {
            _targetListPanel?.Refresh();
            _modelListPanel?.Refresh();
            _targetSettingsPanel?.Refresh();

            UpdateRefreshOverlay();
        }

        protected override void OnRefresh()
        {
            _targetListPanel?.Refresh();
            _modelListPanel?.Refresh();
            _targetSettingsPanel?.Refresh();
        }
    }
}
