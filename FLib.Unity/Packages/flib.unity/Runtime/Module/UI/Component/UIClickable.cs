//==================={By Qcbf|qcbf@qq.com|5/16/2022 11:59:30 PM}===================

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace FLib.Unity
{
    public class UIClickable : MonoBehaviour, IPointerClickHandler
    {
        private static float _lastClickTime;

        public AudioClip SoundAudioClip;
        public UnityEvent<UIClickable> OnClickHandle;
        public object UserData;

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            var t = Time.unscaledTime;
            if (t - _lastClickTime <= 0.1f)
                return;
            _lastClickTime = t;

            if (TriggerClickEffect())
                OnClickHandle?.Invoke(this);
        }

        protected virtual bool TriggerClickEffect()
        {
            if (SoundAudioClip != null)
                Audio.PlayShort(SoundAudioClip);
            return true;
        }

        public virtual void SetClickHandle(UnityAction<UIClickable> handle)
        {
            OnClickHandle.RemoveAllListeners();
            if (handle != null)
            {
                OnClickHandle.AddListener(handle);
            }
        }

        public virtual void SetClickHandle(Func<UIClickable, UniTask> handle)
        {
            if (handle != null) SetClickHandle(v => handle(v).Forget());
        }

        public virtual void AddClickHandle(UnityAction<UIClickable> handle)
        {
            if (handle != null) OnClickHandle.AddListener(handle);
        }

        public virtual void AddClickHandle(Func<UIClickable, UniTask> handle)
        {
            if (handle != null) AddClickHandle(v => handle(v).Forget());
        }
    }
}
