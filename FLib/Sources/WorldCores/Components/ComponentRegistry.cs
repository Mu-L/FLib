// ==================== qcbf@qq.com |2025-12-28 ====================

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FLib.WorldCores
{
    /// <summary>
    /// 这里会对每个Component类型 首次使用时进行注册, 生成比较的id, size, info等等信息.
    /// 目前是通过反射获取的, 后续会增加warmup接口, 或者直接通过source generator实现在编译时生成,运行时0首次注册开销.
    /// </summary>
    public static class ComponentRegistry
    {
        public static readonly Dictionary<Type, ComponentMeta> ComponentTypeMap = new(1024);
        public static ushort ComponentCount { get; private set; }
        private static ComponentInfo[] _componentInfos = new ComponentInfo[1024];
        private static readonly MethodInfo SizeOfMethod = typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf));

        /// <summary>
        /// 
        /// </summary>
        public static IncrementId GetId<T>()
        {
            return GetMeta<T>().Id;
        }

        /// <summary>
        /// 
        /// </summary>
        public static ComponentMeta GetMeta<T>()
        {
            return ComponentGenericMap<T>.IsEmpty ? ComponentGenericMap<T>.Init(Register(typeof(T), SizeOf<T>())) : ComponentGenericMap<T>.Meta;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IncrementId GetId(Type type)
        {
            return GetMeta(type).Id;
        }

        /// <summary>
        /// 
        /// </summary>
        public static ComponentMeta GetMeta(Type type)
        {
            return ComponentTypeMap.TryGetValue(type, out var componentType) ? componentType : Register(type, SizeOf(type));
        }

        /// <summary>
        /// 
        /// </summary>
        public static Type GetType(in IncrementId id)
        {
            return _componentInfos[id].Type;
        }

        /// <summary>
        /// 
        /// </summary>
        public static ref readonly ComponentInfo GetInfo<T>()
        {
            return ref _componentInfos[GetMeta<T>().Id];
        }

        /// <summary>
        /// 
        /// </summary>
        public static ref readonly ComponentInfo GetInfo(Type type)
        {
            return ref _componentInfos[GetMeta(type).Id];
        }

        /// <summary>
        /// 
        /// </summary>
        public static ref readonly ComponentInfo GetInfo(in IncrementId id)
        {
            return ref _componentInfos[id];
        }

        /// <summary>
        /// 
        /// </summary>
        public static ComponentMeta Register(Type type, ushort size)
        {
            var id = new IncrementId(++ComponentCount);
            var cType = new ComponentMeta(id, size, type);
            ComponentTypeMap[type] = cType;

            if (_componentInfos.Length <= id)
                Array.Resize(ref _componentInfos, id + GlobalSetting.CapacityExpandSize);
            _componentInfos[id] = new ComponentInfo(cType, type);
            StaticComponentMask.EnsureCapacity(id);
            return cType;
        }

        /// <summary>
        /// 
        /// </summary>
        public static int GetHash(in ReadOnlySpan<ulong> componentTypeMask)
        {
            var hash = new HashCode();
            hash.AddBytes(MemoryMarshal.AsBytes(componentTypeMask));
            return hash.ToHashCode();
        }

        /// <summary>
        /// 
        /// </summary>
        private static ushort SizeOf<T>()
        {
            return (ushort)(typeof(T).IsValueType ? Unsafe.SizeOf<T>() : IntPtr.Size);
        }

        /// <summary>
        /// 
        /// </summary>
        private static ushort SizeOf(Type type)
        {
            return (ushort)(type.IsValueType ? (int)SizeOfMethod.MakeGenericMethod(type).Invoke(null, null)! : IntPtr.Size);
        }
    }
}