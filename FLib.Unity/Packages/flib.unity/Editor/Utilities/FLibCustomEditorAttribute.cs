// ==================== qcbf@qq.com | 2025-07-01 ====================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace FLib.Unity.Editor
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FLibCustomEditorAttribute : Attribute
    {
        /// <summary>
        /// {ForType} : {EditorType}
        /// </summary>
        public static readonly ReadOnlyDictionary<Type, Type> CustomEditors;

        private readonly Type _forType;
        private readonly bool _editorForChildClasses;
        private readonly int _priority;

        static FLibCustomEditorAttribute()
        {
            var dict = new Dictionary<Type, (FLibCustomEditorAttribute, Type, int)>();
            var dict2 = new ConcurrentDictionary<Type, Type>();

            foreach (var editorType in EditorFLibUtility.UserAssemblyTypes)
            {
                var attr = editorType.GetCustomAttribute<FLibCustomEditorAttribute>();
                if (attr == null)
                    continue;
                if (!dict.TryGetValue(attr._forType, out var editorInfo) || editorInfo.Item3 < attr._priority)
                {
                    dict[attr._forType] = (attr, editorType, attr._priority);
                    dict2[attr._forType] = editorType;
                }
            }
            foreach (var type in EditorFLibUtility.UserAssemblyTypes.AsParallel())
            {
                if (dict.ContainsKey(type))
                    continue;
                var baseType = type.BaseType;
                while (baseType != null)
                {
                    if (dict.TryGetValue(baseType, out var editorInfo) && editorInfo.Item1._editorForChildClasses)
                    {
                        if (!dict2.TryAdd(type, editorInfo.Item2))
                            throw new Exception($"add failure: {type}>{editorInfo.Item2}");
                        break;
                    }
                    baseType = baseType.BaseType;
                }
            }

            CustomEditors = new ReadOnlyDictionary<Type, Type>(dict2);
        }

        public FLibCustomEditorAttribute(Type forType, bool editorForChildClasses = true, int priority = 0)
        {
            _forType = forType;
            _priority = priority;
            _editorForChildClasses = editorForChildClasses;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool TryGetEditor<TEditorType>(Type runtimeType, out TEditorType editor) where TEditorType : class
        {
            editor = GetEditor<TEditorType>(runtimeType);
            return editor != null;
        }

        /// <summary>
        /// 
        /// </summary>
        public static TEditorType GetEditor<TEditorType>(Type runtimeType) where TEditorType : class
        {
            if (runtimeType == null || !CustomEditors.TryGetValue(runtimeType, out var editorType) || !typeof(TEditorType).IsAssignableFrom(editorType))
                return null;
            return TypeAssistant.New(editorType) as TEditorType;
        }
    }
}
