using System;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.Localizations
{
    public class LocalizedDropdown : MonoBehaviour
    {
        private Dropdown? _dropdown;
        private bool _isRegistered;
        private Action? _refreshAction;

        private void Start()
        {
            if (_dropdown != null && _refreshAction != null) Register();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        public void SetDropdown(Dropdown dropdown)
        {
            _dropdown = dropdown;
            if (_dropdown != null && _refreshAction != null && !_isRegistered) Register();
        }

        public void SetRefreshAction(Action refreshAction)
        {
            _refreshAction = refreshAction;
            if (_dropdown != null && _refreshAction != null && !_isRegistered) Register();
        }

        private void Register()
        {
            if (_isRegistered) return;

            LocalizedTextManager.RegisterDropdown(this);
            _isRegistered = true;
        }

        private void Unregister()
        {
            if (!_isRegistered) return;

            LocalizedTextManager.UnregisterDropdown(this);
            _isRegistered = false;
        }

        public void Refresh()
        {
            _refreshAction?.Invoke();
        }
    }
}
