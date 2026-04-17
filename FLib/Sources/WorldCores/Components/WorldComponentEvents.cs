// ==================== qcbf@qq.com | 2026-03-02 ====================

using System;
using FLib.WorldCores.Entities;
using FLib.WorldCores;

#pragma warning disable CA2211
namespace FLib.WorldCores.Components
{
    public static class WorldComponentEvents<T>
    {
        public delegate void Delegate(WorldCore world, WorldEntityId eId, ref T component);

        public static Delegate OnAwake;
        public static Delegate OnDestroy;
        public static Delegate OnStart;

        // public static Delegate OnUpdate;
    }

    public static class WorldComponentEvents
    {
        public delegate void Delegate(WorldCore world, WorldEntityId eId, Type type, ref byte component);

        public static Delegate OnAwake;
        public static Delegate OnDestroy;
        public static Delegate OnStart;

        // public static Delegate OnUpdate;
    }
}