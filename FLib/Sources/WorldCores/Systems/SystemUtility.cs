// ==================== qcbf@qq.com | 2026-02-26 ====================

#nullable enable
using System;
using System.Reflection;

namespace FLib.WorldCores
{
    public delegate void LifeSystemDelegate(ref byte ptr, WorldCore world, Entity entity);

    public static class SystemUtility
    {
        /// <summary>
        /// 
        /// </summary>
        public static LifeSystemDelegate? CreateDelegate(Type interfaceType, Type type, string name)
        {
            if (!interfaceType.IsAssignableFrom(type))
                return null;
            var mi = interfaceType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
            return mi.CreateDelegate<LifeSystemDelegate>();
        }
    }
}