//==================={By Qcbf|qcbf@qq.com|12/23/2021 2:49:15 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class TabAreaUI : VisualElement
    {
        public Action<string, VisualElement> OnChangeTab;

        public VisualElement MenuBar;
        public VisualElement Content;

        public override VisualElement contentContainer => Content;

        public TabAreaUI()
        {
            hierarchy.Add(MenuBar = new VisualElement());
            hierarchy.Add(Content = new VisualElement());
            MenuBar.style.flexDirection = FlexDirection.Row;
            Content.style.flexGrow = style.flexGrow = 1;
        }

        public VisualElement AddTab(string name, VisualElement content)
        {
            var btn = new ToolbarButton() { text = name, userData = content };
            btn.style.flexGrow = 1;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.RegisterCallback<ClickEvent>(e => ActivateTab(((TextElement)e.target).text));
            if (MenuBar.childCount == 0)
            {
                btn.SetEnabled(false);
                Add(content);
            }
            MenuBar.Add(btn);
            return btn;
        }

        /// <summary>
        /// 
        /// </summary>
        public void ActivateTab(string name)
        {
            Clear();
            foreach (TextElement item in MenuBar.Children())
            {
                if (item.text == name)
                {
                    if (!item.enabledSelf) return;
                    item.SetEnabled(false);
                    var content = (VisualElement)item.userData;
                    Add(content);
                    OnChangeTab?.Invoke(name, content);
                }
                else
                {
                    item.SetEnabled(true);
                }
            }
        }

    }
}
