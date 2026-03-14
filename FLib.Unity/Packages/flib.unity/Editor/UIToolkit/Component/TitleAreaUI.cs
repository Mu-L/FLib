//==================={By Qcbf|qcbf@qq.com|9/2/2021 10:36:22 AM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class TitleAreaUI : VisualElement, INotifyValueChanged<bool>
    {
        public VisualElement MenuBarUI;
        public static Color ContentBorderColor = new(0.3f, 0.3f, 0.3f);

        protected VisualElement mContentUI;
        public Label TitleUI;
        public VisualElement TitleBarUI;
        public Toggle ToggleUI;
        public string AutoFoldoutKey;
        protected bool mValue = true;

        public string Title
        {
            get => TitleUI.text;
            set => TitleUI.text = value;
        }

        public string TitleTooltip
        {
            get => TitleUI.tooltip;
            set => TitleUI.tooltip = value;
        }

        public bool value
        {
            get => mValue;
            set
            {
                if (mValue != value)
                {
                    using var changeEvent = ChangeEvent<bool>.GetPooled(mValue, value);
                    changeEvent.target = this;
                    SetValueWithoutNotify(value);
                    SendEvent(changeEvent);
                }
            }
        }


        public override VisualElement contentContainer => mContentUI;

        public TitleAreaUI(string autoFoldoutKey = null)
        {
            if (autoFoldoutKey != null)
                mValue = EditorPrefs.GetBool(autoFoldoutKey, value);
            style.flexGrow = 1;
            MenuBarUI = new VisualElement { focusable = true }.FlexDirection(FlexDirection.Row);
            MenuBarUI.style.alignItems = Align.Center;
            MenuBarUI.Add(ToggleUI = new Toggle { value = mValue });
            ToggleUI.style.marginLeft = ToggleUI.style.marginRight = 0;
            ToggleUI.AddToClassList(Foldout.toggleUssClassName);
            ToggleUI.RegisterValueChangedCallback(e => SetFoldout(e.newValue));

            MenuBarUI.Add(TitleBarUI = new VisualElement().FlexDirection(FlexDirection.Row).FlexGrow(1));
            TitleBarUI.Add(TitleUI = new Label());
            TitleBarUI.RegisterCallback<ClickEvent>(_ => SetFoldout(!value));

            hierarchy.Add(MenuBarUI);
            hierarchy.Add(mContentUI = new VisualElement());
            mContentUI.style.borderLeftWidth = 1;
            mContentUI.style.borderLeftColor = ContentBorderColor;
            mContentUI.style.marginLeft = 7;
            SetValueWithoutNotify(value);
            AutoFoldoutKey = autoFoldoutKey;
        }


        public TitleAreaUI SetTitle(string v, string titleTooltip = null)
        {
            Title = v;
            if (titleTooltip != null)
                TitleTooltip = titleTooltip;
            return this;
        }

        public void RemoveTitleBar()
        {
            MenuBarUI.Remove(TitleBarUI);
        }

        public TitleAreaUI OffsetLeft()
        {
            style.marginLeft = new Length(-12);
            return this;
        }

        protected virtual void SetFoldout(bool v)
        {
            if (TitleUI.style.display.value == DisplayStyle.Flex && TitleUI.visible)
                value = v;
        }

        public virtual void SetValueWithoutNotify(bool newValue)
        {
            mValue = newValue;
            ToggleUI.value = newValue;
            contentContainer.style.display = !newValue ? DisplayStyle.None : DisplayStyle.Flex;
            if (!string.IsNullOrEmpty(AutoFoldoutKey))
                EditorPrefs.SetBool(AutoFoldoutKey, newValue);
        }


        public virtual T AddToMenuBar<T>(T v) where T : VisualElement
        {
            MenuBarUI.Add(v);
            return v;
        }
    }
}
