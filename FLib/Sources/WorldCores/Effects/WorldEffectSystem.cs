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
        public readonly bool IsDisposed => _containerIndex < 0;


        /// <summary>
        /// 初始化效果系统，从对象池租用容器并设置到动态组件中
        /// </summary>
        /// <param name="world">世界核心实例</param>
        /// <param name="entity">关联的实体</param>
        public void Awake(WorldCore world, WorldEntity entity)
        {
            _containerIndex = WorldEffectPool.RentContainer();
        }

        /// <summary>
        /// 销毁效果系统，清空所有效果并归还容器到对象池
        /// </summary>
        /// <param name="world">世界核心实例</param>
        /// <param name="entity">关联的实体</param>
        public void Destroy(WorldCore world, WorldEntity entity)
        {
            var container = Container;
            _containerIndex = -1;
            Clear(container);
            world.Assert(container.Effects.Count == 0);
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
            var mask = flags.Mask;
            return (FlagMask & mask) == mask;
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
        /// <param name="effectType">效果类型</param>
        /// <param name="addedBy">添加此效果的实体</param>
        /// <param name="id">效果 ID</param>
        /// <param name="addCount">添加的层数（默认 1 层）</param>
        /// <returns>添加的效果实例，如果添加失败则返回 null</returns>
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
                effect = InitializeEffect(container, WorldEffectPool.Rent(effectType, ref this), evt.AddedBy);
                if (!Self.DispatchPreEvent(ref evt))
                {
                    FreeEffect(container, effect);
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
                        item.MoreList.Add(effect = InitializeEffect(container, WorldEffectPool.Rent(effectType, ref this), evt.AddedBy));
                        break;
                    case EWorldEffectAddOption.Replace:
                        // remove 
                        effect = item.Single = InitializeEffect(container, WorldEffectPool.Rent(effectType, ref this), evt.AddedBy);
                        break;
                }
            }

            AddEffectStackCount(evt.Effect, ref evt.AddCount);
            evt.Effect.Awake();
            evt.Effect.OnStackCountChange(evt.AddCount);
            Self.DispatchEvent(evt);
            return evt.Effect;
        }

        /// <summary>
        /// 移除指定 ID 的效果
        /// </summary>
        /// <param name="id">效果 ID</param>
        /// <param name="removeCount">要移除的层数（默认移除所有层）</param>
        /// <returns>是否成功移除</returns>
        public bool Remove(uint id, ushort removeCount = ushort.MaxValue)
        {
            var container = Container;
            var idx = container.Effects.GetEntryIndex(id);
            return idx >= 0 && Remove(container.Effects.GetEntryValue(idx).Single!, removeCount);
        }

        /// <summary>
        /// 移除指定的效果实例
        /// </summary>
        /// <param name="effect">要移除的效果实例</param>
        /// <param name="removeCount">要移除的层数（默认移除所有层）</param>
        /// <returns>是否成功移除</returns>
        public bool Remove(WorldEffect effect, ushort removeCount = ushort.MaxValue) => Remove(Container, effect, removeCount);

        /// <summary>
        /// 移除效果实例的内部实现
        /// </summary>
        /// <param name="container">效果容器</param>
        /// <param name="effect">要移除的效果实例</param>
        /// <param name="removeCount">要移除的层数（默认移除所有层）</param>
        /// <returns>是否成功移除</returns>
        private bool Remove(WorldEffectContainer container, WorldEffect effect, ushort removeCount = ushort.MaxValue)
        {
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
                FreeEffect(container, effect);
            }

            return true;
        }

        /// <summary>
        /// 清空所有符合条件的效果
        /// </summary>
        /// <param name="flags">要清空的标志位（默认清空所有）</param>
        /// <param name="idList">可选，用于存储被清空效果的 ID 列表</param>
        public void Clear(uint flags = uint.MaxValue, IList<uint>? idList = null) => Clear(Container, flags, idList);

        /// <summary>
        /// 清空效果的内部实现
        /// </summary>
        /// <param name="container">效果容器</param>
        /// <param name="flags">要清空的标志位（默认清空所有）</param>
        /// <param name="idList">可选，用于存储被清空效果的 ID 列表</param>
        private void Clear(WorldEffectContainer container, uint flags = uint.MaxValue, IList<uint>? idList = null)
        {
            var effectsEnum = container.Effects.GetEnumerator();
            while (effectsEnum.MoveNext())
            {
                if (!effectsEnum.Value.Single!.Data.Flags.All(flags))
                    continue;
                idList?.Add(effectsEnum.Key);
                if (!effectsEnum.Value.MoreList.IsEmpty)
                {
                    for (var i = effectsEnum.Value.MoreList.Count - 1; i >= 0; i--)
                        Remove(container, effectsEnum.Value.MoreList[i]);
                }

                Remove(container, effectsEnum.Value.Single);
            }
        }

        /// <summary>
        /// 释放效果实例，从容器中移除并归还到对象池
        /// </summary>
        /// <param name="container">效果容器</param>
        /// <param name="effect">要释放的效果实例</param>
        private void FreeEffect(WorldEffectContainer container, WorldEffect effect)
        {
            FlagMask &= ~container.RemoveFlags(effect.Data.Flags);
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
            }
            finally
            {
                WorldEffectPool.Free(effect);
            }
        }

        /// <summary>
        /// 初始化效果实例，设置相关属性并更新标志位
        /// </summary>
        /// <param name="container">效果容器</param>
        /// <param name="effect">要初始化的效果实例</param>
        /// <param name="addedBy">添加此效果的实体</param>
        /// <returns>初始化后的效果实例</returns>
        private WorldEffect InitializeEffect(WorldEffectContainer container, WorldEffect effect, in WorldEntity addedBy)
        {
            effect.AddedBy = addedBy;
            FlagMask |= effect.Data.Flags;
            container.AddFlags(effect.Data.Flags.Mask);
            return effect;
        }

        /// <summary>
        /// 增加效果的层数，确保不超过最大层数限制
        /// </summary>
        /// <param name="effect">目标效果</param>
        /// <param name="addCount">要增加的层数（会被修改为实际增加的层数）</param>
        private static void AddEffectStackCount(WorldEffect effect, ref ushort addCount)
        {
            var oldCount = effect.Data.StackCount;
            effect.Data.StackCount = (ushort)Math.Clamp(effect.Data.StackCount + addCount, 1, effect.Data.MaxStackCount);
            addCount = (ushort)(effect.Data.StackCount - oldCount);
        }
    }
}