// ==================== qcbf@qq.com | 2026-02-26 ====================

using System;
using System.Collections.Generic;
using FLib.Worlds;

namespace FLib.WorldCores
{
    public class UpdateSystem
    {
        public Action<WorldCore>[] ModuleActions = new Action<WorldCore>[64];
        public int[] ModulePriorities = new int[64];

        public int Count { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Update(WorldCore world)
        {
            for (var i = 0; i < Count; i++)
                ModuleActions[i](world);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Register(Action<WorldCore> action, int priority = 0)
        {
            var index = Count++;
            if (Count >= ModuleActions.Length)
            {
                Array.Resize(ref ModuleActions, MathEx.GetNextPowerOfTwo(Count));
                Array.Resize(ref ModulePriorities, ModuleActions.Length);
            }

            if (index != 0 && priority > ModulePriorities[index])
            {
                index = Array.BinarySearch(ModulePriorities, 0, Count, priority);
                if (index < 0)
                    index = ~index;
                for (var i = Count - 1; i >= index; i--)
                {
                    ModuleActions[i] = ModuleActions[i - 1];
                    ModulePriorities[i] = ModulePriorities[i - 1];
                }
            }

            ModuleActions[index] = action;
            ModulePriorities[index] = priority;
        }
    }
}