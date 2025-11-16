using UnityEngine;

namespace DuckovCustomModel.UI.Base
{
    public abstract class UIPanel : MonoBehaviour
    {
        protected bool IsInitialized;
        protected bool IsVisible;
        protected GameObject? PanelRoot;

        protected virtual void OnDestroy()
        {
            if (PanelRoot == null) return;
            Destroy(PanelRoot);
            PanelRoot = null;
        }

        public virtual void Initialize()
        {
            if (IsInitialized) return;

            CreatePanel();
            BuildContent();
            IsInitialized = true;
        }

        public virtual void Show()
        {
            if (!IsInitialized) Initialize();

            if (PanelRoot != null)
            {
                PanelRoot.SetActive(true);
                IsVisible = true;
                OnShow();
            }
        }

        public virtual void Hide()
        {
            if (PanelRoot != null)
            {
                PanelRoot.SetActive(false);
                IsVisible = false;
                OnHide();
            }
        }

        public virtual void Refresh()
        {
            if (!IsInitialized || !IsVisible) return;
            OnRefresh();
        }

        protected abstract void CreatePanel();
        protected abstract void BuildContent();

        protected virtual void OnShow()
        {
        }

        protected virtual void OnHide()
        {
        }

        protected virtual void OnRefresh()
        {
        }
    }
}
