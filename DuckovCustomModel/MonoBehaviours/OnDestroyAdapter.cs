using System;
using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    public class OnDestroyAdapter : MonoBehaviour
    {
        private void OnDestroy()
        {
            OnDestroyEvent?.Invoke(gameObject);
        }

        public void ForceInvoke()
        {
            OnDestroyEvent?.Invoke(gameObject);
        }

        public void ClearListeners()
        {
            OnDestroyEvent = null;
        }

        public event Action<GameObject>? OnDestroyEvent;
    }
}
