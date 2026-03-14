// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using FLib.WorldCores;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor.TimeLogic
{
    public class InspectorPanel : EditorWindow
    {
        public StagePanel Stage;

        protected virtual void CreateGUI()
        {
            rootVisualElement.RegisterKeyDown(_ => Close(), KeyCode.Escape, ignoreOnInputFieldFocused: false).Focus();
        }

        protected virtual void OnDestroy()
        {
            if (Stage != null)
            {
                Stage.Selected.UnlistenEvent(OnSelectEvent);
                if (Stage.Window != null)
                    Stage.Window.Focus();
            }
        }

        public virtual void SetStage(StagePanel stage)
        {
            Stage?.Selected.UnlistenEvent(OnSelectEvent);
            Stage = stage;
            Stage.ShortcutKey.RegisterKeyEvent(rootVisualElement);
            Stage.Selected.ListenEvent(OnSelectEvent);
            titleContent.text = "Inspector " + stage.name;
        }

        protected virtual void OnSelectEvent(object dispatcher, in FEventValue<ISelectable>.ChangeEvent e)
        {
            SetValue(e.NewValue.InspectorValue);
        }

        public virtual void SetValue(object value)
        {
            rootVisualElement.Clear();

            var scroll = new ScrollView();
            rootVisualElement.Add(scroll);

            var objField = new AnyObjectField(value.GetType().Name, null, value, new AnyObjectField.OptionData()
            {
                Flags = AnyObjectField.EOptionFlag.ObjectMustComment | AnyObjectField.EOptionFlag.SkipRootObjectFold,
                Hook = AnyObjectFieldHook
            });
            objField.RegisterValueChangedCallback(_ => Stage.Selected.Value.RefreshUI());
            scroll.Add(objField);

            var ui = new MethodButtonEditor.TargetArea(null, value, _ => { Stage.Selected.Value.RefreshUI(); });
            scroll.Add(ui);
        }

        /// <summary>
        /// 
        /// </summary>
        private UIBindData AnyObjectFieldHook(AnyObjectField field, Type arg1, Action<object> arg2, Func<object> arg3)
        {
            if (typeof(IExternalReferenceField).IsAssignableFrom(arg1))
            {
                var ui = new ObjectField(field.Label) { objectType = arg1.GetGenericArguments()[0] }.ShortFieldLabel();
                return ui.BindDataWithUI(v =>
                {
                    var obj = arg3() as IExternalReferenceField ?? (IExternalReferenceField)TypeAssistant.New(arg1);
                    var refIndex = obj.Index;
                    if (refIndex >= 0)
                    {
                        if (!v)
                        {
                            Stage.Runtime.ExternalReferences.Free(refIndex);
                            obj.Index = -1;
                        }
                        else
                        {
                            Stage.Runtime.ExternalReferences[refIndex] = v;
                        }
                    }
                    else
                    {
                        obj.Index = Stage.Runtime.ExternalReferences.Alloc(v);
                    }
                    arg2(obj);
                    Stage.OnExternalReferenceValueChange?.Invoke();
                }, () =>
                {
                    var index = (arg3() as IExternalReferenceField)?.Index;
                    return index >= 0 ? (Object)Stage.Runtime.ExternalReferences[index.Value] : null;
                });
            }
            return null;
        }
    }
}
