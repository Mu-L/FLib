// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using FLib.WorldCores;
using UnityEditor;
using UnityEngine.UIElements;

namespace FLib.Unity.Editor.TimeLogic
{
    public static class Utility
    {
        /// <summary>
        /// 
        /// </summary>
        public static void RemoveUnityObjectStoreRef(object obj, ExternalReferenceStorer storer)
        {
            CollectExternalReferenceField(obj, (_, _, field) => storer.Free(field.Index));
        }

        /// <summary>
        /// 
        /// </summary>
        public static void CollectExternalReferenceField(object obj, Action<object, FieldInfo, IExternalReferenceField> handler)
        {
            if (obj == null)
                return;
            foreach (var fieldInfo in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (typeof(IExternalReferenceField).IsAssignableFrom(fieldInfo.FieldType))
                {
                    var field = fieldInfo.GetValue(obj) as IExternalReferenceField;
                    var index = field?.Index;
                    if (index >= 0)
                        handler(obj, fieldInfo, field);
                }
                if (fieldInfo.FieldType.Namespace?.StartsWith("FLib") != true && Type.GetTypeCode(fieldInfo.FieldType) == TypeCode.Object)
                    CollectExternalReferenceField(fieldInfo.GetValue(obj), handler);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void TryCallMethod(object obj, VisualElement element, string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
                return;
            var method = obj.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)!;
            if (method.IsStatic)
                method.Invoke(null, new object[] { element });
            else
                method.Invoke(obj, new object[] { element });
        }
    }
}
