// ==================== qcbf@qq.com | 2026-02-25 ====================

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FLib.WorldCores
{
    public class UpdateSoaComponentGroup<T> : SoaComponentGroup<T>
    {
        // 后续考虑改为再包一层，保持components紧凑。
        public bool[] Uses;

        public override void EnsureCapacity(int capacity)
        {
            base.EnsureCapacity(capacity);
            Array.Resize(ref Uses, capacity);
        }

        public override int Alloc(in Entity et)
        {
            var index = base.Alloc(et);
            Uses[index] = true;
            return index;
        }

        public override void Free(in Entity et, int index)
        {
            base.Free(et, index);
            Uses[index] = false;
        }
    }
}