// ==================== qcbf@qq.com | 2026-03-03 ====================

#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores.Behaviors
{
    [WorldComponentOption(options: EComponentOption.RejectSoa)]
    public struct WorldBehaviorSystem : IWorldLifecycleAwake
    {
        public WorldEntityHelper Self;
        public uint Mask;
        public int PrimaryId;
        public int SecondaryId;

        public readonly bool HasPrimary => PrimaryId >= 0;
        public readonly bool HasSecondary => SecondaryId >= 0;
        public readonly WorldBehavior? Primary => HasPrimary ? WorldBehaviorPool.Behaviors[PrimaryId] : null;
        public readonly WorldBehavior? Secondary => HasSecondary ? WorldBehaviorPool.Behaviors[SecondaryId] : null;
        public readonly WorldCore World => Self.World;

        void IWorldLifecycleAwake.Awake(WorldCore world, WorldEntity entity)
        {
            SecondaryId = PrimaryId = -1;
            Self = new WorldEntityHelper(world, entity);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Do<TBehavior, TParam>(in TParam param) where TBehavior : WorldBehavior
            => Do(typeof(TBehavior), param);

        /// <summary>
        /// 
        /// </summary>
        public bool Do<T>(Type behaviorType, in T param)
        {
            Behavior<T>.NewParam = param;
            return Do(behaviorType);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Do<T>() where T : WorldBehavior
            => Do(typeof(T));

        /// <summary>
        /// 
        /// </summary>
        public unsafe bool Do(Type behaviorType)
        {
            var evt = new WorldDoBehaviorEvent { SystemPtr = (WorldBehaviorSystem*)Unsafe.AsPointer(ref this) };
            WorldBehavior bhv;
            if (Primary?.GetType() == behaviorType)
            {
                if (!CheckDo(ref evt, bhv = Primary, false))
                    return false;
                Awake(bhv, evt);
            }
            else if (Secondary?.GetType() == behaviorType)
            {
                if (!CheckDo(ref evt, bhv = Secondary, false))
                    return false;
                Awake(bhv, evt);
            }
            else
            {
                if (!DoNewBehavior(behaviorType, evt))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        private unsafe bool DoNewBehavior(Type behaviorType, WorldDoBehaviorEvent evt)
        {
            var bhv = WorldBehaviorPool.Rent(behaviorType);
            bhv.SystemPtr = (WorldBehaviorSystem*)Unsafe.AsPointer(ref this);
            bhv.StartFrame = World.Frame;

            if (!CheckDo(ref evt, bhv, true))
            {
                WorldBehaviorPool.Free(bhv);
                return false;
            }

            bhv.Priority = bhv.GetPriority();
            ref var slot = ref PrimaryId;
            WorldBehavior? stopBhvPrimary = null;
            WorldBehavior? stopBhvSecondary = null;

            var primary = Primary;
            if (primary != null)
            {
                if (primary.CheckPriority(bhv))
                {
                    stopBhvPrimary = primary;
                    var secondary = Secondary;
                    if (secondary != null && !bhv.CheckFriend(secondary))
                    {
                        stopBhvSecondary = secondary;
                        SecondaryId = -1;
                    }
                }
                else if (primary.CheckFriend(bhv))
                {
                    var secondary = Secondary;
                    if (secondary?.CheckPriority(bhv) != false)
                    {
                        slot = ref SecondaryId;
                        stopBhvSecondary = secondary;
                    }
                }
                else
                {
                    WorldBehaviorPool.Free(bhv);
                    return false;
                }
            }

            slot = bhv.Id;
            Awake(bhv, evt);

            if (stopBhvPrimary != null)
                Stop(stopBhvPrimary, true);
            if (stopBhvSecondary != null)
                Stop(stopBhvSecondary, false);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void StopAll(bool force = false)
        {
            StopSecondary();
            StopPrimary();
            if (force && HasPrimary)
            {
                for (var i = 0; i < 100000 && HasPrimary; i++)
                {
                    StopSecondary();
                    StopPrimary();
                }

                if (HasPrimary)
                    World.ThrowException($"stop all failure {Primary}  {Secondary}", Self);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool StopPrimary()
        {
            if (HasPrimary)
            {
                Stop(ref PrimaryId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool StopSecondary()
        {
            if (HasSecondary)
            {
                Stop(ref SecondaryId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Stop<T>() where T : WorldBehavior
        {
            if (Primary is T)
            {
                Stop(ref PrimaryId);
                return true;
            }

            if (Secondary is T)
            {
                Stop(ref SecondaryId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Stop(Type behaviorType)
        {
            if (Primary?.GetType() == behaviorType)
            {
                Stop(ref PrimaryId);
                return true;
            }

            if (Secondary?.GetType() == behaviorType)
            {
                Stop(ref SecondaryId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly bool IsRunning(uint mask)
            => (Mask & mask) == mask;

        /// <summary>
        /// 
        /// </summary>
        public readonly bool IsRunning<T>() where T : WorldBehavior
            => Primary is T || Secondary is T;

        /// <summary>
        /// 
        /// </summary>
        public readonly bool IsRunning(Type behaviorType)
            => Primary?.GetType() == behaviorType || Secondary?.GetType() == behaviorType;


        // ===== privates =====

        /// <summary>
        /// 
        /// </summary>
        private readonly void Awake(WorldBehavior bhv, in WorldDoBehaviorEvent evt)
        {
            (bhv as IBehaviorParameterizable)?.InitializeParam();
            bhv.Awake(evt.IsFirst);
            Self.DispatchEvent(evt);
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly bool CheckDo(ref WorldDoBehaviorEvent e, WorldBehavior bhv, bool isFirst)
        {
            e.Behavior = bhv;
            e.IsFirst = isFirst;
            return bhv.CheckDo() && Self.DispatchPreEvent(ref e);
        }

        /// <summary>
        /// 
        /// </summary>
        private void Stop(ref int id)
        {
            var bhv = WorldBehaviorPool.Behaviors[id];
            var bhvType = bhv.GetType();
            var isPrimary = id == PrimaryId;
            id = -1;
            Stop(bhv, isPrimary);
            if (isPrimary && !HasPrimary)
            {
                var secondary = Secondary;
                if (secondary != null)
                {
                    PrimaryId = SecondaryId;
                    SecondaryId = -1;
                    secondary.OnSwapToPrimary(bhvType);
                    Self.DispatchEvent(new WorldSwapBehaviorEvent(bhvType, secondary));
                }
                else
                {
                    WorldGlobalSetting.DoDefaultBehavior(ref this);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void Stop(WorldBehavior bhv, bool isPrimary)
        {
            Mask &= ~bhv.Mask;
            Self.DispatchEvent(new WorldStopBehaviorEvent(ref this, bhv, isPrimary));
            WorldBehaviorPool.Free(bhv);
        }
    }
}