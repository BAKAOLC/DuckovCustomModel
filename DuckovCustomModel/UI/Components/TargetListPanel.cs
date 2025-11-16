using System;
using System.Collections.Generic;
using System.Linq;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Data;
using SodaCraft.Localizations;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class TargetListPanel : MonoBehaviour
    {
        private readonly Dictionary<string, GameObject> _targetButtons = new();
        private GameObject? _content;
        private ScrollRect? _scrollRect;
        private TargetInfo? _selectedTarget;

        public event Action<TargetInfo>? OnTargetSelected;

        public void Initialize(Transform parent)
        {
            var scrollView = UIFactory.CreateScrollView("TargetListScrollView", parent, out var content);
            UIFactory.SetupRectTransform(scrollView.gameObject, Vector2.zero, Vector2.one, Vector2.zero);

            var scrollViewImage = scrollView.GetComponent<Image>();
            if (scrollViewImage != null)
                scrollViewImage.color = new(0.05f, 0.08f, 0.12f, 0.8f);

            _scrollRect = scrollView;

            _content = content;
            _content.AddComponent<VerticalLayoutGroup>();
            _content.AddComponent<ContentSizeFitter>();

            UIFactory.SetupVerticalLayoutGroup(_content, 10f, new(10, 10, 10, 10), TextAnchor.UpperLeft,
                false);

            var sizeFitter = _content.GetComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        public void Refresh()
        {
            if (_content == null) return;

            var targets = GetAllTargets();
            var usingModel = ModEntry.UsingModel;

            foreach (var target in targets)
            {
                var hasModel = false;
                if (usingModel != null)
                {
                    if (target.TargetType == ModelTarget.AICharacter && target.AICharacterNameKey != null)
                    {
                        if (target.AICharacterNameKey == AICharacters.AllAICharactersKey)
                            hasModel = !string.IsNullOrEmpty(
                                usingModel.GetAICharacterModelID(AICharacters.AllAICharactersKey));
                        else
                            hasModel = !string.IsNullOrEmpty(
                                usingModel.GetAICharacterModelID(target.AICharacterNameKey));
                    }
                    else
                    {
                        hasModel = !string.IsNullOrEmpty(usingModel.GetModelID(target.TargetType));
                    }
                }

                target.HasModel = hasModel;
                target.IsSelected = _selectedTarget != null && _selectedTarget.Id == target.Id;
            }

            var existingButtonIds = new HashSet<string>(_targetButtons.Keys);
            var targetIds = new HashSet<string>(targets.Select(t => t.Id));

            foreach (var target in targets)
                if (_targetButtons.TryGetValue(target.Id, out var existingButton))
                    UpdateTargetButton(existingButton, target);
                else
                    BuildTargetButton(target);

            foreach (var buttonId in existingButtonIds.Except(targetIds))
                if (_targetButtons.TryGetValue(buttonId, out var button))
                {
                    Destroy(button);
                    _targetButtons.Remove(buttonId);
                }

            if (_selectedTarget != null || targets.Count <= 0) return;
            _selectedTarget = targets[0];
            _selectedTarget.IsSelected = true;
            if (_targetButtons.TryGetValue(_selectedTarget.Id, out var firstButton))
                UpdateTargetButton(firstButton, _selectedTarget);
            OnTargetSelected?.Invoke(_selectedTarget);
        }

        private static List<TargetInfo> GetAllTargets()
        {
            var targets = new List<TargetInfo>
            {
                TargetInfo.CreateCharacterTarget(),
                TargetInfo.CreatePetTarget(),
                TargetInfo.CreateAllAICharactersTarget(),
            };

            targets.AddRange(from nameKey in AICharacters.SupportedAICharacters
                let displayName = LocalizationManager.GetPlainText(nameKey)
                select TargetInfo.CreateAICharacterTarget(nameKey, displayName));

            return targets;
        }

        private void BuildTargetButton(TargetInfo targetInfo)
        {
            if (_content == null) return;

            var buttonObj = UIFactory.CreateButton($"TargetButton_{targetInfo.Id}", _content.transform,
                () => OnTargetButtonClicked(targetInfo)).gameObject;
            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new(180, 50);

            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 50;
            layoutElement.preferredHeight = 50;
            layoutElement.preferredWidth = 180;
            layoutElement.flexibleWidth = 0;

            var text = UIFactory.CreateText("Text", buttonObj.transform, targetInfo.DisplayName, 18, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(text);

            SetupTargetButton(buttonObj, targetInfo);

            _targetButtons[targetInfo.Id] = buttonObj;
        }

        private static void UpdateTargetButton(GameObject buttonObj, TargetInfo targetInfo)
        {
            SetupTargetButton(buttonObj, targetInfo);
        }

        private static void SetupTargetButton(GameObject buttonObj, TargetInfo targetInfo)
        {
            var buttonImage = buttonObj.GetComponent<Image>();
            Color highlightedColor = targetInfo.HasModel ? new(0.5f, 0.8f, 0.6f, 1) : new(0.5f, 0.7f, 0.9f, 1);
            Color pressedColor = targetInfo.HasModel ? new(0.4f, 0.7f, 0.5f, 1) : new(0.4f, 0.6f, 0.8f, 1);
            buttonImage.color = highlightedColor;

            var outline = buttonObj.GetComponent<Outline>();
            if (outline == null)
                outline = buttonObj.AddComponent<Outline>();

            if (targetInfo.IsSelected)
                outline.effectColor = new(1f, 0.5f, 0f, 1f);
            else if (targetInfo.HasModel)
                outline.effectColor = new(0.3f, 0.6f, 0.4f, 0.8f);
            else
                outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.6f);
            outline.effectDistance = new(10, 0);

            var button = buttonObj.GetComponent<Button>();
            UIFactory.SetupButtonColors(button, highlightedColor, highlightedColor, pressedColor, highlightedColor);

            var textObj = buttonObj.transform.Find("Text");
            if (textObj == null) return;
            var text = textObj.GetComponent<Text>();
            if (text != null)
                text.text = targetInfo.DisplayName;
        }

        private void OnTargetButtonClicked(TargetInfo targetInfo)
        {
            _selectedTarget = targetInfo;
            Refresh();
            OnTargetSelected?.Invoke(targetInfo);
        }
    }
}
