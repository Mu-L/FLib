//==================={By Qcbf|qcbf@qq.com|12/28/2021 10:49:21 PM}===================

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FLib.Unity
{
    public class UIButton : UIClickable, IPointerDownHandler, IPointerUpHandler
    {
        public StateStyle[] StateStyles = { default, new() { Scale = 0.95f }, new() { GraphicColor = Color.gray, TextColor = Color.gray, Scale = 1 } };

        public Image Graphic;
        public TextMeshProUGUI Label;
        public Action<UIButton, bool> OnPressHandler;

        public EState State { get; private set; }
        public bool IsDisabled => (State & EState.Disabled) != 0;
        public bool IsPressed => (State & EState.Pressed) != 0;

        [Flags]
        public enum EState
        {
            Pressed = 1,
            Disabled = 2,
            // DisabledBlockEvent = 4,
        }

        [Serializable]
        public struct StateStyle
        {
            public Color GraphicColor;
            public Color TextColor;
            public float Scale;
            public Sprite Image;

            public void Apply(UIButton btn)
            {
                if (btn == null)
                    return;
                btn.StopAllCoroutines();
                const float duration = 0.15f;
                if (GraphicColor != default)
                    btn.StartCoroutine(UnityFLibUtility.Tween(btn.Graphic, btn.Graphic.color, GraphicColor, duration, static (o, f, arg3, arg4) => ((Image)o).color = Color.Lerp(arg3, arg4, f)));
                if (btn.Label != null && TextColor != default)
                    btn.StartCoroutine(UnityFLibUtility.Tween(btn.Label, btn.Label.color, TextColor, duration, static (o, f, arg3, arg4) => ((TextMeshProUGUI)o).color = Color.Lerp(arg3, arg4, f)));
                if (Scale != 0)
                    btn.StartCoroutine(UnityFLibUtility.Tween(btn.transform, btn.transform.localScale, new Vector3(Scale, Scale, Scale), duration, static (o, f, arg3, arg4) => ((Transform)o).localScale = Vector3.Lerp(arg3, arg4, f)));
            }
        }


        private void Awake()
        {
            if (StateStyles[0].Scale != 0 || StateStyles[0].GraphicColor != default || StateStyles[0].TextColor != default) return;
            StateStyles[0].GraphicColor = Graphic.color;
            if (Label != null)
                StateStyles[0].TextColor = Label.color;
            StateStyles[0].Scale = transform.localScale.x;
        }

        // public override void OnPointerClick(PointerEventData eventData)
        // {
        //     base.OnPointerClick(eventData);
        // }

        public void OnPointerDown(PointerEventData eventData)
        {
            SetState(EState.Pressed, true);
            OnPressHandler?.Invoke(this, true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetState(EState.Pressed, false);
            OnPressHandler?.Invoke(this, false);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetState(EState state, bool value)
        {
            if (value)
                State |= state;
            else
                State &= ~state;
            if (IsDisabled)
                StateStyles[2].Apply(this);
            else
                StateStyles[0].Apply(this);
            if (IsPressed)
                StateStyles[1].Apply(this);
        }
    }
}
