// ==================== 优化的MonoBehaviour字段自动绑定工具 ====================

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

namespace FLib.Unity.Editor
{
    public class MonoBehaviourFieldBindTool
    {
        private class BindingContext
        {
            public readonly Dictionary<string, FieldInfo> nameToFieldMap;
            public readonly Dictionary<Type, List<FieldInfo>> typeToFieldsMap;
            public readonly HashSet<FieldInfo> boundFields;
            public readonly List<string> bindingLogs;

            public BindingContext(FieldInfo[] fields)
            {
                nameToFieldMap = new Dictionary<string, FieldInfo>(fields.Length);
                typeToFieldsMap = new Dictionary<Type, List<FieldInfo>>();
                boundFields = new HashSet<FieldInfo>();
                bindingLogs = new List<string>();

                // 预处理字段映射
                foreach (var field in fields)
                {
                    // 按名称映射
                    nameToFieldMap[field.Name] = field;
                    
                    // 按类型映射
                    var fieldType = GetFieldElementType(field);
                    if (IsBindableType(fieldType))
                    {
                        if (!typeToFieldsMap.TryGetValue(fieldType, out var fieldList))
                        {
                            fieldList = new List<FieldInfo>();
                            typeToFieldsMap[fieldType] = fieldList;
                        }
                        fieldList.Add(field);
                    }
                }
            }
        }

        [MenuItem("CONTEXT/MonoBehaviour/Bind Serializable Fields", priority = 1)]
        private static void Bind(MenuCommand command)
        {
            var mono = (MonoBehaviour)command.context;
            var logs = BindFields(mono);
            
            if (logs.Count > 0)
            {
                EditorUtility.SetDirty(mono);
                Debug.Log($"自动绑定完成 [{logs.Count}]: {string.Join(", ", logs)}");
            }
            else
            {
                Debug.Log("未找到需要绑定的字段");
            }
        }

        public static List<string> BindFields(MonoBehaviour mono)
        {
            var fields = GetBindableFields(mono.GetType());
            if (fields.Length == 0) return new List<string>();

            var context = new BindingContext(fields);
            
            // 递归绑定
            BindRecursive(mono, mono.transform, context);
            
            return context.bindingLogs;
        }

        private static FieldInfo[] GetBindableFields(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var bindableFields = new List<FieldInfo>();

            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<SerializeField>() != null || 
                    (field.IsPublic && field.GetCustomAttribute<NonSerializedAttribute>() == null))
                {
                    var fieldType = GetFieldElementType(field);
                    if (IsBindableType(fieldType))
                    {
                        bindableFields.Add(field);
                    }
                }
            }

