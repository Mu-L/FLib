// =================================================={By Qcbf|qcbf@qq.com|2024-10-19}==================================================

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace FLib
{
    /// <summary>
    ///
    /// </summary>
    public interface IObjectPoolActivatable
    {
        void ObjectPoolActivate();
    }

    /// <summary>
    ///
    /// </summary>
    public interface IObjectPoolParamActivatable
    {
        void ObjectPoolActivate(ObjectPool pool);
    }

    /// <summary>
    ///
    /// </summary>
    public interface IObjectPoolDeactivatable
    {
        void ObjectPoolDeactivatable();
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class ObjectPool
    {
        public Type ObjectType;
        public Func<Type, object> NewInstanceHook;
        public int MaxRetainedCount;
        public readonly Stack<object> Frees;

        /// <summary>  </summary>
        public ObjectPool(Type objectType, int maxRetainedCount = 128)
        {
            MaxRetainedCount = maxRetainedCount;
            Frees = new Stack<object>(Math.Max(8, maxRetainedCount >> 2));
            ObjectType = objectType;
        }

        /// <summary>
        ///
        /// </summary>
        public object NewInstance()
        {
            return NewInstanceHook?.Invoke(ObjectType) ?? TypeAssistant.New(ObjectType);
        }

        /// <summary>
        ///
        /// </summary>
        public object Create()
        {
            if (!Frees.TryPop(out var inst))
                inst = NewInstance();
            (inst as IObjectPoolActivatable)?.ObjectPoolActivate();
            (inst as IObjectPoolParamActivatable)?.ObjectPoolActivate(this);
            return inst;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Release(object obj)
        {
            if (obj is IObjectPoolDeactivatable deactivatable)
                deactivatable.ObjectPoolDeactivatable();
            if (Frees.Count < MaxRetainedCount)
                Frees.Push(obj);
        }

        /// <summary>
        /// 预分配对象，默认忽略缓存上限；传入 false 时遵守缓存上限。
        /// </summary>
        public void PreAllocate(int count, bool invokeDeactivate = true, bool withResetMaxRetainedCount = true)
        {
            if (count <= Frees.Count)
                return;
            if (count > MaxRetainedCount)
            {
                if (withResetMaxRetainedCount)
                    MaxRetainedCount = count;
                else
                    count = MaxRetainedCount;
            }

            var allocateCount = count - Frees.Count;
            if (allocateCount <= 0)
                return;
#if NET6_0_OR_GREATER
            Frees.EnsureCapacity(count);
#endif
            for (var i = 0; i < allocateCount; i++)
            {
                var inst = NewInstance();
                if (invokeDeactivate && inst is IObjectPoolDeactivatable deactivatable)
                    deactivatable.ObjectPoolDeactivatable();
                Frees.Push(inst);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class MultiObjectPool
    {
        [ThreadStatic] private static MultiObjectPool _global;

        public static MultiObjectPool Global => _global ??= new MultiObjectPool();

        public Dictionary<Type, ObjectPool> Pools = new();
        public Dictionary<Type, Func<Type, object>> NewInstanceHooks;
        public int MaxRetainedCount = 128;

        /// <summary>
        ///
        /// </summary>
        public object NewInstance(Type t)
        {
            return NewInstanceHooks != null && NewInstanceHooks.TryGetValue(t, out var hook) ? hook(t) : TypeAssistant.New(t);
        }

        /// <summary>
        ///
        /// </summary>
        public T Create<T>() where T : class, new() => (T)Create(typeof(T));

        /// <summary>
        ///
        /// </summary>
        public object Create(Type t)
        {
            if (!Pools.TryGetValue(t, out var pool))
                Pools.Add(t, pool = new ObjectPool(t, MaxRetainedCount) { NewInstanceHook = NewInstance });
            return pool.Create();
        }

        /// <summary>
        ///
        /// </summary>
        public void Release<T>(ref T obj) where T : class
        {
            Release(obj);
            obj = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Release(object obj)
        {
            Pools.GetValueOrDefault(obj.GetType())?.Release(obj);
        }

        /// <summary>
        ///
        /// </summary>
        public void FreeAll()
        {
            Pools.Clear();
        }

        /// <summary>
        ///
        /// </summary>
        public void PreAllocate(Type t, int count, bool invokeDeactivate = true, bool ignoreMaxRetainedCount = true)
        {
            if (!Pools.TryGetValue(t, out var pool))
                Pools.Add(t, pool = new ObjectPool(t) { NewInstanceHook = NewInstance, MaxRetainedCount = MaxRetainedCount });
            pool.PreAllocate(count, invokeDeactivate, ignoreMaxRetainedCount);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class GlobalObjectPool<T> where T : new()
    {
        // ReSharper disable once StaticMemberInGenericType
        [ThreadStatic] private static ObjectPool _instance;
        public static ObjectPool Instance => _instance ??= new ObjectPool(typeof(T));

        public static int MaxRetainedCount
        {
            get => Instance.MaxRetainedCount;
            set => Instance.MaxRetainedCount = value;
        }

        public static T NewInstance() => (T)Instance.NewInstance();
        public static T Create() => (T)Instance.Create();
        public static void Release(in T obj) => Instance.Release(obj);
        public static void PreAllocate(int count, bool invokeDeactivate = true, bool ignoreMaxRetainedCount = true) => Instance.PreAllocate(count, invokeDeactivate, ignoreMaxRetainedCount);
    }

    /// <summary>
    /// 
    /// </summary>
    public struct GlobalObjectPoolAutoVal<T> : IDisposable where T : new()
    {
        private T _val;
        public T Val => _val ??= GlobalObjectPool<T>.Create();
        public static implicit operator T(in GlobalObjectPoolAutoVal<T> v) => v.Val;

        public void Dispose()
        {
            if (_val != null)
            {
                GlobalObjectPool<T>.Release(_val);
                _val = default;
            }
        }
    }
}