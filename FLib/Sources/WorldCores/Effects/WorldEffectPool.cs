// ==================== qcbf@qq.com | 2026-03-09 ====================

#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
        public static void EnsureCapacity(int entityCapacities, (Type, int)[]? effectCapacities)
        {
            Containers.EnsureCapacity(entityCapacities);
            for (var i = Containers.Count; i < entityCapacities; i++)
                Containers.Add(new WorldEffectContainer());
            if (effectCapacities == null)
            {
                AllFrees = new ConcurrentDictionary<Type, ConcurrentStack<WorldEffect>>(WorldGlobalSetting.ThreadConcurrencyLevel, entityCapacities);
            }
            else
            {
                AllFrees = new ConcurrentDictionary<Type, ConcurrentStack<WorldEffect>>(WorldGlobalSetting.ThreadConcurrencyLevel, effectCapacities.Sum(v => v.Item2));
                foreach (var (type, count) in effectCapacities)
                {
                    var stack = AllFrees[type] = new ConcurrentStack<WorldEffect>();
                    for (var i = 0; i < count; i++)
                        stack.Push((WorldEffect)TypeAssistant.New(type));
                }
            }
        }
    }
}