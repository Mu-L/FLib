//==================={By Qcbf|qcbf@qq.com|4/6/2022 2:52:00 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FLib.Unity
{
    public class UIEnterable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public object CustomData;
        public Action<UIEnterable, bool> OnEnterEvent;


        public void OnPointerEnter(PointerEventData eventData)
        {
            OnEnterEvent?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnEnterEvent?.Invoke(this, false);
        }

    }
}
