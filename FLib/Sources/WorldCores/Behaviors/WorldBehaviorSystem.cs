// ==================== qcbf@qq.com | 2026-03-03 ====================

#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Behaviors
{
    [WorldComponentOption(options: EComponentOption.RejectSoa)]
    public struct WorldBehaviorSystem : IWorldAwake, IWorldDestroy
    {
        public WorldEntity Self;
        public uint Mask;
        public int PrimaryId;
        public int SecondaryId;

        public readonly bool HasPrimary => PrimaryId >= 0;
        public readonly bool HasSecondary => SecondaryId >= 0;
        public readonly WorldBehavior? Primary => HasPrimary ? WorldBehaviorPool.Behaviors[PrimaryId] : null;
        public readonly WorldBehavior? Secondary => HasSecondary ? WorldBehaviorPool.Behaviors[SecondaryId] : null;
        public readonly WorldCore World => Self.World;

        public override string ToString() => $"{Self}, {Primary}, {Secondary}";

        void IWorldAwake.OnComponentAwake(WorldCore world, WorldEntityId entityId)
        {
            SecondaryId = PrimaryId = -1;
            Self = new WorldEntity(world, entityId);
            WorldGlobalSetting.DoDefaultBehaviorHandler(ref this);
        }

        void IWorldDestroy.OnComponentDestroy(WorldCore world, WorldEntityId entityId)
        {
            StopAll(true, false);
        }

        /// <summary>
        /// 执行指定行为类型并传入参数，如果已有相同行为则复用；返回执行是否成功。
        /// </summary>
        public bool Do<TBehavior, TParam>(in TParam param) where TBehavior : WorldBehavior
            => Do(typeof(TBehavior), param);

        /// <summary>
        /// 执行给定行为类型，并通过静态泛型承载参数。
        /// 参数值会提前存储到 <see cref="WorldBehavior{TParam}.NewParam"/>。
        /// </summary>
        public bool Do<T>(Type behaviorType, in T param)
        {
            World.Assert(typeof(WorldBehavior<T>).IsAssignableFrom(behaviorType));
            WorldBehavior<T>.NewParam = param;
            return Do(behaviorType);
        }

        /// <summary>
        /// 启动指定泛型类型的行为（无参数）。
        /// </summary>
        public bool Do<T>() where T : WorldBehavior
            => Do(typeof(T));

        /// <summary>
        /// 尝试激活或新建指定行为类型：
        /// - 如果当前主或次行为已是该类型，则直接检查并唤醒；
        /// - 否则根据优先级/友好关系决定是否创建新实例并替换。
        /// 返回是否成功执行行为。
        /// </summary>
        public unsafe bool Do(Type behaviorType)
        {
            var evt = new WorldDoBehaviorEvent(ref this);
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
                if (!DoNewBehavior(behaviorType, ref evt)) // 这里传ref还是直接传值copy更好?
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 创建一个新行为并根据当前主/次行为的优先级与友好关系
        /// 将其插入到系统中；必要时会停止被替换的行为。
        /// </summary>
        private unsafe bool DoNewBehavior(Type behaviorType, ref WorldDoBehaviorEvent evt)
        {
            var bhv = WorldBehaviorPool.Rent(behaviorType);
            bhv.SystemPtr = (WorldBehaviorSystem*)Unsafe.AsPointer(ref this);
            bhv.ComponentManaged.Entity = Self;
            bhv.StartFrame = World.Frame;

            if (!CheckDo(ref evt, bhv, true))
            {
                WorldBehaviorPool.Free(bhv);
                return false;
            }

            bhv.Priority = bhv.InitialPriority;
            ref var slot = ref PrimaryId;
            WorldBehavior? stopBhvPrimary = null;
            WorldBehavior? stopBhvSecondary = null;

            var primary = Primary;
            if (primary != null)
            {
                if (primary.CheckPriority(bhv))
                {
                    if (bhv.CheckFriend(primary, true))
                    {
                        Swap(primary, behaviorType, out SecondaryId);
                    }
                    else
                    {
                        stopBhvPrimary = primary;
                        var secondary = Secondary;
                        if (secondary != null && !bhv.CheckFriend(secondary, true))
                        {
                            stopBhvSecondary = secondary;
                        }
                    }
                }
                else if (primary.CheckFriend(bhv, false))
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

            if (stopBhvSecondary != null)
            {
                SecondaryId = -1;
                Stop(stopBhvSecondary, false);
            }

            if (stopBhvPrimary != null)
                Stop(stopBhvPrimary, true);

            return true;
        }

        /// <summary>
        /// 停止系统中运行的所有行为。
        /// 如果 <paramref name="force"/> 为 true，会循环尝试直至彻底清空或抛出错误。
        /// </summary>
        public void StopAll(bool force = false, bool isDoDefault = true)
        {
            StopSecondary();
            StopPrimary(isDoDefault);
            if (force && HasPrimary)
            {
                for (var i = 0; i < 100000 && HasPrimary; i++)
                {
                    StopSecondary();
                    StopPrimary(isDoDefault);
                }

                if (HasPrimary)
                    World.ThrowException($"stop all failure {Primary}  {Secondary}", Self);
            }
        }

        /// <summary>
        /// 停止当前主行为（如果存在）。
        /// </summary>
        public bool StopPrimary(bool isDoDefault = true)
        {
            if (HasPrimary)
            {
                Stop(ref PrimaryId, isDoDefault);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 停止当前次行为（如果存在）。
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
        /// 停止指定类型的行为（无论是主还是次）。
        /// </summary>
        public bool Stop<T>(bool isDoDefault = true) where T : WorldBehavior
        {
            if (Primary is T)
            {
                Stop(ref PrimaryId, isDoDefault);
                return true;
            }

            if (Secondary is T)
            {
                Stop(ref SecondaryId, isDoDefault);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 停止指定类型的行为实例。
        /// </summary>
        public bool Stop(Type behaviorType, bool isDoDefault = true)
        {
            if (Primary?.GetType() == behaviorType)
            {
                Stop(ref PrimaryId, isDoDefault);
                return true;
            }

            if (Secondary?.GetType() == behaviorType)
            {
                Stop(ref SecondaryId, isDoDefault);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取指定类型的行为实例。
        /// </summary>
        public T? Get<T>() where T : WorldBehavior
        {
            return Primary as T ?? Secondary as T;
        }

        /// <summary>
        /// 检查标记组合是否全部被当前行为掩码包含。
        /// </summary>
        public readonly bool IsRunning(uint mask)
            => (Mask & mask) == mask;

        /// <summary>
        /// 判断给定泛型类型的行为是否正在运行。
        /// </summary>
        public readonly bool IsRunning<T>() where T : WorldBehavior
            => Primary is T || Secondary is T;

        /// <summary>
        /// 判断指定类型的行为是否作为主或次正在运行。
        /// </summary>
        public readonly bool IsRunning(Type behaviorType)
            => Primary?.GetType() == behaviorType || Secondary?.GetType() == behaviorType;


        // ===== privates =====

        /// <summary>
        /// 通用唤醒逻辑：初始化参数、调用行为唤醒并派发事件。
        /// </summary>
        private readonly void Awake(WorldBehavior bhv, in WorldDoBehaviorEvent evt)
        {
            bhv.OnAwake(evt.IsFirst);
            Self.DispatchEvent(evt);
        }

        /// <summary>
        /// 在执行行为前进行检查，包括行为自身条件和预事件拦截。
        /// </summary>
        private readonly bool CheckDo(ref WorldDoBehaviorEvent e, WorldBehavior bhv, bool isFirst)
        {
            e.Behavior = bhv;
            e.IsFirst = isFirst;
            return bhv.CheckDo(isFirst) && Self.DispatchPreEvent(ref e);
        }

        /// <summary>
        /// 停止指定 ID 的行为并处理主次切换逻辑。
        /// </summary>
        internal void Stop(ref int id, bool isDoDefault = true)
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
                    SecondaryId = -1;
                    Swap(secondary, bhvType, out PrimaryId);
                }
                else if (isDoDefault)
                {
                    WorldGlobalSetting.DoDefaultBehaviorHandler(ref this);
                }
            }
        }

        /// <summary>
        /// 执行行为停止的底层逻辑：更新掩码、派发事件并回收对象。
        /// </summary>
        private void Stop(WorldBehavior bhv, bool isPrimary)
        {
            Mask &= ~bhv.Mask;
            try
            {
                bhv.OnDestroy();
                bhv.ComponentManaged.Dispose();
                Self.DispatchEvent(new WorldStopBehaviorEvent(ref this, bhv, isPrimary));
            }
            finally
            {
                WorldBehaviorPool.Free(bhv);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void Swap(WorldBehavior bhv, Type conflictType, out int to)
        {
            to = bhv.Id;
            bhv.OnSwap(conflictType);
            Self.DispatchEvent(new WorldSwapBehaviorEvent(conflictType, bhv));
        }
    }
}