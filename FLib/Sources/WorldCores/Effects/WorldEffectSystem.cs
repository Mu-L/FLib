// ==================== qcbf@qq.com | 2026-03-06 ====================

#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Effects
{
    [WorldComponentOption(EComponentOption.AlwaysReceiveDestroy)]
    public struct WorldEffectSystem : IWorldAwake, IWorldDestroy
    {
        public uint Mask;
        public WorldEntityHelper Self;

        private int _containerIndex;

        public readonly WorldCore World => Self.World;


        public void Awake(WorldCore world, WorldEntity entity)
        {
            _containerIndex = WorldEffectPool.Containers.Add();
        }

        public void Destroy(WorldCore world, WorldEntity entity)
        {
            WorldEffectPool.Containers.RemoveAt(_containerIndex, false);
            _containerIndex = -1;
        }

        /// <summary>
        /// 
        /// </summary>
        public WorldEffect? Add(Type effectType, uint id, int addCount = 1)
        {
            var container = WorldEffectPool.Containers[_containerIndex];
            var effects = container.Effects;
            ref var item = ref effects.GetOrAddValueRef(id);

            if (item.Single == null)
                item = new WorldEffectContainer.Item { Single = WorldEffectPool.Rent(effectType, ref this) };
            else if (item.Single.Data.AddOption == EWorldEffectAddOption.IgnoreNew)
                return null;

            ref var data = ref item.Single.Data;
            var addOp = data.AddOption;
            if (addOp is EWorldEffectAddOption.AddStack or EWorldEffectAddOption.AddStackAndResetTime)
            {
                if (data.StackCount >= data.MaxStackCount)
                    return null;
                data.StackCount = (ushort)Math.Clamp(data.StackCount + addCount, 1, data.MaxStackCount);
                item.Single.OnStackCountChange(addCount);
            }
            else if (addOp == EWorldEffectAddOption.ResetTime)
            {
                item.Single.StartTime = World.Frame;
            }
            else if (addOp == EWorldEffectAddOption.Replace)
            {
            }
            else if (addOp == EWorldEffectAddOption.MultipleInstance)
            {
                item.MoreList.Add(WorldEffectPool.Rent(effectType, ref this));
            }

            return item.Single;
        }
    }
}