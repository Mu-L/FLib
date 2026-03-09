// ==================== qcbf@qq.com | 2026-02-13 ====================

#nullable enable
using System;

namespace FLib.WorldCores
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class WorldComponentOptionAttribute : Attribute
    {
        /// <summary>
        /// 执行顺序, 越大越先执行。
        /// </summary>
        public readonly short Order;

        /// <summary>
        /// 
        /// </summary>
        public readonly EComponentOption Options;

        /// <summary>
        /// 动态组件如果是struct会进行boxing. 建议改功能只用于静态组件, 动态组件如果有实现awake, 可以自己去手动添加.
        /// </summary>
        public readonly Type[]? RequiredComponents;

        public WorldComponentOptionAttribute(EComponentOption options)
        {
            RequiredComponents = null;
            Order = 0;
            Options = options;
        }

        public WorldComponentOptionAttribute(short order = 0, EComponentOption options = EComponentOption.None, Type[]? requiredComponents = null)
        {
            RequiredComponents = requiredComponents;
            Order = order;
            Options = options;
        }
    }

    [Flags]
    public enum EComponentOption : byte
    {
        None,
        DoNotResetMemory = 0x1,
        RejectSoa = 0x2,
        RejectChunk = 0x4,
        AlwaysReceiveDestroy = 0x8,
    }
}