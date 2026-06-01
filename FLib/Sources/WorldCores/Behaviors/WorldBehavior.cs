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
        internal WorldBehaviorSystem* BehaviorSystemPtr;
        protected internal byte Version;

        public WorldSoaComponentManaged ComponentManaged;

        public int Id { get; internal set; }
        public ref WorldBehaviorSystem BehaviorSystem => ref *BehaviorSystemPtr;
        public ref WorldEntity Self => ref BehaviorSystemPtr->Self;
        public WorldCore World => BehaviorSystemPtr->Self.World;
        public bool IsEmpty => BehaviorSystemPtr == null;

        public uint StartFrame { get; set; }
        public virtual byte CurrentPriority { get; set; }
        public virtual byte InitialPriority => 0;
        public abstract uint Mask { get; }

        public virtual bool CheckDo(bool isFirst) => isFirst;

        public virtual bool CheckPriority(WorldBehavior target) => target.CurrentPriority >= CurrentPriority;
        public virtual bool CheckFriend(WorldBehavior targetBehavior, bool isFirst) => false;


        public virtual void OnSwap(Type conflictType)
        {
        }

        public virtual void OnBehaviorAwake(bool isFirst)
        {
        }

        public virtual void OnBehaviorDestroy()
        {
        }

        public void StopSelf(bool isDoDefault = true)
        {
            if (BehaviorSystem.PrimaryId == Id)
                BehaviorSystem.Stop(ref BehaviorSystem.PrimaryId, isDoDefault);
            else if (BehaviorSystem.SecondaryId == Id)
                BehaviorSystem.Stop(ref BehaviorSystem.SecondaryId, isDoDefault);
            else
                throw new Exception($"not found behavior {this}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class WorldBehavior<TParam> : WorldBehavior
    {
        [ThreadStatic] protected internal static TParam NewParam;
        public TParam Param;

        public override void OnBehaviorAwake(bool isFirst)
        {
            Param = NewParam;
        }

        public override void OnBehaviorDestroy()
        {
            Param = default;
        }
    }
}