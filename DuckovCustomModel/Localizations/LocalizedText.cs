using System;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.Localizations
{
    public class LocalizedText : MonoBehaviour
    {
        private bool _isRegistered;
        private Text? _text;
        private Func<string>? _textGetter;

        private void Awake()
        {
            _text = GetComponent<Text>();
            if (_text == null)
                ModLogger.LogWarning($"LocalizedText component on {gameObject.name} requires a Text component.");
        }

        private void Start()
        {
            if (_text != null && _textGetter != null)
            {
                Register();
                RefreshText();
            }
        }

        private void OnDestroy()
        {
            Unregister();
        }

        public void SetTextGetter(Func<string> textGetter)
        {
            _textGetter = textGetter;
            if (_text == null || _isRegistered) return;
            Register();
            RefreshText();
        }

        public void RefreshText()
        {
            if (_text == null || _textGetter == null) return;

            try
            {
                _text.text = _textGetter();
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to refresh localized text on {gameObject.name}: {ex.Message}");
            }
        }

        private void Register()
        {
            if (_isRegistered) return;

            LocalizedTextManager.Register(this);
            _isRegistered = true;
        }

        private void Unregister()
        {
            if (!_isRegistered) return;

            LocalizedTextManager.Unregister(this);
            _isRegistered = false;
        }
    }
}
