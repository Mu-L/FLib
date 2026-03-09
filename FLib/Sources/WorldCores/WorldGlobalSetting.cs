// ==================== qcbf@qq.com |2026-01-01 ====================

using System;
using System.Runtime.InteropServices;
using FLib.WorldCores.Behaviors;

namespace FLib.WorldCores
{
    // ReSharper disable ConvertToConstant.Global
#pragma warning disable CA2211
    public static class WorldGlobalSetting
    {
        /// <summary>
        /// 对齐大小
        /// </summary>
        public static int ComponentAlign = 16;

        /// <summary>
        /// 组件每次扩容的大小
        /// </summary>
        public static int CapacityExpandSize = 32;

        /// <summary>
        /// 内存分配
        /// (size, alignment) : pointer
        /// </summary>
        public static unsafe Func<uint, uint, IntPtr> MemAlloc = (size, align) => (IntPtr)NativeMemory.AlignedAlloc(size, align);

        /// <summary>
        /// 内存释放
        /// </summary>
        public static unsafe Action<IntPtr> MemFree = ptr => NativeMemory.AlignedFree((void*)ptr);

        /// <summary>
        /// archetype chunk 内存分配器
        /// </summary>
        public static WorldMemoryAllocator ChunkAllocator = new(16 * 1024, 32, 64);

        /// <summary>
        /// 默认行为类型
        /// </summary>
        public static RWAction<WorldBehaviorSystem> DoDefaultBehavior = (ref WorldBehaviorSystem system) => system.Do(WorldBehaviorPool.Behaviors[0].GetType());
    }
}