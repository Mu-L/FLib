//==================={By Qcbf|qcbf@qq.com|8/16/2021 5:31:34 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class EditorSmallTipsUI : VisualElement
    {
        public class Item : VisualElement
        {
            public EditorSmallTipsUI Owner;
            public Label UILabel;
            public float Time = 2f;
            public Action<Item> OnClosedEvent;

            public Item()
            {
                pickingMode = PickingMode.Ignore;
                style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                style.borderTopWidth = style.borderRightWidth = style.borderBottomWidth = style.borderLeftWidth = 1;
                style.borderTopColor = style.borderRightColor = style.borderBottomColor = style.borderLeftColor = Color.black;
                style.paddingTop = style.paddingBottom = 2;
                style.paddingLeft = style.paddingRight = 5;
                style.marginTop = style.marginBottom = 4;
                style.unityTextAlign = TextAnchor.MiddleCenter;
                style.whiteSpace = WhiteSpace.Normal;
                RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
                Add(UILabel = new Label { pickingMode = PickingMode.Ignore });
            }

            private async void OnAttachToPanelEvent(AttachToPanelEvent evt)
            {
                await Task.Delay((int)(Time * 1000));
                MultiObjectPool.Global.Release(this);
                if (OnClosedEvent != null)
                {
                    OnClosedEvent.Invoke(this);
                    OnClosedEvent = null;
                }
                RemoveFromHierarchy();
            }
        }


        public EditorSmallTipsUI()
        {
            style.justifyContent = Justify.Center;
            pickingMode = PickingMode.Ignore;
            this.StretchToParentSize();
        }

        public void Show(string text)
        {
            var item = MultiObjectPool.Global.Create<Item>();
            item.Owner = this;
            item.UILabel.text = text;
            Add(item);
        }
    }
}
