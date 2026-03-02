// ==================== qcbf@qq.com | 2026-02-13 ====================

using System;

namespace FLib.WorldCores
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class ComponentOptionAttribute : Attribute
    {
        /// <summary>
        /// 执行顺序, 越大越先执行。
        /// </summary>
        public readonly short Order;

        /// <summary>
        /// 
        /// </summary>
        public readonly EComponentOption Options;

        public ComponentOptionAttribute(short order = 0, EComponentOption options = EComponentOption.None)
        {
            Order = order;
            Options = options;
        }

        public bool Op(EComponentOption option) => (Options & option) == option;
    }

    [Flags]
    public enum EComponentOption : byte
    {
        None,
        DoNotResetMemory = 0x1,
        // InvokeOnEntityDestroy = 0x2,
    }
}