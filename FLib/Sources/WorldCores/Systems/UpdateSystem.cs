// ==================== qcbf@qq.com | 2026-02-26 ====================

using System;
using System.Collections.Generic;
using FLib.Worlds;

namespace FLib.WorldCores
{
    public class UpdateSystem
    {
        public readonly WorldCore World;
        public Module[] Modules = new Module[64];

        public int Count { get; private set; }

        public unsafe struct Module
        {
            public delegate*<WorldCore, object, void> Func;
            public object Param;
            public int Priority;
        }

        public UpdateSystem(WorldCore world)
        {
            World = world;
        }

        public unsafe void Update()
        {
            for (var i = 0; i < Count; i++)
            {
                ref readonly var m = ref Modules[i];
                m.Func(World, m.Param);
            }
        }
    }
}