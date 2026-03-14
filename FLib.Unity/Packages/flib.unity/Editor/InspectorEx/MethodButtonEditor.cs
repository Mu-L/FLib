using FLib;
using FLib.Unity.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor
{
    [CustomEditor(typeof(Object), true)]
    internal class MethodButtonEditor : UnityEditor.Editor
    {
        public class TargetArea : VisualElement
        {
            public TargetArea(string name, object target, Action<TargetButtonArea> onClickHook = null)
            {
                var content = new VisualElement();
                var type = target.GetType();
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    var attr = method.GetCustomAttribute<MethodButtonAttribute>(true);
                    if (attr == null)
                        continue;
                    content.Add(new TargetButtonArea(target, method, attr) { OnClickHook = onClickHook });
                }

                if (content.childCount > 0)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        Add(content);
                    }
                    else
                    {
                        var area = new TitleAreaUI(type.FullName).SetTitle(name).OffsetLeft();
                        area.Add(content);
                        Add(area);
                    }
                }
            }
        }

        public class TargetButtonArea : VisualElement
        {
            public Action<TargetButtonArea> OnClickHook;

            public object Target;
            public MethodInfo Method;
            public object[] MethodParams;

            public TargetButtonArea(object target, MethodInfo method, MethodButtonAttribute attribute)
            {
                Target = target;
                Method = method;
                var methodParams = method.GetParameters();
                MethodParams = new object[methodParams.Length];
                VisualElement content = this;
                if (methodParams.Length > 0)
                {
                    var area = new TitleAreaUI(nameof(MethodBase)).OffsetLeft();
                    area.RemoveTitleBar();
                    area.MenuBarUI.Add(new Button(OnClick) { text = attribute.Name ?? method.Name }.FlexGrow(1));
                    Add(content = area);
                }
                else
                {
                    Add(new Button(OnClick) { text = attribute.Name ?? method.Name });
                }

                for (var i = 0; i < methodParams.Length; i++)
                {
                    var t = methodParams[i].ParameterType;
                    var pName = methodParams[i].Name;
                    if (methodParams[i].HasDefaultValue)
                        MethodParams[i] = methodParams[i].DefaultValue;
                    var paramIndex = i;
                    new AnyObjectField(pName, t, userData: pName).BindDataWithUI(v => MethodParams[paramIndex] = v, () => MethodParams[paramIndex]).AddToUI(content);
                }
            }

            private void OnClick()
            {
                Method.Invoke(Target, MethodParams);
                OnClickHook?.Invoke(this);
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var isMultiple = targets.Length > 1;
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            foreach (var item in targets)
                root.Add(new TargetArea(isMultiple ? item.name : null, item));
            return root;
        }
    }
}
