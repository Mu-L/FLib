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
        public uint FlagMask;
        public WorldEntityHelper Self;

        private int _containerIndex;

        public readonly WorldEffectContainer Container => World.Soa.GetGroup<WorldEffectContainer>()[_containerIndex];
        public readonly WorldCore World => Self.World;
        public readonly bool IsDisposed => _containerIndex < 0;


        public void Awake(WorldCore world, WorldEntity entity)
        {
            _containerIndex = world.SetDyn(entity, WorldEffectPool.RentContainer());
        }

        public void Destroy(WorldCore world, WorldEntity entity)
        {
            WorldEffectPool.FreeContainer(Container);
            _containerIndex = -1;
            Clear();
        }

        /// <summary>
        /// 
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
                effect = InitializeEffect(container, WorldEffectPool.Rent(effectType, ref this), evt.AddedBy);
                if (!Self.DispatchPreEvent(ref evt))
                {
                    WorldEffectPool.Free(effect);
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
                        effect.StartTime = World.Time;
                        Self.DispatchEvent(evt);
                        return effect;
                    case EWorldEffectAddOption.AddStack:
                        AddEffectStackCount(effect, ref evt.AddCount);
                        effect.OnStackCountChange(evt.AddCount);
                        Self.DispatchEvent(evt);
                        return effect;
                    case EWorldEffectAddOption.AddStackAndResetTime:
                        effect.StartTime = World.Time;
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
        /// 
        /// </summary>
        public bool Remove(uint id, ushort removeCount = 0)
        {
            var container = Container;
            var idx = container.Effects.GetEntryIndex(id);
            return idx >= 0 && Remove(container.Effects.GetEntryValue(idx).Single!, removeCount);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Remove(WorldEffect effect, ushort removeCount = 0)
        {
            if (removeCount == 0)
                removeCount = effect.Data.StackCount;

            var evt = new WorldRemoveEffectEvent { Effect = effect, RemoveCount = removeCount };
            if (!Self.DispatchPreEvent(ref evt))
                return false;
            
            effect.Data.StackCount -= evt.RemoveCount;
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
                FreeEffect(effect);
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear(uint flags = uint.MaxValue, IList<uint>? idList = null)
        {
            var container = Container;
            var effectsEnum = container.Effects.GetEnumerator();
            while (effectsEnum.MoveNext())
            {
                if (!effectsEnum.Value.Single!.Data.Flags.All(flags))
                    continue;
                idList?.Add(effectsEnum.Key);
                if (!effectsEnum.Value.MoreList.IsEmpty)
                {
                    for (var i = effectsEnum.Value.MoreList.Count - 1; i >= 0; i--)
                    {
                        Remove(effectsEnum.Value.MoreList[i]);
                    }
                }

                Remove(effectsEnum.Value.Single);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void FreeEffect(WorldEffect effect)
        {
            var container = Container;
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
        /// 
        /// </summary>
        private static void AddEffectStackCount(WorldEffect effect, ref ushort addCount)
        {
            var oldCount = effect.Data.StackCount;
            effect.Data.StackCount = (ushort)Math.Clamp(effect.Data.StackCount + addCount, 1, effect.Data.MaxStackCount);
            addCount = (ushort)(effect.Data.StackCount - oldCount);
        }

        /// <summary>
        /// 
        /// </summary>
        private WorldEffect InitializeEffect(WorldEffectContainer container, WorldEffect effect, in WorldEntity addedBy)
        {
            effect.AddedBy = addedBy;
            FlagMask |= effect.Data.Flags;
            container.AddFlags(effect.Data.Flags.Mask);
            return effect;
        }
    }
}