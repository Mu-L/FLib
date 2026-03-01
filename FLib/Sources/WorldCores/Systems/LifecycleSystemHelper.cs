// ==================== qcbf@qq.com | 2026-03-01 ====================

#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FLib.WorldCores
{
    /// <summary>
    /// 
    /// </summary>
    public delegate void OneselfLifecycleSystemDelegate(ref byte ptr, WorldCore world, Entity entity);

    /// <summary>
    /// 
    /// </summary>
    public delegate void ExtensionLifecycleSystemDelegate(ref byte extensionSystem, ref byte component, WorldCore world, Entity entity);

    /// <summary>
    /// 
    /// </summary>
    public struct LifecycleSystemHelper
    {
        /// <summary>
        /// 
        /// </summary>
        public OneselfLifecycleSystemDelegate? OneselfAwake;

        /// <summary>
        /// 
        /// </summary>
        public OneselfLifecycleSystemDelegate? OneselfDestroy;

        /// <summary>
        /// 
        /// </summary>
        public object[] ExtensionAwakeSystem;


        public bool IsEmpty => OneselfAwake == null && OneselfDestroy == null;


        public LifecycleSystemHelper(Type type)
        {
            OneselfAwake = CreateOneselfDelegate(typeof(IAwakeSystem), type, nameof(IAwakeSystem.AwakeOneself));
            OneselfDestroy = CreateOneselfDelegate(typeof(IDestroySystem), type, nameof(IDestroySystem.Destroy));
            ExtensionAwakeSystem = Array.Empty<object>();
        }

        /// <summary>
        /// 
        /// </summary>
        public void RegisterAwakeExtensionSystem(object exSys)
        {
            // (ExtensionAwakeSystem ??= new List<object>()).Add(exSys);
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly void InvokeAwake(ref byte ptr, WorldCore world, Entity entity)
        {
            OneselfAwake?.Invoke(ref ptr, world, entity);
            if (ExtensionAwakeSystem != null)
            {
                foreach (var o in ExtensionAwakeSystem)
                {
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly void InvokeDestroy(ref byte ptr, WorldCore world, Entity entity)
        {
            OneselfDestroy?.Invoke(ref ptr, world, entity);
        }

        /// <summary>
        /// 
        /// </summary>
        private static OneselfLifecycleSystemDelegate? CreateOneselfDelegate(Type interfaceType, Type type, string name)
        {
            if (!interfaceType.IsAssignableFrom(type))
                return null;
            var mi = interfaceType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
            return mi.CreateDelegate<OneselfLifecycleSystemDelegate>();
        }
    }
}