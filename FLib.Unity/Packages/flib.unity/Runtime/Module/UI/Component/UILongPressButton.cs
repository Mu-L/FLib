// ==================== qcbf@qq.com | 2025-08-07 ====================

using System;
using Cysharp.Threading.Tasks;
using FLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FLib.Unity
{
    public class UILongPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool AllowFirstTrigger = true;
        public float FirstDelay = 0.7f;
        public float Delay = 0.1f;
        public Action<bool> OnHandler;

        private float _nextTriggerTime;

        public void OnPointerDown(PointerEventData eventData)
        {
            Trigger().Forget();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _nextTriggerTime = 0;
            OnHandler(false);
        }

        private async UniTaskVoid Trigger()
        {
            if (AllowFirstTrigger)
                OnHandler(true);
            _nextTriggerTime = Time.time + FirstDelay;
            while (_nextTriggerTime != 0)
            {
                var t = Time.time;
                if (t >= _nextTriggerTime)
                {
                    _nextTriggerTime = t + Delay;
                    OnHandler(true);
                }
                await UniTask.NextFrame();
            }
        }
    }
}
