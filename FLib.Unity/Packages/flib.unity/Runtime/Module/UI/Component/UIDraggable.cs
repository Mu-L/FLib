//==================={By Qcbf|qcbf@qq.com|4/6/2022 2:52:00 PM}===================

using FLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FLib.Unity
{
    public class UIDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<EState, PointerEventData, UIDraggable> OnDragEvent;

        public UIClickable SameClickEvent;
        public object UserData;
        private bool _isDisableClickEvent;

        public EState State
        {
            get;
            private set;
        } = EState.Stop;

        public enum EState : byte
        {
            Start,
            Moving,
            Stop,
        }

        public static Vector2 GetResolutionDelta(PointerEventData eventData)
        {
            return eventData.delta * new Vector2(UIRoot.Inst.Scaler.referenceResolution.x / Screen.width, UIRoot.Inst.Scaler.referenceResolution.y / Screen.height);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            State = EState.Moving;
            OnDragEvent?.Invoke(EState.Moving, eventData, this);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            State = EState.Start;
            OnDragEvent?.Invoke(EState.Start, eventData, this);
            if (SameClickEvent == null || !SameClickEvent.enabled) return;
            SameClickEvent.enabled = false;
            _isDisableClickEvent = true;
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            State = EState.Stop;
            OnDragEvent?.Invoke(EState.Stop, eventData, this);
            if (_isDisableClickEvent) SameClickEvent.enabled = true;
        }
    }
}
