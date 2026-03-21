// ==================== qcbf@qq.com | 2026-01-10 ====================

using System;
using FLib.WorldCores;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.SoaComponents
{
    public interface IWorldSoaComponentGroupable
    {
        WorldCore World { get; }
        Array Components { get; }

        /// <summary>
        /// 分配一个动态组件
        /// </summary>
        int Alloc(in WorldEntity et, object component);

        /// <summary>
        /// 释放动态组件
        /// </summary>
        void Free(in WorldEntity et, int index, bool onEntityDestroyed);

        // /// <summary>
        // /// 
        // /// </summary>
        // bool Has(Entity et, int index);

        /// <summary>
        /// 预分配动态组件
        /// </summary>
        bool EnsureCapacity(int count);
    }
}