            return bindableFields.ToArray();
        }

        private static Type GetFieldElementType(FieldInfo field)
        {
            return field.FieldType.IsArray ? field.FieldType.GetElementType() : field.FieldType;
        }

        private static bool IsBindableType(Type type)
        {
            return type == typeof(GameObject) || typeof(Component).IsAssignableFrom(type);
        }

        private static void BindRecursive(object target, Transform node, BindingContext context)
        {
            // 先收集所有节点信息
            var allNodes = new List<(Transform node, string name)>();
            CollectAllNodes(node, allNodes);
            
            // 按优先级排序并绑定
            BindWithPriority(target, allNodes, context);
        }
        
        private static void CollectAllNodes(Transform node, List<(Transform, string)> nodes)
        {
            nodes.Add((node, GetCleanNodeName(node.name)));
            
            var childCount = node.childCount;
            for (int i = 0; i < childCount; i++)
            {
                CollectAllNodes(node.GetChild(i), nodes);
            }
        }
        
        private static void BindWithPriority(object target, List<(Transform node, string name)> allNodes, BindingContext context)
        {
            var bindingCandidates = new List<BindingCandidate>();
            
            // 收集所有绑定候选
            foreach (var (node, name) in allNodes)
            {
                CollectBindingCandidates(target, node, name, bindingCandidates, context);
            }
            
            // 按优先级排序：名称匹配 > 类型匹配，同类型内按字段名排序保证稳定性
            bindingCandidates.Sort((a, b) => 
            {
                var priorityCompare = a.priority.CompareTo(b.priority);
                return priorityCompare != 0 ? priorityCompare : string.Compare(a.field.Name, b.field.Name, StringComparison.Ordinal);
            });
            
            // 执行绑定
            foreach (var candidate in bindingCandidates)
            {
                if (context.boundFields.Contains(candidate.field))
                    continue;
                    
                if (SetFieldValue(target, candidate.field, candidate.node, candidate.bindingType))
                {
                    context.boundFields.Add(candidate.field);
                    context.bindingLogs.Add(candidate.field.Name);
                }
            }
        }
        
        private class BindingCandidate
        {
            public FieldInfo field;
            public Transform node;
            public Type bindingType;
            public int priority; // 数值越小优先级越高
        }
        
        private static void CollectBindingCandidates(object target, Transform node, string nodeName, 
            List<BindingCandidate> candidates, BindingContext context)
        {
            // 优先级1: 直接名称匹配
            AddNameMatchingCandidates(target, node, nodeName, candidates, context, 1);
            
            // 优先级2: 父节点+当前节点名称匹配
            if (node.parent != null)
            {
                var parentName = GetCleanNodeName(node.parent.name);
                var combinedName = parentName + nodeName;
                AddNameMatchingCandidates(target, node, combinedName, candidates, context, 2);
            }
            
            // 优先级3: 类型匹配
            AddTypeMatchingCandidates(target, node, candidates, context, 3);
        }
        
        private static void AddNameMatchingCandidates(object target, Transform node, string fieldName, 
            List<BindingCandidate> candidates, BindingContext context, int priority)
        {
            // 直接名称
            if (context.nameToFieldMap.TryGetValue(fieldName, out var field) && IsFieldEmpty(target, field))
            {
                candidates.Add(new BindingCandidate 
                { 
                    field = field, 
                    node = node, 
                    bindingType = GetFieldElementType(field),
                    priority = priority 
                });
            }
            
            // 复数形式（主要用于数组字段）
            var pluralName = fieldName + "s";
            if (context.nameToFieldMap.TryGetValue(pluralName, out var pluralField) && IsFieldEmpty(target, pluralField))
            {
                candidates.Add(new BindingCandidate 
                { 
                    field = pluralField, 
                    node = node, 
                    bindingType = GetFieldElementType(pluralField),
                    priority = priority 
                });
            }
        }
        
        private static void AddTypeMatchingCandidates(object target, Transform node, 
            List<BindingCandidate> candidates, BindingContext context, int priority)
        {
            // GameObject类型匹配
            if (context.typeToFieldsMap.TryGetValue(typeof(GameObject), out var gameObjectFields))
            {
                foreach (var field in gameObjectFields)
                {
                    if (IsFieldEmpty(target, field))
                    {
                        candidates.Add(new BindingCandidate 
                        { 
                            field = field, 
                            node = node, 
                            bindingType = typeof(GameObject),
                            priority = priority 
                        });
                    }
                }
            }
            
            // 组件类型匹配
            var components = node.GetComponents<Component>();
            foreach (var comp in components)
            {
                AddComponentTypeCandidates(target, node, comp.GetType(), candidates, context, priority);
            }
        }
        
        private static void AddComponentTypeCandidates(object target, Transform node, Type componentType, 
            List<BindingCandidate> candidates, BindingContext context, int priority)
        {
            // 精确类型匹配
            if (context.typeToFieldsMap.TryGetValue(componentType, out var exactFields))
            {
                foreach (var field in exactFields)
                {
                    if (IsFieldEmpty(target, field))
                    {
                        candidates.Add(new BindingCandidate 
                        { 
                            field = field, 
                            node = node, 
                            bindingType = componentType,
                            priority = priority 
                        });
                    }
                }
            }
            
            // 基类匹配
            var baseType = componentType.BaseType;
            while (baseType != null && baseType != typeof(Component) && baseType != typeof(MonoBehaviour))
            {
                if (context.typeToFieldsMap.TryGetValue(baseType, out var baseFields))
                {
                    foreach (var field in baseFields)
                    {
                        if (IsFieldEmpty(target, field))
                        {
                            candidates.Add(new BindingCandidate 
                            { 
                                field = field, 
                                node = node, 
                                bindingType = baseType,
                                priority = priority + 1 // 基类优先级稍低
                            });
                        }
                    }
                }
                baseType = baseType.BaseType;
            }
            
            // 接口匹配
            var interfaces = componentType.GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                if (context.typeToFieldsMap.TryGetValue(interfaceType, out var interfaceFields))
                {
                    foreach (var field in interfaceFields)
                    {
                        if (IsFieldEmpty(target, field))
                        {
                            candidates.Add(new BindingCandidate 
                            { 
                                field = field, 
                                node = node, 
                                bindingType = interfaceType,
                                priority = priority + 2 // 接口优先级最低
                            });
                        }
                    }
                }
            }
        }



        private static bool SetFieldValue(object target, FieldInfo field, Transform node, Type specificType = null)
        {
            if (field.FieldType.IsArray)
            {
                return SetArrayFieldValue(target, field, node, specificType);
            }
            else
            {
                return SetSingleFieldValue(target, field, node, specificType);
            }
        }

        private static bool SetSingleFieldValue(object target, FieldInfo field, Transform node, Type specificType = null)
        {
            if (!IsFieldEmpty(target, field))
                return false;

            var targetType = specificType ?? field.FieldType;
            
            if (targetType == typeof(GameObject))
            {
                field.SetValue(target, node.gameObject);
                return true;
            }
            else if (typeof(Component).IsAssignableFrom(targetType))
            {
                var component = node.GetComponent(targetType);
                if (component != null)
                {
                    field.SetValue(target, component);
                    return true;
                }
            }
            
            return false;
        }

        private static bool SetArrayFieldValue(object target, FieldInfo field, Transform node, Type specificType = null)
        {
            if (!IsFieldEmpty(target, field))
                return false;

            var elementType = specificType ?? field.FieldType.GetElementType();
            var childCount = node.childCount;
            
            if (elementType == typeof(GameObject))
            {
                var gameObjects = new GameObject[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    gameObjects[i] = node.GetChild(i).gameObject;
                }
                field.SetValue(target, gameObjects);
                return true;
            }
            else if (typeof(Component).IsAssignableFrom(elementType))
            {
                var components = new List<Component>();
                for (int i = 0; i < childCount; i++)
                {
                    var comp = node.GetChild(i).GetComponent(elementType);
                    if (comp != null)
                    {
                        components.Add(comp);
                    }
                }
                
                if (components.Count > 0)
                {
                    var array = Array.CreateInstance(elementType, components.Count);
                    for (int i = 0; i < components.Count; i++)
                    {
                        array.SetValue(components[i], i);
                    }
                    field.SetValue(target, array);
                    return true;
                }
            }
            
            return false;
        }

        private static bool IsFieldEmpty(object target, FieldInfo field)
        {
            var value = field.GetValue(target);
            
            if (field.FieldType.IsArray)
            {
                return value == null || ((Array)value).Length == 0;
            }
            else
            {
                return value == null || (value is Object unityObj && unityObj == null);
            }
        }

        private static string GetCleanNodeName(string nodeName)
        {
            // 处理 "测试名称@TestName" 格式，提取@后的部分
            var atIndex = nodeName.IndexOf('@');
            return atIndex >= 0 ? nodeName.Substring(atIndex + 1) : nodeName;
        }
    }
}