// ==================== qcbf@qq.com | 2026-02-26 ====================

using System;
using System.Collections.Generic;
using System.Reflection;

namespace FLib.WorldCores
{
    public sealed class WorldUpdater
    {
        public (Action<WorldCore, object>, object)[] ModuleActions = new (Action<WorldCore, object>, object)[64];
        public int[] ModulePriorities = new int[64];
        public int Count;

        /// <summary>
        /// 
        /// </summary>
        public void Update(WorldCore world)
        {
            for (var i = 0; i < Count; i++)
            {
                ref readonly var data = ref ModuleActions[i];
                data.Item1(world, data.Item2);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Register(MethodInfo methodInfo, int order = 0, object param = null)
        {
            Register((Action<WorldCore, object>)methodInfo.CreateDelegate(typeof(Action<WorldCore, object>)), order, param);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Register(Action<WorldCore, object> action, int order = 0, object param = null)
        {
            var index = Count++;
            if (Count >= ModuleActions.Length)
            {
                Array.Resize(ref ModuleActions, MathEx.GetNextCapacityLength(index));
                Array.Resize(ref ModulePriorities, ModuleActions.Length);
            }

            if (index != 0 && order < ModulePriorities[index])
            {
                index = Array.BinarySearch(ModulePriorities, 0, index, order);
                if (index < 0)
                    index = ~index;
                for (var i = Count - 1; i > index; i--)
                {
                    ModuleActions[i] = ModuleActions[i - 1];
                    ModulePriorities[i] = ModulePriorities[i - 1];
                }
            }

            ModuleActions[index] = (action, param);
            ModulePriorities[index] = order;
        }
    }
}
