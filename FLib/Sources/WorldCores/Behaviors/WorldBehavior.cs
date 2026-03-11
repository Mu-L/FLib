// ==================== qcbf@qq.com | 2026-03-02 ====================

using System;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Behaviors
{
    /// <summary>
    /// 
    /// </summary>
    public abstract unsafe class WorldBehavior
    {
        internal WorldBehaviorSystem* SystemPtr;
        internal int Id;

        public ref WorldBehaviorSystem System => ref *SystemPtr;
        public ref WorldEntityHelper Entity => ref SystemPtr->Self;
        public WorldCore World => SystemPtr->Self.World;
        public bool IsEmpty => SystemPtr == null;

        public byte TypeId { get; internal set; }
        public uint StartFrame { get; internal set; }
        public byte Priority { get; internal set; }
        public abstract uint Mask { get; }


        public virtual byte GetInitialPriority() => 0;
        public virtual bool CheckDo() => !System.IsRunning(Mask);

        public virtual bool CheckPriority(WorldBehavior target) => target.Priority >= Priority;
        public virtual bool CheckFriend(WorldBehavior targetBehavior) => false;


        public virtual void OnSwap(Type conflictType)
        {
        }

        public virtual void Awake(bool isFirst)
        {
        }

        public virtual void Destroy()
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IWorldBehaviorParameterizable
    {
        void InitializeParam();
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class WorldBehavior<TParam> : WorldBehavior, IWorldBehaviorParameterizable
    {
        [ThreadStatic] internal static TParam NewParam;
        public TParam Param;
        public virtual void InitializeParam() => Param = NewParam;
    }
}