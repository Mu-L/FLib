// ==================== qcbf@qq.com | 2026-02-25 ====================

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FLib.WorldCores
{
    public class UpdateSoaComponentGroup<T> : SoaComponentGroup<T> where T : IUpdateSystem
    {
        // 后续考虑改为再包一层，保持components紧凑。
        public Entity[] ComponentEntities;

        public UpdateSoaComponentGroup(WorldCore world) : base(world)
        {
            world.UpdateSystem.Register(Update, ComponentRegistry.GetInfo<T>().Options.Priority);
        }

        public override void EnsureCapacity(int capacity)
        {
            base.EnsureCapacity(capacity);
            Array.Resize(ref ComponentEntities, capacity);
        }

        public override int Alloc(in Entity et)
        {
            var index = base.Alloc(et);
            ComponentEntities[index] = et;
            return index;
        }

        public override void Free(in Entity et, int index)
        {
            base.Free(et, index);
            ComponentEntities[index] = default;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Update(WorldCore world)
        {
            var offset = 0;
            for (var i = 0; i - offset < Count; i++)
            {
                var et = ComponentEntities[i];
                if (et.IsEmpty)
                {
                    offset++;
                    continue;
                }

                Components[i].Update(world, et);
            }
        }
    }
}