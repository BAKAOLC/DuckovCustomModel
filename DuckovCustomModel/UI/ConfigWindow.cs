using System;
using System.Collections;
using System.Collections.Generic;
using Duckov.UI;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.MonoBehaviours;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Components;
using DuckovCustomModel.UI.Tabs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DuckovCustomModel.UI
{
    public class ConfigWindow : MonoBehaviour
    {
        private const float AnimatorParamsWindowWidth = 700f;
        private const float AnimatorParamsWindowHeight = 900f;
        private readonly Dictionary<int, bool> _paramIsChanging = new();
        private readonly Dictionary<int, object> _paramPreviousValues = new();
        private Vector2 _animatorParamsScrollPosition;
        private bool _animatorParamsViewingPet;
        private Rect _animatorParamsWindowRect = new(10, 10, 600, 1000);

        private CharacterInputControl? _charInput;
        private bool _charInputWasEnabled;
        private bool _cursorWasVisible;
        private bool _isInitialized;

        private AnchorPosition _lastAnchorPosition;
        private float _lastOffsetX;
        private float _lastOffsetY;
        private ModelHandler? _modelHandler;
        private ModelSelectionTab? _modelSelectionTab;
        private CursorLockMode _originalCursorLockState;
        private GameObject? _overlay;
        private GameObject? _panelRoot;
        private GUIStyle? _paramLabelStyle;
        private GUIStyle? _paramLabelStyleChanged;
        private GUIStyle? _paramLabelStyleChanging;
        private ModelHandler? _petModelHandler;
        private PlayerInput? _playerInput;
        private bool _playerInputWasActive;
        private GUIStyle? _scrollViewStyle;
        private GameObject? _settingsButton;
        private SettingsTab? _settingsTab;
        private bool _showAnimatorParamsWindow;
        private TabSystem? _tabSystem;
        private bool _uiActive;
        private GameObject? _uiRoot;

        private static UIConfig? UIConfig => ModEntry.UIConfig;
        private static CharacterInputControl? CharacterInputControl => CharacterInputControl.Instance;
        private static PlayerInput? PlayerInput => GameManager.MainPlayerInput;

        private void Update()
        {
            var uiConfig = UIConfig;
            if (uiConfig == null) return;

            if (!_isInitialized)
            {
                InitializeUI();
                return;
            }

            UpdateSettingsButtonVisibility();

            if (uiConfig.DCMButtonAnchor != _lastAnchorPosition ||
                Math.Abs(uiConfig.DCMButtonOffsetX - _lastOffsetX) > 0.01f ||
                Math.Abs(uiConfig.DCMButtonOffsetY - _lastOffsetY) > 0.01f)
            {
                _lastAnchorPosition = uiConfig.DCMButtonAnchor;
                _lastOffsetX = uiConfig.DCMButtonOffsetX;
                _lastOffsetY = uiConfig.DCMButtonOffsetY;
                RefreshSettingsButton();
            }

            if (IsTypingInInputField() || _panelRoot == null) return;

            if (_settingsTab != null && _settingsTab.IsWaitingForKeyInput) return;

            if (Input.GetKeyDown(uiConfig.ToggleKey))
            {
                if (_panelRoot.activeSelf)
                    HidePanel();
                else
                    ShowPanel();
            }

            if (uiConfig.AnimatorParamsToggleKey != KeyCode.None &&
                Input.GetKeyDown(uiConfig.AnimatorParamsToggleKey))
            {
                _showAnimatorParamsWindow = !_showAnimatorParamsWindow;
                _settingsTab?.RefreshAnimatorParamsToggleState(_showAnimatorParamsWindow);
            }

            if (_uiActive && Input.GetKeyDown(KeyCode.Escape)) HidePanel();

            if (!_uiActive) return;

            if (_charInput != null && _charInputWasEnabled && _charInput.enabled) _charInput.enabled = false;

            if (_playerInput != null && _playerInputWasActive && _playerInput.inputIsActive)
                _playerInput.DeactivateInput();
        }

        private void LateUpdate()
        {
            if (!_uiActive || _panelRoot == null || !_panelRoot.activeSelf) return;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void OnGUI()
        {
            if (!_showAnimatorParamsWindow) return;

            UpdateModelHandlers();
            var currentHandler = _animatorParamsViewingPet ? _petModelHandler : _modelHandler;
            if (currentHandler == null) return;

            var customAnimatorControl = currentHandler.CustomAnimatorControl;
            if (customAnimatorControl == null) return;

            _animatorParamsWindowRect.width = AnimatorParamsWindowWidth;
            _animatorParamsWindowRect.height = AnimatorParamsWindowHeight;
            _animatorParamsWindowRect = GUI.Window(100, _animatorParamsWindowRect, DrawAnimatorParamsWindow,
                Localization.AnimatorParameters);
        }

        private void InitializeUI()
        {
            if (_isInitialized) return;

            CreateOrFindUiRoot();
            BuildMainPanel();
            SetupPanels();
            BuildSettingsButton();

            _uiActive = false;
            if (_overlay != null) _overlay.SetActive(false);
            if (_panelRoot != null) _panelRoot.SetActive(false);

            _isInitialized = true;

            var uiConfig = UIConfig;
            if (uiConfig != null)
            {
                _lastAnchorPosition = uiConfig.DCMButtonAnchor;
                _lastOffsetX = uiConfig.DCMButtonOffsetX;
                _lastOffsetY = uiConfig.DCMButtonOffsetY;
            }

            ModLogger.Log("ConfigWindow initialized.");
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

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _uiRoot = canvas;
            DontDestroyOnLoad(_uiRoot);
        }

        private void BuildMainPanel()
        {
            if (_uiRoot == null) return;

            _overlay = UIFactory.CreateImage("Overlay", _uiRoot.transform, new Color(0, 0, 0, 0.5f));
            UIFactory.SetupRectTransform(_overlay, Vector2.zero, Vector2.one, Vector2.zero);

            _panelRoot = UIFactory.CreateImage("MainPanel", _uiRoot.transform, new Color(0.1f, 0.12f, 0.15f, 0.95f));
            var panelRect = _panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new(0.1f, 0.1f);
            panelRect.anchorMax = new(0.9f, 0.9f);
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            var outline = _panelRoot.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(2, -2);

            BuildTitleBar();
        }

        private void BuildTitleBar()
        {
            if (_panelRoot == null) return;

            var titleBar =
                UIFactory.CreateImage("TitleBar", _panelRoot.transform, new Color(0.15f, 0.18f, 0.22f, 0.9f));
            UIFactory.SetupRectTransform(titleBar, new(0, 1), new(1, 1), new(0, 40),
                pivot: new Vector2(0.5f, 1), anchoredPosition: new Vector2(0, 0));

            var titleContainer = new GameObject("TitleContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            titleContainer.transform.SetParent(titleBar.transform, false);
            var titleContainerRect = titleContainer.GetComponent<RectTransform>();
            titleContainerRect.anchorMin = new(0, 0);
            titleContainerRect.anchorMax = new(1, 1);
            titleContainerRect.offsetMin = new(10, 0);
            titleContainerRect.offsetMax = new(-10, 0);

            var titleLayout = titleContainer.GetComponent<HorizontalLayoutGroup>();
            titleLayout.spacing = 8;
            titleLayout.childAlignment = TextAnchor.MiddleCenter;
            titleLayout.childControlWidth = false;
            titleLayout.childControlHeight = false;
            titleLayout.childForceExpandWidth = false;
            titleLayout.childForceExpandHeight = false;
            titleLayout.padding = new(0, 0, 0, 0);

            var titleText = UIFactory.CreateText("Title", titleContainer.transform, Localization.Title,
                20,
                Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.SetLocalizedText(titleText, () => Localization.Title);

            var titleTextRect = titleText.GetComponent<RectTransform>();
            titleTextRect.sizeDelta = new(0, 0);
            var titleSizeFitter = titleText.AddComponent<ContentSizeFitter>();
            titleSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            titleSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var versionLabel = UIFactory.CreateText("Version", titleContainer.transform, $"v{Constant.ModVersion}", 14,
                new Color(0.8f, 0.8f, 0.8f, 1));
            var versionRect = versionLabel.GetComponent<RectTransform>();
            versionRect.sizeDelta = new(0, 0);
            var versionSizeFitter = versionLabel.AddComponent<ContentSizeFitter>();
            versionSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            versionSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutRebuilder.ForceRebuildLayoutImmediate(titleContainerRect);

            var closeButton = UIFactory.CreateButton("CloseButton", titleBar.transform, HidePanel,
                new Color(0.2f, 0.2f, 0.2f, 1));
            UIFactory.SetupRectTransform(closeButton, new(1, 0.5f), new(1, 0.5f), new(36, 36),
                pivot: new Vector2(1, 0.5f), anchoredPosition: new Vector2(-10, 0));

            var closeText = UIFactory.CreateText("Text", closeButton.transform, "×", 24, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupRectTransform(closeText, Vector2.zero, Vector2.one, Vector2.zero);
        }

        private void SetupPanels()
        {
            if (_panelRoot == null) return;

            CreateTabPanels();
            SetupTabSystem();
        }

        private void CreateTabPanels()
        {
            if (_panelRoot == null) return;

            var modelTabContainer = new GameObject("ModelTabContainer", typeof(RectTransform));
            modelTabContainer.transform.SetParent(_panelRoot.transform, false);
            UIFactory.SetupRectTransform(modelTabContainer, new(0, 0), new(1, 1), Vector2.zero);
            modelTabContainer.GetComponent<RectTransform>().offsetMin = new(10, 10);
            modelTabContainer.GetComponent<RectTransform>().offsetMax = new(-10, -100);

            _modelSelectionTab = modelTabContainer.AddComponent<ModelSelectionTab>();

            var settingsTabContainer = new GameObject("SettingsTabContainer", typeof(RectTransform));
            settingsTabContainer.transform.SetParent(_panelRoot.transform, false);
            UIFactory.SetupRectTransform(settingsTabContainer, new(0, 0), new(1, 1), Vector2.zero);
            settingsTabContainer.GetComponent<RectTransform>().offsetMin = new(10, 10);
            settingsTabContainer.GetComponent<RectTransform>().offsetMax = new(-10, -100);

            _settingsTab = settingsTabContainer.AddComponent<SettingsTab>();
            _settingsTab.OnAnimatorParamsToggleChanged += SetAnimatorParamsWindowVisible;
        }

        private void SetupTabSystem()
        {
            if (_panelRoot == null || _modelSelectionTab == null || _settingsTab == null) return;

            _tabSystem = gameObject.AddComponent<TabSystem>();
            _tabSystem.Initialize(_panelRoot.transform);

            _tabSystem.AddTab(Localization.ModelSelection, _modelSelectionTab.gameObject, true, "ModelSelection");
            _tabSystem.AddTab(Localization.Settings, _settingsTab.gameObject, false, "Settings");
        }

        private static bool IsTypingInInputField()
        {
            var current = EventSystem.current;
            if (current == null || current.currentSelectedGameObject == null) return false;

            var inputField = current.currentSelectedGameObject.GetComponent<InputField>();
            return inputField != null && inputField.isFocused;
        }

        public void ShowPanel()
        {
            if (!_isInitialized || _panelRoot == null)
            {
                ModLogger.LogWarning("Cannot show panel - not initialized!");
                return;
            }

            _uiActive = true;
            if (_overlay != null) _overlay.SetActive(true);
            if (_panelRoot != null) _panelRoot.SetActive(true);

            _modelSelectionTab?.Initialize();
            _settingsTab?.Initialize();

            _tabSystem?.SwitchToTab(0);
            _modelSelectionTab?.Show();

            _cursorWasVisible = Cursor.visible;
            _originalCursorLockState = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var current = EventSystem.current;
            if (current == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(eventSystem);
            }

            _charInput = CharacterInputControl;
            if (_charInput != null)
            {
                _charInputWasEnabled = _charInput.enabled;
                if (_charInputWasEnabled)
                {
                    _charInput.enabled = false;
                    ModLogger.Log("CharacterInputControl disabled.");
                }
            }

            _playerInput = PlayerInput;
            if (_playerInput != null)
            {
                _playerInputWasActive = _playerInput.inputIsActive;
                if (_playerInputWasActive)
                {
                    _playerInput.DeactivateInput();
                    ModLogger.Log("PlayerInput deactivated (game input blocked).");
                }
            }

            StartCoroutine(ForceCursorFree());
            ModLogger.Log("Config window opened.");
        }

        public void HidePanel()
        {
            _uiActive = false;
            StopAllCoroutines();

            if (_overlay != null) _overlay.SetActive(false);
            if (_panelRoot != null) _panelRoot.SetActive(false);

            if (_charInput != null && _charInputWasEnabled)
            {
                _charInput.enabled = true;
                ModLogger.Log("CharacterInputControl re-enabled.");
            }

            _charInput = null;
            _charInputWasEnabled = false;

            if (_playerInput != null && _playerInputWasActive)
            {
                _playerInput.ActivateInput();
                ModLogger.Log("PlayerInput reactivated (game input restored).");
            }

            _playerInput = null;
            _playerInputWasActive = false;

            Cursor.visible = _cursorWasVisible;
            Cursor.lockState = _originalCursorLockState;
            ModLogger.Log("Config window closed.");
        }

        private IEnumerator ForceCursorFree()
        {
            while (_uiActive)
            {
                if (Cursor.lockState != CursorLockMode.None)
                    Cursor.lockState = CursorLockMode.None;
                if (!Cursor.visible)
                    Cursor.visible = true;
                yield return null;
            }
        }

        private void DrawAnimatorParamsWindow(int windowID)
        {
            var currentHandler = _animatorParamsViewingPet ? _petModelHandler : _modelHandler;
            if (currentHandler == null) return;

            var customAnimatorControl = currentHandler.CustomAnimatorControl;
            if (customAnimatorControl == null) return;

            InitializeParamStyles();

            GUILayout.BeginArea(new(10, 30, AnimatorParamsWindowWidth - 20, AnimatorParamsWindowHeight - 40));

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{Localization.TargetType}: ", GUILayout.Width(80));

            var characterButtonStyle = _animatorParamsViewingPet ? GUI.skin.button : GUI.skin.box;
            if (GUILayout.Button(Localization.TargetCharacter, characterButtonStyle, GUILayout.Width(100)))
                if (_animatorParamsViewingPet)
                {
                    _paramIsChanging.Clear();
                    _paramPreviousValues.Clear();
                    _animatorParamsViewingPet = false;
                }

            var petButtonStyle = _animatorParamsViewingPet ? GUI.skin.box : GUI.skin.button;
            if (GUILayout.Button(Localization.TargetPet, petButtonStyle, GUILayout.Width(100)))
                if (!_animatorParamsViewingPet && _petModelHandler != null)
                {
                    _paramIsChanging.Clear();
                    _paramPreviousValues.Clear();
                    _animatorParamsViewingPet = true;
                }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            _animatorParamsScrollPosition = GUILayout.BeginScrollView(_animatorParamsScrollPosition,
                _scrollViewStyle, GUILayout.Width(AnimatorParamsWindowWidth - 20),
                GUILayout.Height(AnimatorParamsWindowHeight - 80));

            var paramInfos = GetCustomAnimatorParams();
            for (var i = 0; i < paramInfos.Count; i += 2)
            {
                GUILayout.BeginHorizontal();

                var paramInfo1 = paramInfos[i];
                var paramName1 = $"{paramInfo1.Name} ({paramInfo1.Type})";
                var paramValue1 = GetParameterValue(customAnimatorControl, paramInfo1);
                var paramValueObj1 = GetParameterValueObject(customAnimatorControl, paramInfo1);
                var style1 = GetParamStyle(paramInfo1, paramValueObj1);

                GUILayout.Label($"{paramName1}: {paramValue1}", style1,
                    GUILayout.Width((AnimatorParamsWindowWidth - 40) / 2f));

                UpdateParamState(paramInfo1.Hash, paramValueObj1, paramInfo1.Type);

                if (i + 1 < paramInfos.Count)
                {
                    var paramInfo2 = paramInfos[i + 1];
                    var paramName2 = $"{paramInfo2.Name} ({paramInfo2.Type})";
                    var paramValue2 = GetParameterValue(customAnimatorControl, paramInfo2);
                    var paramValueObj2 = GetParameterValueObject(customAnimatorControl, paramInfo2);
                    var style2 = GetParamStyle(paramInfo2, paramValueObj2);

                    GUILayout.Label($"{paramName2}: {paramValue2}", style2,
                        GUILayout.Width((AnimatorParamsWindowWidth - 40) / 2f));

                    UpdateParamState(paramInfo2.Hash, paramValueObj2, paramInfo2.Type);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Localization.Close, GUILayout.Width(100), GUILayout.Height(20)))
                _showAnimatorParamsWindow = false;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            GUI.DragWindow(new(0, 0, AnimatorParamsWindowWidth, 20));
        }

        private void InitializeParamStyles()
        {
            _paramLabelStyle ??= new(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white },
            };

            _paramLabelStyleChanged ??= new(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = new(1f, 0.8f, 0.2f, 1f) },
            };

            _paramLabelStyleChanging ??= new(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = new(1f, 0.4f, 0.1f, 1f) },
            };

            _scrollViewStyle ??= new(GUI.skin.scrollView)
            {
                normal = { background = MakeTex(2, 2, new(0.1f, 0.1f, 0.1f, 0.8f)) },
            };
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            var pix = new Color[width * height];
            for (var i = 0; i < pix.Length; i++)
                pix[i] = col;

            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private static List<AnimatorParamInfo> GetCustomAnimatorParams()
        {
            return CustomAnimatorHash.GetAllParams();
        }

        private static string GetParameterValue(CustomAnimatorControl customAnimatorControl,
            AnimatorParamInfo paramInfo)
        {
            if (customAnimatorControl == null) return "N/A";

            try
            {
                return paramInfo.Type switch
                {
                    "float" => customAnimatorControl.GetParameterFloat(paramInfo.Hash).ToString("F3"),
                    "int" => customAnimatorControl.GetParameterInteger(paramInfo.Hash).ToString(),
                    "bool" => customAnimatorControl.GetParameterBool(paramInfo.Hash).ToString(),
                    "trigger" => "Trigger",
                    _ => "Unknown",
                };
            }
            catch
            {
                return "N/A";
            }
        }

        private static object? GetParameterValueObject(CustomAnimatorControl customAnimatorControl,
            AnimatorParamInfo paramInfo)
        {
            if (customAnimatorControl == null) return null;

            try
            {
                return paramInfo.Type switch
                {
                    "float" => customAnimatorControl.GetParameterFloat(paramInfo.Hash),
                    "int" => customAnimatorControl.GetParameterInteger(paramInfo.Hash),
                    "bool" => customAnimatorControl.GetParameterBool(paramInfo.Hash),
                    _ => null,
                };
            }
            catch
            {
                return null;
            }
        }

        private void UpdateParamState(int hash, object? currentValue, string paramType)
        {
            if (currentValue == null || paramType == "trigger") return;

            if (!_paramPreviousValues.TryGetValue(hash, out var previousValue))
            {
                _paramPreviousValues[hash] = currentValue;
                _paramIsChanging[hash] = false;
                return;
            }

            var isValueChanged = !ValuesEqual(currentValue, previousValue, paramType);
            _paramIsChanging[hash] = isValueChanged;
            _paramPreviousValues[hash] = currentValue;
        }

        private static bool ValuesEqual(object? value1, object? value2, string paramType)
        {
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;

            return paramType switch
            {
                "float" => Math.Abs((float)value1 - (float)value2) < 0.0001f,
                "int" => (int)value1 == (int)value2,
                "bool" => (bool)value1 == (bool)value2,
                _ => value1.Equals(value2),
            };
        }

        private GUIStyle GetParamStyle(AnimatorParamInfo paramInfo, object? currentValue)
        {
            if (currentValue == null || paramInfo.InitialValue == null)
                return _paramLabelStyle ?? GUI.skin.label;

            var isChanged = !ValuesEqual(currentValue, paramInfo.InitialValue, paramInfo.Type);
            var isChanging = _paramIsChanging.GetValueOrDefault(paramInfo.Hash, false);

            if (isChanging)
                return _paramLabelStyleChanging ?? _paramLabelStyle ?? GUI.skin.label;

            if (isChanged)
                return _paramLabelStyleChanged ?? _paramLabelStyle ?? GUI.skin.label;

            return _paramLabelStyle ?? GUI.skin.label;
        }

        private void UpdateModelHandlers()
        {
            if (LevelManager.Instance == null) return;

            var mainCharacterControl = LevelManager.Instance.MainCharacter;
            if (mainCharacterControl != null)
            {
                _modelHandler = mainCharacterControl.GetComponent<ModelHandler>();
                if (_modelHandler == null)
                    _modelHandler = ModelManager.InitializeModelHandler(mainCharacterControl);
            }

            var petCharacterControl = LevelManager.Instance.PetCharacter;
            if (petCharacterControl == null) return;
            _petModelHandler = petCharacterControl.GetComponent<ModelHandler>();
            if (_petModelHandler == null)
                _petModelHandler = ModelManager.InitializeModelHandler(petCharacterControl, ModelTarget.Pet);
        }

        public void SetAnimatorParamsWindowVisible(bool visible)
        {
            _showAnimatorParamsWindow = visible;
        }

        public bool IsAnimatorParamsWindowVisible()
        {
            return _showAnimatorParamsWindow;
        }

        private void BuildSettingsButton()
        {
            if (_uiRoot == null) return;

            var uiConfig = UIConfig;
            if (uiConfig == null) return;

            _settingsButton = UIFactory.CreateButton("SettingsButton", _uiRoot.transform, OnSettingsButtonClicked,
                new Color(0.2f, 0.25f, 0.3f, 0.9f));
            var buttonRect = _settingsButton.GetComponent<RectTransform>();

            var anchorMin = GetAnchorValue(uiConfig.DCMButtonAnchor);
            var anchorMax = anchorMin;
            var pivot = anchorMin;

            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.pivot = pivot;
            buttonRect.sizeDelta = new(80, 50);
            buttonRect.anchoredPosition = new(uiConfig.DCMButtonOffsetX, uiConfig.DCMButtonOffsetY);

            var outline = _settingsButton.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(1, -1);

            var buttonText = UIFactory.CreateText("Text", _settingsButton.transform, "DCM", 24, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(buttonText, 16, 24);

            UIFactory.SetupButtonColors(_settingsButton.GetComponent<Button>(),
                new(0.2f, 0.25f, 0.3f, 0.9f),
                new(0.3f, 0.35f, 0.4f, 0.9f),
                new(0.15f, 0.2f, 0.25f, 0.9f));

            SetSettingsButtonVisible(false);
        }

        private void OnSettingsButtonClicked()
        {
            if (!_isInitialized)
            {
                ModLogger.LogWarning("Cannot toggle panel - not initialized!");
                return;
            }

            if (_panelRoot == null)
            {
                ModLogger.LogWarning("Cannot toggle panel - panel root is null!");
                return;
            }

            if (_panelRoot.activeSelf)
                HidePanel();
            else
                ShowPanel();
        }

        private void SetSettingsButtonVisible(bool visible)
        {
            if (_settingsButton != null) _settingsButton.SetActive(visible);
        }

        public void RefreshSettingsButton()
        {
            if (_settingsButton == null || UIConfig == null) return;

            var buttonRect = _settingsButton.GetComponent<RectTransform>();
            if (buttonRect == null) return;

            var anchorMin = GetAnchorValue(UIConfig.DCMButtonAnchor);
            var anchorMax = anchorMin;
            var pivot = anchorMin;

            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.pivot = pivot;
            buttonRect.anchoredPosition = new(UIConfig.DCMButtonOffsetX, UIConfig.DCMButtonOffsetY);

            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRect);
        }

        private void UpdateSettingsButtonVisibility()
        {
            var uiConfig = UIConfig;
            if (uiConfig == null)
            {
                SetSettingsButtonVisible(false);
                return;
            }

            var shouldShow = uiConfig.ShowDCMButton && (IsInMainMenu() || IsInLootView());
            SetSettingsButtonVisible(shouldShow);
        }

        private static bool IsInMainMenu()
        {
            var currentScene = SceneManager.GetActiveScene();
            return currentScene.name == "MainMenu";
        }

        private static bool IsInLootView()
        {
            var activeView = View.ActiveView;
            var lootView = LootView.Instance;
            return activeView != null && lootView != null && activeView == lootView;
        }

        private static Vector2 GetAnchorValue(AnchorPosition position)
        {
            return position switch
            {
                AnchorPosition.TopLeft => new(0f, 1f),
                AnchorPosition.TopCenter => new(0.5f, 1f),
                AnchorPosition.TopRight => new(1f, 1f),
                AnchorPosition.MiddleLeft => new(0f, 0.5f),
                AnchorPosition.MiddleCenter => new(0.5f, 0.5f),
                AnchorPosition.MiddleRight => new(1f, 0.5f),
                AnchorPosition.BottomLeft => new(0f, 0f),
                AnchorPosition.BottomCenter => new(0.5f, 0f),
                AnchorPosition.BottomRight => new(1f, 0f),
                _ => new(0f, 1f),
            };
        }
    }
}
