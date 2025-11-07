using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using SodaCraft.Localizations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DuckovCustomModel.MonoBehaviours
{
    public class ModelSelectorUI : MonoBehaviour
    {
        private readonly List<ModelBundleInfo> _filteredModelBundles = [];
        private MonoBehaviour? _cameraController;
        private bool _cameraLockDisabled;

        private CharacterInputControl? _charInput;
        private ModelTarget _currentTargetType = ModelTarget.Character;
        private bool _isInitialized;
        private bool _isRefreshing;
        private bool _isWaitingForKeyInput;
        private Text? _keyButtonText;
        private GameObject? _loadingStatusText;
        private ModelHandler? _modelHandler;
        private GameObject? _modelListContent;
        private ScrollRect? _modelScrollRect;
        private GameObject? _overlay;
        private GameObject? _panelRoot;
        private ModelHandler? _petModelHandler;
        private PlayerInput? _playerInput;
        private Button? _refreshButton;
        private Text? _refreshButtonText;
        private CancellationTokenSource? _refreshCancellationTokenSource;
        private UniTaskCompletionSource? _refreshCompletionSource;

        private InputField? _searchField;
        private string _searchText = string.Empty;
        private Dropdown? _targetTypeDropdown;
        private bool _uiActive;

        private UIConfig? _uiConfig;
        private GameObject? _uiRoot;
        private UsingModel? _usingModel;

        private void Start()
        {
            _uiConfig = ModBehaviour.Instance?.UIConfig;
            _usingModel = ModBehaviour.Instance?.UsingModel;

            ModelListManager.OnRefreshStarted += OnModelListRefreshStarted;
            ModelListManager.OnRefreshCompleted += OnModelListRefreshCompleted;
            ModelListManager.OnRefreshProgress += OnModelListRefreshProgress;

            LocalizationManager.OnSetLanguage += OnLanguageChanged;
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                if (CharacterMainControl.Main != null) InitializeUI();

                return;
            }

            if (_uiConfig == null) return;

            if (IsTypingInInputField() || _panelRoot == null) return;

            switch (_isWaitingForKeyInput)
            {
                case true:
                    HandleKeyInputCapture();
                    return;
                case false when Input.GetKeyDown(_uiConfig.ToggleKey):
                {
                    if (_panelRoot.activeSelf)
                        HidePanel();
                    else
                        ShowPanel();
                    break;
                }
            }

            if (_uiActive && !_isWaitingForKeyInput && Input.GetKeyDown(KeyCode.Escape)) HidePanel();

            if (!_uiActive) return;
            if (_charInput != null && _charInput.enabled) _charInput.enabled = false;

            if (_playerInput != null && _playerInput.inputIsActive) _playerInput.DeactivateInput();
        }

        private void LateUpdate()
        {
            if (!_uiActive || _panelRoot == null || !_panelRoot.activeSelf) return;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void OnDestroy()
        {
            _refreshCancellationTokenSource?.Cancel();
            _refreshCancellationTokenSource?.Dispose();
            _refreshCancellationTokenSource = null;

            ModelListManager.OnRefreshStarted -= OnModelListRefreshStarted;
            ModelListManager.OnRefreshCompleted -= OnModelListRefreshCompleted;
            ModelListManager.OnRefreshProgress -= OnModelListRefreshProgress;

            LocalizationManager.OnSetLanguage -= OnLanguageChanged;
        }

        private void OnLanguageChanged(SystemLanguage language)
        {
            if (!_isInitialized) return;

            RebuildUIAsync().Forget();
        }

        private async UniTaskVoid RebuildUIAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.Update);

            RebuildUI();
        }

        private void RebuildUI()
        {
            var wasActive = _uiActive && _panelRoot != null && _panelRoot.activeSelf;

            if (_panelRoot != null)
            {
                Destroy(_panelRoot);
                _panelRoot = null;
            }

            if (_overlay != null)
            {
                Destroy(_overlay);
                _overlay = null;
            }

            _keyButtonText = null;
            _refreshButtonText = null;
            _searchField = null;
            _refreshButton = null;
            _modelListContent = null;
            _modelScrollRect = null;
            _loadingStatusText = null;

            BuildPanel();
            RefreshModelList();

            if (wasActive) return;
            if (_panelRoot != null) _panelRoot.SetActive(false);
            if (_overlay != null) _overlay.SetActive(false);
            _uiActive = false;
        }

        private void OnModelListRefreshStarted()
        {
            _isRefreshing = true;
            UpdateRefreshButtonState(true);

            if (_modelListContent != null) _modelListContent.SetActive(false);
            if (_loadingStatusText != null)
            {
                _loadingStatusText.SetActive(true);
                var statusText = _loadingStatusText.GetComponent<Text>();
                if (statusText != null) statusText.text = ModelSelectorUILocalization.LoadingModelList;
            }
        }

        private void OnModelListRefreshCompleted()
        {
            _isRefreshing = false;
            UpdateRefreshButtonState(false);

            if (_loadingStatusText != null) _loadingStatusText.SetActive(false);
            if (_modelListContent != null) _modelListContent.SetActive(true);

            RefreshModelList();
        }

        private void OnModelListRefreshProgress(string message)
        {
            if (_loadingStatusText != null)
            {
                var statusText = _loadingStatusText.GetComponent<Text>();
                if (statusText != null) statusText.text = message;
            }
        }

        private void InitializeUI()
        {
            if (_isInitialized) return;

            CreateOrFindUiRoot();
            BuildPanel();
            RefreshModelList();
            HidePanel();
            _isInitialized = true;
            ModLogger.Log("ModelSelectorUI initialized.");
        }

        private void CreateOrFindUiRoot()
        {
            var existing = GameObject.Find("DuckovCustomModelCanvas");
            if (existing != null)
            {
                _uiRoot = existing;
                return;
            }

            var canvas = new GameObject("DuckovCustomModelCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.sortingOrder = 9999;
            canvas.AddComponent<GraphicRaycaster>();

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _uiRoot = canvas;
            DontDestroyOnLoad(_uiRoot);
        }

        private void BuildPanel()
        {
            if (_uiRoot == null) return;

            _overlay = new("Overlay", typeof(Image));
            _overlay.transform.SetParent(_uiRoot.transform, false);
            var overlayImage = _overlay.GetComponent<Image>();
            overlayImage.color = new(0, 0, 0, 0.5f);
            var overlayRect = _overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            _panelRoot = new("Panel", typeof(Image));
            _panelRoot.transform.SetParent(_uiRoot.transform, false);
            var panelImage = _panelRoot.GetComponent<Image>();
            panelImage.color = new(0.1f, 0.12f, 0.15f, 0.95f);
            var panelRect = _panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new(0.5f, 0.5f);
            panelRect.anchorMax = new(0.5f, 0.5f);
            panelRect.pivot = new(0.5f, 0.5f);
            panelRect.sizeDelta = new(1200, 700);
            panelRect.anchoredPosition = Vector2.zero;

            var outline = _panelRoot.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(2, -2);

            BuildVersionLabel();
            BuildTitle();
            BuildTargetTypeSelector();
            BuildSearchField();
            BuildModelList();
            BuildSettings();
            BuildCloseButton();
            BuildRefreshButton();
        }

        private void BuildVersionLabel()
        {
            if (_panelRoot == null) return;

            var versionLabel = new GameObject("VersionLabel", typeof(Text));
            versionLabel.transform.SetParent(_panelRoot.transform, false);
            var versionText = versionLabel.GetComponent<Text>();
            versionText.text = $"v{Constant.ModVersion}";
            versionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            versionText.fontSize = 14;
            versionText.color = new(0.8f, 0.8f, 0.8f, 1);
            versionText.alignment = TextAnchor.UpperLeft;
            var versionRect = versionLabel.GetComponent<RectTransform>();
            versionRect.anchorMin = new(0, 1);
            versionRect.anchorMax = new(0, 1);
            versionRect.pivot = new(0, 1);
            versionRect.anchoredPosition = new(10, -10);
            versionRect.sizeDelta = new(100, 20);
        }

        private void BuildTitle()
        {
            if (_panelRoot == null) return;

            var title = new GameObject("Title", typeof(Text));
            title.transform.SetParent(_panelRoot.transform, false);
            var titleText = title.GetComponent<Text>();
            titleText.text = ModelSelectorUILocalization.Title;
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new(0, 1);
            titleRect.anchorMax = new(1, 1);
            titleRect.pivot = new(0.5f, 1);
            titleRect.anchoredPosition = new(0, -20);
            titleRect.sizeDelta = new(0, 40);
        }

        private void BuildTargetTypeSelector()
        {
            if (_panelRoot == null) return;

            var targetTypeLabel = new GameObject("TargetTypeLabel", typeof(Text));
            targetTypeLabel.transform.SetParent(_panelRoot.transform, false);
            var labelText = targetTypeLabel.GetComponent<Text>();
            labelText.text = ModelSelectorUILocalization.TargetType;
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelRect = targetTypeLabel.GetComponent<RectTransform>();
            labelRect.anchorMin = new(0, 1);
            labelRect.anchorMax = new(0, 1);
            labelRect.pivot = new(0, 1);
            labelRect.anchoredPosition = new(20, -70);
            labelRect.sizeDelta = new(100, 30);

            var dropdownObj = new GameObject("TargetTypeDropdown", typeof(Image), typeof(Dropdown));
            dropdownObj.transform.SetParent(_panelRoot.transform, false);
            var dropdownImage = dropdownObj.GetComponent<Image>();
            dropdownImage.color = new(0.1f, 0.12f, 0.15f, 0.9f);
            var dropdownRect = dropdownObj.GetComponent<RectTransform>();
            dropdownRect.anchorMin = new(0, 1);
            dropdownRect.anchorMax = new(0, 1);
            dropdownRect.pivot = new(0, 1);
            dropdownRect.anchoredPosition = new(130, -70);
            dropdownRect.sizeDelta = new(150, 30);

            _targetTypeDropdown = dropdownObj.GetComponent<Dropdown>();
            _targetTypeDropdown.options.Clear();
            _targetTypeDropdown.options.Add(new(ModelSelectorUILocalization.TargetCharacter));
            _targetTypeDropdown.options.Add(new(ModelSelectorUILocalization.TargetPet));
            _targetTypeDropdown.value = _currentTargetType == ModelTarget.Character ? 0 : 1;
            _targetTypeDropdown.onValueChanged.AddListener(OnTargetTypeChanged);

            var labelObj = new GameObject("Label", typeof(Text));
            labelObj.transform.SetParent(dropdownObj.transform, false);
            var labelTextComponent = labelObj.GetComponent<Text>();
            labelTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelTextComponent.fontSize = 14;
            labelTextComponent.color = Color.white;
            labelTextComponent.alignment = TextAnchor.MiddleLeft;
            var labelTextRect = labelObj.GetComponent<RectTransform>();
            labelTextRect.anchorMin = new(0, 0);
            labelTextRect.anchorMax = new(1, 1);
            labelTextRect.offsetMin = new(10, 0);
            labelTextRect.offsetMax = new(-25, 0);
            _targetTypeDropdown.captionText = labelTextComponent;

            var arrowObj = new GameObject("Arrow", typeof(Image));
            arrowObj.transform.SetParent(dropdownObj.transform, false);
            var arrowImage = arrowObj.GetComponent<Image>();
            arrowImage.color = Color.white;
            var arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new(1, 0.5f);
            arrowRect.anchorMax = new(1, 0.5f);
            arrowRect.pivot = new(1, 0.5f);
            arrowRect.anchoredPosition = new(-10, 0);
            arrowRect.sizeDelta = new(20, 20);
            _targetTypeDropdown.targetGraphic = arrowImage;

            var templateObj = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateObj.transform.SetParent(dropdownObj.transform, false);
            var templateImage = templateObj.GetComponent<Image>();
            templateImage.color = new(0.1f, 0.12f, 0.15f, 0.95f);
            var templateRect = templateObj.GetComponent<RectTransform>();
            templateRect.anchorMin = new(0, 0);
            templateRect.anchorMax = new(1, 0);
            templateRect.pivot = new(0.5f, 1);
            templateRect.anchoredPosition = new(0, 2);
            templateRect.sizeDelta = new(0, 60);
            templateObj.SetActive(false);
            _targetTypeDropdown.template = templateRect;

            var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewportObj.transform.SetParent(templateObj.transform, false);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            var viewportMask = viewportObj.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(ToggleGroup));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new(0, 1);
            contentRect.anchorMax = new(1, 1);
            contentRect.pivot = new(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new(0, 30);

            var itemObj = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image));
            itemObj.transform.SetParent(contentObj.transform, false);
            var itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.anchorMin = new(0, 0.5f);
            itemRect.anchorMax = new(1, 0.5f);
            itemRect.sizeDelta = new(0, 30);

            var itemLabelObj = new GameObject("Item Label", typeof(Text));
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            var itemLabelText = itemLabelObj.GetComponent<Text>();
            itemLabelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            itemLabelText.fontSize = 14;
            itemLabelText.color = Color.white;
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            var itemLabelRect = itemLabelObj.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new(10, 0);
            itemLabelRect.offsetMax = new(-10, 0);

            var toggle = itemObj.GetComponent<Toggle>();
            toggle.targetGraphic = itemObj.GetComponent<Image>();
            toggle.graphic = itemLabelText;

            _targetTypeDropdown.itemText = itemLabelText;
        }

        private void OnTargetTypeChanged(int value)
        {
            _currentTargetType = value == 0 ? ModelTarget.Character : ModelTarget.Pet;
            UpdateModelHandler();
            RefreshModelList();
        }

        private void BuildSearchField()
        {
            if (_panelRoot == null) return;

            _searchField = BuildInput(ModelSelectorUILocalization.SearchPlaceholder);
            _searchField.transform.SetParent(_panelRoot.transform, false);
            var searchRect = _searchField.GetComponent<RectTransform>();
            searchRect.anchorMin = new(0, 1);
            searchRect.anchorMax = new(1, 1);
            searchRect.pivot = new(0.5f, 1);
            searchRect.anchoredPosition = new(0, -110);
            searchRect.sizeDelta = new(-40, 32);

            _searchField.onValueChanged.AddListener(OnSearchChanged);
        }

        private void BuildModelList()
        {
            if (_panelRoot == null) return;

            var scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect),
                typeof(Image));
            scrollView.transform.SetParent(_panelRoot.transform, false);
            var scrollRect = scrollView.GetComponent<RectTransform>();
            scrollRect.anchorMin = new(0, 0);
            scrollRect.anchorMax = new(1, 1);
            scrollRect.offsetMin = new(20, 100);
            scrollRect.offsetMax = new(-20, -140);

            var scrollImage = scrollView.GetComponent<Image>();
            scrollImage.color = new(0.05f, 0.08f, 0.12f, 0.8f);

            var mask = scrollView.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            _modelScrollRect = scrollView.GetComponent<ScrollRect>();
            _modelScrollRect.horizontal = false;
            _modelScrollRect.vertical = true;
            _modelScrollRect.scrollSensitivity = 1;

            _modelListContent = new("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            _modelListContent.transform.SetParent(scrollView.transform, false);
            var contentRect = _modelListContent.GetComponent<RectTransform>();
            contentRect.anchorMin = new(0, 1);
            contentRect.anchorMax = new(1, 1);
            contentRect.pivot = new(0, 1);
            contentRect.anchoredPosition = Vector2.zero;

            var layoutGroup = _modelListContent.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new(10, 10, 10, 10);
            layoutGroup.spacing = 10;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            var sizeFitter = _modelListContent.GetComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _modelScrollRect.content = contentRect;

            _loadingStatusText = new("LoadingStatus", typeof(Text));
            _loadingStatusText.transform.SetParent(scrollView.transform, false);
            var statusText = _loadingStatusText.GetComponent<Text>();
            statusText.text = "";
            statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            statusText.fontSize = 16;
            statusText.color = Color.white;
            statusText.alignment = TextAnchor.MiddleCenter;
            var statusRect = _loadingStatusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new(0, 0);
            statusRect.anchorMax = new(1, 1);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            _loadingStatusText.SetActive(false);
        }

        private void BuildCloseButton()
        {
            if (_panelRoot == null) return;

            var closeButton = new GameObject("CloseButton", typeof(Image), typeof(Button));
            closeButton.transform.SetParent(_panelRoot.transform, false);
            var closeImage = closeButton.GetComponent<Image>();
            closeImage.color = new(0.2f, 0.2f, 0.2f, 1);

            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new(1, 1);
            closeRect.anchorMax = new(1, 1);
            closeRect.pivot = new(1, 1);
            closeRect.anchoredPosition = new(-10, -10);
            closeRect.sizeDelta = new(30, 30);

            var closeText = new GameObject("Text", typeof(Text));
            closeText.transform.SetParent(closeButton.transform, false);
            var textComponent = closeText.GetComponent<Text>();
            textComponent.text = "×";
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 20;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            var textRect = closeText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var button = closeButton.GetComponent<Button>();
            button.onClick.AddListener(HidePanel);
        }

        private void BuildRefreshButton()
        {
            if (_panelRoot == null) return;

            var refreshButton = new GameObject("RefreshButton", typeof(Image), typeof(Button));
            refreshButton.transform.SetParent(_panelRoot.transform, false);
            var refreshImage = refreshButton.GetComponent<Image>();
            refreshImage.color = new(0.2f, 0.3f, 0.4f, 1);

            var refreshRect = refreshButton.GetComponent<RectTransform>();
            refreshRect.anchorMin = new(1, 0);
            refreshRect.anchorMax = new(1, 0);
            refreshRect.pivot = new(1, 0);
            refreshRect.anchoredPosition = new(-10, 10);
            refreshRect.sizeDelta = new(100, 30);

            var refreshText = new GameObject("Text", typeof(Text));
            refreshText.transform.SetParent(refreshButton.transform, false);
            var textComponent = refreshText.GetComponent<Text>();
            textComponent.text = ModelSelectorUILocalization.Refresh;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            var textRect = refreshText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            _refreshButton = refreshButton.GetComponent<Button>();
            var colors = _refreshButton.colors;
            colors.normalColor = new(1, 1, 1, 1);
            colors.highlightedColor = new(0.4f, 0.5f, 0.6f, 1);
            colors.pressedColor = new(0.3f, 0.4f, 0.5f, 1);
            colors.selectedColor = new(0.4f, 0.5f, 0.6f, 1);
            _refreshButton.colors = colors;

            _refreshButtonText = textComponent;
            _refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        }

        private void BuildSettings()
        {
            if (_panelRoot == null) return;

            var settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            settingsPanel.transform.SetParent(_panelRoot.transform, false);
            var settingsRect = settingsPanel.GetComponent<RectTransform>();
            settingsRect.anchorMin = new(0, 0);
            settingsRect.anchorMax = new(1, 0);
            settingsRect.pivot = new(0.5f, 0);
            settingsRect.anchoredPosition = new(0, 10);
            settingsRect.sizeDelta = new(-40, 40);

            var layoutGroup = settingsPanel.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.padding = new(10, 10, 10, 10);
            layoutGroup.spacing = 10;
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            BuildHideEquipmentToggle(settingsPanel, ModelSelectorUILocalization.HideCharacterEquipment,
                _uiConfig?.HideCharacterEquipment ?? false, OnHideCharacterEquipmentToggleChanged);

            BuildHideEquipmentToggle(settingsPanel, ModelSelectorUILocalization.HidePetEquipment,
                _uiConfig?.HidePetEquipment ?? false, OnHidePetEquipmentToggleChanged);

            BuildKeySetting(settingsPanel);
        }

        private void BuildKeySetting(GameObject settingsPanel)
        {
            if (_panelRoot == null) return;

            var keyLabelObj = new GameObject("KeyLabel", typeof(Text));
            keyLabelObj.transform.SetParent(settingsPanel.transform, false);
            var keyLabelText = keyLabelObj.GetComponent<Text>();
            keyLabelText.text = ModelSelectorUILocalization.Hotkey;
            keyLabelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            keyLabelText.fontSize = 14;
            keyLabelText.color = Color.white;
            keyLabelText.alignment = TextAnchor.MiddleLeft;
            var keyLabelRect = keyLabelObj.GetComponent<RectTransform>();
            keyLabelRect.sizeDelta = new(80, 20);

            var keyButtonObj = new GameObject("KeyButton", typeof(Image), typeof(Button));
            keyButtonObj.transform.SetParent(settingsPanel.transform, false);
            var keyButtonImage = keyButtonObj.GetComponent<Image>();
            keyButtonImage.color = new(0.2f, 0.2f, 0.2f, 1);
            var keyButtonRect = keyButtonObj.GetComponent<RectTransform>();
            keyButtonRect.sizeDelta = new(120, 30);

            var keyButtonTextObj = new GameObject("Text", typeof(Text));
            keyButtonTextObj.transform.SetParent(keyButtonObj.transform, false);
            _keyButtonText = keyButtonTextObj.GetComponent<Text>();
            _keyButtonText.text = GetKeyCodeDisplayName(_uiConfig?.ToggleKey ?? KeyCode.Backslash);
            _keyButtonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _keyButtonText.fontSize = 14;
            _keyButtonText.color = Color.white;
            _keyButtonText.alignment = TextAnchor.MiddleCenter;
            var keyButtonTextRect = keyButtonTextObj.GetComponent<RectTransform>();
            keyButtonTextRect.anchorMin = Vector2.zero;
            keyButtonTextRect.anchorMax = Vector2.one;
            keyButtonTextRect.sizeDelta = Vector2.zero;

            var keyButton = keyButtonObj.GetComponent<Button>();
            keyButton.onClick.AddListener(OnKeyButtonClicked);
        }

        private void OnKeyButtonClicked()
        {
            if (_uiConfig == null) return;
            _isWaitingForKeyInput = true;
            if (_keyButtonText != null)
                _keyButtonText.text = ModelSelectorUILocalization.PressAnyKey;
        }

        private void HandleKeyInputCapture()
        {
            if (!_isWaitingForKeyInput || _uiConfig == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _isWaitingForKeyInput = false;
                if (_keyButtonText != null)
                    _keyButtonText.text = GetKeyCodeDisplayName(_uiConfig.ToggleKey);
                return;
            }

            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                if (Input.GetKeyDown(keyCode))
                {
                    if (keyCode == KeyCode.Mouse0 || keyCode == KeyCode.Mouse1 || keyCode == KeyCode.Mouse2 ||
                        keyCode == KeyCode.Mouse3 || keyCode == KeyCode.Mouse4 || keyCode == KeyCode.Mouse5 ||
                        keyCode == KeyCode.Mouse6)
                        continue;

                    _uiConfig.ToggleKey = keyCode;
                    ConfigManager.SaveConfigToFile(_uiConfig, "UIConfig.json");
                    _isWaitingForKeyInput = false;
                    if (_keyButtonText != null)
                        _keyButtonText.text = GetKeyCodeDisplayName(keyCode);
                    return;
                }
        }

        private static string GetKeyCodeDisplayName(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.Alpha0 => "0",
                KeyCode.Alpha1 => "1",
                KeyCode.Alpha2 => "2",
                KeyCode.Alpha3 => "3",
                KeyCode.Alpha4 => "4",
                KeyCode.Alpha5 => "5",
                KeyCode.Alpha6 => "6",
                KeyCode.Alpha7 => "7",
                KeyCode.Alpha8 => "8",
                KeyCode.Alpha9 => "9",
                KeyCode.Backslash => "\\",
                KeyCode.Slash => "/",
                KeyCode.LeftBracket => "[",
                KeyCode.RightBracket => "]",
                KeyCode.Semicolon => ";",
                KeyCode.Quote => "'",
                KeyCode.Comma => ",",
                KeyCode.Period => ".",
                KeyCode.Equals => "=",
                KeyCode.Minus => "-",
                KeyCode.Plus => "+",
                KeyCode.LeftShift => "Left Shift",
                KeyCode.RightShift => "Right Shift",
                KeyCode.LeftControl => "Left Ctrl",
                KeyCode.RightControl => "Right Ctrl",
                KeyCode.LeftAlt => "Left Alt",
                KeyCode.RightAlt => "Right Alt",
                KeyCode.LeftCommand => "Left Cmd",
                KeyCode.RightCommand => "Right Cmd",
                KeyCode.LeftWindows => "Left Win",
                KeyCode.RightWindows => "Right Win",
                _ => keyCode.ToString(),
            };
        }

        private void BuildHideEquipmentToggle(GameObject settingsPanel, string labelText, bool isOn,
            UnityAction<bool> onValueChanged)
        {
            var toggleObj = new GameObject("HideEquipmentToggle", typeof(Toggle));
            toggleObj.transform.SetParent(settingsPanel.transform, false);
            var toggle = toggleObj.GetComponent<Toggle>();
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(onValueChanged);

            var toggleImage = toggleObj.AddComponent<Image>();
            toggleImage.color = new(0.2f, 0.2f, 0.2f, 1);
            var toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.sizeDelta = new(20, 20);

            var checkmark = new GameObject("Checkmark", typeof(Image));
            checkmark.transform.SetParent(toggleObj.transform, false);
            var checkmarkImage = checkmark.GetComponent<Image>();
            checkmarkImage.color = new(0.2f, 0.8f, 0.2f, 1);
            var checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new(0.2f, 0.2f);
            checkmarkRect.anchorMax = new(0.8f, 0.8f);
            checkmarkRect.sizeDelta = Vector2.zero;
            toggle.graphic = checkmarkImage;

            var labelObj = new GameObject("Label", typeof(Text));
            labelObj.transform.SetParent(settingsPanel.transform, false);
            var labelTextComponent = labelObj.GetComponent<Text>();
            labelTextComponent.text = labelText;
            labelTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelTextComponent.fontSize = 14;
            labelTextComponent.color = Color.white;
            labelTextComponent.alignment = TextAnchor.MiddleLeft;
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.sizeDelta = new(150, 20);
        }

        private void OnHideCharacterEquipmentToggleChanged(bool value)
        {
            if (_uiConfig == null) return;

            _uiConfig.HideCharacterEquipment = value;
            ConfigManager.SaveConfigToFile(_uiConfig, "UIConfig.json");
            ModLogger.Log($"HideCharacterEquipment setting changed to: {value}");
        }

        private void OnHidePetEquipmentToggleChanged(bool value)
        {
            if (_uiConfig == null) return;

            _uiConfig.HidePetEquipment = value;
            ConfigManager.SaveConfigToFile(_uiConfig, "UIConfig.json");
            ModLogger.Log($"HidePetEquipment setting changed to: {value}");
        }

        private void OnRefreshButtonClicked()
        {
            if (_isRefreshing) return;

            var priorityModelIDs = new List<string>();
            if (_usingModel != null)
            {
                if (!string.IsNullOrEmpty(_usingModel.ModelID)) priorityModelIDs.Add(_usingModel.ModelID);
                if (!string.IsNullOrEmpty(_usingModel.PetModelID)) priorityModelIDs.Add(_usingModel.PetModelID);
            }

            ModelListManager.RefreshModelList(priorityModelIDs);
        }

        public void RefreshModelList()
        {
            if (_isRefreshing) return;

            UpdateModelHandler();

            _refreshCancellationTokenSource = new();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _refreshCancellationTokenSource.Token,
                this.GetCancellationTokenOnDestroy()
            );

            _refreshCompletionSource = new();
            RefreshModelListAsync(linkedCts.Token, linkedCts, _refreshCompletionSource).Forget();
        }

        private async UniTaskVoid RefreshModelListAsync(CancellationToken cancellationToken,
            CancellationTokenSource? linkedCts, UniTaskCompletionSource? completionSource)
        {
            if (_modelListContent == null)
            {
                completionSource?.TrySetResult();
                linkedCts?.Dispose();
                return;
            }

            _isRefreshing = true;
            UpdateRefreshButtonState(true);

            try
            {
                foreach (Transform child in _modelListContent.transform) Destroy(child.gameObject);

                _filteredModelBundles.Clear();

                var searchLower = _searchText.ToLowerInvariant();
                foreach (var bundle in ModelManager.ModelBundles
                             .Where(bundle => string.IsNullOrEmpty(searchLower)
                                              || bundle.BundleName.ToLowerInvariant().Contains(searchLower)
                                              || bundle.Models.Any(m => m.Name.ToLowerInvariant()
                                                                            .Contains(searchLower)
                                                                        || m.ModelID.ToLowerInvariant()
                                                                            .Contains(searchLower))))
                {
                    var compatibleModels = bundle.Models.Where(m => m.CompatibleWithType(_currentTargetType)).ToArray();
                    if (compatibleModels.Length > 0)
                    {
                        var filteredBundle = bundle.CreateFilteredCopy(compatibleModels);
                        _filteredModelBundles.Add(filteredBundle);
                    }
                }

                BuildNoneModelButton();

                var totalCount = _filteredModelBundles.Sum(b => b.Models.Length);
                var count = 0;
                foreach (var bundle in _filteredModelBundles)
                foreach (var model in bundle.Models)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await BuildModelButtonAsync(bundle, model, cancellationToken);
                    count++;

                    if (count % 5 != 0) continue;
                    UpdateRefreshButtonText(ModelSelectorUILocalization.GetLoadingProgress(count, totalCount));
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                await ApplyModelAfterRefresh(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                completionSource?.TrySetResult();
            }
            finally
            {
                _isRefreshing = false;
                UpdateRefreshButtonState(false);
                completionSource?.TrySetResult();
                _refreshCompletionSource = null;
                linkedCts?.Dispose();
            }
        }


        private async UniTask ApplyModelAfterRefresh(CancellationToken cancellationToken)
        {
            if (_usingModel == null) return;

            try
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                UpdateModelHandler();

                if (!string.IsNullOrEmpty(_usingModel.ModelID) && _modelHandler != null)
                {
                    if (ModelManager.FindModelByID(_usingModel.ModelID, out var bundleInfo, out var modelInfo))
                    {
                        if (modelInfo.CompatibleWithType(ModelTarget.Character))
                        {
                            _modelHandler.InitializeCustomModel(bundleInfo, modelInfo);
                            _modelHandler.ChangeToCustomModel();
                            ModLogger.Log(
                                $"Auto-reapplied Character model after refresh: {modelInfo.Name} ({_usingModel.ModelID})");
                        }
                        else
                        {
                            ModLogger.LogWarning(
                                $"Character model '{_usingModel.ModelID}' is not compatible with Character. Restoring to original model.");
                            _usingModel.ModelID = string.Empty;
                            ConfigManager.SaveConfigToFile(_usingModel, "UsingModel.json");
                            _modelHandler.RestoreOriginalModel();
                        }
                    }
                    else
                    {
                        ModLogger.LogWarning(
                            $"Previously used Character model '{_usingModel.ModelID}' not found after refresh. Restoring to original model.");
                        _usingModel.ModelID = string.Empty;
                        ConfigManager.SaveConfigToFile(_usingModel, "UsingModel.json");
                        _modelHandler.RestoreOriginalModel();
                    }
                }

                if (!string.IsNullOrEmpty(_usingModel.PetModelID) && _petModelHandler != null)
                {
                    if (ModelManager.FindModelByID(_usingModel.PetModelID, out var bundleInfo, out var modelInfo))
                    {
                        if (modelInfo.CompatibleWithType(ModelTarget.Pet))
                        {
                            _petModelHandler.InitializeCustomModel(bundleInfo, modelInfo);
                            _petModelHandler.ChangeToCustomModel();
                            ModLogger.Log(
                                $"Auto-reapplied Pet model after refresh: {modelInfo.Name} ({_usingModel.PetModelID})");
                        }
                        else
                        {
                            ModLogger.LogWarning(
                                $"Pet model '{_usingModel.PetModelID}' is not compatible with Pet. Restoring to original model.");
                            _usingModel.PetModelID = string.Empty;
                            ConfigManager.SaveConfigToFile(_usingModel, "UsingModel.json");
                            _petModelHandler.RestoreOriginalModel();
                        }
                    }
                    else
                    {
                        ModLogger.LogWarning(
                            $"Previously used Pet model '{_usingModel.PetModelID}' not found after refresh. Restoring to original model.");
                        _usingModel.PetModelID = string.Empty;
                        ConfigManager.SaveConfigToFile(_usingModel, "UsingModel.json");
                        _petModelHandler.RestoreOriginalModel();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to auto-reapply model after refresh: {ex.Message}");
                if (_modelHandler != null)
                    try
                    {
                        _modelHandler.RestoreOriginalModel();
                    }
                    catch
                    {
                        // ignored
                    }

                if (_petModelHandler != null)
                    try
                    {
                        _petModelHandler.RestoreOriginalModel();
                    }
                    catch
                    {
                        // ignored
                    }
            }
        }

        private void UpdateRefreshButtonState(bool isLoading)
        {
            if (_refreshButton != null) _refreshButton.interactable = !isLoading;

            UpdateRefreshButtonText(isLoading
                ? ModelSelectorUILocalization.Loading
                : ModelSelectorUILocalization.Refresh);
        }

        private void UpdateRefreshButtonText(string text)
        {
            if (_refreshButtonText != null) _refreshButtonText.text = text;
        }

        private async UniTask BuildModelButtonAsync(ModelBundleInfo bundle, ModelInfo model,
            CancellationToken cancellationToken)
        {
            if (_modelListContent == null) return;

            var (isValid, errorMessage) =
                await AssetBundleManager.CheckBundleStatusAsync(bundle, model, cancellationToken);
            var hasError = !isValid;

            var buttonObj = new GameObject($"ModelButton_{model.ModelID}", typeof(Image), typeof(Button),
                typeof(LayoutElement));
            buttonObj.transform.SetParent(_modelListContent.transform, false);

            var buttonImage = buttonObj.GetComponent<Image>();
            buttonImage.color = hasError ? new(0.22f, 0.15f, 0.15f, 0.8f) : new(0.15f, 0.18f, 0.22f, 0.8f);

            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new(1140, 100);

            var layoutElement = buttonObj.GetComponent<LayoutElement>();
            layoutElement.minHeight = 100;
            layoutElement.preferredHeight = 100;
            layoutElement.preferredWidth = 1140;
            layoutElement.flexibleWidth = 0;

            var outline = buttonObj.AddComponent<Outline>();
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
            thumbnailRect.sizeDelta = new(80, 80);

            var thumbnailLayoutElement = thumbnailImage.GetComponent<LayoutElement>();
            thumbnailLayoutElement.minWidth = 80;
            thumbnailLayoutElement.minHeight = 80;
            thumbnailLayoutElement.preferredWidth = 80;
            thumbnailLayoutElement.preferredHeight = 80;
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

                var placeholderText = new GameObject("PlaceholderText", typeof(Text));
                placeholderText.transform.SetParent(thumbnailImage.transform, false);
                var placeholderTextComponent = placeholderText.GetComponent<Text>();
                placeholderTextComponent.text = ModelSelectorUILocalization.NoPreview;
                placeholderTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                placeholderTextComponent.fontSize = 11;
                placeholderTextComponent.color = new(0.6f, 0.6f, 0.6f, 1);
                placeholderTextComponent.alignment = TextAnchor.MiddleCenter;
                var placeholderRect = placeholderText.GetComponent<RectTransform>();
                placeholderRect.anchorMin = Vector2.zero;
                placeholderRect.anchorMax = Vector2.one;
                placeholderRect.sizeDelta = Vector2.zero;
            }

            var contentArea = new GameObject("ContentArea", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentArea.transform.SetParent(buttonObj.transform, false);
            var contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.anchorMin = new(0, 0);
            contentRect.anchorMax = new(1, 1);
            contentRect.offsetMin = new(100, 10);
            contentRect.offsetMax = new(-10, -10);

            var layoutGroup = contentArea.GetComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 2;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.padding = new(0, 0, 0, 0);

            var nameText = new GameObject("Name", typeof(Text));
            nameText.transform.SetParent(contentArea.transform, false);
            var nameTextComponent = nameText.GetComponent<Text>();
            nameTextComponent.text = string.IsNullOrEmpty(model.Name) ? model.ModelID : model.Name;
            nameTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameTextComponent.fontSize = 16;
            nameTextComponent.fontStyle = FontStyle.Bold;
            nameTextComponent.color = hasError ? new(1f, 0.6f, 0.6f, 1) : Color.white;
            nameTextComponent.alignment = TextAnchor.UpperLeft;
            nameTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameTextComponent.verticalOverflow = VerticalWrapMode.Truncate;
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.sizeDelta = new(0, 20);

            var infoText = new GameObject("Info", typeof(Text));
            infoText.transform.SetParent(contentArea.transform, false);
            var infoTextComponent = infoText.GetComponent<Text>();
            infoTextComponent.text =
                ModelSelectorUILocalization.GetModelInfo(model.ModelID, model.Author, model.Version);
            infoTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            infoTextComponent.fontSize = 12;
            infoTextComponent.color = hasError ? new(1f, 0.7f, 0.7f, 1) : new(0.8f, 0.8f, 0.8f, 1);
            infoTextComponent.alignment = TextAnchor.UpperLeft;
            infoTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            infoTextComponent.verticalOverflow = VerticalWrapMode.Truncate;
            var infoRect = infoText.GetComponent<RectTransform>();
            infoRect.sizeDelta = new(0, 18);

            if (hasError && !string.IsNullOrEmpty(errorMessage))
            {
                var errorText = new GameObject("Error", typeof(Text));
                errorText.transform.SetParent(contentArea.transform, false);
                var errorTextComponent = errorText.GetComponent<Text>();
                errorTextComponent.text = $"⚠ {errorMessage}";
                errorTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                errorTextComponent.fontSize = 11;
                errorTextComponent.fontStyle = FontStyle.Bold;
                errorTextComponent.color = new(1f, 0.4f, 0.4f, 1);
                errorTextComponent.alignment = TextAnchor.UpperLeft;
                errorTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                errorTextComponent.verticalOverflow = VerticalWrapMode.Truncate;
                var errorRect = errorText.GetComponent<RectTransform>();
                errorRect.sizeDelta = new(0, 16);
            }

            if (!string.IsNullOrEmpty(model.Description))
            {
                var descText = new GameObject("Description", typeof(Text), typeof(ContentSizeFitter));
                descText.transform.SetParent(contentArea.transform, false);
                var descTextComponent = descText.GetComponent<Text>();
                descTextComponent.text = model.Description;
                descTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                descTextComponent.fontSize = 11;
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
            var colors = button.colors;
            colors.normalColor = new(1, 1, 1, 1);
            colors.highlightedColor = hasError ? new(0.7f, 0.5f, 0.5f, 1) : new(0.5f, 0.7f, 0.9f, 1);
            colors.pressedColor = hasError ? new(0.6f, 0.4f, 0.4f, 1) : new(0.4f, 0.6f, 0.8f, 1);
            colors.selectedColor = hasError ? new(0.7f, 0.5f, 0.5f, 1) : new(0.5f, 0.7f, 0.9f, 1);
            button.colors = colors;

            button.interactable = !hasError;
            button.onClick.AddListener(() => OnModelSelected(bundle, model));
        }

        private void BuildNoneModelButton()
        {
            if (_modelListContent == null) return;

            var buttonObj = new GameObject("NoneModelButton", typeof(Image), typeof(Button),
                typeof(LayoutElement));
            buttonObj.transform.SetParent(_modelListContent.transform, false);
            buttonObj.transform.SetAsFirstSibling();

            var buttonImage = buttonObj.GetComponent<Image>();
            buttonImage.color = new(0.2f, 0.15f, 0.15f, 0.8f);

            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new(1140, 80);

            var layoutElement = buttonObj.GetComponent<LayoutElement>();
            layoutElement.minHeight = 80;
            layoutElement.preferredHeight = 80;
            layoutElement.preferredWidth = 1140;
            layoutElement.flexibleWidth = 0;

            var outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = new(0.4f, 0.3f, 0.3f, 0.6f);
            outline.effectDistance = new(1, -1);

            var nameText = new GameObject("Name", typeof(Text));
            nameText.transform.SetParent(buttonObj.transform, false);
            var nameTextComponent = nameText.GetComponent<Text>();
            nameTextComponent.text = ModelSelectorUILocalization.NoModel;
            nameTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameTextComponent.fontSize = 16;
            nameTextComponent.fontStyle = FontStyle.Bold;
            nameTextComponent.color = Color.white;
            nameTextComponent.alignment = TextAnchor.MiddleCenter;
            nameTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameTextComponent.verticalOverflow = VerticalWrapMode.Truncate;
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.pivot = new(0.5f, 0.5f);
            nameRect.offsetMin = new(10, 0);
            nameRect.offsetMax = new(-10, 0);

            var button = buttonObj.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new(1, 1, 1, 1);
            colors.highlightedColor = new(0.7f, 0.5f, 0.5f, 1);
            colors.pressedColor = new(0.6f, 0.4f, 0.4f, 1);
            colors.selectedColor = new(0.7f, 0.5f, 0.5f, 1);
            button.colors = colors;

            button.onClick.AddListener(OnNoneModelSelected);
        }

        private void OnModelSelected(ModelBundleInfo bundle, ModelInfo model)
        {
            if (_usingModel == null)
            {
                ModLogger.LogError("UsingModel is null.");
                return;
            }

            ModelHandler? targetHandler;
            if (_currentTargetType == ModelTarget.Character)
            {
                if (_modelHandler == null)
                {
                    ModLogger.LogError("ModelHandler for Character is null.");
                    return;
                }

                targetHandler = _modelHandler;
                _usingModel.ModelID = model.ModelID;
            }
            else
            {
                if (_petModelHandler == null)
                {
                    ModLogger.LogError("ModelHandler for Pet is null.");
                    return;
                }

                targetHandler = _petModelHandler;
                _usingModel.PetModelID = model.ModelID;
            }

            ConfigManager.SaveConfigToFile(_usingModel, "UsingModel.json");

            targetHandler.InitializeCustomModel(bundle, model);
            targetHandler.ChangeToCustomModel();

            ModLogger.Log($"Selected model for {_currentTargetType}: {model.Name} ({model.ModelID})");
            HidePanel();
        }

        private void OnNoneModelSelected()
        {
            if (_usingModel == null)
            {
                ModLogger.LogError("UsingModel is null.");
                return;
            }

            ModelHandler? targetHandler;
            if (_currentTargetType == ModelTarget.Character)
            {
                if (_modelHandler == null)
                {
                    ModLogger.LogError("ModelHandler for Character is null.");
                    return;
                }

                targetHandler = _modelHandler;
                _usingModel.ModelID = string.Empty;
            }
            else
            {
                if (_petModelHandler == null)
                {
                    ModLogger.LogError("ModelHandler for Pet is null.");
                    return;
                }

                targetHandler = _petModelHandler;
                _usingModel.PetModelID = string.Empty;
            }

            ConfigManager.SaveConfigToFile(_usingModel, "UsingModel.json");

            targetHandler.RestoreOriginalModel();

            ModLogger.Log($"Restored to original model for {_currentTargetType}.");
            HidePanel();
        }

        private void OnSearchChanged(string text)
        {
            _searchText = text;
            RefreshModelList();
        }

        private static InputField BuildInput(string placeholder)
        {
            var inputObj = new GameObject("Input", typeof(Image));
            var inputImage = inputObj.GetComponent<Image>();
            inputImage.color = new(0.1f, 0.12f, 0.15f, 0.9f);

            var outline = inputObj.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(1, -1);

            var textObj = new GameObject("Text", typeof(Text));
            textObj.transform.SetParent(inputObj.transform, false);
            var textComponent = textObj.GetComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleLeft;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new(0, 0);
            textRect.anchorMax = new(1, 1);
            textRect.offsetMin = new(8, 0);
            textRect.offsetMax = new(-8, 0);

            var placeholderObj = new GameObject("Placeholder", typeof(Text));
            placeholderObj.transform.SetParent(inputObj.transform, false);
            var placeholderComponent = placeholderObj.GetComponent<Text>();
            placeholderComponent.text = placeholder;
            placeholderComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholderComponent.color = new(1, 1, 1, 0.4f);
            placeholderComponent.alignment = TextAnchor.MiddleLeft;
            var placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new(0, 0);
            placeholderRect.anchorMax = new(1, 1);
            placeholderRect.offsetMin = new(8, 0);
            placeholderRect.offsetMax = new(-8, 0);

            var inputField = inputObj.AddComponent<InputField>();
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderComponent;

            return inputField;
        }

        private static bool IsTypingInInputField()
        {
            var current = EventSystem.current;
            if (current == null || current.currentSelectedGameObject == null) return false;

            var inputField = current.currentSelectedGameObject.GetComponent<InputField>();
            return inputField != null && inputField.isFocused;
        }

        private void ShowPanel()
        {
            if (!_isInitialized || _panelRoot == null)
            {
                ModLogger.LogWarning("Cannot show panel - not initialized!");
                return;
            }

            UpdateModelHandler();

            _uiActive = true;
            if (_overlay != null) _overlay.SetActive(true);

            if (_panelRoot != null) _panelRoot.SetActive(true);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var current = EventSystem.current;
            if (current == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                DontDestroyOnLoad(eventSystem);
            }

            _charInput = FindObjectOfType<CharacterInputControl>();
            if (_charInput != null)
            {
                _charInput.enabled = false;
                ModLogger.Log("CharacterInputControl disabled.");
            }

            _playerInput = FindObjectOfType<PlayerInput>();
            if (_playerInput != null)
            {
                _playerInput.DeactivateInput();
                ModLogger.Log("PlayerInput deactivated (game input blocked).");
            }

            var allBehaviours = FindObjectsOfType<MonoBehaviour>();
            foreach (var behaviour in allBehaviours)
            {
                var type = behaviour.GetType();
                if (!type.Name.Contains("CameraController") && !type.Name.Contains("MouseLook")) continue;
                behaviour.enabled = false;
                _cameraController = behaviour;
                _cameraLockDisabled = true;
                ModLogger.Log($"Camera controller disabled: {type.FullName}");
                break;
            }

            StartCoroutine(ForceCursorFree());
            ModLogger.Log("Model selector panel opened.");
        }

        private void HidePanel()
        {
            _uiActive = false;
            StopAllCoroutines();

            if (_overlay != null) _overlay.SetActive(false);

            if (_panelRoot != null) _panelRoot.SetActive(false);

            if (_charInput != null)
            {
                _charInput.enabled = true;
                _charInput = null;
                ModLogger.Log("CharacterInputControl re-enabled.");
            }

            if (_playerInput != null)
            {
                _playerInput.ActivateInput();
                _playerInput = null;
                ModLogger.Log("PlayerInput reactivated (game input restored).");
            }

            if (_cameraLockDisabled && _cameraController != null)
            {
                _cameraController.enabled = true;
                _cameraController = null;
                _cameraLockDisabled = false;
                ModLogger.Log("Camera controller re-enabled.");
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            ModLogger.Log("Model selector panel closed.");

            if (_isRefreshing && _refreshCompletionSource != null) EnsureModelAppliedAfterRefresh().Forget();
        }

        private async UniTaskVoid EnsureModelAppliedAfterRefresh()
        {
            try
            {
                if (_refreshCompletionSource != null) await _refreshCompletionSource.Task;

                await UniTask.Yield(PlayerLoopTiming.Update);

                UpdateModelHandler();

                if (_usingModel == null) return;

                if (!string.IsNullOrEmpty(_usingModel.ModelID) && _modelHandler != null)
                {
                    if (ModelManager.FindModelByID(_usingModel.ModelID, out var bundleInfo, out var modelInfo))
                    {
                        if (modelInfo.CompatibleWithType(ModelTarget.Character))
                        {
                            try
                            {
                                _modelHandler.InitializeCustomModel(bundleInfo, modelInfo);
                                _modelHandler.ChangeToCustomModel();
                                ModLogger.Log(
                                    $"Character model reapplied after window close: {modelInfo.Name} ({_usingModel.ModelID})");
                            }
                            catch (Exception ex)
                            {
                                ModLogger.LogError(
                                    $"Failed to reapply Character model after window close: {ex.Message}");
                                _modelHandler.RestoreOriginalModel();
                            }
                        }
                        else
                        {
                            ModLogger.LogWarning(
                                $"Character model '{_usingModel.ModelID}' is not compatible with Character. Restoring to original model.");
                            _usingModel.ModelID = string.Empty;
                            ConfigManager.SaveConfigToFile(_usingModel, "UsingModel.json");
                            _modelHandler.RestoreOriginalModel();
                        }
                    }
                    else
                    {
                        ModLogger.LogWarning(
                            $"Character model '{_usingModel.ModelID}' not found. Restoring to original model.");
                        _usingModel.ModelID = string.Empty;
                        ConfigManager.SaveConfigToFile(_usingModel, "UsingModel.json");
                        _modelHandler.RestoreOriginalModel();
                    }
                }

                if (!string.IsNullOrEmpty(_usingModel.PetModelID) && _petModelHandler != null)
                {
                    if (ModelManager.FindModelByID(_usingModel.PetModelID, out var bundleInfo, out var modelInfo))
                    {
                        if (modelInfo.CompatibleWithType(ModelTarget.Pet))
                        {
                            try
                            {
                                _petModelHandler.InitializeCustomModel(bundleInfo, modelInfo);
                                _petModelHandler.ChangeToCustomModel();
                                ModLogger.Log(
                                    $"Pet model reapplied after window close: {modelInfo.Name} ({_usingModel.PetModelID})");
                            }
                            catch (Exception ex)
                            {
                                ModLogger.LogError($"Failed to reapply Pet model after window close: {ex.Message}");
                                _petModelHandler.RestoreOriginalModel();
                            }
                        }
                        else
                        {
                            ModLogger.LogWarning(
                                $"Pet model '{_usingModel.PetModelID}' is not compatible with Pet. Restoring to original model.");
                            _usingModel.PetModelID = string.Empty;
                            ConfigManager.SaveConfigToFile(_usingModel, "UsingModel.json");
                            _petModelHandler.RestoreOriginalModel();
                        }
                    }
                    else
                    {
                        ModLogger.LogWarning(
                            $"Pet model '{_usingModel.PetModelID}' not found. Restoring to original model.");
                        _usingModel.PetModelID = string.Empty;
                        ConfigManager.SaveConfigToFile(_usingModel, "UsingModel.json");
                        _petModelHandler.RestoreOriginalModel();
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error ensuring model applied after refresh: {ex.Message}");
            }
        }

        private IEnumerator ForceCursorFree()
        {
            while (_uiActive)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                yield return null;
            }
        }

        private void UpdateModelHandler()
        {
            if (LevelManager.Instance == null) return;

            var mainCharacterControl = LevelManager.Instance.MainCharacter;
            if (mainCharacterControl != null)
            {
                _modelHandler = mainCharacterControl.GetComponent<ModelHandler>();
                if (_modelHandler == null) _modelHandler = ModelManager.InitializeModelHandler(mainCharacterControl);
            }

            var petCharacterControl = LevelManager.Instance.PetCharacter;
            if (petCharacterControl == null) return;
            _petModelHandler = petCharacterControl.GetComponent<ModelHandler>();
            if (_petModelHandler == null)
                _petModelHandler = ModelManager.InitializeModelHandler(petCharacterControl, true);
        }
    }
}