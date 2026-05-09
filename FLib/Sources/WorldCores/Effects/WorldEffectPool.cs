// ==================== qcbf@qq.com | 2026-03-09 ====================

#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Effects
{
    public static class WorldEffectPool
    {
        [ThreadStatic] public static FixedIndexList<WorldEffectContainer> Containers;

        /// <summary>
        /// 
        /// </summary>
        public static int RentContainer()
        {
            var idx = Containers.Add();
            Containers[idx] ??= new WorldEffectContainer();
            return idx;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void FreeContainer(int index)
        {
            Containers[index].Clear();
            Containers.RemoveAt(index, false);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void EnsureCapacity(int entityCapacities)
        {
            Containers.EnsureCapacity(entityCapacities);
            for (var i = Containers.Count; i < entityCapacities; i++)
                Containers.Add(new WorldEffectContainer());
        }

        /// <summary>
        /// 
        /// </summary>
        public static Ref<WorldEffectSystem> GetEffectSystem(this in WorldEntity et) => et.GetSta<WorldEffectSystem>();
    }
}