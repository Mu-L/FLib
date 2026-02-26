// ==================== qcbf@qq.com | 2026-01-10 ====================

using System;
using System.Diagnostics;
using System.Reflection;

namespace FLib.WorldCores
{
    public readonly struct ComponentInfo
    {
        public ComponentMeta Meta { get; }
        public readonly Type Type;
        public readonly LifeSystemDelegate ComponentAwake;
        public readonly LifeSystemDelegate ComponentDestroy;
        public readonly ComponentOptionAttribute Options;

        public bool IsHasLifeInvoker => ComponentAwake != null || ComponentDestroy != null;

        public ComponentInfo(ComponentMeta meta, Type type)
        {
            Type = type;
            Meta = meta;
            Options = type.GetCustomAttribute<ComponentOptionAttribute>();
            ComponentAwake = SystemUtility.CreateDelegate(typeof(IAwakeSystem), type, nameof(IAwakeSystem.Awake));
            ComponentDestroy = SystemUtility.CreateDelegate(typeof(IDestroySystem), type, nameof(IDestroySystem.Destroy));
        }
    }
}