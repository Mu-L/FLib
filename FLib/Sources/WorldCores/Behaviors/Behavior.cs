// ==================== qcbf@qq.com | 2026-03-02 ====================

using System;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores.Behaviors
{
    /// <summary>
    /// 
    /// </summary>
    public abstract unsafe class Behavior
    {
        internal BehaviorSystem* SystemPtr;
        internal int Id;

        public ref BehaviorSystem System => ref *SystemPtr;
        public ref EntityHelper Entity => ref SystemPtr->Self;
        public WorldCore World => SystemPtr->Self.World;
        public bool IsEmpty => SystemPtr == null;

        public byte TypeId { get; internal set; }
        public uint StartFrame { get; internal set; }
        public int Priority { get; internal set; }
        public abstract uint Mask { get; }


        public virtual int GetPriority() => 0;
        public virtual bool CheckDo() => !System.IsRunning(Mask);

        public virtual bool CheckPriority(Behavior target) => target.Priority >= Priority;
        public virtual bool CheckFriend(Behavior targetBehavior) => false;


        public virtual void OnSwapToPrimary(Type oldPrimaryType)
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
    public interface IBehaviorParameterizable
    {
        void InitializeParam();
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class Behavior<TParam> : Behavior, IBehaviorParameterizable
    {
        [ThreadStatic] internal static TParam NewParam;
        public TParam Param;
        public virtual void InitializeParam() => Param = NewParam;
    }
}