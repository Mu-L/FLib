// ==================== qcbf@qq.com | 2025-07-01 ====================

using FLib;
using UnityEngine;
using UnityEngine.Events;

namespace FLib.Unity
{
    [ExecuteAlways]
    public class OnUIScalerChangeAction : MonoBehaviour
    {
        public UnityEvent OnLandscapeEvent;
        public UnityEvent OnVerticalEvent;

        private void OnEnable()
        {
            UIRoot.OnChangeUIScaler += OnChangeUIScaler;
            OnChangeUIScaler();
        }

        private void OnDisable()
        {
            UIRoot.OnChangeUIScaler -= OnChangeUIScaler;
        }

#if UNITY_EDITOR
        private bool _isLandscape;
        private void LateUpdate()
        {
            if (Application.isPlaying) return;
            var isLandscape = Screen.width > Screen.height;
            if (isLandscape == _isLandscape) return;
            _isLandscape = isLandscape;
            OnChangeUIScaler();
        }
#endif

        private void OnChangeUIScaler()
        {
            if (Screen.width > Screen.height)
                OnLandscapeEvent?.Invoke();
            else
                OnVerticalEvent?.Invoke();
        }
    }
}
