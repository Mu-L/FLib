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
        public WorldEntity Entity;
        public uint FlagMask;
        private int _containerIndex;
        
        /// <summary>
        /// 获取效果容器
        /// </summary>
        public readonly WorldEffectContainer Container => WorldEffectPool.Containers[_containerIndex];
        
        /// <summary>
        /// 获取世界核心实例
        /// </summary>
        public readonly WorldCore World => Entity.World;
        
        /// <summary>
        /// 获取系统是否已释放
        /// </summary>
        public readonly bool IsDisposed => (FlagMask & int.MaxValue) == 0x80000000;
        
        
        /// <summary>
        /// 初始化效果系统，从对象池租用容器并设置到动态组件中
        /// </summary>
        public void OnAwake(WorldCore world, WorldEntityId entityId)
        {
            Entity = entityId.AsEntity(world);
            _containerIndex = WorldEffectPool.RentContainer();
        }
        
        /// <summary>
        /// 销毁效果系统，清空所有效果并归还容器到对象池
        /// </summary>
        public void OnDestroy(WorldCore world, WorldEntityId entityId)
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
        public bool HasFlags(uint flags)
        {
            return (FlagMask & flags) != 0;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public WorldEffectBase? Get(uint id)
        {
            var effects = Container.Effects;
            var index = effects.GetEntryIndex(id);
            return index < 0 ? null : effects.GetEntryValue(index).Single;
        }
        
        /// <summary>
        /// 添加效果实例到实体
        /// </summary>
        public WorldEffectBase? Add(in WorldEntityId addedBy, uint id, ushort addCount = 1)
        {
            World.Assert(!IsDisposed);
            var container = Container;
            var effects = container.Effects;
            ref var item = ref effects.GetOrAddValueRef(id);
            var evt = new WorldAddEffectEvent { AddCount = addCount, AddedBy = addedBy.IsEmpty ? Entity : addedBy, Id = id, Effect = item.Single };
            ref var effect = ref evt.Effect;
            
            if (effect == null)
            {
                effect = CreateEffect(evt);
                if (!Entity.DispatchPreEvent(ref evt))
                {
                    DestroyEffect(effect, false);
                    return null;
                }
                
                item.Single = effect;
            }
            else if (effect.AddOption == EWorldEffectAddOption.IgnoreNew ||
                     (effect.AddOption is EWorldEffectAddOption.AddStack or EWorldEffectAddOption.AddStackAndResetTime && effect.StackCount >= effect.MaxStackCount))
            {
                return null;
            }
            else
            {
                if (!Entity.DispatchPreEvent(ref evt))
                    return null;
                switch (effect.AddOption)
                {
                    case EWorldEffectAddOption.ResetTime:
                        effect.Time.RefreshTime(World.Time);
                        Entity.DispatchEvent(evt);
                        return effect;
                    case EWorldEffectAddOption.AddStack:
                        AddEffectStackCount(effect, ref evt.AddCount);
                        effect.OnStackCountChange(evt.AddCount);
                        Entity.DispatchEvent(evt);
                        return effect;
                    case EWorldEffectAddOption.AddStackAndResetTime:
                        effect.Time.RefreshTime(World.Time);
                        AddEffectStackCount(effect, ref evt.AddCount);
                        effect.OnStackCountChange(evt.AddCount);
                        Entity.DispatchEvent(evt);
                        return effect;
                    case EWorldEffectAddOption.MultipleInstance:
                        item.MoreList.Add(effect = CreateEffect(evt));
                        break;
                    case EWorldEffectAddOption.Replace:
                        Remove(effect);
                        effect = item.Single = CreateEffect(evt);
                        break;
                }
            }
            
            AddEffectStackCount(evt.Effect, ref evt.AddCount);
            evt.Effect.OnAwake();
            evt.Effect.OnStackCountChange(evt.AddCount);
            Entity.DispatchEvent(evt);
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
        public bool Remove(WorldEffectBase effect, ushort removeCount = ushort.MaxValue)
        {
            if (effect.IsRemoving)
            {
                Log.Warn?.Write($"frequent remove effect {effect}");
                return true;
            }
            
            var evt = new WorldRemoveEffectEvent { Effect = effect, RemoveCount = removeCount };
            if (!Entity.DispatchPreEvent(ref evt))
            {
                World.Assert(evt.RemoveCount < ushort.MaxValue, msg: "cannot stop remove");
                return false;
            }
            
            effect.StackCount = evt.RemoveCount == ushort.MaxValue ? ushort.MinValue : (ushort)(effect.StackCount - evt.RemoveCount);
            effect.OnStackCountChange(evt.RemoveCount);
            
            if (effect.StackCount > 0)
            {
                Entity.DispatchEvent(evt);
                return true;
            }
            
            try
            {
                Entity.DispatchEvent(evt);
            }
            finally
            {
                DestroyEffect(effect, true);
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
                if ((effectsEnum.Value.Single!.FlagsMask | flags) == 0)
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
        /// 释放效果实例
        /// </summary>
        private unsafe void DestroyEffect(WorldEffectBase effect, bool isInvokeDestroy)
        {
            var container = Container;
            FlagMask &= ~container.RemoveFlags(effect.FlagsMask);
            ref var item = ref container.Effects[effect.Id];
            try
            {
                if (item.Single == effect && !item.TryPopMoreList())
                {
                    container.Effects.Remove(effect.Id);
                }
                else
                {
                    if (!item.MoreList.Remove(effect))
                        World.ThrowException("not found effect instance", Entity);
                }
                
                effect.IsRemoving = true;
                if (isInvokeDestroy)
                    effect.OnDestroy();
            }
            finally
            {
                World.Soa.GetGroup<WorldEffectTime>().Free(Entity, effect.TimeComponentId, false);
                effect.Dispose();
                WorldGlobalSetting.DestroyEffect(this, effect);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        private unsafe WorldEffectBase CreateEffect(in WorldAddEffectEvent evt)
        {
            var effect = WorldGlobalSetting.CreateEffect(this, evt.AddedBy, evt.Id, evt.AddCount);
            effect.SystemPtr = (WorldEffectSystem*)Unsafe.AsPointer(ref this);
            effect.AddedBy = evt.AddedBy;
            effect.Id = evt.Id;
            if (effect.MaxStackCount == 0)
                effect.MaxStackCount = ushort.MaxValue;
            effect.TimeComponentId = World.Soa.GetGroup<WorldEffectTime>().Alloc(Entity, new WorldEffectTime(effect));
            effect.Time.RefreshTime(World.Time);
            
            var mask = effect.FlagsMask;
            FlagMask |= mask;
            Container.AddFlags(mask);
            
            return effect;
        }
        
        /// <summary>
        /// 增加效果的层数，确保不超过最大层数限制
        /// </summary>
        private static void AddEffectStackCount(WorldEffectBase effect, ref ushort addCount)
        {
            var oldCount = effect.StackCount;
            effect.StackCount = (ushort)Math.Clamp(effect.StackCount + addCount, 1, effect.MaxStackCount);
            addCount = (ushort)(effect.StackCount - oldCount);
        }
    }
}