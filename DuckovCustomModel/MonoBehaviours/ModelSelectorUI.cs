using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Data;
using DuckovCustomModel.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DuckovCustomModel.MonoBehaviours
{
    public class ModelSelectorUI : MonoBehaviour
    {
        private readonly List<ModelBundleInfo> _filteredModelBundles = new();
        private MonoBehaviour? _cameraController;
        private bool _cameraLockDisabled;

        private CharacterInputControl? _charInput;
        private bool _isInitialized;
        private ModelHandler? _modelHandler;
        private GameObject? _modelListContent;
        private ScrollRect? _modelScrollRect;
        private GameObject? _overlay;
        private GameObject? _panelRoot;
        private PlayerInput? _playerInput;

        private InputField? _searchField;
        private string _searchText = string.Empty;
        private bool _uiActive;

        private UIConfig? _uiConfig;
        private GameObject? _uiRoot;
        private UsingModel? _usingModel;

        private void Start()
        {
            _uiConfig = ModBehaviour.Instance?.UIConfig;
            _usingModel = ModBehaviour.Instance?.UsingModel;
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

            if (Input.GetKeyDown(_uiConfig.ToggleKey))
            {
                if (_panelRoot.activeSelf)
                    HidePanel();
                else
                    ShowPanel();
            }

            if (_uiActive && Input.GetKeyDown(KeyCode.Escape)) HidePanel();

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
            panelRect.sizeDelta = new(800, 600);
            panelRect.anchoredPosition = Vector2.zero;

            var outline = _panelRoot.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(2, -2);

            BuildTitle();
            BuildSearchField();
            BuildModelList();
            BuildCloseButton();
        }

        private void BuildTitle()
        {
            if (_panelRoot == null) return;

            var title = new GameObject("Title", typeof(Text));
            title.transform.SetParent(_panelRoot.transform, false);
            var titleText = title.GetComponent<Text>();
            titleText.text = "模型选择";
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

        private void BuildSearchField()
        {
            if (_panelRoot == null) return;

            _searchField = BuildInput("搜索模型...");
            _searchField.transform.SetParent(_panelRoot.transform, false);
            var searchRect = _searchField.GetComponent<RectTransform>();
            searchRect.anchorMin = new(0, 1);
            searchRect.anchorMax = new(1, 1);
            searchRect.pivot = new(0.5f, 1);
            searchRect.anchoredPosition = new(0, -70);
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
            scrollRect.offsetMin = new(20, 60);
            scrollRect.offsetMax = new(-20, -120);

            var scrollImage = scrollView.GetComponent<Image>();
            scrollImage.color = new(0.05f, 0.08f, 0.12f, 0.8f);

            _modelScrollRect = scrollView.GetComponent<ScrollRect>();
            _modelScrollRect.horizontal = false;
            _modelScrollRect.vertical = true;
            _modelScrollRect.scrollSensitivity = 20;

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
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            var sizeFitter = _modelListContent.GetComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _modelScrollRect.content = contentRect;
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

        private void RefreshModelList()
        {
            if (_modelListContent == null) return;

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
                _filteredModelBundles.Add(bundle);

            foreach (var bundle in _filteredModelBundles)
            foreach (var model in bundle.Models)
                BuildModelButton(bundle, model);
        }

        private void BuildModelButton(ModelBundleInfo bundle, ModelInfo model)
        {
            if (_modelListContent == null) return;

            var buttonObj = new GameObject($"ModelButton_{model.ModelID}", typeof(Image), typeof(Button),
                typeof(LayoutElement));
            buttonObj.transform.SetParent(_modelListContent.transform, false);

            var buttonImage = buttonObj.GetComponent<Image>();
            buttonImage.color = new(0.15f, 0.18f, 0.22f, 0.8f);

            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new(0, 100);

            var layoutElement = buttonObj.GetComponent<LayoutElement>();
            layoutElement.minHeight = 100;
            layoutElement.preferredHeight = 100;

            var outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.6f);
            outline.effectDistance = new(1, -1);

            var thumbnailImage = new GameObject("Thumbnail", typeof(Image));
            thumbnailImage.transform.SetParent(buttonObj.transform, false);
            var thumbnailImageComponent = thumbnailImage.GetComponent<Image>();
            thumbnailImageComponent.color = new(0.2f, 0.2f, 0.2f, 1);
            var thumbnailRect = thumbnailImage.GetComponent<RectTransform>();
            thumbnailRect.anchorMin = new(0, 0);
            thumbnailRect.anchorMax = new(0, 1);
            thumbnailRect.pivot = new(0, 0.5f);
            thumbnailRect.anchoredPosition = new(10, 0);
            thumbnailRect.sizeDelta = new(80, 80);

            var texture = AssetBundleManager.LoadThumbnailTexture(bundle, model);
            if (texture != null)
            {
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                thumbnailImageComponent.sprite = sprite;
                thumbnailImageComponent.preserveAspect = true;
            }

            var contentArea = new GameObject("ContentArea", typeof(RectTransform));
            contentArea.transform.SetParent(buttonObj.transform, false);
            var contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.anchorMin = new(0, 0);
            contentRect.anchorMax = new(1, 1);
            contentRect.offsetMin = new(100, 0);
            contentRect.offsetMax = new(-10, 0);

            var nameText = new GameObject("Name", typeof(Text));
            nameText.transform.SetParent(contentArea.transform, false);
            var nameTextComponent = nameText.GetComponent<Text>();
            nameTextComponent.text = string.IsNullOrEmpty(model.Name) ? model.ModelID : model.Name;
            nameTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameTextComponent.fontSize = 16;
            nameTextComponent.fontStyle = FontStyle.Bold;
            nameTextComponent.color = Color.white;
            nameTextComponent.alignment = TextAnchor.UpperLeft;
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new(0, 0.5f);
            nameRect.anchorMax = new(1, 1);
            nameRect.pivot = new(0, 1);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            var infoText = new GameObject("Info", typeof(Text));
            infoText.transform.SetParent(contentArea.transform, false);
            var infoTextComponent = infoText.GetComponent<Text>();
            var infoTextStr = $"ID: {model.ModelID}";
            if (!string.IsNullOrEmpty(model.Author))
                infoTextStr += $" | 作者: {model.Author}";
            if (!string.IsNullOrEmpty(model.Version))
                infoTextStr += $" | 版本: {model.Version}";
            infoTextComponent.text = infoTextStr;
            infoTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            infoTextComponent.fontSize = 12;
            infoTextComponent.color = new(0.8f, 0.8f, 0.8f, 1);
            infoTextComponent.alignment = TextAnchor.UpperLeft;
            var infoRect = infoText.GetComponent<RectTransform>();
            infoRect.anchorMin = new(0, 0.25f);
            infoRect.anchorMax = new(1, 0.5f);
            infoRect.pivot = new(0, 0.5f);
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;

            if (!string.IsNullOrEmpty(model.Description))
            {
                var descText = new GameObject("Description", typeof(Text));
                descText.transform.SetParent(contentArea.transform, false);
                var descTextComponent = descText.GetComponent<Text>();
                descTextComponent.text = model.Description;
                descTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                descTextComponent.fontSize = 11;
                descTextComponent.color = new(0.7f, 0.7f, 0.7f, 1);
                descTextComponent.alignment = TextAnchor.UpperLeft;
                descTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                descTextComponent.verticalOverflow = VerticalWrapMode.Truncate;
                var descRect = descText.GetComponent<RectTransform>();
                descRect.anchorMin = new(0, 0);
                descRect.anchorMax = new(1, 0.25f);
                descRect.pivot = new(0, 0.5f);
                descRect.offsetMin = Vector2.zero;
                descRect.offsetMax = Vector2.zero;
            }

            var button = buttonObj.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new(1, 1, 1, 1);
            colors.highlightedColor = new(0.5f, 0.7f, 0.9f, 1);
            colors.pressedColor = new(0.4f, 0.6f, 0.8f, 1);
            colors.selectedColor = new(0.5f, 0.7f, 0.9f, 1);
            button.colors = colors;

            button.onClick.AddListener(() => OnModelSelected(bundle, model));
        }

        private void OnModelSelected(ModelBundleInfo bundle, ModelInfo model)
        {
            if (_usingModel == null || _modelHandler == null)
            {
                ModLogger.LogError("UsingModel or ModelHandler is null.");
                return;
            }

            _usingModel.ModelID = model.ModelID;
            _usingModel.SaveToFile("UsingModel.json");

            _modelHandler.InitializeCustomModel(bundle, model);
            _modelHandler.ChangeToCustomModel();

            ModLogger.Log($"Selected model: {model.Name} ({model.ModelID})");
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

            RefreshModelList();
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
            if (CharacterMainControl.Main == null) return;

            _modelHandler = CharacterMainControl.Main.GetComponent<ModelHandler>();
            if (_modelHandler == null) _modelHandler = ModelManager.InitializeModelHandler(CharacterMainControl.Main);
        }
    }
}
