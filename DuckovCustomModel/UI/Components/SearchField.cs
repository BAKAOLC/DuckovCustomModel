using System;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class SearchField : MonoBehaviour
    {
        private InputField? _inputField;
        private string _searchText = string.Empty;

        public event Action<string>? OnSearchChanged;

        public void Initialize(Transform parent)
        {
            _inputField = UIFactory.CreateInputField("SearchField", parent, Localization.SearchPlaceholder);
            UIFactory.SetupRectTransform(_inputField.gameObject, Vector2.zero, Vector2.one, Vector2.zero);
            _inputField.onValueChanged.AddListener(OnSearchValueChanged);

            if (_inputField != null && _inputField.placeholder != null)
            {
                var placeholderText = _inputField.placeholder.GetComponent<Text>();
                if (placeholderText != null)
                {
                    placeholderText.fontSize += 4;
                    UIFactory.SetLocalizedText(_inputField.placeholder.gameObject,
                        () => Localization.SearchPlaceholder);
                }
            }
        }

        private void OnSearchValueChanged(string text)
        {
            _searchText = text;
            OnSearchChanged?.Invoke(_searchText);
        }
    }
}
