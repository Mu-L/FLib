//==================={By Qcbf|qcbf@qq.com|12/28/2021 12:30:35 AM}===================

using Cysharp.Threading.Tasks;
using FLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FLib.Unity
{
    public class UIPressable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public AudioClip SoundAudioClip;
        public Action<UIPressable, bool> OnPressHandler;
        public object UserData;

        public void OnPointerUp(PointerEventData eventData)
        {
            OnPressHandler?.Invoke(this, false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (SoundAudioClip != null)
                Audio.PlayShort(SoundAudioClip);
            OnPressHandler?.Invoke(this, true);
        }
    }
}
