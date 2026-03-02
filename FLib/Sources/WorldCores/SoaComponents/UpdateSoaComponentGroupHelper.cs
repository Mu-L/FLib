// ==================== qcbf@qq.com | 2026-02-27 ====================

using System;
using System.Reflection;

namespace FLib.WorldCores
{
    internal static class UpdateSoaComponentGroupHelper
    {
        public static MethodInfo UpdateMethodDefine = typeof(UpdateSoaComponentGroupHelper).GetMethod(nameof(Update), BindingFlags.Static | BindingFlags.Public);
        public static MethodInfo UpdateStartMethodDefine = typeof(UpdateSoaComponentGroupHelper).GetMethod(nameof(UpdateStart), BindingFlags.Static | BindingFlags.Public);

        /// <summary>
        /// 
        /// </summary>
        public static void Update<T>(WorldCore world, object arg) where T : IUpdateSystem
        {
            var group = (UpdateSoaComponentGroup<T>)arg;
            var offset = 0;
            for (var i = 0; i - offset < group.Count; i++)
            {
                var et = group.ComponentEntities[i];
                if (et.IsEmpty)
                {
                    offset++;
                    continue;
                }

                ref var comp = ref group.Components[i];
                comp.Update(world, et);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void UpdateStart<T>(WorldCore world, object arg) where T : IUpdateStartSystem
        {
            var group = (UpdateSoaComponentGroup<T>)arg;

            foreach (var i in group.StartComponentIndexes)
            {
                var et = group.ComponentEntities[i];
                ref var comp = ref group.Components[i];
                try
                {
                    comp.Start(world, et);
                    ComponentEvents<T>.OnStart?.Invoke(world, et, ref comp);
                }
                catch (Exception e)
                {
                    Log.Error?.Write($"{et} {comp} {e}");
                }
            }

            group.StartComponentIndexes.Clear();
        }
    }
}