// ==================== qcbf@qq.com | 2026-03-06 ====================

#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Effects
{
    /// <summary>
    /// 效果系统
    /// </summary>
    [WorldComponentOption(EComponentOption.AlwaysReceiveDestroy)]
    public struct WorldEffectSystem : IWorldAwake, IWorldDestroy
    {
        public WorldEntityHelper Self;
        public uint FlagMask;
        private int _containerIndex;

        /// <summary>
        /// 获取效果容器
        /// </summary>
        public readonly WorldEffectContainer Container => WorldEffectPool.Containers[_containerIndex];

        /// <summary>
        /// 获取世界核心实例
        /// </summary>
        public readonly WorldCore World => Self.World;

        /// <summary>
        /// 获取系统是否已释放
        /// </summary>
        public readonly bool IsDisposed => (FlagMask & int.MaxValue) == 0x80000000;


        /// <summary>
        /// 初始化效果系统，从对象池租用容器并设置到动态组件中
        /// </summary>
        public void OnAwake(WorldCore world, WorldEntity entity)
        {
            Self = entity.AsHelper(world);
            _containerIndex = WorldEffectPool.RentContainer();
        }

        /// <summary>
        /// 销毁效果系统，清空所有效果并归还容器到对象池
        /// </summary>
        public void OnDestroy(WorldCore world, WorldEntity entity)
        {
            FlagMask |= 0x80000000;
            Clear();
            world.Assert(Container.Effects.Count == 0);
            WorldEffectPool.FreeContainer(_containerIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasEffect(uint id)
        {
            return Container.Effects.ContainsKey(id);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasFlags(BitFlags flags)
        {
            return (FlagMask & flags.Mask) != 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public WorldEffect? Get(uint id)
        {
            var effects = Container.Effects;
            var index = effects.GetEntryIndex(id);
            return index < 0 ? null : effects.GetEntryValue(index).Single;
        }

        /// <summary>
        /// 添加效果实例到实体
        /// </summary>
        public WorldEffect? Add(Type effectType, in WorldEntity addedBy, uint id, ushort addCount = 1)
        {
            World.Assert(!IsDisposed);
            var container = Container;
            var effects = container.Effects;
            ref var item = ref effects.GetOrAddValueRef(id);
            var evt = new WorldAddEffectEvent { AddCount = addCount, AddedBy = addedBy.IsEmpty ? Self : addedBy, Id = id, Effect = item.Single };
            ref var effect = ref evt.Effect;

            if (effect == null)
            {
                effect = InitializeEffect(WorldEffectPool.Rent(effectType, ref this), evt);
                if (!Self.DispatchPreEvent(ref evt))
                {
                    FreeEffect(effect, false);
                    return null;
                }

                item.Single = effect;
            }
            else if (effect.Data.AddOption == EWorldEffectAddOption.IgnoreNew ||
                     (effect.Data.AddOption is EWorldEffectAddOption.AddStack or EWorldEffectAddOption.AddStackAndResetTime && effect.Data.StackCount >= effect.Data.MaxStackCount))
            {
                return null;
            }
            else
            {
                if (!Self.DispatchPreEvent(ref evt))
                    return null;
                switch (effect.Data.AddOption)
                {
                    case EWorldEffectAddOption.ResetTime:
                        effect.Time.RefreshTime(World.Time);
                        Self.DispatchEvent(evt);
                        return effect;
                    case EWorldEffectAddOption.AddStack:
                        AddEffectStackCount(effect, ref evt.AddCount);
                        effect.OnStackCountChange(evt.AddCount);
                        Self.DispatchEvent(evt);
                        return effect;
                    case EWorldEffectAddOption.AddStackAndResetTime:
                        effect.Time.RefreshTime(World.Time);
                        AddEffectStackCount(effect, ref evt.AddCount);
                        effect.OnStackCountChange(evt.AddCount);
                        Self.DispatchEvent(evt);
                        return effect;
                    case EWorldEffectAddOption.MultipleInstance:
                        item.MoreList.Add(effect = InitializeEffect(WorldEffectPool.Rent(effectType, ref this), evt));
                        break;
                    case EWorldEffectAddOption.Replace:
                        // remove 
                        effect = item.Single = InitializeEffect(WorldEffectPool.Rent(effectType, ref this), evt);
                        break;
                }
            }

            AddEffectStackCount(evt.Effect, ref evt.AddCount);
            evt.Effect.OnAwake();
            evt.Effect.OnStackCountChange(evt.AddCount);
            Self.DispatchEvent(evt);
            return evt.Effect;
        }

        /// <summary>
        /// 移除指定 ID 的效果
        /// </summary>
        public bool Remove(uint id, ushort removeCount = ushort.MaxValue)
        {
            var container = Container;
            var idx = container.Effects.GetEntryIndex(id);
            return idx >= 0 && Remove(container.Effects.GetEntryValue(idx).Single!, removeCount);
        }

        /// <summary>
        /// 移除效果实例的内部实现
        /// </summary>
        public bool Remove(WorldEffect effect, ushort removeCount = ushort.MaxValue)
        {
            if (effect.IsRemoving)
            {
                Log.Warn?.Write($"frequent remove effect {effect}");
                return true;
            }

            var evt = new WorldRemoveEffectEvent { Effect = effect, RemoveCount = removeCount };
            if (!Self.DispatchPreEvent(ref evt))
            {
                World.Assert(evt.RemoveCount < ushort.MaxValue, msg: "cannot stop remove");
                return false;
            }

            effect.Data.StackCount = evt.RemoveCount == ushort.MaxValue ? ushort.MinValue : (ushort)(effect.Data.StackCount - evt.RemoveCount);
            effect.OnStackCountChange(evt.RemoveCount);

            if (effect.Data.StackCount > 0)
            {
                Self.DispatchEvent(evt);
                return true;
            }

            try
            {
                Self.DispatchEvent(evt);
            }
            finally
            {
                FreeEffect(effect, true);
            }

            return true;
        }

        /// <summary>
        /// 清空效果的内部实现
        /// </summary>
        private void Clear(uint flags = uint.MaxValue, IList<uint>? idList = null)
        {
            var effectsEnum = Container.Effects.GetEnumerator();
            while (effectsEnum.MoveNext())
            {
                if (!effectsEnum.Value.Single!.Data.Flags.Any(flags))
                    continue;
                idList?.Add(effectsEnum.Key);
                if (!effectsEnum.Value.MoreList.IsEmpty)
                {
                    for (var i = effectsEnum.Value.MoreList.Count - 1; i >= 0; i--)
                        Remove(effectsEnum.Value.MoreList[i]);
                }

                Remove(effectsEnum.Value.Single);
            }
        }

        /// <summary>
        /// 释放效果实例，从容器中移除并归还到对象池
        /// </summary>
        private void FreeEffect(WorldEffect effect, bool isInvokeDestroy)
        {
            var container = Container;
            FlagMask &= ~container.RemoveFlags(effect.Data.Flags.Mask);
            ref var item = ref container.Effects[effect.Data.Id];
            try
            {
                if (item.Single == effect && !item.TryPopMoreList())
                {
                    container.Effects.Remove(effect.Data.Id);
                }
                else
                {
                    if (!item.MoreList.Remove(effect))
                        World.ThrowException("not found effect instance", Self);
                }

                effect.IsRemoving = true;

                if (isInvokeDestroy)
                    effect.OnDestroy();
            }
            finally
            {
                WorldEffectPool.Free(effect);
            }
        }

        /// <summary>
        /// 初始化效果实例，设置相关属性并更新标志位
        /// </summary>
        private WorldEffect InitializeEffect(WorldEffect effect, in WorldAddEffectEvent evt)
        {
            effect.AddedBy = evt.AddedBy;
            effect.Data.Id = evt.Id;
            try
            {
                WorldGlobalSetting.InitializeEffect.Invoke(effect);
                World.Assert(effect.Data.Id == evt.Id);
                if (effect.Data.MaxStackCount == 0)
                    effect.Data.MaxStackCount = ushort.MaxValue;
                effect.Time.RefreshTime(World.Time);
            }
            catch (Exception e)
            {
                Log.Error?.Write(e);
            }

            var mask = effect.Data.Flags.Mask;
            FlagMask |= mask;
            Container.AddFlags(mask);
            return effect;
        }

        /// <summary>
        /// 增加效果的层数，确保不超过最大层数限制
        /// </summary>
        private static void AddEffectStackCount(WorldEffect effect, ref ushort addCount)
        {
            var oldCount = effect.Data.StackCount;
            effect.Data.StackCount = (ushort)Math.Clamp(effect.Data.StackCount + addCount, 1, effect.Data.MaxStackCount);
            addCount = (ushort)(effect.Data.StackCount - oldCount);
        }
    }
}