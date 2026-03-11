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
        public static ConcurrentDictionary<Type, ConcurrentStack<WorldEffect>> AllFrees = new();
        public static ConcurrentStack<WorldEffectContainer> Containers = new();

        /// <summary>
        /// 
        /// </summary>
        public static unsafe WorldEffect Rent(Type type, ref WorldEffectSystem system)
        {
            if (!AllFrees.TryGetValue(type, out var frees) || !frees.TryPop(out var effect))
                effect = (WorldEffect)Activator.CreateInstance(type)!;
            effect.SystemPtr = (WorldEffectSystem*)Unsafe.AsPointer(ref system);
            return effect;
        }

        /// <summary>
        /// 
        /// </summary>
        public static unsafe void Free(WorldEffect effect)
        {
            effect.Data = default;
            effect.SystemPtr = null;
            AllFrees.GetOrAdd(effect.GetType(), _ => new ConcurrentStack<WorldEffect>()).Push(effect);
        }

        /// <summary>
        /// 
        /// </summary>
        public static WorldEffectContainer RentContainer()
        {
            return Containers.TryPop(out var container) ? container : new WorldEffectContainer();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void FreeContainer(WorldEffectContainer container)
        {
            container.Clear();
            Containers.Push(container);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void EnsureCapacity(int entityCapacities, (Type, int)[]? effectCapacities)
        {
            for (var i = Containers.Count; i < entityCapacities; i++)
                Containers.Push(new WorldEffectContainer());
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