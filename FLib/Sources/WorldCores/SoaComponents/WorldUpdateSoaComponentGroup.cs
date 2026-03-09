// ==================== qcbf@qq.com | 2026-02-25 ====================

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FLib.WorldCores
{
    public class WorldUpdateSoaComponentGroup<T> : WorldSoaComponentGroup<T>
    {
        // 后续考虑改为再包一层，保持components紧凑。
        public WorldEntity[] ComponentEntities;

        public HashSet<int> StartComponentIndexes;

        public WorldUpdateSoaComponentGroup(WorldCore world) : base(world)
        {
            var order = WorldComponentRegistry.GetInfo<T>().Options?.Order ?? 0;
            world.Update2.Register(WorldUpdateSoaComponentGroupHelper.UpdateMethodDefine.MakeGenericMethod(typeof(T)), order, this);
            if (typeof(IWorldLifecycleStart).IsAssignableFrom(typeof(T)))
            {
                StartComponentIndexes = new HashSet<int>();
                world.Update1.Register(WorldUpdateSoaComponentGroupHelper.UpdateStartMethodDefine.MakeGenericMethod(typeof(T)), order, this);
            }
        }

        public override void EnsureCapacity(int capacity)
        {
            base.EnsureCapacity(capacity);
            Array.Resize(ref ComponentEntities, capacity);
        }

        public override int Alloc(in WorldEntity et, in T component)
        {
            var index = base.Alloc(et, component);
            ComponentEntities[index] = et;
            StartComponentIndexes?.Add(index);
            return index;
        }

        public override void Free(in WorldEntity et, int index, bool onEntityDestroyed)
        {
            base.Free(et, index, onEntityDestroyed);
            ComponentEntities[index] = default;
            StartComponentIndexes?.Remove(index);
        }
    }
}