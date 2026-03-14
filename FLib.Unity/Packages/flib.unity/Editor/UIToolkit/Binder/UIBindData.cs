//==================={By Qcbf|qcbf@qq.com|12/6/2021 3:24:18 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor
{
    public class UIBindData
    {
        public Action Dirty;
        public Action<UIBindData> OnValueChangeEvent;
        public IUIBindChangeValueReceivable ChangeValueReceiver;
        public Object UndoObject;
        public object UserData;
        public VisualElement UI;

        public static implicit operator VisualElement(UIBindData data) => data.UI;

        public UIBindData To(Action<UIBindData> receiver)
        {
            OnValueChangeEvent = receiver;
            return this;
        }

        public UIBindData To(IUIBindChangeValueReceivable receiver)
        {
            ChangeValueReceiver = receiver;
            return this;
        }

        public UIBindData To(IUIBindChangeValueReceivable receiver, VisualElement uiContainer)
        {
            ChangeValueReceiver = receiver;
            uiContainer.Add(UI);
            return this;
        }

        public UIBindData To(IUIBindChangeValueReceivable receiver, VisualElement uiContainer, int index)
        {
            ChangeValueReceiver = receiver;
            uiContainer.Insert(index, UI);
            return this;
        }

        public UIBindData AddToUI(VisualElement uiContainer)
        {
            uiContainer.Add(UI);
            return this;
        }

        public UIBindData AddToUI(VisualElement uiContainer, int index)
        {
            uiContainer.Insert(index, UI);
            return this;
        }

        public UIBindData SetUndoObj(Object undoObject)
        {
            UndoObject = undoObject;
            return this;
        }

        public UIBindData SetAction(Action<UIBindData> action)
        {
            action(this);
            return this;
        }

        public UIBindData ListenEvent<T>(ref FEventValue<T> e)
        {
            e.ListenEvent((object _, in FEventValue<T>.ChangeEvent _) => Dirty());
            return this;
        }

        public UIBindData AddGroup(ref UIBindGroup group)
        {
            group.Add(this);
            return this;
        }
    }

    public class UIBindData<T> : UIBindData where T : VisualElement
    {
        public new T UI
        {
            get => (T)base.UI;
            internal set => base.UI = value;
        }

        public new UIBindData<T> AddToUI(VisualElement uiContainer)
        {
            base.AddToUI(uiContainer);
            return this;
        }

        public new UIBindData<T> AddToUI(VisualElement uiContainer, int index)
        {
            base.AddToUI(uiContainer, index);
            return this;
        }

        public new UIBindData<T> SetUndoObj(Object undoObject)
        {
            UndoObject = undoObject;
            return this;
        }
    }
}
