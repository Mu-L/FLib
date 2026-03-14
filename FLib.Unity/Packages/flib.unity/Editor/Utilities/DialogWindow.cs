//==================={By Qcbf|qcbf@qq.com|10/8/2021 10:33:46 AM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class DialogWindow : EditorWindow
    {
        public OptionData Options;
        public int SelectBtnIndex;

        [Serializable]
        public struct OptionData
        {
            public string Title;
            public string[] Btns;
            public string Text;
            public Func<DialogWindow, bool> BtnClickHook;
            public VisualElement CustomUI;
        }

        public enum EOpenType : byte
        {
            None,
            Normal,
            Modal,
            ModalUtility,
        }

        public static DialogWindow Open(OptionData options, EOpenType open = EOpenType.None, Vector2 size = default)
        {
            var window = GetWindow<DialogWindow>(true, options.Title ?? "Tips Box");
            if (size.sqrMagnitude > 0)
            {
                var pos = window.position;
                pos.size = size;
                window.minSize = Vector2.Min(size, window.minSize);
                window.position = pos;
            }

            window.Options = options;
            if (!(window.Options.Btns?.Length > 0))
                window.Options.Btns = new[] { "Sure" };
            window.Display();
            if (open == EOpenType.Normal)
                window.Show();
            else if (open == EOpenType.Modal)
                window.ShowModal();
            else if (open == EOpenType.ModalUtility)
                window.ShowModalUtility();
            return window;
        }

        public static TextField CreateTextArea(string defaultText = null)
        {
            var ui = new TextField { multiline = true, value = defaultText };
            ui.AddToClassList("unity-base-text-field__input");
            return ui;
        }

        public void Display()
        {
            if (!string.IsNullOrEmpty(Options.Text))
            {
                rootVisualElement.Add(new Label(Options.Text));
            }

            if (Options.CustomUI != null)
            {
                rootVisualElement.Add(Options.CustomUI);
            }

            var btnArea = new VisualElement().FlexDirection(FlexDirection.Row).FlexGrow(1);
            btnArea.style.alignItems = Align.FlexEnd;
            rootVisualElement.Add(btnArea);
            for (var i = 0; i < Options.Btns.Length; i++)
            {
                var btn = new Button { text = Options.Btns[i], userData = i }.FlexGrow(1);
                btn.RegisterCallback<ClickEvent>(e =>
                {
                    SelectBtnIndex = (int)((VisualElement)e.currentTarget).userData;
                    if (Options.BtnClickHook?.Invoke(this) != true)
                        Close();
                });
                btnArea.Add(btn);
            }
        }

        public T GetCustomUI<T>(int conditionalBtnIndex) where T : VisualElement
        {
            if (SelectBtnIndex == conditionalBtnIndex)
                return Options.CustomUI as T;
            return null;
        }

        public T GetCustomUI<T>() where T : VisualElement => Options.CustomUI as T;
    }
}