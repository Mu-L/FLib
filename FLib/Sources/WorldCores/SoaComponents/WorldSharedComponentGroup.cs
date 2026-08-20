// ==================== qcbf@qq.com | 2026-01-15 ====================

using System.Runtime.CompilerServices;
using FLib.WorldCores;
using FLib.WorldCores.Components;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.SoaComponents
{
    public struct WorldSharedComponentGroupRef
    {
        public int Index;
        public int RefCount;
    }

    public interface IWorldSharedComponentGroupable
    {
        public int Version { get; }
        public int GetIndexFromHash(int hash);
    }

    public class WorldSharedComponentGroup<T> : WorldSoaComponentGroup<T>, IWorldSharedComponentGroupable where T : IWorldSharedComponent
    {
        public Dictionary2<int, WorldSharedComponentGroupRef> Groups = new();

        public int Version { get; private set; }

        public WorldSharedComponentGroup(WorldCore world) : base(world)
        {
        }

        public override bool EnsureCapacity(int capacity)
        {
            if (base.EnsureCapacity(capacity))
            {
                Groups.EnsureCapacity(capacity);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public override int Alloc(in WorldEntityId et, in T component)
        {
            var hash = component.GetHashCode();
            Alloc(et, component, hash);
            return hash;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Alloc(in WorldEntityId et, in T component, int hash)
        {
            ref var r = ref Groups.GetValueRefOrAdd(hash);
            if (r.RefCount == 0)
                r.Index = base.Alloc(et, component);
            ++r.RefCount;
            ++Version;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Free(in WorldEntityId et, int hash, bool onEntityDestroyed)
        {
            ref var r = ref Groups.GetValueRefOrNullRef(hash);
            if (Unsafe.IsNullRef(ref r))
                return;
            if (--r.RefCount > 0) return;
            base.Free(in et, r.Index, onEntityDestroyed);
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
            return Groups.TryGetValue(hash, out var r) ? r.Index : -1;
        }
    }
}
