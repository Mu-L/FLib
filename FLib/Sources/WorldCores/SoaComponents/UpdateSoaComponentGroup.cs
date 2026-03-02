// ==================== qcbf@qq.com | 2026-02-25 ====================

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FLib.WorldCores
{
    public class UpdateSoaComponentGroup<T> : SoaComponentGroup<T>
    {
        // 后续考虑改为再包一层，保持components紧凑。
        public Entity[] ComponentEntities;

        public HashSet<int> StartComponentIndexes;

        public UpdateSoaComponentGroup(WorldCore world) : base(world)
        {
            var order = ComponentRegistry.GetInfo<T>().Options?.Order ?? 0;
            world.UpdateSystem2.Register(UpdateSoaComponentGroupHelper.UpdateMethodDefine.MakeGenericMethod(typeof(T)), order, this);
            if (typeof(IUpdateStartSystem).IsAssignableFrom(typeof(T)))
            {
                StartComponentIndexes = new HashSet<int>();
                world.UpdateSystem1.Register(UpdateSoaComponentGroupHelper.UpdateStartMethodDefine.MakeGenericMethod(typeof(T)), order, this);
            }
        }

        public override void EnsureCapacity(int capacity)
        {
            base.EnsureCapacity(capacity);
            Array.Resize(ref ComponentEntities, capacity);
        }

        public override int Alloc(in Entity et, in T component)
        {
            var index = base.Alloc(et, component);
            ComponentEntities[index] = et;
            StartComponentIndexes?.Add(index);
            return index;
        }

        public override void Free(in Entity et, int index)
        {
            base.Free(et, index);
            ComponentEntities[index] = default;
            StartComponentIndexes?.Remove(index);
        }
    }
}