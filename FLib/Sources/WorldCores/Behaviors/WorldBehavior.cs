// ==================== qcbf@qq.com | 2026-03-02 ====================

using System;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;
using FLib.WorldCores.SoaComponents;

namespace FLib.WorldCores.Behaviors
{
    /// <summary>
    /// 
    /// </summary>
    public abstract unsafe class WorldBehavior
    {
        internal int Id;
        internal WorldBehaviorSystem* SystemPtr;
        public WorldSoaComponentManaged ComponentManaged;

        public ref WorldBehaviorSystem System => ref *SystemPtr;
        public ref WorldEntity Entity => ref SystemPtr->Self;
        public WorldCore World => SystemPtr->Self.World;
        public bool IsEmpty => SystemPtr == null;

        public byte TypeId { get; internal set; }
        public FNum StartTime { get; set; }
        public virtual byte Priority { get; set; }
        public virtual byte InitialPriority => 0;
        public abstract uint Mask { get; }

        public virtual bool CheckDo(bool isFirst) => isFirst;

        public virtual bool CheckPriority(WorldBehavior target) => target.Priority >= Priority;
        public virtual bool CheckFriend(WorldBehavior targetBehavior, bool isFirst) => false;


        public virtual void OnSwap(Type conflictType)
        {
        }

        public virtual void OnAwake(bool isFirst)
        {
        }

        public virtual void OnDestroy()
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
        [ThreadStatic] protected internal static TParam NewParam;
        public TParam Param;
        public virtual void InitializeParam() => Param = NewParam;
    }
}