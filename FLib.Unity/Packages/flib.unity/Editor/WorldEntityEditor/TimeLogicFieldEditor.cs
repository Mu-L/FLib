// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using FLib.Unity.Editor.TimeLogic;
using FLib.WorldCores.TimeLogic;
using UnityEditor;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor
{
    public class TimeLogicFieldEditor : EditorWindow
    {
        public Action<object> SetValue;

        [FLibCustomEditor(typeof(TimeLogicRuntime))]
        public class RegisterCustom : AnyObjectField.ICustomEditor
        {
            UIBindData AnyObjectField.ICustomEditor.CreateUI(AnyObjectField field, Type type, Action<object> setValue, Func<object> getValue)
            {
                return new UIBindData()
                {
                    UI = new Button(()
                        => GetWindow<TimeLogicFieldEditor>(field.Label).SetData(
                            (TimeLogicRuntime)(getValue() ?? TypeAssistant.New(type)), setValue
                        )) { text = $"编辑TimeLogic {field.Label}" }
                };
            }
        }

        public void SetData(TimeLogicRuntime runtime, Action<object> setValue)
        {
            SetValue = setValue;
            rootVisualElement.Add(new StagePanel(runtime, true));
        }
    }
}
