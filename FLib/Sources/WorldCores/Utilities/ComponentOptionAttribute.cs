// ==================== qcbf@qq.com | 2026-02-13 ====================

using System;

namespace FLib.WorldCores
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class ComponentOptionAttribute : Attribute
    {
        /// <summary>
        /// 执行优先级, 越大越先执行。
        /// </summary>
        public short Priority;

        /// <summary>
        /// 
        /// </summary>
        public bool DoNotResetMemory;
    }
}