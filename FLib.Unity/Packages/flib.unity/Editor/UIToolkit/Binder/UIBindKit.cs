//==================={By Qcbf|qcbf@qq.com|12/6/2021 3:22:23 PM}===================

using FLib;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public static class UIBindKit
    {
        /// <summary>
        /// 
        /// </summary>
        public static UIBindData BindDataWithUI<T>(this INotifyValueChanged<T> ui, Action<T> dataSetter, Func<T> dataGetter, bool isFocusOutChange = false)
        {
            var data = new UIBindData { UI = (VisualElement)ui };
            if (isFocusOutChange)
            {
                data.UI.RegisterCallback<FocusOutEvent>(_ =>
                {
                    if (!EqualityComparer<T>.Default.Equals(ui.value, dataGetter()))
                    {
                        OnChangeValue(null);
                    }
                });
            }
            else
            {
                ui.RegisterValueChangedCallback(OnChangeValue);
            }

            data.Dirty = OnDirty;
            if (dataGetter != null)
            {
                ui.SetValueWithoutNotify(dataGetter());
            }

            return data;

            void OnChangeValue(EventBase evt)
            {
                if (data.UndoObject)
                {
                    EditorUtility.SetDirty(data.UndoObject);
                    Undo.RecordObject(data.UndoObject, "set value");
                }
                dataSetter(ui.value);
                data.OnValueChangeEvent?.Invoke(data);
                data.ChangeValueReceiver?.OnUIBindChangeValue(data);
            }

            void OnDirty()
            {
                if (dataGetter != null)
                {
                    ui.SetValueWithoutNotify(dataGetter());
                    data.OnValueChangeEvent?.Invoke(data);
                    data.ChangeValueReceiver?.OnUIBindChangeValue(data);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static UIBindData BindDataWithUIVerify<T>(this INotifyValueChanged<T> ui, Func<T, bool> dataSetter, Func<T> dataGetter, bool isFocusOutChange = false)
        {
            var data = new UIBindData { UI = (VisualElement)ui };
            if (isFocusOutChange)
            {
                data.UI.RegisterCallback<FocusOutEvent>(e =>
                {
                    if (!EqualityComparer<T>.Default.Equals(ui.value, dataGetter()))
                    {
                        OnChangeValue(null);
                    }
                });
            }
            else
            {
                ui.RegisterValueChangedCallback(OnChangeValue);
            }

            data.Dirty = OnDirty;
            if (dataGetter != null)
            {
                ui.SetValueWithoutNotify(dataGetter());
            }

            return data;

            void OnChangeValue(EventBase evt)
            {
                if (data.UndoObject != null)
                {
                    EditorUtility.SetDirty(data.UndoObject);
                    Undo.RecordObject(data.UndoObject, "set value");
                }
                if (!dataSetter(ui.value) && dataGetter != null)
                {
                    ui.SetValueWithoutNotify(dataGetter());
                }
                else
                {
                    data.OnValueChangeEvent?.Invoke(data);
                    data.ChangeValueReceiver?.OnUIBindChangeValue(data);
                }
            }

            void OnDirty()
            {
                if (dataGetter != null)
                {
                    ui.SetValueWithoutNotify(dataGetter());
                    data.OnValueChangeEvent?.Invoke(data);
                    data.ChangeValueReceiver?.OnUIBindChangeValue(data);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static UIBindData<TUI> BindDataToUI<TUI>(this TUI ui, Action<TUI> onDataChange) where TUI : VisualElement
        {
            var data = new UIBindData<TUI> { UI = ui };
            data.Dirty = OnDirty;
            onDataChange(ui);
            return data;

            void OnDirty()
            {
                onDataChange(ui);
                data.OnValueChangeEvent?.Invoke(data);
                data.ChangeValueReceiver?.OnUIBindChangeValue(data);
            }
        }
    }
}
