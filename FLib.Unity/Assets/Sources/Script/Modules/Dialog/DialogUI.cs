using System;
using System.Collections.Generic;
using System.Linq;
using FLib.Unity;
using Modules;
using TMPro;
using UnityEngine;

namespace Modules.Dialog
{
    public class DialogContext : MultiUIContext
    {
        public bool IsHideCloseBtn;
        public bool CloseButtonCallBack;
        public Action<int> CloseCallback;
        public string[] ButtonTexts = Array.Empty<string>();

        public DialogContext SetButtons(params string[] btns)
        {
            ButtonTexts = btns;
            return this;
        }

        public DialogContext SetCloseCallback(Action<int> callback, bool containCloseButton = false)
        {
            CloseButtonCallBack = containCloseButton;
            CloseCallback = callback;
            return this;
        }
    }

    public abstract class DialogUI<T> : UIBase<T> where T : DialogContext
    {
        public UIButton[] Buttons;
        public RectTransform ContentRoot;

        protected virtual void Start()
        {
            if (SelfContext.IsHideCloseBtn)
                CloseButtons[0].gameObject.SetActive(false);
            SetButtons(SelfContext.ButtonTexts);
        }

        protected override void OnClickCloseButton(UIClickable arg0)
        {
            if (!SelfContext.IsHideCloseBtn)
            {
                base.OnClickCloseButton(arg0);
                if (SelfContext.CloseButtonCallBack)
                    SelfContext.CloseCallback(-1);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void SetButtons(string[] buttonTexts)
        {
            for (var i = 0; i < Buttons.Length; i++)
            {
                if (i < buttonTexts.Length)
                {
                    Buttons[i].Label.text = buttonTexts[i];
                    Buttons[i].SetClickHandle(OnClickButton);
                }
                else
                {
                    Buttons[i].gameObject.SetActive(false);
                }
            }
            if (buttonTexts.Length == 0)
            {
                var v = ContentRoot.offsetMin;
                v.y = v.x;
                ContentRoot.offsetMin = v;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnClickButton(UIClickable arg0)
        {
            Close();
            SelfContext.CloseCallback(arg0.transform.GetSiblingIndex());
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            CloseButtons ??= new[] { transform.Find("Panel/Close").GetComponent<UIButton>(), transform.Find("Bg").GetComponent<UIClickable>() };
            Buttons ??= transform.Find("Panel/ButtonRoot").GetComponentsInChildren<UIButton>();
        }
#endif
    }
}
