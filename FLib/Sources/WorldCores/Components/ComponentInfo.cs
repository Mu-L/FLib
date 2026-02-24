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
        public readonly LifeInvokers.Delegate ComponentAwake;
        public readonly LifeInvokers.Delegate ComponentDestroy;

        // public readonly LifeInvoker.Delegate ComponentEnable; // working
        // public readonly LifeInvoker.Delegate ComponentDisable;

        public ComponentInfo(ComponentMeta meta, Type type)
        {
            Type = type;
            Meta = meta;
            LifeInvokers.Get(type, out ComponentAwake, out ComponentDestroy);
        }
    }
}