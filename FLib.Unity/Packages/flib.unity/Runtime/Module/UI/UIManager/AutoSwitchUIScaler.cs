// ==================== qcbf@qq.com | 2025-07-01 ====================

using FLib;
using UnityEngine;

namespace FLib.Unity
{
    public class AutoSwitchUIScaler : MonoBehaviour
    {
        public float UpdateInterval = 1f;

        private float _nextTriggerTime;
        private bool _isLandscape;
        public static bool IsLandscape => Screen.width > Screen.height;

        private void LateUpdate()
        {
            var t = Time.unscaledTime;
            if (t < _nextTriggerTime)
                return;
            _nextTriggerTime = t + UpdateInterval;
            if (IsLandscape != _isLandscape)
            {
                _isLandscape = IsLandscape;
                UIRoot.SetPrimaryScaler(_isLandscape == UIRoot.PrimaryScaler.IsLandscape);
            }
        }
    }
}
