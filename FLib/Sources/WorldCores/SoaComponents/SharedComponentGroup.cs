// ==================== qcbf@qq.com | 2026-01-15 ====================

using System;
using System.Collections.Generic;

namespace FLib.WorldCores
{
    public struct SharedComponentGroupRef
    {
        public int Index;
        public int RefCount;
    }

    public interface ISharedComponentGroupable
    {
        public int Version { get; }
        public int GetIndexFromHash(int hash);
    }

    public class SharedComponentGroup<T> : SoaComponentGroup<T>, ISharedComponentGroupable where T : ISharedComponent
    {
        public SlimDictionary<int, SharedComponentGroupRef> Groups = new();

        public int Version { get; private set; }

        public override void EnsureCapacity(int capacity)
        {
            base.EnsureCapacity(capacity);
            Groups.EnsureCapacity(capacity);
        }

        public override int Alloc(in Entity et) => throw new NotSupportedException("need component value");

        /// <summary>
        /// 
        /// </summary>
        public int Alloc(in Entity et, in T value)
        {
            var hash = value.GetHashCode();
            Alloc(et, value, hash);
            return hash;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Alloc(in Entity et, in T value, int hash)
        {
            ref var r = ref Groups.GetOrAddValueRef(hash);
            if (r.RefCount == 0)
            {
                r.Index = base.Alloc(et);
                Components[r.Index] = value;
            }
            ++r.RefCount;
            ++Version;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="et"></param>
        /// <param name="hash"></param>
        public override void Free(in Entity et, int hash)
        {
            var idx = Groups.GetEntryIndex(hash);
            if (idx < 0) return;
            ref var r = ref Groups.GetEntryValue(idx);
            if (--r.RefCount > 0) return;
            base.Free(in et, r.Index);
            Groups.Remove(hash);
            ++Version;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public int GetIndexFromHash(int hash)
        {
            throw new NotSupportedException("need component value");
        }
    }
}