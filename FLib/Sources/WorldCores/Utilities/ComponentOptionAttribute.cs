// ==================== qcbf@qq.com | 2026-02-13 ====================

using System;

namespace FLib.WorldCores
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class ComponentOptionAttribute : Attribute
    {
        /// <summary>
        /// 在指定组件之后执行
        /// </summary>
        public Type After;

        /// <summary>
        /// 在指定组件之前执行
        /// </summary>
        public Type Before;

        /// <summary>
        /// 以数值排序，越小越先执行。
        /// </summary>
        public short Order;
    }
}