// ==================== qcbf@qq.com | 2026-03-02 ====================

namespace FLib.WorldCores.Behaviors
{
    public abstract class Behavior
    {
        public int Priority { get; internal set; }
        public EntityHelper Entity { get; internal set; }
        public abstract uint Mask { get; }
        public virtual int GetPriority(in BehaviorSystem system) => 0;
        public virtual bool CheckDo(in BehaviorSystem system) => !system.IsRunning(Mask);
        public virtual bool CheckPriority(in BehaviorSystem system, int targetPriority, Behavior target) => targetPriority >= Priority;
        public virtual bool IsFriend(in BehaviorSystem system, Behavior targetBehavior) => false;

        public virtual void Awake()
        {
        }

        public virtual void Destroy()
        {
        }
    }
}