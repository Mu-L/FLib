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
        public Behavior? Do(Type behaviorType)
        {
            if (Primary?.GetType() == behaviorType)
            {
                return !Primary.CheckDo(this) ? null : Primary;
            }

            if (Secondary?.GetType() == behaviorType)
            {
                return !Secondary.CheckDo(this) ? null : Secondary;
            }

            var bhv = BehaviorPool.Rent(behaviorType);


            return bhv;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Do<T>(Type behaviorType, in T param)
        {
            Behavior<T> bhv;
            if (Primary?.GetType() == behaviorType)
            {
                bhv = (Behavior<T>)Primary;
                if (!bhv.CheckDo(this, param))
                    return false;
            }
            else if (Secondary?.GetType() == behaviorType)
            {
                bhv = (Behavior<T>)Secondary;
                if (!bhv.CheckDo(this, param))
                    return false;
            }
            else
            {
                bhv = (Behavior<T>)BehaviorPool.Rent(behaviorType);
                if (!bhv.CheckDo(this, param))
                {
                    BehaviorPool.Free(bhv);
                    return false;
                }

                if (HasPrimary)
                {
                    var priority = bhv.GetPriority(this, param);
                    if (Primary!.CheckPriority(priority, bhv))
                    {
                        PrimaryId = bhv.Id;
                        if (HasSecondary && !bhv.CheckFriend(Secondary))
                        {
                            // stop secondary
                        }
                    }
                    else if (Primary.CheckFriend(bhv))
                    {
                        if (HasSecondary && Secondary!.CheckPriority(priority, bhv))
                        {
                            SecondaryId = bhv.Id;
                            // stop secondary
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
            }
            
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Do(Behavior behavior)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop(int id)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly bool IsRunning(uint mask)
        {
            return (Mask & mask) == mask;
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly bool IsRunning(Type behaviorType)
        {
            return true;
        }
    }
}