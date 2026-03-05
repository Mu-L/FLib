// ==================== qcbf@qq.com | 2026-03-03 ====================

#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores.Behaviors
{
    [ComponentOption(options: EComponentOption.RejectSoa)]
    public struct BehaviorSystem : ILifecycleAwake
    {
        public EntityHelper Self;
        public uint Mask;
        public int PrimaryId;
        public int SecondaryId;

        public readonly bool HasPrimary => PrimaryId >= 0;
        public readonly bool HasSecondary => SecondaryId >= 0;
        public readonly Behavior? Primary => HasPrimary ? BehaviorPool.Behaviors[PrimaryId] : null;
        public readonly Behavior? Secondary => HasSecondary ? BehaviorPool.Behaviors[SecondaryId] : null;
        public readonly WorldCore World => Self.World;

        void ILifecycleAwake.Awake(WorldCore world, Entity entity)
        {
            SecondaryId = PrimaryId = -1;
            Self = new EntityHelper(world, entity);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Do<TBehavior, TParam>(in TParam param) where TBehavior : Behavior
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
        public bool Do<T>() where T : Behavior
            => Do(typeof(T));

        /// <summary>
        /// 
        /// </summary>
        public unsafe bool Do(Type behaviorType)
        {
            var evt = new DoBehaviorEvent { SystemPtr = (BehaviorSystem*)Unsafe.AsPointer(ref this) };
            Behavior bhv;
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
        private unsafe bool DoNewBehavior(Type behaviorType, DoBehaviorEvent evt)
        {
            var bhv = BehaviorPool.Rent(behaviorType);
            bhv.SystemPtr = (BehaviorSystem*)Unsafe.AsPointer(ref this);
            bhv.StartFrame = World.Frame;

            if (!CheckDo(ref evt, bhv, true))
            {
                BehaviorPool.Free(bhv);
                return false;
            }

            bhv.Priority = bhv.GetPriority();
            var stopId = -1;

            if (HasPrimary)
            {
                var primary = Primary!;
                if (primary!.CheckPriority(bhv))
                {
                    PrimaryId = bhv.Id;
                    Mask = Mask & ~primary.Mask | bhv.Mask;
                    var secondary = Secondary!;
                    if (HasSecondary && !bhv.CheckFriend(secondary))
                    {
                        Mask &= ~secondary.Mask;
                        stopId = SecondaryId;
                        SecondaryId = -1;
                    }
                }
                else if (primary.CheckFriend(bhv))
                {
                    var secondary = Secondary!;
                    if (HasSecondary && secondary.CheckPriority(bhv))
                    {
                        Mask &= ~secondary.Mask;
                        stopId = PrimaryId;
                        SecondaryId = bhv.Id;
                    }
                }
                else
                {
                    BehaviorPool.Free(bhv);
                    return false;
                }
            }
            else
            {
                PrimaryId = bhv.Id;
            }

            Awake(bhv, evt);

            if (stopId >= 0)
                Stop(stopId);

            return true;
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
        public bool Stop<T>() where T : Behavior
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
        public readonly bool IsRunning<T>() where T : Behavior
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
        private void Awake(Behavior bhv, in DoBehaviorEvent evt)
        {
            (bhv as IBehaviorParamable)?.InitializeParam();
            bhv.Awake(evt.IsFirst);
            Self.DispatchEvent(evt);
        }

        /// <summary>
        /// 
        /// </summary>
        private bool CheckDo(ref DoBehaviorEvent e, Behavior bhv, bool isFirst)
        {
            e.Behavior = bhv;
            e.IsFirst = isFirst;
            return bhv.CheckDo() && Self.DispatchPreEventById(0, ref e) && Self.DispatchPreEvent(ref e);
        }


        /// <summary>
        /// 
        /// </summary>
        private void Stop(ref int id)
        {
            Mask &= ~BehaviorPool.Behaviors[id].Mask;
            var tempId = id;
            id = -1;
            Stop(tempId);
        }

        /// <summary>
        /// 
        /// </summary>
        private static void Stop(int id)
        {
            BehaviorPool.Free(BehaviorPool.Behaviors[id]);
        }
    }
}