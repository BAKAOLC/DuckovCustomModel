using System;
using System.Diagnostics;
using System.IO;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class FunctionButtonBar : MonoBehaviour
    {
        private bool _isRefreshing;
        private Button? _openModelFolderButton;
        private Button? _refreshButton;
        private Button? _resetInvalidModelsButton;

        private void OnDestroy()
        {
            ModelListManager.OnRefreshStarted -= OnModelListRefreshStarted;
            ModelListManager.OnRefreshCompleted -= OnModelListRefreshCompleted;
        }

        public event Action? OnRefresh;
        public event Action? OnResetInvalidModels;

        public void Initialize(Transform parent)
        {
            var buttonBar = UIFactory.CreateImage("FunctionButtonBar", parent, new(0.15f, 0.18f, 0.22f, 0.9f));
            UIFactory.SetupRectTransform(buttonBar, Vector2.zero, Vector2.one, Vector2.zero);

            var layoutElement = buttonBar.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1;
            layoutElement.flexibleHeight = 1;
            layoutElement.preferredHeight = 80;

            buttonBar.AddComponent<HorizontalLayoutGroup>();
            UIFactory.SetupHorizontalLayoutGroup(buttonBar, 10f, new(10, 10, 10, 10));

            BuildRefreshButton(buttonBar);
            BuildResetInvalidModelsButton(buttonBar);
            BuildOpenModelFolderButton(buttonBar);

            ModelListManager.OnRefreshStarted += OnModelListRefreshStarted;
            ModelListManager.OnRefreshCompleted += OnModelListRefreshCompleted;
        }

        private void BuildRefreshButton(GameObject parent)
        {
            _refreshButton = UIFactory.CreateButton("RefreshButton", parent.transform, OnRefreshButtonClicked,
                new(0.2f, 0.3f, 0.4f, 1)).GetComponent<Button>();
            var refreshButtonRect = _refreshButton.GetComponent<RectTransform>();
            refreshButtonRect.sizeDelta = new(180, 0);

            var refreshButtonLayoutElement = _refreshButton.gameObject.AddComponent<LayoutElement>();
            refreshButtonLayoutElement.preferredWidth = 180;
            refreshButtonLayoutElement.flexibleWidth = 0;
            refreshButtonLayoutElement.flexibleHeight = 1;

            var refreshTextObj = UIFactory.CreateLocalizedText("Text", _refreshButton.transform,
                () => _isRefreshing ? Localization.Loading : Localization.Refresh, 18,
                Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(refreshTextObj);
            UIFactory.SetupButtonColors(_refreshButton, new(1, 1, 1, 1), new(0.4f, 0.5f, 0.6f, 1),
                new(0.3f, 0.4f, 0.5f, 1), new(0.4f, 0.5f, 0.6f, 1));
        }

        private void BuildResetInvalidModelsButton(GameObject parent)
        {
            _resetInvalidModelsButton = UIFactory.CreateButton("ResetInvalidModelsButton", parent.transform,
                OnResetInvalidModelsButtonClicked, new(0.4f, 0.2f, 0.2f, 1)).GetComponent<Button>();
            var resetButtonRect = _resetInvalidModelsButton.GetComponent<RectTransform>();
            resetButtonRect.sizeDelta = new(180, 0);

            var resetButtonLayoutElement = _resetInvalidModelsButton.gameObject.AddComponent<LayoutElement>();
            resetButtonLayoutElement.preferredWidth = 180;
            resetButtonLayoutElement.flexibleWidth = 0;
            resetButtonLayoutElement.flexibleHeight = 1;

            var resetTextObj = UIFactory.CreateText("Text", _resetInvalidModelsButton.transform,
                Localization.ResetInvalidModels, 18, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(resetTextObj);
            UIFactory.SetLocalizedText(resetTextObj, () => Localization.ResetInvalidModels);
            UIFactory.SetupButtonColors(_resetInvalidModelsButton, new(1, 1, 1, 1), new(0.6f, 0.4f, 0.4f, 1),
                new(0.5f, 0.3f, 0.3f, 1), new(0.6f, 0.4f, 0.4f, 1));
        }

        private void OnRefreshButtonClicked()
        {
            if (_isRefreshing) return;
            OnRefresh?.Invoke();
        }

        private void BuildOpenModelFolderButton(GameObject parent)
        {
            _openModelFolderButton = UIFactory.CreateButton("OpenModelFolderButton", parent.transform,
                OnOpenModelFolderButtonClicked, new(0.3f, 0.5f, 0.3f, 1)).GetComponent<Button>();
            var openFolderButtonRect = _openModelFolderButton.GetComponent<RectTransform>();
            openFolderButtonRect.sizeDelta = new(180, 0);

            var openFolderButtonLayoutElement = _openModelFolderButton.gameObject.AddComponent<LayoutElement>();
            openFolderButtonLayoutElement.preferredWidth = 180;
            openFolderButtonLayoutElement.flexibleWidth = 0;
            openFolderButtonLayoutElement.flexibleHeight = 1;

            var openFolderTextObj = UIFactory.CreateText("Text", _openModelFolderButton.transform,
                Localization.OpenModelFolder, 18, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(openFolderTextObj);
            UIFactory.SetLocalizedText(openFolderTextObj, () => Localization.OpenModelFolder);
            UIFactory.SetupButtonColors(_openModelFolderButton, new(1, 1, 1, 1), new(0.4f, 0.6f, 0.4f, 1),
                new(0.3f, 0.5f, 0.3f, 1), new(0.4f, 0.6f, 0.4f, 1));
        }

        private void OnResetInvalidModelsButtonClicked()
        {
            OnResetInvalidModels?.Invoke();
        }

        private static void OnOpenModelFolderButtonClicked()
        {
            try
            {
                var modelsDirectory = ModelManager.ModelsDirectory;
                if (!Directory.Exists(modelsDirectory))
                    Directory.CreateDirectory(modelsDirectory);

                Process.Start(new ProcessStartInfo
                {
                    FileName = modelsDirectory,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to open model folder: {ex.Message}");
            }
        }

        private void OnModelListRefreshStarted()
        {
            _isRefreshing = true;
            UpdateRefreshButtonState(true);
        }

        private void OnModelListRefreshCompleted()
        {
            _isRefreshing = false;
            UpdateRefreshButtonState(false);
        }

        private void UpdateRefreshButtonState(bool isLoading)
        {
            if (_refreshButton == null) return;
            _refreshButton.interactable = !isLoading;
            var textObj = _refreshButton.transform.Find("Text");
            if (textObj == null) return;
            var localizedText = textObj.GetComponent<LocalizedText>();
            if (localizedText != null)
                localizedText.RefreshText();
        }
    }
}
