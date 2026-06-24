// ==================={By Qcbf|qcbf@qq.com|4/29/2022 3:14:28 PM}===================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace FLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class RenamedTypeAttribute : ObjectInjectToAttribute
    {
        public string[] MoreOldNames;

        public RenamedTypeAttribute(string oldName, string[] moreOldNames = null) : base(nameof(TypeAssistant))
        {
            StrParam = oldName;
            MoreOldNames = moreOldNames;
        }
    }

    [ObjectInjectionReceiver(nameof(TypeAssistant), nameof(ReceiveInjection))]
    public static class TypeAssistant
    {
        internal static int IdGen;
        [NonSerialized] public static Assembly[] AllAssemblies = { typeof(TypeAssistant).Assembly };
        public static IReadOnlyDictionary<string, Type> CustomTypeMap;
        [ThreadStatic] private static Dictionary<string, Type> _typeFinderBuffer;


        private static readonly Func<Assembly, string, bool, Type> TypeFinder = (_, name, ignoreCase) =>
        {
            switch (name)
            {
                #region base

                case "short":
                case "System.Int16": return typeof(short);
                case "int":
                case "System.Int32": return typeof(int);
                case "long":
                case "System.Int64": return typeof(long);
                case "ushort":
                case "System.UInt16": return typeof(ushort);
                case "boolean":
                case "bool":
                case "System.Boolean": return typeof(bool);
                case "uint":
                case "System.UInt32": return typeof(uint);
                case "ulong":
                case "System.UInt64": return typeof(ulong);
                case "byte":
                case "System.Byte": return typeof(byte);
                case "char":
                case "System.Char": return typeof(char);
                case "sbyte":
                case "System.SByte": return typeof(sbyte);
                case "string":
                case "System.String": return typeof(string);
                case "type":
                case "System.Type": return typeof(Type);
                case "float":
                case "System.Single": return typeof(float);
                case "fnum": return typeof(FNum);
                case "double":
                case "System.Double": return typeof(double);
                case "list":
                case "System.Collections.Generic.List`1": return typeof(List<>);
                case "dict":
                case "System.Collections.Generic.Dictionary`2": return typeof(Dictionary<,>);
                case "object":
                case "System.Object": return typeof(object);

                #endregion

                default:
                    if (string.IsNullOrEmpty(name))
                        return null;
                    Type t = null;
                    if (CustomTypeMap?.TryGetValue(name, out t) == true || _typeFinderBuffer?.TryGetValue(name, out t) == true)
                        return t;
                    foreach (var asm in AllAssemblies)
                    {
                        var found = asm.GetType(name, false, ignoreCase);
                        if (found != null)
                        {
                            (_typeFinderBuffer ??= new Dictionary<string, Type>()).Add(name, found);
                            return found;
                        }
                    }

                    return Type.GetType(name, false, ignoreCase);
            }
        };

#if UNITY_PROJ
        [UnityEngine.Scripting.Preserve]
#endif
        public static void ReceiveInjection(List<(object info, ObjectInjectToAttribute attr)> list)
        {
            var renames = new Dictionary<string, Type>(list.Count);
            foreach (var item in list)
            {
                if (item.info is Type t)
                {
                    var attr = (RenamedTypeAttribute)item.attr;
                    var typeName = GetTypeName(t);
                    renames.Add(ReplaceTypeNameSpan(typeName, attr.StrParam), t);
                    if (attr.MoreOldNames != null)
                    {
                        foreach (var name in attr.MoreOldNames)
                            renames.Add(ReplaceTypeNameSpan(typeName, name), t);
                    }
                }
            }

            RegisterCustomFinderType(renames);

            return;

            static string ReplaceTypeNameSpan(string typeName, string newName)
            {
                if (newName.Contains('.', StringComparison.Ordinal)) // 默认newName包含完整的命名空间
                    return newName;

                var lastSeparatorIndex = -1;
                for (var i = typeName.Length - 1; i >= 0; i--)
                {
                    if (typeName[i] == '+' || typeName[i] == '.')
                    {
                        lastSeparatorIndex = i;
                        break;
                    }
                }

                if (lastSeparatorIndex == -1)
                    return newName;
                var prefixLength = lastSeparatorIndex + 1;
                var resultLength = prefixLength + newName.Length;

                Span<char> buffer = stackalloc char[resultLength];
                typeName.AsSpan(0, prefixLength).CopyTo(buffer);
                newName.AsSpan().CopyTo(buffer[prefixLength..]);
                return new string(buffer);
            }
        }

        public static void Clear()
        {
            Array.Resize(ref AllAssemblies, 1);
            CustomTypeMap = null;
        }

        /// <summary>
        ///
        /// </summary>
        public static void RemoveAssembly(Assembly target)
        {
            ArrayFLibUtility.Remove(ref AllAssemblies, target);
        }

        /// <summary>
        ///
        /// </summary>
        public static void AddAssemblies(params Assembly[] assemblies)
        {
            lock (AllAssemblies)
            {
                var hash = new HashSet<Assembly>(AllAssemblies);
                foreach (var item in assemblies)
                    hash.Add(item);
                AllAssemblies = hash.ToArray();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public static void AddCallingAssembly()
        {
            var asm = Assembly.GetCallingAssembly();
            if (AllAssemblies.Contains(asm))
                return;
            ArrayFLibUtility.Add(ref AllAssemblies, asm);
        }

        public static void UnregisterCustomFinderType(params string[] names)
        {
            if (CustomTypeMap == null)
                return;
            var dict = CustomTypeMap.ToDictionary(k => k.Key, v => v.Value);
            foreach (var s in names)
                dict.Remove(s);
            CustomTypeMap = new Dictionary<string, Type>(dict);
        }

        public static void RegisterCustomFinderType<T>(string name = null)
        {
            RegisterCustomFinderType(new Dictionary<string, Type>() { { (name ?? typeof(T).FullName)!, typeof(T) } });
        }

        public static void RegisterCustomFinderType(Dictionary<string, Type> types)
        {
            if (CustomTypeMap != null)
            {
                foreach (var type in CustomTypeMap)
                    types.TryAdd(type.Key, type.Value);
            }

            CustomTypeMap = new Dictionary<string, Type>(types);
        }


        public static T New<T>()
        {
            return Activator.CreateInstance<T>();
        }


        public static object New(Type t)
        {
            return Activator.CreateInstance(t, Array.Empty<object>());
        }


        public static object New(Type t, params object[] args)
        {
            return Activator.CreateInstance(t, args);
        }


        public static object New(string name, bool ignoreCase = false, bool isThrowOnError = true, object[] args = null)
        {
            var type = GetType(name, ignoreCase, isThrowOnError);
            return type == null ? null : Activator.CreateInstance(type, args ?? Array.Empty<object>());
        }

        public static Type GetType(string name, bool ignoreCase = false, bool isThrowOnError = true)
        {
            var result = Type.GetType(name, null, TypeFinder, false, ignoreCase);
            if (isThrowOnError && result == null)
                throw new TypeLoadException(name);
            return result;
        }

        public static string GetTypeName(Type t)
        {
            if (t == typeof(byte)) return "b";
            if (t == typeof(short)) return "s";
            if (t == typeof(int)) return "i";
            if (t == typeof(long)) return "l";
            if (t == typeof(sbyte)) return "sb";
            if (t == typeof(ushort)) return "us";
            if (t == typeof(ulong)) return "ul";
            if (t == typeof(string)) return "str";
            if (t == typeof(char)) return "c";
            if (t == typeof(float)) return "f";
            if (t == typeof(double)) return "d";
            return t == typeof(Type) ? "type" : t.ToString();
        }
    }

    // ReSharper disable once UnusedTypeParameter
    public static class TypeId<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int Id = Interlocked.Increment(ref TypeAssistant.IdGen);
    }
}