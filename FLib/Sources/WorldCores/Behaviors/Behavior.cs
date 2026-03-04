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
        private BehaviorSystem* _systemPtr;
        internal int Id;

        public ref BehaviorSystem System => ref *_systemPtr;
        public ref EntityHelper Entity => ref _systemPtr->Self;
        public WorldCore World => _systemPtr->Self.World;
        public bool IsEmpty => _systemPtr == null;

        public uint StartFrame { get; internal set; }
        public int Priority { get; internal set; }
        public abstract uint Mask { get; }


        public virtual int GetPriority(in BehaviorSystem system) => 0;
        public virtual bool CheckDo(in BehaviorSystem system) => !system.IsRunning(Mask);

        public virtual bool CheckPriority(int targetPriority, Behavior target) => targetPriority >= Priority;
        public virtual bool CheckFriend(Behavior targetBehavior) => false;


        protected internal virtual Behavior Do()
        {
            _systemPtr->Do(this);
            return this;
        }

        public virtual void Awake()
        {
        }

        public virtual void Destroy()
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class Behavior<TParam> : Behavior
    {
        public TParam Param;

        public override int GetPriority(in BehaviorSystem system) => throw new NotSupportedException();
        public override bool CheckDo(in BehaviorSystem system) => throw new NotSupportedException();
        public override void Awake() => throw new NotSupportedException();

        public virtual int GetPriority(in BehaviorSystem system, in TParam newParam) => base.GetPriority(system);
        public virtual bool CheckDo(in BehaviorSystem system, in TParam newParam) => base.CheckDo(system);

        public virtual void Awake(in TParam newParam)
        {
            Param = newParam;
        }
    }
}