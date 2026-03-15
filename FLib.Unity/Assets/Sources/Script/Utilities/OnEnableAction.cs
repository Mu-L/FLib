// ==================== qcbf@qq.com | 2025-07-01 ====================

using UnityEngine;
using UnityEngine.Events;

namespace Utilities
{
    public class OnEnableAction : MonoBehaviour
    {
        public UnityEvent OnActivated;
        public UnityEvent OnDeactivated;

        private void OnEnable()
        {
            OnActivated?.Invoke();
        }

        private void OnDisable()
        {
            OnDeactivated?.Invoke();
        }
    }
}
