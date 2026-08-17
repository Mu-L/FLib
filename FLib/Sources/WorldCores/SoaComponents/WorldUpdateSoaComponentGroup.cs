// ==================== qcbf@qq.com | 2026-02-25 ====================

using System;
using System.Collections.Generic;
using FLib.WorldCores;
using FLib.WorldCores.Components;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.SoaComponents
{
    public class WorldUpdateSoaComponentGroup<T> : WorldSoaComponentGroup<T>
    {
        public HashSet2<int> StartComponentIndexes;
        internal HashSet2<int> ProcessingStartComponentIndexes;
        public PauseCounter PauseUpdate;

        public WorldUpdateSoaComponentGroup(WorldCore world) : base(world)
        {
            var order = WorldComponentRegistry.GetInfo<T>().Options?.UpdateOrder ?? 0;
            if (typeof(IWorldUpdate).IsAssignableFrom(typeof(T)))
                world.Update2.Register(WorldUpdateSoaComponentGroupHelper.UpdateMethodDefine.MakeGenericMethod(typeof(T)), order, this);
            if (typeof(IWorldStart).IsAssignableFrom(typeof(T)))
            {
                StartComponentIndexes = new HashSet2<int>();
                ProcessingStartComponentIndexes = new HashSet2<int>();
                world.Update1.Register(WorldUpdateSoaComponentGroupHelper.UpdateStartMethodDefine.MakeGenericMethod(typeof(T)), order, this);
            }
        }

        public override int Alloc(in WorldEntityId et, in T component)
        {
            var index = base.Alloc(et, component);
            StartComponentIndexes?.Add(index);
            return index;
        }

        public override void Free(in WorldEntityId et, int index, bool onEntityDestroyed)
        {
            base.Free(et, index, onEntityDestroyed);
            StartComponentIndexes?.Remove(index);
            ProcessingStartComponentIndexes?.Remove(index);
        }
    }
}
