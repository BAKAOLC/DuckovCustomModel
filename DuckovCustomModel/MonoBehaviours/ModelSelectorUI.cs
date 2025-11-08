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
        private HideEquipmentConfig? _hideEquipmentConfig;
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
            _hideEquipmentConfig = ModBehaviour.Instance?.HideEquipmentConfig;
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

            _overlay = CreateImage("Overlay", _uiRoot.transform);
            SetupRectTransform(_overlay, Vector2.zero, Vector2.one, Vector2.zero);
            _overlay.GetComponent<Image>().color = new(0, 0, 0, 0.5f);

            _panelRoot = CreateImage("Panel", _uiRoot.transform);
            var panelRect = _panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new(0.5f, 0.5f);
            panelRect.anchorMax = new(0.5f, 0.5f);
            panelRect.pivot = new(0.5f, 0.5f);
            panelRect.sizeDelta = new(1200, 700);
            panelRect.anchoredPosition = Vector2.zero;
            _panelRoot.GetComponent<Image>().color = new(0.1f, 0.12f, 0.15f, 0.95f);

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

            var versionLabel = CreateText("VersionLabel", _panelRoot.transform, $"v{Constant.ModVersion}", 14,
                new(0.8f, 0.8f, 0.8f, 1), TextAnchor.UpperLeft);
            SetupRectTransform(versionLabel, new(0, 1), new(0, 1), new(100, 20), new(0, 1), new(10, -10));
        }

        private void BuildTitle()
        {
            if (_panelRoot == null) return;

            var title = CreateText("Title", _panelRoot.transform, ModelSelectorUILocalization.Title, 24, Color.white,
                TextAnchor.MiddleCenter);
            var titleText = title.GetComponent<Text>();
            titleText.fontStyle = FontStyle.Bold;
            SetupRectTransform(title, new(0, 1), new(1, 1), new(0, 40), new(0.5f, 1), new(0, -20));
        }

        private void BuildTargetTypeSelector()
        {
            if (_panelRoot == null) return;

            var targetTypeLabel = CreateText("TargetTypeLabel", _panelRoot.transform,
                ModelSelectorUILocalization.TargetType, 14, Color.white);
            SetupRectTransform(targetTypeLabel, new(0, 1), new(0, 1), new(100, 30), new(0, 1), new(20, -70));

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

            var labelObj = CreateText("Label", dropdownObj.transform, "", 14, Color.white);
            var labelTextComponent = labelObj.GetComponent<Text>();
            var labelTextRect = labelObj.GetComponent<RectTransform>();
            labelTextRect.anchorMin = new(0, 0);
            labelTextRect.anchorMax = new(1, 1);
            labelTextRect.offsetMin = new(10, 0);
            labelTextRect.offsetMax = new(-25, 0);
            _targetTypeDropdown.captionText = labelTextComponent;

            var arrowObj = CreateImage("Arrow", dropdownObj.transform);
            var arrowImage = arrowObj.GetComponent<Image>();
            arrowImage.color = Color.black;
            SetupRectTransform(arrowObj, new(1, 0.5f), new(1, 0.5f), new(20, 20), new(1, 0.5f), new(-10, 0));
            _targetTypeDropdown.targetGraphic = arrowImage;

            var templateObj = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateObj.transform.SetParent(dropdownObj.transform, false);
            var templateImage = templateObj.GetComponent<Image>();
            templateImage.color = new(0.15f, 0.18f, 0.22f, 0.98f);
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
            var itemImage = itemObj.GetComponent<Image>();
            itemImage.color = new(0.15f, 0.18f, 0.22f, 1f);
            var itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.anchorMin = new(0, 0.5f);
            itemRect.anchorMax = new(1, 0.5f);
            itemRect.sizeDelta = new(0, 30);

            var itemLabelObj = CreateText("Item Label", itemObj.transform, "", 14,
                new(0.95f, 0.95f, 0.95f, 1f));
            var itemLabelText = itemLabelObj.GetComponent<Text>();
            var itemLabelRect = itemLabelObj.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new(10, 0);
            itemLabelRect.offsetMax = new(-10, 0);

            var checkmarkObj = CreateImage("Checkmark", itemObj.transform);
            var checkmarkImage = checkmarkObj.GetComponent<Image>();
            checkmarkImage.color = new(0.2f, 0.8f, 0.2f, 1f);
            SetupRectTransform(checkmarkObj, new(0, 0.5f), new(0, 0.5f), new(16, 16), new(0.5f, 0.5f), new(5, 0));
            checkmarkObj.SetActive(false);

            var toggle = itemObj.GetComponent<Toggle>();
            var colors = toggle.colors;
            colors.normalColor = new(0.15f, 0.18f, 0.22f, 1f);
            colors.highlightedColor = new(0.25f, 0.3f, 0.35f, 1f);
            colors.pressedColor = new(0.2f, 0.25f, 0.3f, 1f);
            colors.selectedColor = new(0.25f, 0.3f, 0.35f, 1f);
            colors.disabledColor = new(0.1f, 0.12f, 0.15f, 0.5f);
            toggle.colors = colors;
            toggle.targetGraphic = itemImage;
            toggle.graphic = checkmarkImage;

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

            _loadingStatusText = CreateText("LoadingStatus", scrollView.transform, "", 16, Color.white,
                TextAnchor.MiddleCenter);
            SetupRectTransform(_loadingStatusText, new(0, 0), new(1, 1), Vector2.zero);
            _loadingStatusText.SetActive(false);
        }

        private void BuildCloseButton()
        {
            if (_panelRoot == null) return;

            var closeButton = CreateButton("CloseButton", _panelRoot.transform, HidePanel);
            closeButton.GetComponent<Image>().color = new(0.2f, 0.2f, 0.2f, 1);
            SetupRectTransform(closeButton, new(1, 1), new(1, 1), new(30, 30), new(1, 1), new(-10, -10));

            var closeText = CreateText("Text", closeButton.transform, "×", 20, Color.white, TextAnchor.MiddleCenter);
            SetupRectTransform(closeText, Vector2.zero, Vector2.one, Vector2.zero);
        }

        private void BuildRefreshButton()
        {
            if (_panelRoot == null) return;

            var refreshButton = CreateButton("RefreshButton", _panelRoot.transform, OnRefreshButtonClicked);
            refreshButton.GetComponent<Image>().color = new(0.2f, 0.3f, 0.4f, 1);
            SetupRectTransform(refreshButton, new(1, 0), new(1, 0), new(100, 30), new(1, 0), new(-10, 10));

            var refreshText = CreateText("Text", refreshButton.transform, ModelSelectorUILocalization.Refresh, 14,
                Color.white, TextAnchor.MiddleCenter);
            SetupRectTransform(refreshText, Vector2.zero, Vector2.one, Vector2.zero);

            _refreshButton = refreshButton.GetComponent<Button>();
            SetupButtonColors(_refreshButton, new(1, 1, 1, 1), new(0.4f, 0.5f, 0.6f, 1), new(0.3f, 0.4f, 0.5f, 1),
                new(0.4f, 0.5f, 0.6f, 1));
            _refreshButtonText = refreshText.GetComponent<Text>();
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
                _hideEquipmentConfig?.GetHideEquipment(ModelTarget.Character) ?? false,
                OnHideCharacterEquipmentToggleChanged);

            BuildHideEquipmentToggle(settingsPanel, ModelSelectorUILocalization.HidePetEquipment,
                _hideEquipmentConfig?.GetHideEquipment(ModelTarget.Pet) ?? false,
                OnHidePetEquipmentToggleChanged);

            BuildKeySetting(settingsPanel);
        }

        private void BuildKeySetting(GameObject settingsPanel)
        {
            if (_panelRoot == null) return;

            var keyLabelObj = CreateText("KeyLabel", settingsPanel.transform, ModelSelectorUILocalization.Hotkey, 14,
                Color.white);
            var keyLabelRect = keyLabelObj.GetComponent<RectTransform>();
            keyLabelRect.sizeDelta = new(80, 20);

            var keyButtonObj = CreateButton("KeyButton", settingsPanel.transform, OnKeyButtonClicked);
            keyButtonObj.GetComponent<Image>().color = new(0.2f, 0.2f, 0.2f, 1);
            var keyButtonRect = keyButtonObj.GetComponent<RectTransform>();
            keyButtonRect.sizeDelta = new(120, 30);

            var keyButtonTextObj = CreateText("Text", keyButtonObj.transform,
                GetKeyCodeDisplayName(_uiConfig?.ToggleKey ?? KeyCode.Backslash), 14, Color.white,
                TextAnchor.MiddleCenter);
            _keyButtonText = keyButtonTextObj.GetComponent<Text>();
            SetupRectTransform(keyButtonTextObj, Vector2.zero, Vector2.one, Vector2.zero);
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

            var checkmark = CreateImage("Checkmark", toggleObj.transform);
            var checkmarkImage = checkmark.GetComponent<Image>();
            checkmarkImage.color = new(0.2f, 0.8f, 0.2f, 1);
            var checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new(0.2f, 0.2f);
            checkmarkRect.anchorMax = new(0.8f, 0.8f);
            checkmarkRect.sizeDelta = Vector2.zero;
            toggle.graphic = checkmarkImage;

            var labelObj = CreateText("Label", settingsPanel.transform, labelText, 14, Color.white);
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.sizeDelta = new(150, 20);
        }

        private void OnHideCharacterEquipmentToggleChanged(bool value)
        {
            if (_hideEquipmentConfig == null) return;

            _hideEquipmentConfig.SetHideEquipment(ModelTarget.Character, value);
            ConfigManager.SaveConfigToFile(_hideEquipmentConfig, "HideEquipmentConfig.json");
            ModLogger.Log($"HideEquipment setting for {ModelTarget.Character} changed to: {value}");
        }

        private void OnHidePetEquipmentToggleChanged(bool value)
        {
            if (_hideEquipmentConfig == null) return;

            _hideEquipmentConfig.SetHideEquipment(ModelTarget.Pet, value);
            ConfigManager.SaveConfigToFile(_hideEquipmentConfig, "HideEquipmentConfig.json");
            ModLogger.Log($"HideEquipment setting for {ModelTarget.Pet} changed to: {value}");
        }

        private void OnRefreshButtonClicked()
        {
            if (_isRefreshing) return;

            var priorityModelIDs = new List<string>();
            if (_usingModel != null)
                priorityModelIDs.AddRange(from ModelTarget target in Enum.GetValues(typeof(ModelTarget))
                    select _usingModel.GetModelID(target)
                    into modelID
                    where !string.IsNullOrEmpty(modelID)
                    select modelID);

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
                    if (compatibleModels.Length <= 0) continue;
                    var filteredBundle = bundle.CreateFilteredCopy(compatibleModels);
                    _filteredModelBundles.Add(filteredBundle);
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

                var bundlesToReload = ModelListManager.CurrentRefreshingBundles;

                foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
                {
                    var modelID = _usingModel.GetModelID(target);
                    if (string.IsNullOrEmpty(modelID)) continue;

                    ModelListManager.ApplyModelToTargetAfterRefresh(target, modelID, bundlesToReload);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to auto-reapply model after refresh: {ex.Message}");
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

            var isInUse = _usingModel != null && _usingModel.GetModelID(_currentTargetType) == model.ModelID;

            var buttonObj = new GameObject($"ModelButton_{model.ModelID}", typeof(Image), typeof(Button),
                typeof(LayoutElement));
            buttonObj.transform.SetParent(_modelListContent.transform, false);

            var buttonImage = buttonObj.GetComponent<Image>();
            Color baseColor = hasError ? new(0.22f, 0.15f, 0.15f, 0.8f) : new(0.15f, 0.18f, 0.22f, 0.8f);
            if (isInUse && !hasError) baseColor = new(0.15f, 0.22f, 0.18f, 0.8f);
            buttonImage.color = baseColor;

            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new(1140, 100);

            var layoutElement = buttonObj.GetComponent<LayoutElement>();
            layoutElement.minHeight = 100;
            layoutElement.preferredHeight = 100;
            layoutElement.preferredWidth = 1140;
            layoutElement.flexibleWidth = 0;

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

            var nameText = CreateText("Name", contentArea.transform,
                string.IsNullOrEmpty(model.Name) ? model.ModelID : model.Name, 16,
                hasError ? new(1f, 0.6f, 0.6f, 1) : Color.white, TextAnchor.UpperLeft);
            var nameTextComponent = nameText.GetComponent<Text>();
            nameTextComponent.fontStyle = FontStyle.Bold;
            nameTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameTextComponent.verticalOverflow = VerticalWrapMode.Truncate;
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.sizeDelta = new(0, 20);

            var infoText = CreateText("Info", contentArea.transform,
                ModelSelectorUILocalization.GetModelInfo(model.ModelID, model.Author, model.Version, model.BundleName),
                12,
                hasError ? new(1f, 0.7f, 0.7f, 1) : new(0.8f, 0.8f, 0.8f, 1), TextAnchor.UpperLeft);
            var infoTextComponent = infoText.GetComponent<Text>();
            infoTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            infoTextComponent.verticalOverflow = VerticalWrapMode.Truncate;
            var infoRect = infoText.GetComponent<RectTransform>();
            infoRect.sizeDelta = new(0, 18);

            if (hasError && !string.IsNullOrEmpty(errorMessage))
            {
                var errorText = CreateText("Error", contentArea.transform, $"⚠ {errorMessage}", 11,
                    new(1f, 0.4f, 0.4f, 1), TextAnchor.UpperLeft);
                var errorTextComponent = errorText.GetComponent<Text>();
                errorTextComponent.fontStyle = FontStyle.Bold;
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
            if (isInUse && !hasError)
                SetupButtonColors(button, new(1, 1, 1, 1), new(0.5f, 0.8f, 0.6f, 1), new(0.4f, 0.7f, 0.5f, 1),
                    new(0.5f, 0.8f, 0.6f, 1));
            else
                SetupButtonColors(button, new(1, 1, 1, 1),
                    hasError ? new(0.7f, 0.5f, 0.5f, 1) : new(0.5f, 0.7f, 0.9f, 1),
                    hasError ? new(0.6f, 0.4f, 0.4f, 1) : new(0.4f, 0.6f, 0.8f, 1),
                    hasError ? new(0.7f, 0.5f, 0.5f, 1) : new(0.5f, 0.7f, 0.9f, 1));

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

            var nameText = CreateText("Name", buttonObj.transform, ModelSelectorUILocalization.NoModel, 16,
                Color.white, TextAnchor.MiddleCenter);
            var nameTextComponent = nameText.GetComponent<Text>();
            nameTextComponent.fontStyle = FontStyle.Bold;
            nameTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameTextComponent.verticalOverflow = VerticalWrapMode.Truncate;
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.pivot = new(0.5f, 0.5f);
            nameRect.offsetMin = new(10, 0);
            nameRect.offsetMax = new(-10, 0);

            var button = buttonObj.GetComponent<Button>();
            SetupButtonColors(button, new(1, 1, 1, 1), new(0.7f, 0.5f, 0.5f, 1), new(0.6f, 0.4f, 0.4f, 1),
                new(0.7f, 0.5f, 0.5f, 1));
            button.onClick.AddListener(OnNoneModelSelected);
        }

        private void OnModelSelected(ModelBundleInfo bundle, ModelInfo model)
        {
            if (_usingModel == null)
            {
                ModLogger.LogError("UsingModel is null.");
                return;
            }

            _usingModel.SetModelID(_currentTargetType, model.ModelID);
            ConfigManager.SaveConfigToFile(_usingModel, "UsingModel.json");

            ApplyModelWithRefreshCheck(_currentTargetType, bundle, model).Forget();
        }

        private async UniTaskVoid ApplyModelWithRefreshCheck(ModelTarget target, ModelBundleInfo bundle,
            ModelInfo model)
        {
            try
            {
                if (ModelListManager.IsRefreshing && ModelListManager.CurrentRefreshingBundles != null)
                    if (ModelListManager.CurrentRefreshingBundles.Contains(bundle.BundleName))
                    {
                        var handlers = ModelManager.GetAllModelHandlers(target);
                        foreach (var handler in handlers.Where(handler => handler.IsHiddenOriginalModel))
                            handler.CleanupCustomModel();

                        ModLogger.Log(
                            $"Temporarily cleaned up {target} custom model while waiting for bundle update: {bundle.BundleName}");

                        await ModelListManager.WaitForRefreshCompletion();
                        await UniTask.Yield(PlayerLoopTiming.Update);
                    }

                ModelListManager.ApplyModelToTarget(target, model.ModelID, true);
                HidePanel();
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to apply model '{model.ModelID}' to {target}: {ex.Message}");
            }
        }

        private void OnNoneModelSelected()
        {
            if (_usingModel == null)
            {
                ModLogger.LogError("UsingModel is null.");
                return;
            }

            _usingModel.SetModelID(_currentTargetType, string.Empty);
            ConfigManager.SaveConfigToFile(_usingModel, "UsingModel.json");

            ModelListManager.RestoreOriginalModelForTarget(_currentTargetType);
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
            RefreshModelList();

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

                foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
                {
                    var modelID = _usingModel.GetModelID(target);
                    if (string.IsNullOrEmpty(modelID)) continue;

                    ModelListManager.ApplyModelToTarget(target, modelID);
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
                _petModelHandler = ModelManager.InitializeModelHandler(petCharacterControl, ModelTarget.Pet);
        }

        private static GameObject CreateImage(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(Image));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static GameObject CreateText(string name, Transform parent, string text, int fontSize = 14,
            Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var obj = new GameObject(name, typeof(Text));
            obj.transform.SetParent(parent, false);
            var textComponent = obj.GetComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = color ?? Color.white;
            textComponent.alignment = alignment;
            return obj;
        }

        private static GameObject CreateButton(string name, Transform parent, UnityAction? onClick = null)
        {
            var obj = new GameObject(name, typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            var button = obj.GetComponent<Button>();
            if (onClick != null) button.onClick.AddListener(onClick);
            return obj;
        }

        private static void SetupRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta,
            Vector2? pivot = null, Vector2? anchoredPosition = null)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            if (pivot.HasValue) rect.pivot = pivot.Value;
            if (anchoredPosition.HasValue) rect.anchoredPosition = anchoredPosition.Value;
        }

        private static void SetupButtonColors(Button button, Color? normalColor = null, Color? highlightedColor = null,
            Color? pressedColor = null, Color? selectedColor = null)
        {
            var colors = button.colors;
            colors.normalColor = normalColor ?? new(1, 1, 1, 1);
            colors.highlightedColor = highlightedColor ?? new(0.5f, 0.7f, 0.9f, 1);
            colors.pressedColor = pressedColor ?? new(0.4f, 0.6f, 0.8f, 1);
            colors.selectedColor = selectedColor ?? new(0.5f, 0.7f, 0.9f, 1);
            button.colors = colors;
        }
    }
}