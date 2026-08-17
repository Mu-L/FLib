// ==================== qcbf@qq.com | 2026-02-27 ====================

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using FLib.WorldCores;
using FLib.WorldCores.Components;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.SoaComponents
{
    internal static class WorldUpdateSoaComponentGroupHelper
    {
        public static MethodInfo UpdateMethodDefine = typeof(WorldUpdateSoaComponentGroupHelper).GetMethod(nameof(Update), BindingFlags.Static | BindingFlags.Public);
        public static MethodInfo UpdateStartMethodDefine = typeof(WorldUpdateSoaComponentGroupHelper).GetMethod(nameof(UpdateStart), BindingFlags.Static | BindingFlags.Public);

        /// <summary>
        /// 
        /// </summary>
        public static void Update<T>(WorldCore world, object arg) where T : IWorldUpdate
        {
            var group = (WorldUpdateSoaComponentGroup<T>)arg;
            if (group.PauseUpdate)
                return;
            for (var i = 0; i < group.IndexAllocator.EndCount; i++)
            {
                var et = group.ComponentEntities[i];
                if (et.IsEmpty)
                    continue;
                ref var comp = ref group.Components[i];
                comp.OnComponentUpdate(world, et);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void UpdateStart<T>(WorldCore world, object arg) where T : IWorldStart
        {
            var group = (WorldUpdateSoaComponentGroup<T>)arg;
            // Keep additions made by callbacks in the next batch; only removals touch the current batch.
            var processingIndexes = group.StartComponentIndexes;
            group.StartComponentIndexes = group.ProcessingStartComponentIndexes;
            group.ProcessingStartComponentIndexes = processingIndexes;
            group.StartComponentIndexes.Clear();

            try
            {
                foreach (var i in processingIndexes)
                {
                    var et = group.ComponentEntities[i];
                    ref var comp = ref group.Components[i];
                    try
                    {
                        WorldComponentEvents.OnStart?.Invoke(world, et, typeof(T), ref Unsafe.As<T, byte>(ref comp));
                        WorldComponentEvents<T>.OnStart?.Invoke(world, et, ref comp);
                        comp.OnComponentStart(world, et);
                    }
                    catch (Exception e)
                    {
                        Log.Error?.Write($"{et} {comp} {e}");
                    }
                }
            }
            finally
            {
                processingIndexes.Clear();
            }
        }
    }
}
